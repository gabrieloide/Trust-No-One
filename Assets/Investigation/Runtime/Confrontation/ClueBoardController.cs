using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VisualNovelSystem;

namespace Investigation
{
    // Pizarra de pistas: solo lectura. Cada pista recolectada aparece clavada en una
    // posición pseudo-aleatoria pero determinista (según su id). Confrontar a un NPC con
    // una pista se hace desde el menú de diálogo (ConversationController), no desde acá.
    public class ClueBoardController : MonoBehaviour
    {
        public static ClueBoardController Instance { get; private set; }

        [SerializeField] private GameObject boardPanel;
        [SerializeField] private RectTransform pinArea;
        [SerializeField] private TextMeshProUGUI detailText;

        private readonly Dictionary<string, RectTransform> pinnedCards = new Dictionary<string, RectTransform>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CaseState.OnClueCollected += Refresh;
            StoryInteractable.OnOpenClueBoardRequested += Toggle;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                CaseState.OnClueCollected -= Refresh;
                StoryInteractable.OnOpenClueBoardRequested -= Toggle;
            }
        }

        private void Start()
        {
            Refresh();
            if (boardPanel != null) boardPanel.SetActive(false);
        }

        public void Toggle()
        {
            if (boardPanel == null) return;
            boardPanel.SetActive(!boardPanel.activeSelf);
        }

        public void Close()
        {
            if (boardPanel != null) boardPanel.SetActive(false);
        }

        public void Refresh()
        {
            if (pinArea == null || CaseState.Instance == null || DialogueDatabase.Instance == null) return;

            foreach (var clueId in CaseState.Instance.CollectedClues)
            {
                if (pinnedCards.ContainsKey(clueId)) continue;
                var clue = DialogueDatabase.Instance.GetClue(clueId);
                CreatePin(clueId, clue != null ? clue.displayName : clueId, clue != null ? clue.description : "");
            }
        }

        private void CreatePin(string clueId, string label, string description)
        {
            var go = new GameObject("Pin_" + clueId, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pinArea, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(150f, 50f);
            rect.anchoredPosition = FindFreeSpot(rect.sizeDelta, clueId);

            go.GetComponent<Image>().color = new Color(0.55f, 0.45f, 0.15f, 0.95f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 4f);
            labelRect.offsetMax = new Vector2(-6f, -4f);
            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => ShowDetail(label, description));

            pinnedCards[clueId] = rect;
        }

        private void ShowDetail(string label, string description)
        {
            if (detailText != null) detailText.text = $"{label}\n\n{description}";
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
                foreach (var existing in pinnedCards.Values)
                {
                    if (Vector2.Distance(existing.anchoredPosition, candidate) < size.x * 0.6f)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps) return candidate;
            }

            return Vector2.zero;
        }

        // Hash estable entre sesiones (a diferencia de string.GetHashCode, que puede
        // variar entre corridas del proceso), para que cada pista siempre caiga en la
        // misma posición de la pizarra.
        private static int DeterministicHash(string s)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in s) hash = hash * 31 + c;
                return hash;
            }
        }
    }
}
