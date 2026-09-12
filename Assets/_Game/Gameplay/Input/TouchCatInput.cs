using System.Collections.Generic;
using Parallax.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Parallax.Gameplay
{
    public sealed class TouchCatInput : MonoBehaviour, ICatCommandSource
    {
        [SerializeField] Rect moveLeftZone = new Rect(0.00f, 0.00f, 0.16f, 0.45f);
        [SerializeField] Rect moveRightZone = new Rect(0.16f, 0.00f, 0.16f, 0.45f);
        [SerializeField] Rect jumpZone = new Rect(0.78f, 0.00f, 0.22f, 0.45f);
        [SerializeField] bool showDebugOverlay = true;

        readonly Dictionary<int, TouchZone> fingerZones = new Dictionary<int, TouchZone>();
        readonly List<int> moveFingerOrder = new List<int>();
        int jumpFingerCount;
        bool jumpPressedLatch;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            fingerZones.Clear();
            moveFingerOrder.Clear();
            jumpFingerCount = 0;
        }

        void Update()
        {
            Rect safeArea = Screen.safeArea;
            var touches = Touch.activeTouches;

            for (int i = 0; i < touches.Count; i++)
            {
                Touch touch = touches[i];
                int id = touch.finger.index;

                if (touch.phase == TouchPhase.Began)
                {
                    Vector2 norm = TouchZones.ToSafeAreaNormalised(touch.screenPosition, safeArea);
                    TouchZone zone = TouchZones.Classify(norm, moveLeftZone, moveRightZone, jumpZone);
                    fingerZones[id] = zone;

                    if (zone == TouchZone.MoveLeft || zone == TouchZone.MoveRight)
                    {
                        moveFingerOrder.Remove(id);
                        moveFingerOrder.Add(id);
                    }
                    else if (zone == TouchZone.Jump)
                    {
                        jumpFingerCount++;
                        jumpPressedLatch = true;
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    EndFinger(id);
                }
            }
        }

        void EndFinger(int id)
        {
            if (!fingerZones.TryGetValue(id, out TouchZone zone)) return;
            fingerZones.Remove(id);

            if (zone == TouchZone.MoveLeft || zone == TouchZone.MoveRight)
            {
                moveFingerOrder.Remove(id);
            }
            else if (zone == TouchZone.Jump)
            {
                jumpFingerCount = Mathf.Max(0, jumpFingerCount - 1);
            }
        }

        public CatCommand Read()
        {
            var cmd = CatCommand.None;

            if (moveFingerOrder.Count > 0)
            {
                TouchZone activeZone = fingerZones[moveFingerOrder[moveFingerOrder.Count - 1]];
                cmd.Move = activeZone == TouchZone.MoveLeft ? -1f : 1f;
            }

            cmd.JumpHeld = jumpFingerCount > 0;
            cmd.JumpPressed = jumpPressedLatch;
            jumpPressedLatch = false;

            // Interact arrives in Phase 4.
            cmd.InteractPressed = false;
            cmd.InteractHeld = false;

            return cmd;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (!showDebugOverlay) return;

            Rect safeArea = Screen.safeArea;
            DrawZone(moveLeftZone, safeArea, HasFingerInZone(TouchZone.MoveLeft));
            DrawZone(moveRightZone, safeArea, HasFingerInZone(TouchZone.MoveRight));
            DrawZone(jumpZone, safeArea, jumpFingerCount > 0);
        }

        bool HasFingerInZone(TouchZone zone)
        {
            foreach (var pair in fingerZones)
            {
                if (pair.Value == zone) return true;
            }
            return false;
        }

        static void DrawZone(Rect normRect, Rect safeArea, bool active)
        {
            Rect pixelRect = new Rect(
                safeArea.x + normRect.x * safeArea.width,
                safeArea.y + normRect.y * safeArea.height,
                normRect.width * safeArea.width,
                normRect.height * safeArea.height);

            // OnGUI's Y axis is top-down; safe-area/touch coordinates are bottom-up.
            pixelRect.y = Screen.height - pixelRect.y - pixelRect.height;

            Color prev = GUI.color;
            GUI.color = active ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 1f, 1f, 0.15f);
            GUI.DrawTexture(pixelRect, Texture2D.whiteTexture);
            GUI.color = prev;
        }
#endif
    }
}
