using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-A09: the thin line beside the cat's portrait: one mark per checkpoint gate (D-091), and a dot that moves on when the cat passes one.
    /// A level with no gates has nothing to measure, so it shows the portrait alone. Presentation only: it reads the room, and eases on real time.</summary>
    public sealed class LevelProgressLine : MonoBehaviour
    {
        [SerializeField] RoomManager rooms;
        [SerializeField] GameObject line;
        [SerializeField] Image lit;
        [SerializeField] RectTransform dot;
        [SerializeField] RectTransform gateTemplate;
        [SerializeField] float ease = 5f;

        readonly List<GameObject> gates = new List<GameObject>();
        int shownCount = -1;
        float shown;

        void Update()
        {
            if (rooms == null) return;
            int count = rooms.SectionCount;
            if (count != shownCount) Build(count);
            float target = SectionProgress.LineFraction(rooms.CurrentSection, count);
            shown = Mathf.MoveTowards(shown, target, ease * Time.unscaledDeltaTime * Mathf.Max(0.05f, Mathf.Abs(target - shown)));
            Show(shown);
        }

        void Build(int count)
        {
            shownCount = count;
            shown = 0f;
            for (int i = 0; i < gates.Count; i++) if (gates[i] != null) Destroy(gates[i]);
            gates.Clear();
            bool hasGates = count > 1;
            if (line != null) line.SetActive(hasGates);
            if (!hasGates || gateTemplate == null) return;
            for (int i = 1; i < count; i++)
            {
                RectTransform gate = Instantiate(gateTemplate, gateTemplate.parent);
                gate.gameObject.SetActive(true);
                float x = SectionProgress.LineFraction(i, count);
                gate.anchorMin = new Vector2(x, 0.5f);
                gate.anchorMax = new Vector2(x, 0.5f);
                gates.Add(gate.gameObject);
            }
            if (dot != null) dot.SetAsLastSibling();
        }

        void Show(float fraction)
        {
            if (lit != null) lit.fillAmount = fraction;
            if (dot == null) return;
            dot.anchorMin = new Vector2(fraction, 0.5f);
            dot.anchorMax = new Vector2(fraction, 0.5f);
        }
    }
}
