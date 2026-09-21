using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Parallax.Gameplay.Input
{
    public sealed class TouchStickCatInput : MonoBehaviour, ICatCommandSource
    {
        [SerializeField] Rect stickZone = new Rect(0.00f, 0.00f, 0.5f, 0.6f);
        [SerializeField] Rect jumpZone = new Rect(0.78f, 0.00f, 0.22f, 0.45f);
        [SerializeField] Rect interactZone = new Rect(0.56f, 0.00f, 0.18f, 0.40f);
        [SerializeField] float stickRadius = 0.12f;
        [SerializeField] float deadZone = 0.15f;
        [SerializeField] StickProjection projection = StickProjection.CatRelative;
        [SerializeField] bool showDebugOverlay = true;
        [SerializeField] MonoBehaviour[] reservedRegions = System.Array.Empty<MonoBehaviour>();

        GravityReceiver gravityReceiver;
        bool warnedNoGravityFrame;

        int stickFingerId = -1;
        Vector2 stickOrigin;
        Vector2 stickCurrent;
        Vector2 lastStickVector;
        float currentMove;

        readonly HashSet<int> jumpFingerIds = new HashSet<int>();
        readonly HashSet<int> interactFingerIds = new HashSet<int>();
        readonly HashSet<int> activeIds = new HashSet<int>();
        readonly List<int> staleIds = new List<int>();
        readonly HashSet<int> ignoredFingerIds = new HashSet<int>();
        readonly List<ITouchReservedRegion> validReservedRegions = new List<ITouchReservedRegion>();
        bool jumpPressedLatch;
        bool interactPressedLatch;

        void Awake()
        {
            validReservedRegions.Clear();
            foreach (MonoBehaviour region in reservedRegions)
            {
                if (region == null) continue;

                if (region is ITouchReservedRegion reserved)
                {
                    validReservedRegions.Add(reserved);
                }
                else
                {
                    Debug.LogError($"TouchStickCatInput on '{gameObject.name}': '{region.GetType().Name}' does not implement ITouchReservedRegion. Skipping.", this);
                }
            }
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            ResetTransientState();
        }

        public void SetGravityFrame(GravityReceiver g)
        {
            gravityReceiver = g;
        }

        void Update()
        {
            Rect safeArea = Screen.safeArea;
            var touches = Touch.activeTouches;

            activeIds.Clear();

            for (int i = 0; i < touches.Count; i++)
            {
                Touch touch = touches[i];
                int id = touch.finger.index;
                activeIds.Add(id);

                if (touch.phase == TouchPhase.Began)
                {
                    if (ReservedRegionCheck.IsReserved(validReservedRegions, touch.screenPosition))
                    {
                        ignoredFingerIds.Add(id);
                        continue;
                    }

                    Vector2 norm = TouchZones.ToSafeAreaNormalised(touch.screenPosition, safeArea);

                    if (interactZone.Contains(norm))
                    {
                        interactFingerIds.Add(id);
                        interactPressedLatch = true;
                    }
                    else if (jumpZone.Contains(norm))
                    {
                        jumpFingerIds.Add(id);
                        jumpPressedLatch = true;
                    }
                    else if (stickZone.Contains(norm) && stickFingerId < 0)
                    {
                        stickFingerId = id;
                        stickOrigin = touch.screenPosition;
                        stickCurrent = touch.screenPosition;
                    }
                }
                else if (ignoredFingerIds.Contains(id))
                {
                    // A touch that began inside a reserved region is never claimed
                    // for its whole lifetime, even if it later drifts elsewhere.
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    EndFinger(id);
                }
                else if (id == stickFingerId)
                {
                    stickCurrent = touch.screenPosition;
                }
            }

            // Self-heal: a finger can disappear with no Ended/Canceled phase ever
            // arriving (app backgrounded mid-touch — notification, call, power
            // button), so prune anything the OS no longer reports rather than
            // trusting phase transitions alone.
            staleIds.Clear();
            if (stickFingerId >= 0 && !activeIds.Contains(stickFingerId)) staleIds.Add(stickFingerId);
            foreach (int id in jumpFingerIds)
            {
                if (!activeIds.Contains(id)) staleIds.Add(id);
            }
            foreach (int id in interactFingerIds)
            {
                if (!activeIds.Contains(id)) staleIds.Add(id);
            }
            for (int i = 0; i < staleIds.Count; i++) EndFinger(staleIds[i]);

            staleIds.Clear();
            foreach (int id in ignoredFingerIds)
            {
                if (!activeIds.Contains(id)) staleIds.Add(id);
            }
            for (int i = 0; i < staleIds.Count; i++) ignoredFingerIds.Remove(staleIds[i]);

            float radiusPixels = stickRadius * safeArea.height;
            lastStickVector = stickFingerId >= 0
                ? VirtualStick.Evaluate(stickOrigin, stickCurrent, radiusPixels, deadZone)
                : Vector2.zero;

            if (gravityReceiver == null)
            {
                if (!warnedNoGravityFrame)
                {
                    Debug.LogWarning($"TouchStickCatInput on '{gameObject.name}' has no gravity frame bound. Move will read 0 until SetGravityFrame is called.", this);
                    warnedNoGravityFrame = true;
                }
                currentMove = 0f;
                return;
            }

            Vector2 up = -gravityReceiver.Direction;
            Vector2 catRight = new Vector2(up.y, -up.x);
            currentMove = VirtualStick.ToMove(lastStickVector, catRight, projection);
        }

        void EndFinger(int id)
        {
            if (id == stickFingerId) stickFingerId = -1;
            jumpFingerIds.Remove(id);
            interactFingerIds.Remove(id);
        }

        public CatCommand Read()
        {
            var cmd = CatCommand.None;

            cmd.Move = currentMove;

            cmd.JumpHeld = jumpFingerIds.Count > 0;
            cmd.JumpPressed = jumpPressedLatch;
            jumpPressedLatch = false;

            cmd.InteractPressed = interactPressedLatch;
            cmd.InteractHeld = interactFingerIds.Count > 0;
            interactPressedLatch = false;

            return cmd;
        }

        public void ResetTransientState()
        {
            stickFingerId = -1;
            jumpFingerIds.Clear();
            interactFingerIds.Clear();
            ignoredFingerIds.Clear();
            lastStickVector = Vector2.zero;
            currentMove = 0f;
            jumpPressedLatch = false;
            interactPressedLatch = false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!showDebugOverlay || !Debug.isDebugBuild) return;

            Rect safeArea = Screen.safeArea;

            GUI.Label(new Rect(10f, 10f, 400f, 20f), $"Move: {currentMove:F2}  Projection: {projection}");
            DrawZone(interactZone, safeArea, new Color(0.4f, 0.9f, 1f, 0.2f));

            if (stickFingerId >= 0)
            {
                DrawDot(stickOrigin, 10f, Color.white);
                DrawDot(stickCurrent, 8f, Color.green);
                DrawRing(stickOrigin, stickRadius * safeArea.height, Color.white);
            }
        }

        static void DrawDot(Vector2 point, float size, Color color)
        {
            Vector2 guiPoint = new Vector2(point.x, Screen.height - point.y);
            Rect rect = new Rect(guiPoint.x - size * 0.5f, guiPoint.y - size * 0.5f, size, size);

            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        static void DrawRing(Vector2 centre, float radius, Color color)
        {
            const int segments = 24;
            Vector2 guiCentre = new Vector2(centre.x, Screen.height - centre.y);

            Color prev = GUI.color;
            GUI.color = color;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector2 point = guiCentre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                GUI.DrawTexture(new Rect(point.x - 1f, point.y - 1f, 2f, 2f), Texture2D.whiteTexture);
            }
            GUI.color = prev;
        }

        static void DrawZone(Rect normalised, Rect safeArea, Color color)
        {
            var rect = new Rect(
                safeArea.x + normalised.x * safeArea.width,
                Screen.height - safeArea.y - (normalised.y + normalised.height) * safeArea.height,
                normalised.width * safeArea.width,
                normalised.height * safeArea.height);
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
#endif
    }
}
