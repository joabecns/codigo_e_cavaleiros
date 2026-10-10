using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using CodigoECavaleiros.Questions;

namespace CodigoECavaleiros.UI
{
    /// <summary>
    /// Interface da batalha, montada por código (sem prefabs de UI):
    /// caixa do vilão (nome + vida), caixa do herói, painel de pergunta com 4 botões,
    /// barra de tempo, botão "Depurar", faixa de mensagens e tela final.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        public event Action<int> OptionChosen;
        public event Action SkillToggled;
        public event Action Continue;

        static readonly Color Panel = new Color(0.08f, 0.10f, 0.18f, 0.92f);
        static readonly Color Gold = new Color(1f, 0.86f, 0.43f);

        RectTransform root;
        // vilão
        TMP_Text enemyName, enemyInfo, enemyHpText;
        RectTransform enemyFill; Image enemyFillImg;
        float enemyShown = 1f, enemyTarget = 1f;
        // herói
        TMP_Text playerName, playerHpText, playerFcText, comboText;
        RectTransform playerFill; Image playerFillImg;
        float playerShown = 1f, playerTarget = 1f;
        // pergunta
        GameObject questionPanel;
        TMP_Text questionText, skillLabel;
        Button[] optionButtons = new Button[4];
        TMP_Text[] optionTexts = new TMP_Text[4];
        RectTransform timerFill; Image timerFillImg;
        Image skillImage;
        // mensagens
        GameObject banner; TMP_Text bannerText;
        GameObject endPanel; TMP_Text endTitle, endBody, endButtonLabel;

        bool questionActive, endActive;

