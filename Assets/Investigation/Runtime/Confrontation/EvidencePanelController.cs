using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VisualNovelSystem;

namespace Investigation
{
    // Tira de pistas recolectadas, siempre visible (no vive dentro de un panel de
    // locación) para poder arrastrar una pista sobre el hotspot de un personaje en
    // cualquier locación y disparar ConversationController.Confront a través de
    // ClueConfrontTarget. Se repuebla cada vez que CaseState.OnClueCollected dispara.
    public class EvidencePanelController : MonoBehaviour
    {
        public static EvidencePanelController Instance { get; private set; }

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

        public void Refresh()
        {
            if (container == null) return;

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
