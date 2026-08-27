using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VisualNovelSystem;

namespace Investigation
{
    // Tira de pistas recolectadas. Permite arrastrar una pista sobre el hotspot de un personaje
    // para confrontarlo. Se oculta automáticamente si no hay pistas recolectadas.
    public class EvidencePanelController : MonoBehaviour
    {
        public static EvidencePanelController Instance { get; private set; }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform container;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CaseState.OnClueCollected += Refresh;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                CaseState.OnClueCollected -= Refresh;
            }
        }

        private void Start()
        {
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            var target = panelRoot != null ? panelRoot : (container != null && container.parent != null ? container.parent.gameObject : null);
            if (target != null)
            {
                target.SetActive(visible && CaseState.Instance.CollectedClues.Count > 0);
            }
        }

        public void Refresh()
        {
            int clueCount = CaseState.Instance != null ? CaseState.Instance.CollectedClues.Count : 0;
            var target = panelRoot != null ? panelRoot : (container != null && container.parent != null ? container.parent.gameObject : null);

            if (target != null)
            {
                target.SetActive(clueCount > 0);
            }

            if (container == null || clueCount == 0) return;

            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            foreach (var clueId in CaseState.Instance.CollectedClues)
            {
                var clue = DialogueDatabase.Instance.GetClue(clueId);
                CreateChip(clueId, clue != null ? clue.displayName : clueId);
            }
        }

        private void CreateChip(string clueId, string label)
        {
            var go = new GameObject("Clue_" + clueId, typeof(RectTransform), typeof(CanvasGroup), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(container, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 56f);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 160f;
            layoutElement.preferredHeight = 56f;

            go.GetComponent<Image>().color = new Color(0.5f, 0.42f, 0.1f, 0.95f);

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6f, 4f);
            labelRect.offsetMax = new Vector2(-6f, -4f);
            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var draggable = go.AddComponent<StoryDraggable>();
            draggable.SetItem(clueId, label);
        }
    }
}
