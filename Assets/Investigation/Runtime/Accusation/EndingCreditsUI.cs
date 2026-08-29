using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VisualNovelSystem;

namespace Investigation
{
    // Pantalla de créditos del final único (Día 3): sin skip, construida por código igual
    // que AccusationEvidenceBoardUI. Termina en un botón "Play Again" en vez de un menú
    // principal porque el juego corre en web y no hay a dónde más volver.
    public class EndingCreditsUI : MonoBehaviour
    {
        private static EndingCreditsUI instance;

        public static EndingCreditsUI Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("EndingCreditsUI");
                    instance = go.AddComponent<EndingCreditsUI>();
                }
                return instance;
            }
        }

        private const string GameSceneName = "Investigation";

        private GameObject panelRoot;
        private CanvasGroup titleGroup;
        private CanvasGroup thanksGroup;
        private CanvasGroup buttonGroup;
        private bool playAgainClicked;

        public IEnumerator PlayRoutine()
        {
            BuildPanelIfNeeded();
            playAgainClicked = false;
            panelRoot.SetActive(true);

            yield return FadeGroup(titleGroup, 1.5f);
            yield return new WaitForSeconds(2f);
            yield return FadeGroup(thanksGroup, 1.5f);
            yield return new WaitForSeconds(2f);
            yield return FadeGroup(buttonGroup, 1f);
            buttonGroup.interactable = true;
            buttonGroup.blocksRaycasts = true;

            while (!playAgainClicked)
            {
                yield return null;
            }

            panelRoot.SetActive(false);

            CaseState.ResetForNewGame();
            PhaseController.ResetForNewGame();
            SceneManager.LoadScene(GameSceneName);
        }

        private void OnPlayAgainClicked()
        {
            if (playAgainClicked) return;
            playAgainClicked = true;
        }

        private static IEnumerator FadeGroup(CanvasGroup group, float duration)
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, 1f, elapsed / duration);
                yield return null;
            }
            group.alpha = 1f;
        }

        private void BuildPanelIfNeeded()
        {
            if (panelRoot != null) return;

            var canvasGO = new GameObject("EndingCreditsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var backdrop = panelRoot.GetComponent<Image>();
            backdrop.color = Color.black;
            backdrop.raycastTarget = true;

            var font = StoryUIController.Instance != null && StoryUIController.Instance.DialogueUI != null
                ? StoryUIController.Instance.DialogueUI.BodyFont
                : null;

            titleGroup = CreateLabel(panelRoot.transform, "Title", "TRUST NO ONE", 48f, new Vector2(0.5f, 0.6f), font);
            thanksGroup = CreateLabel(panelRoot.transform, "Thanks", "Thank you for playing.", 26f, new Vector2(0.5f, 0.48f), font);
            buttonGroup = CreateButton(panelRoot.transform, "Play Again", OnPlayAgainClicked, new Vector2(0.5f, 0.32f), font);

            panelRoot.SetActive(false);
        }

        private static CanvasGroup CreateLabel(Transform parent, string name, string text, float fontSize, Vector2 anchor, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1200f, 100f);
            rect.anchoredPosition = Vector2.zero;

            var label = go.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            return group;
        }

        private static CanvasGroup CreateButton(Transform parent, string text, UnityAction onClick, Vector2 anchor, TMP_FontAsset font)
        {
            var go = new GameObject("PlayAgainButton", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 64f);
            rect.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGO.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            go.GetComponent<Button>().onClick.AddListener(onClick);

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }
    }
}
