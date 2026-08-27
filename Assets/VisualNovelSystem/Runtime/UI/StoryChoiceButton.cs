using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    public class StoryChoiceButton : MonoBehaviour
    {
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
                button.onClick.AddListener(() => onClickCallback?.Invoke());
            }
        }
    }
}
