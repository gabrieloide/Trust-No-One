using UnityEngine;
using UnityEngine.EventSystems;
using VisualNovelSystem;

namespace Investigation
{
    // Se coloca sobre el hotspot UI de un personaje. Acepta CUALQUIER StoryDraggable
    // (una pista) y delega la reacción a ConversationController/DialogueVariantData en
    // vez de tener una única reacción de éxito/fallo como StoryDropZone.
    public class ClueConfrontTarget : MonoBehaviour, IDropHandler
    {
        [SerializeField] private string characterId = "";

        public void OnDrop(PointerEventData eventData)
        {
            var draggable = StoryDraggable.CurrentlyDraggedItem;
            if (draggable == null && eventData.pointerDrag != null)
            {
                draggable = eventData.pointerDrag.GetComponent<StoryDraggable>();
            }

            if (draggable == null || string.IsNullOrEmpty(characterId)) return;

            draggable.NotifyDropSuccess(false);
            ConversationController.Instance.Confront(characterId, draggable.ItemId);
        }
    }
}
