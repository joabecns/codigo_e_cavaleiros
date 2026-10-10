using CodigoECavaleiros.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CodigoECavaleiros.UI
{
    /// <summary>Pequenas funções para montar interface (uGUI + TextMeshPro) por código.</summary>
    public static class UIKit
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.10f, 0.18f, 0.92f);
        public static readonly Color Gold = new Color(1f, 0.86f, 0.43f);

        public static RectTransform CreateCanvas(Transform parent, string name = "Canvas")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return go.GetComponent<RectTransform>();
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent, Vector2 aMin, Vector2 aMax)
        {
            var rt = Rect(name, parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image AddImage(GameObject go, Color c)
        {
            var img = go.AddComponent<Image>();
            img.color = c;
            return img;
        }

        public static TMP_Text Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax)
        {
            var rt = Stretch(name, parent, aMin, aMax);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, Color color, float fontSize, out TMP_Text text)
        {
            var rt = Rect("Botao", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 90));
            var img = AddImage(rt.gameObject, color);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            btn.onClick.AddListener(() => AudioManager.Sfx("sfx_click"));
            text = Text("Texto", rt, label, fontSize, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            text.fontStyle = FontStyles.Bold;
            return btn;
        }
    }
}
