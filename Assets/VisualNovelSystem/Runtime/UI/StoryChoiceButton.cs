using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    public class StoryChoiceButton : MonoBehaviour, IPointerEnterHandler
    {
        public static event Action OnAnyChoiceClicked;
        public static event Action OnAnyChoiceHovered;

        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI buttonText;

        public void Setup(string text, Action onClickCallback)
        {
            if (button == null) button = GetComponent<Button>();
            if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null) buttonText.text = text;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    OnAnyChoiceClicked?.Invoke();
                    onClickCallback?.Invoke();
                });
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnAnyChoiceHovered?.Invoke();
        }
    }
}
