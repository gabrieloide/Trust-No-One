using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    public enum PortraitEmotion
    {
        None,
        Shake,    // Frenético / Enojado / Sacudida
        Bounce,   // Salto / Sorpresa / Énfasis
        Punch,    // Impacto / Acusación
        Tremble,  // Miedo / Tensión / Ansiedad
        Nod       // Asentimiento / Calma
    }

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
        [SerializeField] private AudioClip typingAudioClip;
        [SerializeField] [Range(0f, 1f)] private float typingAudioVolume = 0.35f;

        [Header("Settings")]
        [SerializeField] private float defaultTypewriterSpeed = 0.03f;

        private Coroutine activeDialogueRoutine;
        private Coroutine activePortraitRoutine;
        private bool isTyping = false;
        private bool skipRequested = false;

        private Vector2 originalPortraitPos;
        private Vector3 originalPortraitScale;
        private Quaternion originalPortraitRot;
        private RectTransform portraitRect;

        public bool IsTyping => isTyping;
        public bool IsActive => canvasGroup != null && canvasGroup.blocksRaycasts && canvasGroup.alpha > 0f;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (characterPortrait != null)
            {
                portraitRect = characterPortrait.rectTransform;
                originalPortraitPos = portraitRect.anchoredPosition;
                originalPortraitScale = portraitRect.localScale;
                originalPortraitRot = portraitRect.localRotation;
            }

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

            ResetPortraitTransform();

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

            // Detectar etiquetas emocionales explícitas ([shake], [bounce], [punch], [tremble], [nod]) o automáticas por puntuación
            PortraitEmotion emotion = DetectEmotion(ref text);

            if (characterPortrait != null)
            {
                if (portrait != null)
                {
                    characterPortrait.sprite = portrait;
                    characterPortrait.color = Color.white;
                    characterPortrait.gameObject.SetActive(true);
                }
                else if (!string.IsNullOrEmpty(speakerName) && speakerName != "Gabe" && speakerName != "Narrador")
                {
                    // Placeholder visual estilizado con tinte para que la animación sea visible antes de importar arte final
                    characterPortrait.color = GetSpeakerPlaceholderColor(speakerName);
                    characterPortrait.gameObject.SetActive(true);
                }
                else
                {
                    characterPortrait.gameObject.SetActive(false);
                }

                if (characterPortrait.gameObject.activeSelf && emotion != PortraitEmotion.None)
                {
                    PlayPortraitEmotion(emotion);
                }
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

                    // Reproducir teletipo al tipear caracteres (evitando espacios)
                    if (i > 0 && i < text.Length && text[i - 1] != ' ' && voiceAudioSource != null && typingAudioClip != null)
                    {
                        voiceAudioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                        voiceAudioSource.PlayOneShot(typingAudioClip, typingAudioVolume);
                    }

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

        private PortraitEmotion DetectEmotion(ref string text)
        {
            if (string.IsNullOrEmpty(text)) return PortraitEmotion.None;

            // Etiquetas explícitas
            if (text.StartsWith("[shake]", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(7).TrimStart();
                return PortraitEmotion.Shake;
            }
            if (text.StartsWith("[bounce]", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(8).TrimStart();
                return PortraitEmotion.Bounce;
            }
            if (text.StartsWith("[punch]", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(7).TrimStart();
                return PortraitEmotion.Punch;
            }
            if (text.StartsWith("[tremble]", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(9).TrimStart();
                return PortraitEmotion.Tremble;
            }
            if (text.StartsWith("[nod]", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(5).TrimStart();
                return PortraitEmotion.Nod;
            }

            // Detección automática por puntuación y palabras clave emocionales
            if (text.Contains("¡") || text.Contains("!") || text.Contains("¿¡") || text.Contains("!?"))
            {
                if (text.Contains("¡No!") || text.Contains("¡Cállese!") || text.Contains("¡Medio pueblo") || text.Contains("¡Tenía terror!") || text.Contains("despavorida"))
                {
                    return PortraitEmotion.Shake; // Reacción frenética
                }
                return PortraitEmotion.Bounce; // Salto / Exaltación
            }

            if (text.Contains("tiemblan") || text.Contains("miedo") || text.Contains("nervios") || text.Contains("terror"))
            {
                return PortraitEmotion.Tremble;
            }

            return PortraitEmotion.None;
        }

        public void PlayPortraitEmotion(PortraitEmotion emotion)
        {
            if (portraitRect == null) return;

            if (activePortraitRoutine != null)
            {
                StopCoroutine(activePortraitRoutine);
            }

            ResetPortraitTransform();

            switch (emotion)
            {
                case PortraitEmotion.Shake:
                    activePortraitRoutine = StartCoroutine(ShakePortraitRoutine());
                    break;
                case PortraitEmotion.Bounce:
                    activePortraitRoutine = StartCoroutine(BouncePortraitRoutine());
                    break;
                case PortraitEmotion.Punch:
                    activePortraitRoutine = StartCoroutine(PunchPortraitRoutine());
                    break;
                case PortraitEmotion.Tremble:
                    activePortraitRoutine = StartCoroutine(TremblePortraitRoutine());
                    break;
                case PortraitEmotion.Nod:
                    activePortraitRoutine = StartCoroutine(NodPortraitRoutine());
                    break;
            }
        }

        private IEnumerator ShakePortraitRoutine()
        {
            float duration = 0.45f;
            float elapsed = 0f;
            float intensity = 14f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / duration);
                float offsetX = Mathf.Sin(elapsed * 45f) * intensity * damp;
                float offsetY = Mathf.Cos(elapsed * 35f) * (intensity * 0.4f) * damp;
                float rotZ = Mathf.Sin(elapsed * 40f) * 3.5f * damp;

                portraitRect.anchoredPosition = originalPortraitPos + new Vector2(offsetX, offsetY);
                portraitRect.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                yield return null;
            }

            ResetPortraitTransform();
            activePortraitRoutine = null;
        }

        private IEnumerator BouncePortraitRoutine()
        {
            float duration = 0.35f;
            float elapsed = 0f;
            float jumpHeight = 26f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

                // Squash & Stretch
                float scaleY = 1f + Mathf.Sin(t * Mathf.PI) * 0.16f;
                float scaleX = 1f - Mathf.Sin(t * Mathf.PI) * 0.12f;

                portraitRect.anchoredPosition = originalPortraitPos + new Vector2(0f, height);
                portraitRect.localScale = new Vector3(originalPortraitScale.x * scaleX, originalPortraitScale.y * scaleY, originalPortraitScale.z);
                yield return null;
            }

            ResetPortraitTransform();
            activePortraitRoutine = null;
        }

        private IEnumerator PunchPortraitRoutine()
        {
            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scaleBoost = Mathf.Sin(t * Mathf.PI) * 0.25f;

                portraitRect.localScale = originalPortraitScale * (1f + scaleBoost);
                yield return null;
            }

            ResetPortraitTransform();
            activePortraitRoutine = null;
        }

        private IEnumerator TremblePortraitRoutine()
        {
            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offsetX = (UnityEngine.Random.value - 0.5f) * 6f;
                float offsetY = (UnityEngine.Random.value - 0.5f) * 4f;

                portraitRect.anchoredPosition = originalPortraitPos + new Vector2(offsetX, offsetY);
                yield return null;
            }

            ResetPortraitTransform();
            activePortraitRoutine = null;
        }

        private IEnumerator NodPortraitRoutine()
        {
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float offsetY = -Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 10f;

                portraitRect.anchoredPosition = originalPortraitPos + new Vector2(0f, offsetY);
                yield return null;
            }

            ResetPortraitTransform();
            activePortraitRoutine = null;
        }

        private void ResetPortraitTransform()
        {
            if (portraitRect != null)
            {
                portraitRect.anchoredPosition = originalPortraitPos;
                portraitRect.localScale = originalPortraitScale;
                portraitRect.localRotation = originalPortraitRot;
            }
        }

        private Color GetSpeakerPlaceholderColor(string speakerName)
        {
            switch (speakerName)
            {
                case "Elena": return new Color(0.85f, 0.45f, 0.45f, 1f);
                case "Ernesto": return new Color(0.75f, 0.55f, 0.35f, 1f);
                case "Robert": return new Color(0.45f, 0.65f, 0.75f, 1f);
                case "Mark": return new Color(0.65f, 0.45f, 0.75f, 1f);
                case "Frank": return new Color(0.75f, 0.75f, 0.45f, 1f);
                case "Gus": return new Color(0.45f, 0.75f, 0.55f, 1f);
                case "Marta": return new Color(0.85f, 0.65f, 0.55f, 1f);
                default: return new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }
    }
}
