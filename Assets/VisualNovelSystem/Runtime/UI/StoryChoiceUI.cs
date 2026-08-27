using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VisualNovelSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class StoryChoiceUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private Transform buttonsContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        private int selectedIndex = -1;
        private bool choiceMade = false;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            HideChoices();
        }

        public void HideChoices()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            // Clear old buttons
            if (buttonsContainer != null)
            {
                foreach (Transform child in buttonsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public IEnumerator ShowChoicesRoutine(string prompt, List<StoryChoiceOption> options, Action<int> onSelected)
        {
            choiceMade = false;
            selectedIndex = -1;

            if (StoryUIController.Instance != null)
            {
                StoryUIController.Instance.HideDialogue();
            }

            if (promptText != null)
            {
                promptText.text = prompt;
                promptText.gameObject.SetActive(!string.IsNullOrEmpty(prompt));
            }

            // Clear existing buttons
            if (buttonsContainer != null)
            {
                foreach (Transform child in buttonsContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Create buttons
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                GameObject btnObj = null;

                if (choiceButtonPrefab != null)
                {
                    btnObj = Instantiate(choiceButtonPrefab, buttonsContainer);
                }
                else
                {
                    // Fallback create default button
                    btnObj = new GameObject($"ChoiceBtn_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement), typeof(StoryChoiceButton));
                    btnObj.transform.SetParent(buttonsContainer, false);

                    var btnRect = btnObj.GetComponent<RectTransform>();
                    btnRect.sizeDelta = new Vector2(0f, 48f);
                    btnObj.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 48f;
                    btnObj.GetComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

                    var labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    labelObj.transform.SetParent(btnObj.transform, false);
                    var labelRect = labelObj.GetComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(12f, 4f);
                    labelRect.offsetMax = new Vector2(-12f, -4f);
                    var label = labelObj.GetComponent<TextMeshProUGUI>();
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.color = Color.white;
                    label.fontSize = 24f;
                }

                var choiceBtn = btnObj.GetComponent<StoryChoiceButton>();
                if (choiceBtn == null) choiceBtn = btnObj.AddComponent<StoryChoiceButton>();

                choiceBtn.Setup(options[i].text, () =>
                {
                    selectedIndex = index;
                    choiceMade = true;
                });
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            while (!choiceMade)
            {
                yield return null;
            }

            onSelected?.Invoke(selectedIndex);
            HideChoices();
        }
    }
}
