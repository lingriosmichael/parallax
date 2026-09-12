using System.Collections.Generic;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay
{
    public sealed class CatInputRouter : MonoBehaviour, ICatCommandSource
    {
        [SerializeField] MonoBehaviour[] sources = System.Array.Empty<MonoBehaviour>();

        readonly List<ICatCommandSource> validSources = new List<ICatCommandSource>();

        void Awake()
        {
            validSources.Clear();

            foreach (MonoBehaviour source in sources)
            {
                if (source == null) continue;

                if (source is ICatCommandSource commandSource)
                {
                    validSources.Add(commandSource);
                }
                else
                {
                    Debug.LogError($"CatInputRouter on '{gameObject.name}': '{source.GetType().Name}' does not implement ICatCommandSource. Skipping.", this);
                }
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
