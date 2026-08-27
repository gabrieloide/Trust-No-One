using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class StoryDialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image characterPortrait;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private AudioSource voiceAudioSource;

        [Header("Settings")]
        [SerializeField] private float defaultTypewriterSpeed = 0.03f;

        private Coroutine activeDialogueRoutine;
        private bool isTyping = false;
        private bool skipRequested = false;

        public bool IsTyping => isTyping;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            HideDialogue();
        }

        private void Update()
        {
            if (StoryInput.ContinuePressed())
            {
                skipRequested = true;
            }
        }

        public void HideDialogue()
        {
            if (activeDialogueRoutine != null)
            {
                StopCoroutine(activeDialogueRoutine);
                activeDialogueRoutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (continueIndicator != null) continueIndicator.SetActive(false);
            if (dialogueText != null) dialogueText.text = "";
            if (speakerNameText != null)
            {
                speakerNameText.text = "";
                speakerNameText.gameObject.SetActive(false);
            }
            if (characterPortrait != null) characterPortrait.gameObject.SetActive(false);
        }

        public IEnumerator ShowDialogueRoutine(string speakerName, string text, Sprite portrait, AudioClip voiceClip, float typewriterSpeed = -1f, bool waitForClick = true)
        {
            skipRequested = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
                speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
            }

            if (characterPortrait != null)
            {
                characterPortrait.sprite = portrait;
                characterPortrait.gameObject.SetActive(portrait != null);
            }

            if (continueIndicator != null) continueIndicator.SetActive(false);

            if (voiceClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.PlayOneShot(voiceClip);
            }

            float speed = typewriterSpeed > 0f ? typewriterSpeed : defaultTypewriterSpeed;

            if (dialogueText != null)
            {
                dialogueText.text = "";
                isTyping = true;

                for (int i = 0; i <= text.Length; i++)
                {
                    if (skipRequested)
                    {
                        dialogueText.text = text;
                        break;
                    }

                    dialogueText.text = text.Substring(0, i);
                    yield return new WaitForSeconds(speed);
                }

                dialogueText.text = text;
                isTyping = false;
            }

            // Small delay so the skip click doesn't instantly advance the dialogue
            yield return new WaitForSeconds(0.1f);
            skipRequested = false;

            if (continueIndicator != null) continueIndicator.SetActive(true);

            if (waitForClick)
            {
                while (!skipRequested)
                {
                    yield return null;
                }
            }

            if (continueIndicator != null) continueIndicator.SetActive(false);
            activeDialogueRoutine = null;
        }
    }
}
