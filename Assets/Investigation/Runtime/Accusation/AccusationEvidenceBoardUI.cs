using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Investigation
{
    // Picker de evidencia para la acusación final: mismo lenguaje visual que ClueBoardController
    // (pines en un corcho) pero interactivo. Clic izquierdo confirma la pista como evidencia.
    // Clic derecho solo tacha/destacha con una X (ayuda visual, no bloquea la selección).
    // Construye toda su UI por código, sin depender de wiring en la escena.
    public class AccusationEvidenceBoardUI : MonoBehaviour
    {
        private static AccusationEvidenceBoardUI instance;

        public static AccusationEvidenceBoardUI Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AccusationEvidenceBoardUI");
                    instance = go.AddComponent<AccusationEvidenceBoardUI>();
                }
                return instance;
            }
        }

        private GameObject panelRoot;
        private RectTransform pinArea;
        private TextMeshProUGUI detailText;
        private TMP_FontAsset boardFont;
        private const string DetailPlaceholder = "Hover a pin to read it.";

        private readonly List<GameObject> spawnedPins = new List<GameObject>();

        private bool selectionMade;
        private string selectedClueId;

        public IEnumerator SelectEvidenceRoutine(List<ClueData> collectedClues, Action<string> onSelected)
        {
            if (VisualNovelSystem.StoryUIController.Instance != null)
            {
                VisualNovelSystem.StoryUIController.Instance.HideDialogue();
            }

            BuildPanelIfNeeded();
            SpawnPins(collectedClues);

            selectionMade = false;
            selectedClueId = null;
            detailText.text = DetailPlaceholder;
            panelRoot.SetActive(true);

            while (!selectionMade)
            {
                yield return null;
            }

            panelRoot.SetActive(false);
            ClearPins();

            onSelected?.Invoke(selectedClueId);
        }

        private void Confirm(string clueId)
        {
            if (selectionMade) return;
            selectedClueId = clueId;
            selectionMade = true;
        }

        private void SpawnPins(List<ClueData> collectedClues)
        {
            ClearPins();

            foreach (var clue in collectedClues)
            {
                var rect = CreatePin(clue);
                spawnedPins.Add(rect.gameObject);
            }
        }

        private void ClearPins()
        {
            foreach (var pin in spawnedPins)
            {
                if (pin != null) Destroy(pin);
            }
            spawnedPins.Clear();
        }

        private RectTransform CreatePin(ClueData clue)
        {
            var go = new GameObject("Pin_" + clue.id, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pinArea, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(170f, 56f);
            rect.anchoredPosition = FindFreeSpot(rect.sizeDelta, clue.id);

            go.GetComponent<Image>().color = new Color(0.55f, 0.45f, 0.15f, 0.95f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 4f);
            labelRect.offsetMax = new Vector2(-16f, -4f);
            var label = labelGO.GetComponent<TextMeshProUGUI>();
            if (boardFont != null) label.font = boardFont;
            label.text = clue.displayName;
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var markGO = new GameObject("DiscardMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            markGO.transform.SetParent(go.transform, false);
            var markRect = markGO.GetComponent<RectTransform>();
            markRect.anchorMin = markRect.anchorMax = new Vector2(1f, 1f);
            markRect.pivot = new Vector2(1f, 1f);
            markRect.sizeDelta = new Vector2(18f, 18f);
            markRect.anchoredPosition = new Vector2(-2f, -2f);
            var mark = markGO.GetComponent<TextMeshProUGUI>();
            if (boardFont != null) mark.font = boardFont;
            mark.text = "X";
            mark.fontSize = 16f;
            mark.alignment = TextAlignmentOptions.Center;
            mark.color = new Color(0.9f, 0.15f, 0.15f, 1f);
            mark.raycastTarget = false;
            markGO.SetActive(false);

            var pin = go.AddComponent<EvidencePinView>();
            pin.Init(clue.id, clue.displayName, clue.description, markGO,
                onSelect: Confirm,
                onHover: (l, d) => detailText.text = $"{l}\n\n{d}",
                onHoverExit: () => detailText.text = DetailPlaceholder);

            return rect;
        }

        private Vector2 FindFreeSpot(Vector2 size, string seedKey)
        {
            var boardSize = pinArea.rect.size;
            float halfW = Mathf.Max(0f, boardSize.x * 0.5f - size.x * 0.5f);
            float halfH = Mathf.Max(0f, boardSize.y * 0.5f - size.y * 0.5f);

            var rng = new System.Random(DeterministicHash(seedKey));

            for (int attempt = 0; attempt < 30; attempt++)
            {
                var candidate = new Vector2(
                    (float)(rng.NextDouble() * 2 - 1) * halfW,
                    (float)(rng.NextDouble() * 2 - 1) * halfH);

                bool overlaps = false;
                foreach (var pinGO in spawnedPins)
                {
                    var existing = pinGO.GetComponent<RectTransform>();
                    if (existing != null && Vector2.Distance(existing.anchoredPosition, candidate) < size.x * 0.6f)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps) return candidate;
            }

            return Vector2.zero;
        }

        // Hash estable entre sesiones (a diferencia de string.GetHashCode), así cada pista
        // siempre cae en la misma posición del corcho — igual que ClueBoardController.
        private static int DeterministicHash(string s)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in s) hash = hash * 31 + c;
                return hash;
            }
        }

        private void BuildPanelIfNeeded()
        {
            if (panelRoot != null) return;

            var dialogueUI = VisualNovelSystem.StoryUIController.Instance != null
                ? VisualNovelSystem.StoryUIController.Instance.DialogueUI
                : null;
            boardFont = dialogueUI != null ? dialogueUI.BodyFont : null;

            var canvasGO = new GameObject("AccusationEvidenceBoardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
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
            backdrop.color = new Color(0f, 0f, 0f, 0.85f);
            backdrop.raycastTarget = true;

            // Header: prompt + botón "sin evidencia".
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(panelRoot.transform, false);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 90f);
            headerRect.anchoredPosition = Vector2.zero;

            var promptGO = new GameObject("Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            promptGO.transform.SetParent(header.transform, false);
            var promptRect = promptGO.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0f, 0f);
            promptRect.anchorMax = new Vector2(0.7f, 1f);
            promptRect.offsetMin = new Vector2(32f, 0f);
            promptRect.offsetMax = new Vector2(0f, -12f);
            var prompt = promptGO.GetComponent<TextMeshProUGUI>();
            if (boardFont != null) prompt.font = boardFont;
            prompt.text = "What do I present as evidence?";
            prompt.fontSize = 26f;
            prompt.alignment = TextAlignmentOptions.MidlineLeft;
            prompt.color = Color.white;
            prompt.raycastTarget = false;

            CreateNoEvidenceButton(header.transform);

            // Área del corcho.
            var pinAreaGO = new GameObject("PinArea", typeof(RectTransform));
            pinAreaGO.transform.SetParent(panelRoot.transform, false);
            pinArea = pinAreaGO.GetComponent<RectTransform>();
            pinArea.anchorMin = new Vector2(0.05f, 0.22f);
            pinArea.anchorMax = new Vector2(0.95f, 0.85f);
            pinArea.offsetMin = Vector2.zero;
            pinArea.offsetMax = Vector2.zero;

            // Panel de detalle (hover).
            var detailGO = new GameObject("DetailText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            detailGO.transform.SetParent(panelRoot.transform, false);
            var detailRect = detailGO.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.08f, 0.04f);
            detailRect.anchorMax = new Vector2(0.92f, 0.19f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;
            detailText = detailGO.GetComponent<TextMeshProUGUI>();
            if (boardFont != null) detailText.font = boardFont;
            detailText.fontSize = 20f;
            detailText.alignment = TextAlignmentOptions.TopLeft;
            detailText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            detailText.raycastTarget = false;

            panelRoot.SetActive(false);
        }

        private void CreateNoEvidenceButton(Transform parent)
        {
            var btnGO = new GameObject("NoEvidenceButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.72f, 0.15f);
            btnRect.anchorMax = new Vector2(0.98f, 0.85f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            btnGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(btnGO.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);
            var label = labelGO.GetComponent<TextMeshProUGUI>();
            if (boardFont != null) label.font = boardFont;
            label.text = "Accuse without evidence";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            btnGO.GetComponent<Button>().onClick.AddListener(() => Confirm(null));
        }
    }

    // Componente por-pin: clic izquierdo selecciona, clic derecho tacha/destacha (solo visual).
    // También se puede arrastrar y reacomodar en el corcho (solo estético, no afecta la
    // selección) — clickear sin mover sigue seleccionando la pista.
    public class EvidencePinView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private string clueId;
        private GameObject discardMark;
        private bool discarded;

        private Action<string> onSelect;
        private Action<string, string> onHover;
        private Action onHoverExit;

        private RectTransform rectTransform;
        private RectTransform boardArea;
        private bool wasDragged;

        public void Init(string clueId, string label, string description, GameObject discardMark,
            Action<string> onSelect, Action<string, string> onHover, Action onHoverExit)
        {
            this.clueId = clueId;
            this.discardMark = discardMark;
            this.onSelect = onSelect;
            this.onHover = onHover;
            this.onHoverExit = onHoverExit;
            this.label = label;
            this.description = description;
        }

        private string label;
        private string description;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            boardArea = transform.parent as RectTransform;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            wasDragged = false;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (boardArea == null || rectTransform == null) return;

            wasDragged = true;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(boardArea, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                rectTransform.anchoredPosition = ClampToBoard(localPoint);
            }
        }

        public void OnEndDrag(PointerEventData eventData) { }

        private Vector2 ClampToBoard(Vector2 position)
        {
            var boardSize = boardArea.rect.size;
            var size = rectTransform.sizeDelta;
            float halfW = Mathf.Max(0f, boardSize.x * 0.5f - size.x * 0.5f);
            float halfH = Mathf.Max(0f, boardSize.y * 0.5f - size.y * 0.5f);
            position.x = Mathf.Clamp(position.x, -halfW, halfW);
            position.y = Mathf.Clamp(position.y, -halfH, halfH);
            return position;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (wasDragged)
            {
                wasDragged = false;
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                onSelect?.Invoke(clueId);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                discarded = !discarded;
                if (discardMark != null) discardMark.SetActive(discarded);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => onHover?.Invoke(label, description);

        public void OnPointerExit(PointerEventData eventData) => onHoverExit?.Invoke();
    }
}
