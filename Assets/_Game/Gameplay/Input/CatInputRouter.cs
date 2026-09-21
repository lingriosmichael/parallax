using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Gameplay.Input
{
    public sealed class CatInputRouter : MonoBehaviour, ICatCommandSource
    {
        [SerializeField] MonoBehaviour[] sources = System.Array.Empty<MonoBehaviour>();

        readonly List<ICatCommandSource> validSources = new List<ICatCommandSource>();
        readonly List<TouchStickCatInput> touchSources = new List<TouchStickCatInput>();
        readonly List<KeyboardCatInput> keyboardSources = new List<KeyboardCatInput>();

        void Awake()
        {
            validSources.Clear();
            touchSources.Clear();
            keyboardSources.Clear();

            foreach (MonoBehaviour source in sources)
            {
                if (source == null) continue;

                if (source is ICatCommandSource commandSource)
                {
                    validSources.Add(commandSource);

                    if (source is TouchStickCatInput touchSource)
                    {
                        touchSources.Add(touchSource);
                    }
                    if (source is KeyboardCatInput keyboardSource)
                    {
                        keyboardSources.Add(keyboardSource);
                    }
                }
                else
                {
                    Debug.LogError($"CatInputRouter on '{gameObject.name}': '{source.GetType().Name}' does not implement ICatCommandSource. Skipping.", this);
                }
            }
        }

        public void ResetTransientState()
        {
            for (int i = 0; i < validSources.Count; i++)
            {
                validSources[i].ResetTransientState();
            }
        }

        public void SetGravityFrame(GravityReceiver g)
        {
            for (int i = 0; i < touchSources.Count; i++)
            {
                touchSources[i].SetGravityFrame(g);
            }
            for (int i = 0; i < keyboardSources.Count; i++)
            {
                keyboardSources[i].SetGravityFrame(g);
            }
        }

        // Calls Read() on every source exactly once per tick, never early-outs,
        // so no source's latched edges leak into a later tick unread.
        public CatCommand Read()
        {
            var result = CatCommand.None;
            float bestMagnitude = -1f;

            for (int i = 0; i < validSources.Count; i++)
            {
                CatCommand cmd = validSources[i].Read();

                float magnitude = Mathf.Abs(cmd.Move);
                if (magnitude > bestMagnitude)
                {
                    bestMagnitude = magnitude;
                    result.Move = cmd.Move;
                }

                result.JumpPressed     |= cmd.JumpPressed;
                result.JumpHeld        |= cmd.JumpHeld;
                result.InteractPressed |= cmd.InteractPressed;
                result.InteractHeld    |= cmd.InteractHeld;
            }

            return result;
        }
    }
}
