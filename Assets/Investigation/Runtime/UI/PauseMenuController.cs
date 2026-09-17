using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Investigation
{
    public class PauseMenuController : MonoBehaviour
    {
        private static PauseMenuController instance;
        public static PauseMenuController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
                    if (instance == null)
                    {
                        var go = new GameObject("PauseMenuController");
                        instance = go.AddComponent<PauseMenuController>();
                    }
                }
                return instance;
            }
        }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_FontAsset noirFont;

        private Canvas pauseCanvas;
        private bool isPaused = false;
        public bool IsPaused => isPaused;

        public static event Action<bool> OnPauseToggled;

        private TextMeshProUGUI masterValText;
        private TextMeshProUGUI musicValText;
        private TextMeshProUGUI sfxValText;
        private TextMeshProUGUI textSpeedValText;
        private TextMeshProUGUI feedbackText;
        private GameObject pauseTriggerButton;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            BuildTriggerButtonIfNeeded();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            SetPaused(!isPaused);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            if (isPaused)
            {
                BuildUIIfNeeded();
                UpdateSettingsDisplay();
                if (panelRoot != null) panelRoot.SetActive(true);
                if (pauseTriggerButton != null) pauseTriggerButton.SetActive(false);
            }
            else
            {
                if (panelRoot != null) panelRoot.SetActive(false);
                if (pauseTriggerButton != null) pauseTriggerButton.SetActive(true);
            }

            OnPauseToggled?.Invoke(isPaused);
        }

        public void Resume()
        {
            SetPaused(false);
        }

        public void SaveCurrentGame()
        {
            bool success = SaveGameService.Instance.Save();
            if (feedbackText != null)
            {
                feedbackText.text = success ? "Case saved successfully." : "Failed to save.";
            }
        }

        public void RestartCurrentDay()
        {
            if (PhaseController.Instance != null && CaseState.Instance != null)
            {
                int day = CaseState.Instance.currentDay;
                PhaseController.Instance.SetPhase(day, 1);
                SaveGameService.Instance.Save();
            }
            Resume();
        }

        public void ReturnToTitleOrReload()
        {
            Resume();
            CaseState.ResetForNewGame();
            PhaseController.ResetForNewGame();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void AdjustMasterVolume(float delta)
        {
            var audio = AudioSettingsService.Instance;
            audio.SetMasterVolume(Mathf.Clamp01(audio.MasterVolume + delta));
            UpdateSettingsDisplay();
        }

        public void AdjustMusicVolume(float delta)
        {
            var audio = AudioSettingsService.Instance;
            audio.SetMusicVolume(Mathf.Clamp01(audio.MusicVolume + delta));
            UpdateSettingsDisplay();
        }

        public void AdjustSfxVolume(float delta)
        {
            var audio = AudioSettingsService.Instance;
            audio.SetSfxVolume(Mathf.Clamp01(audio.SfxVolume + delta));
            UpdateSettingsDisplay();
        }

        public void CycleTextSpeed()
        {
            var audio = AudioSettingsService.Instance;
            TextSpeedSetting next = (TextSpeedSetting)(((int)audio.TextSpeed + 1) % 3);
            audio.SetTextSpeed(next);
            UpdateSettingsDisplay();
        }

        private void UpdateSettingsDisplay()
        {
            var audio = AudioSettingsService.Instance;
            if (masterValText != null) masterValText.text = $"{Mathf.RoundToInt(audio.MasterVolume * 100)}%";
            if (musicValText != null) musicValText.text = $"{Mathf.RoundToInt(audio.MusicVolume * 100)}%";
            if (sfxValText != null) sfxValText.text = $"{Mathf.RoundToInt(audio.SfxVolume * 100)}%";
            if (textSpeedValText != null) textSpeedValText.text = audio.TextSpeed.ToString();
            if (feedbackText != null) feedbackText.text = "";
        }

        private void EnsureCanvas()
        {
            if (pauseCanvas != null) return;

            var dialogueUI = VisualNovelSystem.StoryUIController.Instance != null
                ? VisualNovelSystem.StoryUIController.Instance.DialogueUI
                : null;
            if (noirFont == null && dialogueUI != null) noirFont = dialogueUI.BodyFont;

            var canvasGO = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            pauseCanvas = canvasGO.GetComponent<Canvas>();
            pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.sortingOrder = 2000;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        public void BuildTriggerButtonIfNeeded()
        {
            if (pauseTriggerButton != null) return;
            EnsureCanvas();

            pauseTriggerButton = new GameObject("MobilePauseTriggerButton", typeof(RectTransform), typeof(Image), typeof(Button));
            pauseTriggerButton.transform.SetParent(pauseCanvas.transform, false);

            var rect = pauseTriggerButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(56f, 56f);
            rect.anchoredPosition = new Vector2(-28f, -28f);

            var img = pauseTriggerButton.GetComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.16f, 0.75f);

            var btn = pauseTriggerButton.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.12f, 0.16f, 0.75f);
            colors.highlightedColor = new Color(0.24f, 0.24f, 0.30f, 0.90f);
            colors.pressedColor = new Color(0.08f, 0.08f, 0.10f, 0.95f);
            btn.colors = colors;
            btn.onClick.AddListener(TogglePause);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(pauseTriggerButton.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            if (noirFont != null) tmp.font = noirFont;
            tmp.text = "||";
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        private void BuildUIIfNeeded()
        {
            if (panelRoot != null) return;

            EnsureCanvas();

            panelRoot = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(pauseCanvas.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var backdrop = panelRoot.GetComponent<Image>();
            backdrop.color = new Color(0.04f, 0.04f, 0.06f, 0.92f);
            backdrop.raycastTarget = true;

            // Contenedor central estilo Noir
            var modalBox = new GameObject("ModalBox", typeof(RectTransform), typeof(Image));
            modalBox.transform.SetParent(panelRoot.transform, false);
            var modalRect = modalBox.GetComponent<RectTransform>();
            modalRect.sizeDelta = new Vector2(620f, 680f);
            modalRect.anchoredPosition = Vector2.zero;
            var boxImg = modalBox.GetComponent<Image>();
            boxImg.color = new Color(0.10f, 0.10f, 0.12f, 0.98f);

            // Title
            var titleGO = CreateText("PAUSED", modalBox.transform, new Vector2(0, 270), new Vector2(500, 50), 32, TextAlignmentOptions.Center, Color.white);

            // Subtitle
            var subtitleGO = CreateText("CASE NOTEBOOK & SETTINGS", modalBox.transform, new Vector2(0, 230), new Vector2(500, 30), 16, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f, 1f));

            // Controls
            masterValText = CreateSettingRow("Master Volume", 170, modalBox.transform, () => AdjustMasterVolume(-0.1f), () => AdjustMasterVolume(0.1f));
            musicValText = CreateSettingRow("Music Volume", 115, modalBox.transform, () => AdjustMusicVolume(-0.1f), () => AdjustMusicVolume(0.1f));
            sfxValText = CreateSettingRow("SFX Volume", 60, modalBox.transform, () => AdjustSfxVolume(-0.1f), () => AdjustSfxVolume(0.1f));
            textSpeedValText = CreateSettingToggleRow("Text Speed", 5, modalBox.transform, CycleTextSpeed);

            // Feedback Text
            var fbGO = CreateText("", modalBox.transform, new Vector2(0, -50), new Vector2(500, 30), 15, TextAlignmentOptions.Center, new Color(0.85f, 0.75f, 0.35f, 1f));
            feedbackText = fbGO.GetComponent<TextMeshProUGUI>();

            // Buttons
            CreateButton("RESUME INVESTIGATION", modalBox.transform, new Vector2(0, -100), new Vector2(360, 48), Resume, new Color(0.2f, 0.45f, 0.25f, 1f));
            CreateButton("SAVE PROGRESS", modalBox.transform, new Vector2(0, -160), new Vector2(360, 44), SaveCurrentGame, new Color(0.25f, 0.28f, 0.35f, 1f));
            CreateButton("RESTART CURRENT DAY", modalBox.transform, new Vector2(0, -215), new Vector2(360, 44), RestartCurrentDay, new Color(0.45f, 0.3f, 0.2f, 1f));
            CreateButton("RESTART CASE", modalBox.transform, new Vector2(0, -270), new Vector2(360, 44), ReturnToTitleOrReload, new Color(0.45f, 0.18f, 0.18f, 1f));
        }

        private TextMeshProUGUI CreateSettingRow(string label, float yPos, Transform parent, Action onDec, Action onInc)
        {
            CreateText(label, parent, new Vector2(-120, yPos), new Vector2(220, 35), 18, TextAlignmentOptions.MidlineLeft, Color.white);
            CreateButton("-", parent, new Vector2(70, yPos), new Vector2(36, 34), onDec, new Color(0.2f, 0.2f, 0.22f, 1f));
            var valGO = CreateText("100%", parent, new Vector2(125, yPos), new Vector2(65, 34), 16, TextAlignmentOptions.Center, Color.white);
            CreateButton("+", parent, new Vector2(180, yPos), new Vector2(36, 34), onInc, new Color(0.2f, 0.2f, 0.22f, 1f));
            return valGO.GetComponent<TextMeshProUGUI>();
        }

        private TextMeshProUGUI CreateSettingToggleRow(string label, float yPos, Transform parent, Action onToggle)
        {
            CreateText(label, parent, new Vector2(-120, yPos), new Vector2(220, 35), 18, TextAlignmentOptions.MidlineLeft, Color.white);
            var valGO = CreateText("Normal", parent, new Vector2(110, yPos), new Vector2(100, 34), 16, TextAlignmentOptions.Center, Color.white);
            CreateButton("CHANGE", parent, new Vector2(205, yPos), new Vector2(80, 34), onToggle, new Color(0.22f, 0.25f, 0.3f, 1f));
            return valGO.GetComponent<TextMeshProUGUI>();
        }

        private GameObject CreateText(string content, Transform parent, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject("Text_" + content, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (noirFont != null) text.font = noirFont;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            return go;
        }

        private GameObject CreateButton(string label, Transform parent, Vector2 pos, Vector2 size, Action onClick, Color normalColor)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.color = normalColor;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = normalColor * 1.25f;
            colors.pressedColor = normalColor * 0.8f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            if (noirFont != null) tmp.font = noirFont;
            tmp.text = label;
            tmp.fontSize = 15f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return go;
        }
    }
}
