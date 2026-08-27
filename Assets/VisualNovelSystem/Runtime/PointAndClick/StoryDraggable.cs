using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class StoryDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static StoryDraggable CurrentlyDraggedItem { get; private set; }

        [Header("Item Identification")]
        [SerializeField] private string itemId = "Key_01";
        [SerializeField] private string itemName = "Llave Antigua";

        [Header("Drag Settings")]
        [SerializeField] private bool returnToOriginalPosition = true;
        [SerializeField] private float dragAlpha = 0.6f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        private Vector3 originalPosition;
        private Transform originalParent;
        private bool isDroppedOnValidZone = false;

        public string ItemId => itemId;
        public string ItemName => itemName;

        // Para ítems creados dinámicamente en runtime (ej. un panel de inventario que
        // instancia un StoryDraggable por pista recolectada).
        public void SetItem(string id, string displayName)
        {
            itemId = id;
            itemName = displayName;
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            CurrentlyDraggedItem = this;
            isDroppedOnValidZone = false;
            originalPosition = rectTransform != null ? rectTransform.position : transform.position;
            originalParent = transform.parent;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = dragAlpha;
                canvasGroup.blocksRaycasts = false; // Allows drop zone below to receive drop event!
            }

            // Move to top of hierarchy so it's not hidden behind other elements
            if (parentCanvas != null)
            {
                transform.SetParent(parentCanvas.transform, true);
                transform.SetAsLastSibling();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                transform.position = eventData.position;
            }
            else if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPos))
            {
                transform.position = worldPos;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CurrentlyDraggedItem = null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (!isDroppedOnValidZone && returnToOriginalPosition)
            {
                transform.SetParent(originalParent, true);
                if (rectTransform != null) rectTransform.position = originalPosition;
                else transform.position = originalPosition;
            }
        }

        public void NotifyDropSuccess(bool consumeItem)
        {
            isDroppedOnValidZone = true;
            if (consumeItem)
            {
                gameObject.SetActive(false);
            }
        }

        #region 2D World Fallback Dragging
        private Vector3 screenPoint;
        private Vector3 offset;
        private bool is2DDragging = false;

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (rectTransform != null) return; // UI handles it via interfaces

            CurrentlyDraggedItem = this;
            is2DDragging = true;
            isDroppedOnValidZone = false;
            originalPosition = transform.position;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 mousePos = StoryInput.MousePosition();
                screenPoint = cam.WorldToScreenPoint(gameObject.transform.position);
                offset = gameObject.transform.position - cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, screenPoint.z));
            }
        }

        private void OnMouseDrag()
        {
            if (!is2DDragging) return;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 mousePos = StoryInput.MousePosition();
                Vector3 curScreenPoint = new Vector3(mousePos.x, mousePos.y, screenPoint.z);
                Vector3 curPosition = cam.ScreenToWorldPoint(curScreenPoint) + offset;
                transform.position = curPosition;
            }
        }

        private void OnMouseUp()
        {
            if (!is2DDragging) return;
            is2DDragging = false;
            CurrentlyDraggedItem = null;

            // Check 2D drop zone using Raycast2D
            Camera cam = Camera.main;
            if (cam != null)
            {
                RaycastHit2D hit = Physics2D.Raycast(cam.ScreenToWorldPoint(StoryInput.MousePosition()), Vector2.zero);
                if (hit.collider != null)
                {
                    var dropZone = hit.collider.GetComponent<StoryDropZone>();
                    if (dropZone != null)
                    {
                        dropZone.HandleDrop(this);
                        return;
                    }
                }
            }

            if (!isDroppedOnValidZone && returnToOriginalPosition)
            {
                transform.position = originalPosition;
            }
        }
        #endregion
    }
}