        // ------------------------------------------------------------------ montagem
        public void Build()
        {
            var canvasGo = new GameObject("BattleCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            root = canvasGo.GetComponent<RectTransform>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                var module = es.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }

            BuildEnemyBox();
            BuildPlayerBox();
            BuildQuestionPanel();
            BuildBanner();
            BuildEndPanel();
            questionPanel.SetActive(false);
            banner.SetActive(false);
            endPanel.SetActive(false);
        }

        void BuildEnemyBox()
        {
            var box = MakeRect("CaixaVilao", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -40), new Vector2(620, 140));
            AddImage(box.gameObject, Panel);
            enemyName = MakeText("Nome", box, "Vilão", 40, Color.white, TextAlignmentOptions.Left, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.98f));
            enemyName.fontStyle = FontStyles.Bold;
            MakeBar(box, new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.52f), out enemyFill, out enemyFillImg);
            enemyHpText = MakeText("HP", box, "", 24, Color.white, TextAlignmentOptions.Right, new Vector2(0.5f, 0.52f), new Vector2(0.96f, 0.70f));
            enemyInfo = MakeText("Info", box, "", 24, new Color(0.75f, 0.82f, 1f), TextAlignmentOptions.Left, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.32f));
        }

        void BuildPlayerBox()
        {
            var box = MakeRect("CaixaHeroi", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 360), new Vector2(620, 160));
            AddImage(box.gameObject, Panel);
            playerName = MakeText("Nome", box, "Herói", 40, Color.white, TextAlignmentOptions.Left, new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.98f));
            playerName.fontStyle = FontStyles.Bold;
            MakeBar(box, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.56f), out playerFill, out playerFillImg);
            playerHpText = MakeText("HP", box, "", 26, Color.white, TextAlignmentOptions.Right, new Vector2(0.5f, 0.56f), new Vector2(0.96f, 0.72f));
            playerFcText = MakeText("FC", box, "", 24, new Color(0.6f, 0.8f, 1f), TextAlignmentOptions.Left, new Vector2(0.04f, 0.04f), new Vector2(0.45f, 0.38f));
            comboText = MakeText("Combo", box, "", 28, Gold, TextAlignmentOptions.Right, new Vector2(0.4f, 0.04f), new Vector2(0.96f, 0.38f));
            comboText.fontStyle = FontStyles.Bold;
        }

        void BuildQuestionPanel()
        {
            var p = MakeRect("PainelPergunta", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(-80, 310));
            AddImage(p.gameObject, Panel);
            questionPanel = p.gameObject;
            questionText = MakeText("Pergunta", p, "", 38, Color.white, TextAlignmentOptions.Left, new Vector2(0.02f, 0.62f), new Vector2(0.98f, 0.97f));

            var grid = MakeRect("Opcoes", p, new Vector2(0.02f, 0.15f), new Vector2(0.98f, 0.60f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            grid.offsetMin = grid.offsetMax = Vector2.zero;
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(860, 64);
            layout.spacing = new Vector2(14, 10);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var btn = MakeButton(grid, "", new Color(0.20f, 0.30f, 0.55f), out optionTexts[i]);
                optionTexts[i].fontSize = 30;
                optionTexts[i].alignment = TextAlignmentOptions.Left;
                optionTexts[i].margin = new Vector4(20, 0, 10, 0);
                btn.onClick.AddListener(() => { if (questionActive && OptionChosen != null) OptionChosen(index); });
                optionButtons[i] = btn;
            }

            // barra de tempo
            var bg = MakeRect("Tempo", p, new Vector2(0.02f, 0.04f), new Vector2(0.60f, 0.11f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            AddImage(bg.gameObject, new Color(0, 0, 0, 0.55f));
            timerFill = MakeRect("Fill", bg, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            timerFill.offsetMin = timerFill.offsetMax = Vector2.zero;
            timerFillImg = AddImage(timerFill.gameObject, new Color(0.4f, 0.9f, 0.5f));

            // habilidade
            var skill = MakeButton(p, "", new Color(0.55f, 0.35f, 0.75f), out skillLabel);
            var sr = (RectTransform)skill.transform;
            sr.anchorMin = new Vector2(0.64f, 0.03f); sr.anchorMax = new Vector2(0.98f, 0.13f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;
            skillLabel.fontSize = 26;
            skillImage = skill.GetComponent<Image>();
            skill.onClick.AddListener(() => { if (questionActive && SkillToggled != null) SkillToggled(); });
        }

        void BuildBanner()
        {
            var b = MakeRect("Faixa", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(1300, 190));
            AddImage(b.gameObject, new Color(0.05f, 0.07f, 0.14f, 0.88f));
            banner = b.gameObject;
            bannerText = MakeText("Texto", b, "", 46, Color.white, TextAlignmentOptions.Center, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f));
        }

        void BuildEndPanel()
        {
            var e = MakeRect("Final", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            e.offsetMin = e.offsetMax = Vector2.zero;
            AddImage(e.gameObject, new Color(0.02f, 0.03f, 0.08f, 0.78f));
            endPanel = e.gameObject;
            endTitle = MakeText("Titulo", e, "", 96, Gold, TextAlignmentOptions.Center, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.85f));
            endTitle.fontStyle = FontStyles.Bold;
            endBody = MakeText("Corpo", e, "", 38, Color.white, TextAlignmentOptions.Center, new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.60f));
            var btn = MakeButton(e, "", new Color(0.25f, 0.6f, 0.35f), out endButtonLabel);
            var br = (RectTransform)btn.transform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.2f);
            br.sizeDelta = new Vector2(560, 100);
            endButtonLabel.fontSize = 42;
            btn.onClick.AddListener(() => { if (endActive && Continue != null) Continue(); });
        }

        // ------------------------------------------------------------------ API pública
        public void SetEnemy(string name, string info, int hp, int maxHp, bool instant = false)
        {
            enemyName.text = name;
            enemyInfo.text = info;
            enemyHpText.text = hp + " / " + maxHp;
            enemyTarget = maxHp > 0 ? (float)hp / maxHp : 0f;
            if (instant) enemyShown = enemyTarget;
        }

        public void SetPlayer(string name, int level, int hp, int maxHp, int fc, int maxFc, string combo, bool instant = false)
        {
            playerName.text = name + "  Nv." + level;
            playerHpText.text = hp + " / " + maxHp;
            playerFcText.text = "Foco " + fc + "/" + maxFc;
            comboText.text = combo;
            playerTarget = maxHp > 0 ? (float)hp / maxHp : 0f;
            if (instant) playerShown = playerTarget;
        }

        public void ShowQuestion(Question q, int number, int total)
        {
            questionText.text = "<color=#FFDC6E>" + number + "/" + total + "</color>  " + q.text;
            for (int i = 0; i < 4; i++)
            {
                optionTexts[i].text = (i + 1) + ")  " + q.options[i];
                optionButtons[i].interactable = true;
                optionButtons[i].GetComponent<Image>().color = new Color(0.20f, 0.30f, 0.55f);
            }
            questionPanel.SetActive(true);
            questionActive = true;
            SetTimer(1f);
        }

        public void HideQuestion()
        {
            questionActive = false;
            questionPanel.SetActive(false);
        }

        public void SetTimer(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            timerFill.anchorMax = new Vector2(ratio, 1f);
            timerFillImg.color = ratio > 0.5f ? new Color(0.4f, 0.9f, 0.5f) : (ratio > 0.25f ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.35f, 0.35f));
        }

        public void SetSkill(bool armed, bool available, int cost)
        {
            skillLabel.text = armed ? "DEPURAR ATIVO  (S)" : "Depurar  -" + cost + " Foco  (S)";
            skillImage.color = armed ? new Color(0.95f, 0.65f, 0.15f) : (available ? new Color(0.55f, 0.35f, 0.75f) : new Color(0.3f, 0.3f, 0.35f));
        }

        public void ShowBanner(string text) { bannerText.text = text; banner.SetActive(true); }
        public void HideBanner() { banner.SetActive(false); }

        public void ShowEnd(string title, string body, string button)
        {
            endTitle.text = title; endBody.text = body; endButtonLabel.text = button;
            endPanel.SetActive(true);
            endActive = true;
        }

        public void HideEnd() { endActive = false; endPanel.SetActive(false); }

        // ------------------------------------------------------------------ loop
        void Update()
        {
            enemyShown = Mathf.MoveTowards(enemyShown, enemyTarget, Time.deltaTime * 0.8f);
            playerShown = Mathf.MoveTowards(playerShown, playerTarget, Time.deltaTime * 0.8f);
            ApplyBar(enemyFill, enemyFillImg, enemyShown);
            ApplyBar(playerFill, playerFillImg, playerShown);

            var kb = Keyboard.current;
            if (kb == null) return;
            if (questionActive)
            {
                Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
                Key[] pad = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4 };
                for (int i = 0; i < 4; i++)
                    if ((kb[keys[i]].wasPressedThisFrame || kb[pad[i]].wasPressedThisFrame) && OptionChosen != null) OptionChosen(i);
                if (kb[Key.S].wasPressedThisFrame && SkillToggled != null) SkillToggled();
            }
            if (endActive && (kb[Key.Enter].wasPressedThisFrame || kb[Key.Space].wasPressedThisFrame) && Continue != null) Continue();
        }

        static void ApplyBar(RectTransform fill, Image img, float ratio)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            img.color = ratio > 0.5f ? new Color(0.35f, 0.85f, 0.45f) : (ratio > 0.25f ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.33f, 0.33f));
        }

        // ------------------------------------------------------------------ helpers
        static RectTransform MakeRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static Image AddImage(GameObject go, Color c)
        {
            var img = go.AddComponent<Image>();
            img.color = c;
            return img;
        }

        static TMP_Text MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax)
        {
            var rt = MakeRect(name, parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        static void MakeBar(Transform parent, Vector2 aMin, Vector2 aMax, out RectTransform fill, out Image fillImg)
        {
            var bg = MakeRect("Barra", parent, aMin, aMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            AddImage(bg.gameObject, new Color(0, 0, 0, 0.6f));
            fill = MakeRect("Fill", bg, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fillImg = AddImage(fill.gameObject, new Color(0.35f, 0.85f, 0.45f));
        }

        static Button MakeButton(Transform parent, string label, Color color, out TMP_Text text)
        {
            var rt = MakeRect("Botao", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 60));
            var img = AddImage(rt.gameObject, color);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            btn.colors = colors;
            text = MakeText("Texto", rt, label, 30, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            return btn;
        }
    }
}
