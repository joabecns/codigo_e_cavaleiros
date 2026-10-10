using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CodigoECavaleiros.Audio;
using CodigoECavaleiros.Characters;

namespace CodigoECavaleiros.UI
{
    /// <summary>
    /// Menu inicial + escolha do herói. O herói escolhido fica em GameSession.HeroIndex
    /// e acompanha o jogador até o fim da jornada. Vilões e chefe não são selecionáveis.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("Cena")]
        public GameObject[] heroPrefabs;
        public Transform podium;
        public Camera cam;

        enum Screen { Principal, ComoJogar, Selecao }
        Screen screen = Screen.Principal;

        static readonly string[] HeroTitles = { "Leo", "Theo", "Bruno", "Max", "Caio" };
        static readonly string[] HeroBlurbs =
        {
            "Curioso e determinado. Aprende com cada erro.",
            "Observador e cheio de estilo. Nada passa pelos óculos dele.",
            "Calmo sob pressão. Respira fundo antes de responder.",
            "Ritmo e foco: com o fone nos ouvidos, resolve qualquer bug.",
            "Estrategista. Planeja cada resposta antes de agir."
        };

        RectTransform root;
        GameObject mainPanel, howPanel, selectPanel;
        TMP_Text heroName, heroBlurb, heroCounter;

        int index;
        GameObject heroObject;
        CharacterActor actor;
        float camTargetX = 0f;
        GameObject[] podiumObjs;
        Vector2 downPos; bool dragging; float lastClick = -1f;
        Sprite dot;
        readonly List<Image> nodes = new List<Image>();

        void Start()
        {
            podiumObjs = new[] { GameObject.Find("Podio"), GameObject.Find("PodioBase") };
            index = Mathf.Clamp(GameSession.HeroIndex, 0, heroPrefabs.Length - 1);
            AudioManager.Music("music_menu");
            root = UIKit.CreateCanvas(transform, "MenuCanvas");
            BuildMain();
            BuildHow();
            BuildSelect();
            ShowHero(index, false);
            SetScreen(Screen.Principal);
        }

        // ------------------------------------------------------------------ painéis
        void BuildMain()
        {
            var p = UIKit.Stretch("Principal", root, Vector2.zero, Vector2.one);
            mainPanel = p.gameObject;
            BuildTech(p);

            var title = UIKit.Text("Titulo", p, Settings.GameTitle, 78, UIKit.Gold, TextAlignmentOptions.Left, new Vector2(0.05f, 0.62f), new Vector2(0.62f, 0.88f));
            title.fontStyle = FontStyles.Bold;
            UIKit.Text("Subtitulo", p, Settings.GameSubtitle, 48, Color.white, TextAlignmentOptions.Left, new Vector2(0.05f, 0.54f), new Vector2(0.65f, 0.64f));

            MakeMenuButton(p, "JOGAR", 0.42f, new Color(0.25f, 0.62f, 0.38f), () => SetScreen(Screen.Selecao));
            MakeMenuButton(p, "COMO JOGAR", 0.30f, new Color(0.25f, 0.40f, 0.75f), () => SetScreen(Screen.ComoJogar));
            MakeMenuButton(p, "SAIR", 0.18f, new Color(0.55f, 0.25f, 0.30f), Quit);

            UIKit.Text("Rodape", p, "Projeto de Inteligência Artificial • 5 fases • 5 perguntas por rodada", 26, new Color(1, 1, 1, 0.6f),
                TextAlignmentOptions.Left, new Vector2(0.05f, 0.03f), new Vector2(0.7f, 0.09f));
        }

