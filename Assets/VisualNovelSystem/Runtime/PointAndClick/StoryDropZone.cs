using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualNovelSystem
{
    public class StoryDropZone : MonoBehaviour, IDropHandler
    {
        [Header("Item Requirements")]
        [SerializeField] private string acceptedItemId = "Key_01";
        [SerializeField] private bool consumeItemOnSuccess = true;

        [Header("Success Actions")]
        [SerializeField] private InteractType successActionType = InteractType.QuickDialogue;
        [SerializeField] private string successSpeaker = "Protagonista";
        [TextArea(2, 3)]
        [SerializeField] private string successDialogue = "¡Encaja perfectamente y se abre!";
        [SerializeField] private StoryGraph successStoryGraph;
        [SerializeField] private string successSceneEvent = "";
        [SerializeField] private string setVariableOnSuccess = "";
        [SerializeField] private string setVariableValue = "true";

        [Header("Fail Feedback")]
        [SerializeField] private bool showFailDialogue = true;
        [SerializeField] private string failSpeaker = "Protagonista";
        [TextArea(2, 3)]
        [SerializeField] private string failDialogue = "No creo que esto sirva aquí.";

        public void OnDrop(PointerEventData eventData)
        {
            var draggable = StoryDraggable.CurrentlyDraggedItem;
            if (draggable == null && eventData.pointerDrag != null)
            {
                draggable = eventData.pointerDrag.GetComponent<StoryDraggable>();
            }

            if (draggable != null)
            {
                HandleDrop(draggable);
            }
        }

        public void HandleDrop(StoryDraggable item)
        {
            if (item == null) return;

            if (string.Equals(item.ItemId, acceptedItemId, StringComparison.OrdinalIgnoreCase))
            {
                // SUCCESS
                item.NotifyDropSuccess(consumeItemOnSuccess);

                if (!string.IsNullOrEmpty(setVariableOnSuccess))
                {
                    var runner = StoryRunner.ActiveRunner;
                    if (runner != null && runner.Blackboard != null)
                    {
                        runner.Blackboard.SetString(setVariableOnSuccess, setVariableValue);
                    }
                }

                switch (successActionType)
                {
                    case InteractType.QuickDialogue:
                        if (StoryUIController.Instance != null)
                        {
                            StartCoroutine(StoryUIController.Instance.ShowDialogue(successSpeaker, successDialogue, null, null, -1f, true));
                        }
                        break;

                    case InteractType.TriggerStoryGraph:
                        if (successStoryGraph != null && StoryRunner.ActiveRunner != null)
                        {
                            StoryRunner.ActiveRunner.StartStory(successStoryGraph);
                        }
                        break;

                    case InteractType.ResumeExplorationMode:
                        if (StoryRunner.ActiveRunner != null)
                        {
                            StoryRunner.ActiveRunner.CompleteExploration();
                        }
                        break;

                    case InteractType.TriggerSceneEvent:
                        if (!string.IsNullOrEmpty(successSceneEvent) && StorySceneEvents.Instance != null)
                        {
                            StorySceneEvents.Instance.TriggerEvent(successSceneEvent);
                        }
                        break;
                }
            }
            else
            {
                // FAIL
                if (showFailDialogue && StoryUIController.Instance != null)
                {
                    StartCoroutine(StoryUIController.Instance.ShowDialogue(failSpeaker, failDialogue, null, null, -1f, true));
                }
            }
        }
    }
}
