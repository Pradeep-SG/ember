using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Kindrith.Core;
using Kindrith.Persistence;

namespace Kindrith.UI
{
    public sealed class SessionLogView : MonoBehaviour
    {
        const int MaxEntries = 3;

        Text _text;
        Palette _palette;
        BattleStore _store;

        public static SessionLogView Create(Transform parent, Palette palette, BattleStore store)
        {
            var go = new GameObject("SessionLogView",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.1f, 0.05f);
            rt.anchorMax = new Vector2(0.9f, 0.25f);
            rt.sizeDelta = Vector2.zero;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = palette.Bone;
            text.alignment = TextAnchor.UpperLeft;

            var view = go.AddComponent<SessionLogView>();
            view._text = text;
            view._palette = palette;
            view._store = store;
            view.Refresh();
            return view;
        }

        public void Refresh()
        {
            if (_text == null || _store == null) return;
            var records = _store.LoadRecent(MaxEntries);
            if (records == null || records.Length == 0)
            {
                _text.text = "No battles yet.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Last battles:");
            foreach (var record in records)
            {
                if (record == null) continue;
                sb.AppendLine($"  {record.outcome ?? "?"} — {record.demon_archetype_used ?? "?"} — {record.ended_at ?? "?"}");
            }
            _text.text = sb.ToString();
        }
    }
}
