using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CodigoECavaleiros.UI
{
    /// <summary>Pausa a batalha com ESC: congela tempo, animações e música; oferece continuar, reiniciar ou voltar ao menu.</summary>
    public class PauseMenu : MonoBehaviour
    {
        public static bool Paused { get; private set; }
        GameObject panel;

        void Start()
        {
            Resume();
            var root = UIKit.CreateCanvas(transform, "PauseCanvas");
            root.GetComponent<Canvas>().sortingOrder = 200;
            panel = UIKit.Stretch("PainelPausa", root, Vector2.zero, Vector2.one).gameObject;
            UIKit.AddImage(panel, new Color(0.03f, 0.05f, 0.12f, 0.82f));
            var box = UIKit.Stretch("Caixa", panel.transform, new Vector2(0.32f, 0.16f), new Vector2(0.68f, 0.84f));
            UIKit.AddImage(box.gameObject, UIKit.PanelColor);
            var title = UIKit.Text("Titulo", box, "PAUSADO", 88, UIKit.Gold, TextAlignmentOptions.Center, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.96f));
            title.fontStyle = FontStyles.Bold;
            MakeButton(box, "CONTINUAR", 0.60f, new Color(0.25f, 0.62f, 0.38f), Toggle);
            MakeButton(box, "REINICIAR", 0.43f, new Color(0.25f, 0.40f, 0.75f), RestartGame);
            MakeButton(box, "VOLTAR AO MENU", 0.26f, new Color(0.55f, 0.25f, 0.30f), ToMenu);
            UIKit.Text("Dica", box, "ESC para continuar", 30, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Center, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.14f));
            panel.SetActive(false);
        }

        void MakeButton(Transform parent, string label, float y, Color color, UnityEngine.Events.UnityAction onClick)
        {
            TMP_Text t;
            var b = UIKit.Button(parent, label, color, 44, out t);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(560, 92);
            b.onClick.AddListener(onClick);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[Key.Escape].wasPressedThisFrame) Toggle();
        }

        void Toggle()
        {
            if (panel == null) return;
            Paused = !Paused;
            panel.SetActive(Paused);
            Time.timeScale = Paused ? 0f : 1f;
            AudioListener.pause = Paused;
        }

        static void Resume()
        {
            Paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void RestartGame() { Resume(); SceneManager.LoadScene(Settings.BattleScene); }
        void ToMenu() { Resume(); SceneManager.LoadScene(Settings.MenuScene); }
        void OnDestroy() { Resume(); }
    }
}
