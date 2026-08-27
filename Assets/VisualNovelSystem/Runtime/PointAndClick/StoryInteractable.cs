using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    public enum InteractType
    {
        QuickDialogue,
        TriggerStoryGraph,
        ResumeExplorationMode,
        TriggerSceneEvent,
        OpenConversation,
        InvestigateSpot,
        GoToLocation,
        OpenAccusation
    }

    public class StoryInteractable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // Desacoplados a propósito: VisualNovelSystem no referencia el código del juego
        // (Investigation). Los listeners (ConversationController/LocationController) se
        // suscriben desde afuera.
        public static event Action<string> OnOpenConversationRequested;
        public static event Action<string> OnInvestigateRequested;
        public static event Action<string> OnGoToLocationRequested;
        public static event Action OnOpenAccusationRequested;

        [Header("Interaction Settings")]
        [SerializeField] private InteractType interactType = InteractType.QuickDialogue;
        [SerializeField] private CursorIconType cursorOnHover = CursorIconType.Inspect;
        [SerializeField] private bool interactable = true;

        [Header("Quick Dialogue Settings")]
        [SerializeField] private string speakerName = "Protagonista";
        [TextArea(2, 4)]
        [SerializeField] private string dialogueText = "Parece un objeto interesante.";
        [SerializeField] private Sprite characterPortrait;

        [Header("Story Graph Trigger")]
        [SerializeField] private StoryGraph targetStoryGraph;

        [Header("Open Conversation Trigger")]
        [SerializeField] private string conversationCharacterId = "";

        [Header("Investigate Spot Trigger")]
        [SerializeField] private string investigateSpotId = "";

        [Header("Go To Location Trigger")]
        [SerializeField] private string targetLocationId = "";

        [Header("Scene Event Trigger")]
        [SerializeField] private string sceneEventName = "";

        [Header("Blackboard Conditions & Changes")]
        [SerializeField] private string conditionVariableName = "";
        [SerializeField] private string conditionRequiredValue = "true";
        [SerializeField] private string setVariableOnInteract = "";
        [SerializeField] private string setVariableValue = "true";
        [SerializeField] private bool disableAfterInteraction = false;

        [Header("Visual Feedback (Optional)")]
        [SerializeField] private Graphic uiGraphic;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hoverColor = new Color(1.2f, 1.2f, 1.2f, 1f);

        private Color originalColor;
        private bool isHovered = false;

        public bool IsInteractable
        {
            get => interactable && CheckCondition();
            set => interactable = value;
        }

        private void Awake()
        {
            if (uiGraphic == null) uiGraphic = GetComponent<Graphic>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (uiGraphic != null) originalColor = uiGraphic.color;
            else if (spriteRenderer != null) originalColor = spriteRenderer.color;
        }

        private bool CheckCondition()
        {
            if (string.IsNullOrEmpty(conditionVariableName)) return true;

            var runner = StoryRunner.ActiveRunner;
            if (runner != null && runner.Blackboard != null)
            {
                return runner.Blackboard.EvaluateCondition(conditionVariableName, ConditionComparison.Equal, conditionRequiredValue);
            }
            return true;
        }

        public void Interact()
        {
            if (!IsInteractable) return;

            // Apply Blackboard change
            if (!string.IsNullOrEmpty(setVariableOnInteract))
            {
                var runner = StoryRunner.ActiveRunner;
                if (runner != null && runner.Blackboard != null)
                {
                    runner.Blackboard.SetString(setVariableOnInteract, setVariableValue);
                }
            }

            switch (interactType)
            {
                case InteractType.QuickDialogue:
                    if (StoryUIController.Instance != null)
                    {
                        StartCoroutine(StoryUIController.Instance.ShowDialogue(speakerName, dialogueText, characterPortrait, null, -1f, true));
                    }
                    else
                    {
                        Debug.Log($"[{speakerName}] {dialogueText}");
                    }
                    break;

                case InteractType.TriggerStoryGraph:
                    if (targetStoryGraph != null)
                    {
                        var runner = StoryRunner.ActiveRunner;
                        if (runner != null)
                        {
                            runner.StartStory(targetStoryGraph);
                        }
                    }
                    break;

                case InteractType.ResumeExplorationMode:
                    var activeRunner = StoryRunner.ActiveRunner;
                    if (activeRunner != null)
                    {
                        activeRunner.CompleteExploration();
                    }
                    break;

                case InteractType.TriggerSceneEvent:
                    if (!string.IsNullOrEmpty(sceneEventName) && StorySceneEvents.Instance != null)
                    {
                        StorySceneEvents.Instance.TriggerEvent(sceneEventName);
                    }
                    break;

                case InteractType.OpenConversation:
                    if (!string.IsNullOrEmpty(conversationCharacterId))
                    {
                        OnOpenConversationRequested?.Invoke(conversationCharacterId);
                    }
                    break;

                case InteractType.InvestigateSpot:
                    if (!string.IsNullOrEmpty(investigateSpotId))
                    {
                        OnInvestigateRequested?.Invoke(investigateSpotId);
                    }
                    break;

                case InteractType.GoToLocation:
                    if (!string.IsNullOrEmpty(targetLocationId))
                    {
                        OnGoToLocationRequested?.Invoke(targetLocationId);
                    }
                    break;

                case InteractType.OpenAccusation:
                    OnOpenAccusationRequested?.Invoke();
                    break;
            }

            if (disableAfterInteraction)
            {
                IsInteractable = false;
                OnPointerExit(null);
            }
        }

        #region UI Pointer Events
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Interact();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable) return;
            isHovered = true;

            if (StoryCursorManager.Instance != null)
            {
                StoryCursorManager.Instance.SetCursor(cursorOnHover);
            }

            ApplyHoverVisual(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isHovered) return;
            isHovered = false;

            if (StoryCursorManager.Instance != null)
            {
                StoryCursorManager.Instance.ResetCursor();
            }

            ApplyHoverVisual(false);
        }
        #endregion

        #region 2D/3D Mouse Events (Physics)
        private void OnMouseDown()
        {
            // Avoid conflict if clicking UI element over 2D object
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Interact();
        }

        private void OnMouseEnter()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            OnPointerEnter(null);
        }

        private void OnMouseExit()
        {
            OnPointerExit(null);
        }
        #endregion

        private void ApplyHoverVisual(bool hover)
        {
            Color target = hover ? hoverColor : originalColor;
            if (uiGraphic != null) uiGraphic.color = target;
            if (spriteRenderer != null) spriteRenderer.color = target;
        }

        private void OnDisable()
        {
            if (isHovered && StoryCursorManager.Instance != null)
            {
                StoryCursorManager.Instance.ResetCursor();
            }
        }
    }
}