                // ------------------------------------------------------------------ símbolo de IA / tecnologia
        Sprite MakeDot()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(n / 2f - 1f - d)));
                }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        Image MakeImg(string name, RectTransform parent, Sprite sp, Color c, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var im = go.GetComponent<Image>();
            im.sprite = sp; im.color = c; im.raycastTarget = false;
            return im;
        }

        void BuildTech(Transform parent)
        {
            var box = new GameObject("SimboloIA", typeof(RectTransform)).GetComponent<RectTransform>();
            box.SetParent(parent, false);
            box.anchorMin = box.anchorMax = new Vector2(0.77f, 0.54f);
            box.sizeDelta = new Vector2(760, 760);
            dot = MakeDot();
            var cyan = new Color(0.35f, 0.85f, 1f);

            int[] layers = { 3, 5, 5, 3 };
            var pts = new List<Vector2[]>();
            for (int l = 0; l < layers.Length; l++)
            {
                var col = new Vector2[layers[l]];
                for (int k = 0; k < layers[l]; k++)
                    col[k] = new Vector2(-285f + l * 190f, (k - (layers[l] - 1) / 2f) * 120f);
                pts.Add(col);
            }
            MakeImg("Anel", box, dot, new Color(cyan.r, cyan.g, cyan.b, 0.10f), Vector2.zero, new Vector2(700, 700));
            MakeImg("AnelInterno", box, dot, new Color(0.07f, 0.10f, 0.22f, 1f), Vector2.zero, new Vector2(676, 676));
            for (int l = 0; l < layers.Length - 1; l++)
                foreach (var a in pts[l])
                    foreach (var c in pts[l + 1])
                    {
                        var d = c - a;
                        var line = MakeImg("Sinapse", box, null, new Color(cyan.r, cyan.g, cyan.b, 0.28f), (a + c) / 2f, new Vector2(d.magnitude, 3f));
                        line.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    }
            foreach (var col in pts)
                foreach (var pt in col)
                {
                    MakeImg("Brilho", box, dot, new Color(cyan.r, cyan.g, cyan.b, 0.18f), pt, new Vector2(64, 64));
                    nodes.Add(MakeImg("Neuronio", box, dot, cyan, pt, new Vector2(34, 34)));
                }
            var lbl = UIKit.Text("LegendaIA", box, "INTELIGÊNCIA ARTIFICIAL", 34, cyan, TextAlignmentOptions.Center, new Vector2(0f, 0.01f), new Vector2(1f, 0.08f));
            lbl.fontStyle = FontStyles.Bold;
        }

        void PulseSymbol()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                float w = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f + i * 0.55f);
                nodes[i].color = Color.Lerp(new Color(0.2f, 0.45f, 0.7f), new Color(0.6f, 1f, 1f), w);
                nodes[i].rectTransform.localScale = Vector3.one * (0.8f + 0.4f * w);
            }
        }

        // ------------------------------------------------------------------ giro 360° e duplo clique
        void HandleHeroInput()
        {
            if (screen == Screen.Principal) { PulseSymbol(); return; }
            if (screen != Screen.Selecao) return;
            var m = Mouse.current;
            if (m == null) return;
            if (m.leftButton.wasPressedThisFrame)
            {
                downPos = m.position.ReadValue();
                dragging = !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
            }
            if (dragging && m.leftButton.isPressed)
                heroObject.transform.Rotate(0f, -m.delta.ReadValue().x * 0.45f, 0f, Space.World);
            if (dragging && m.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                if ((m.position.ReadValue() - downPos).magnitude < 10f)
                {
                    if (Time.unscaledTime - lastClick < 0.4f) Confirm();
                    lastClick = Time.unscaledTime;
                }
            }
        }

        void MakeMenuButton(Transform parent, string label, float y, Color color, UnityEngine.Events.UnityAction onClick)
        {
            TMP_Text t;
            var b = UIKit.Button(parent, label, color, 46, out t);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.05f, y);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(520, 100);
            b.onClick.AddListener(onClick);
        }

        void BuildHow()
        {
            var p = UIKit.Stretch("ComoJogar", root, Vector2.zero, Vector2.one);
            howPanel = p.gameObject;
            var bg = UIKit.Stretch("Fundo", p, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f));
            UIKit.AddImage(bg.gameObject, UIKit.PanelColor);
            var h = UIKit.Text("Titulo", bg, "COMO JOGAR", 80, UIKit.Gold, TextAlignmentOptions.Center, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f));
            h.fontStyle = FontStyles.Bold;
            UIKit.Text("Regras", bg,
                "• Cada luta tem perguntas de programação. Responda antes que o tempo acabe.\n" +
                "• <color=#7CFF9A>Acertou:</color> o vilão se abala e perde vida. Acertos seguidos formam um <color=#FFDC6E>combo</color> com mais dano.\n" +
                "• <color=#FF7A7A>Errou ou o tempo acabou:</color> o vilão fica furioso, escolhe um ataque ao acaso e você perde vida.\n" +
                "• <color=#C79BFF>Depurar (tecla S):</color> gasta Foco e aumenta o dano do próximo acerto.\n" +
                "• Cada rodada tem 5 perguntas. Vença os 4 vilões e derrote o Compilador Sombrio!\n" +
                "• Teclas: 1 a 4 para responder, S para Depurar. O mouse também funciona.",
                36, Color.white, TextAlignmentOptions.TopLeft, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.80f));
            TMP_Text t;
            var back = UIKit.Button(bg, "VOLTAR", new Color(0.25f, 0.40f, 0.75f), 42, out t);
            var rt = (RectTransform)back.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.09f);
            rt.sizeDelta = new Vector2(420, 90);
            back.onClick.AddListener(() => SetScreen(Screen.Principal));
        }

        void BuildSelect()
        {
            var p = UIKit.Stretch("Selecao", root, Vector2.zero, Vector2.one);
            selectPanel = p.gameObject;

            var head = UIKit.Text("Titulo", p, "ESCOLHA SEU HERÓI", 84, UIKit.Gold, TextAlignmentOptions.Center, new Vector2(0.1f, 0.84f), new Vector2(0.9f, 0.97f));
            head.fontStyle = FontStyles.Bold;
            UIKit.Text("Aviso", p, "Arraste para girar 360° • Dois cliques para escolher", 34, new Color(1, 1, 1, 0.75f), TextAlignmentOptions.Center, new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.85f));

            var box = UIKit.Stretch("Info", p, new Vector2(0.30f, 0.06f), new Vector2(0.70f, 0.24f));
            UIKit.AddImage(box.gameObject, UIKit.PanelColor);
            heroName = UIKit.Text("Nome", box, "", 64, Color.white, TextAlignmentOptions.Center, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.98f));
            heroName.fontStyle = FontStyles.Bold;
            heroBlurb = UIKit.Text("Descricao", box, "", 32, new Color(0.8f, 0.88f, 1f), TextAlignmentOptions.Center, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.55f));
            heroCounter = UIKit.Text("Contador", p, "", 30, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.Center, new Vector2(0.4f, 0.25f), new Vector2(0.6f, 0.30f));

            MakeArrow(p, "<", 0.12f, () => Step(-1));
            MakeArrow(p, ">", 0.88f, () => Step(1));

            TMP_Text t;
            var ok = UIKit.Button(p, "ESCOLHER", new Color(0.25f, 0.62f, 0.38f), 46, out t);
            var okRt = (RectTransform)ok.transform;
            okRt.anchorMin = okRt.anchorMax = new Vector2(0.86f, 0.1f);
            okRt.sizeDelta = new Vector2(440, 100);
            ok.onClick.AddListener(Confirm);

            var back = UIKit.Button(p, "VOLTAR", new Color(0.55f, 0.25f, 0.30f), 40, out t);
            var bRt = (RectTransform)back.transform;
            bRt.anchorMin = bRt.anchorMax = new Vector2(0.14f, 0.1f);
            bRt.sizeDelta = new Vector2(360, 100);
            back.onClick.AddListener(() => SetScreen(Screen.Principal));
        }

        void MakeArrow(Transform parent, string label, float x, UnityEngine.Events.UnityAction onClick)
        {
            TMP_Text t;
            var b = UIKit.Button(parent, label, new Color(0.25f, 0.40f, 0.75f), 90, out t);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, 0.52f);
            rt.sizeDelta = new Vector2(150, 150);
            b.onClick.AddListener(onClick);
        }

        // ------------------------------------------------------------------ lógica
        void SetScreen(Screen s)
        {
            screen = s;
            mainPanel.SetActive(s == Screen.Principal);
            howPanel.SetActive(s == Screen.ComoJogar);
            selectPanel.SetActive(s == Screen.Selecao);
            camTargetX = 0f;
            foreach (var po in podiumObjs) if (po != null) po.SetActive(s == Screen.Selecao);
            if (heroObject != null) { heroObject.SetActive(s == Screen.Selecao); if (s == Screen.Selecao) heroObject.transform.rotation = podium.rotation; }
            if (s == Screen.Selecao) UpdateSelectTexts();
        }

        void Step(int dir)
        {
            index = (index + dir + heroPrefabs.Length) % heroPrefabs.Length;
            AudioManager.Sfx("sfx_combo", 0.6f);
            ShowHero(index, true);
            UpdateSelectTexts();
        }

        void UpdateSelectTexts()
        {
            heroName.text = HeroTitles[index];
            heroBlurb.text = HeroBlurbs[index];
            heroCounter.text = (index + 1) + " / " + heroPrefabs.Length;
        }

        void ShowHero(int i, bool cheer)
        {
            if (heroObject != null) Destroy(heroObject);
            heroObject = Instantiate(heroPrefabs[i], podium.position, podium.rotation);
            actor = heroObject.GetComponent<CharacterActor>();
            if (cheer && actor != null) StartCoroutine(CheerThenIdle(actor));
        }

        System.Collections.IEnumerator CheerThenIdle(CharacterActor a)
        {
            yield return null;
            if (a != null) a.PlayCheer();
            yield return new WaitForSeconds(1.6f);
            if (a != null) a.PlayIdle();
        }

        void Confirm()
        {
            GameSession.HeroIndex = index;
            SceneManager.LoadScene(Settings.BattleScene);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
            if (heroObject != null && screen != Screen.ComoJogar)
                HandleHeroInput();

            if (cam != null)
            {
                var p = cam.transform.position;
                p.x = Mathf.Lerp(p.x, camTargetX, 1f - Mathf.Exp(-6f * Time.deltaTime));
                cam.transform.position = p;
            }

            var kb = Keyboard.current;
            if (kb == null) return;
            if (screen == Screen.Selecao)
            {
                if (kb[Key.LeftArrow].wasPressedThisFrame || kb[Key.A].wasPressedThisFrame) Step(-1);
                if (kb[Key.RightArrow].wasPressedThisFrame || kb[Key.D].wasPressedThisFrame) Step(1);
                if (kb[Key.Enter].wasPressedThisFrame) Confirm();
                if (kb[Key.Escape].wasPressedThisFrame) SetScreen(Screen.Principal);
            }
            else if (screen == Screen.ComoJogar)
            {
                if (kb[Key.Escape].wasPressedThisFrame || kb[Key.Enter].wasPressedThisFrame) SetScreen(Screen.Principal);
            }
        }
    }
}
