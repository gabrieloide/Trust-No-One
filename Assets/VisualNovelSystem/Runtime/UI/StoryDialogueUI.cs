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
        Shake,    // Sorpresa / Impacto / Shock
        Bounce,   // Alegría / Éxito / Entusiasmo
        Punch,    // Enojo / Determinación / Énfasis
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
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioClip typingAudioClip;
        [SerializeField] [Range(0f, 1f)] private float typingAudioVolume = 0.35f;

        // Paneles construidos por código (evidence board, ending credits) no tienen forma de
        // arrastrar una referencia de fuente en el Inspector; toman la misma que usa el diálogo
        // en vez de caer en el default de TMP Settings (LiberationSans), que no matchea el juego.
        public TMP_FontAsset BodyFont => dialogueText != null ? dialogueText.font : null;

        [Header("Character Stage References (Dual Stage)")]
        [SerializeField] private Image leftCharacterPortrait;   // Gabe / Protagonista (Izquierda)
        [SerializeField] private Image rightCharacterPortrait;  // Interlocutor / NPC (Derecha)
        [SerializeField] private Image characterPortrait;       // Fallback / Centro

        [Header("Settings")]
        [SerializeField] private float defaultTypewriterSpeed = 0.03f;
        [SerializeField] private float inactiveScale = 0.92f;
        [SerializeField] private Color inactiveDimColor = new Color(0.42f, 0.42f, 0.48f, 1f);

        private Coroutine activeDialogueRoutine;
        private Coroutine activePortraitRoutine;
        private Coroutine activeFocusRoutine;
        private Coroutine talkBobRoutine;
        private Coroutine idleBreathRoutine;
        private const float talkBobAmp = 3.5f;
        private const float talkBobDur = 0.11f;
        private bool isTyping = false;
        private bool skipRequested = false;

        private Vector2 originalLeftPos, originalRightPos, originalCenterPos;
        private Vector3 originalLeftScale, originalRightScale, originalCenterScale;
        private Quaternion originalLeftRot, originalRightRot, originalCenterRot;

        private RectTransform leftRect, rightRect, centerRect;
        private string currentInterlocutor = "";
        private Sprite currentInterlocutorSprite = null;
        private Sprite gabeSprite = null;

        public bool IsTyping => isTyping;
        public bool IsActive => canvasGroup != null && canvasGroup.blocksRaycasts && canvasGroup.alpha > 0f;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            InitPortraitTransforms();
            HideDialogue();
        }

        private void InitPortraitTransforms()
        {
            if (leftCharacterPortrait != null)
            {
                leftRect = leftCharacterPortrait.rectTransform;
                originalLeftPos = leftRect.anchoredPosition;
                originalLeftScale = Vector3.one;
                originalLeftRot = leftRect.localRotation;
            }
            if (rightCharacterPortrait != null)
            {
                rightRect = rightCharacterPortrait.rectTransform;
                originalRightPos = rightRect.anchoredPosition;
                originalRightScale = Vector3.one;
                originalRightRot = rightRect.localRotation;
            }
            if (characterPortrait != null)
            {
                centerRect = characterPortrait.rectTransform;
                originalCenterPos = centerRect.anchoredPosition;
                originalCenterScale = Vector3.one;
                originalCenterRot = centerRect.localRotation;
            }
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
            if (activeFocusRoutine != null)
            {
                StopCoroutine(activeFocusRoutine);
                activeFocusRoutine = null;
            }
            if (talkBobRoutine != null)
            {
                StopCoroutine(talkBobRoutine);
                talkBobRoutine = null;
            }
            if (idleBreathRoutine != null)
            {
                StopCoroutine(idleBreathRoutine);
                idleBreathRoutine = null;
            }
            if (activePortraitRoutine != null)
            {
                StopCoroutine(activePortraitRoutine);
                activePortraitRoutine = null;
            }

            ResetAllPortraitTransforms();

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

            if (leftCharacterPortrait != null) leftCharacterPortrait.gameObject.SetActive(false);
            if (rightCharacterPortrait != null) rightCharacterPortrait.gameObject.SetActive(false);
            if (characterPortrait != null) characterPortrait.gameObject.SetActive(false);

            currentInterlocutor = "";
            currentInterlocutorSprite = null;
        }

        public void ClearInterlocutor()
        {
            currentInterlocutor = "";
            currentInterlocutorSprite = null;
            if (rightCharacterPortrait != null) rightCharacterPortrait.gameObject.SetActive(false);
        }

        public void PrepareSpeakerStage(string speakerName, Sprite portrait)
        {
            if (speakerNameText != null)
            {
                speakerNameText.text = speakerName;
                speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
            }

            if (dialogueText != null)
            {
                dialogueText.text = "";
            }

            UpdateSpeakerStage(speakerName, portrait, PortraitEmotion.None);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
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

            // Detectar etiquetas emocionales explícitas ([shake], [bounce], etc.)
            PortraitEmotion emotion = DetectEmotion(ref text);

            // Actualizar el estado de los personajes en escena (Dual Speaker Stage)
            UpdateSpeakerStage(speakerName, portrait, emotion);

            bool isGabeSpeaking = IsProtagonistSpeaker(speakerName);
            bool isNpcSpeaking = !isGabeSpeaking && !IsNarratorSpeaker(speakerName);
            RectTransform activeSpeakerRect = isGabeSpeaking ? leftRect : (isNpcSpeaking ? rightRect : (characterPortrait != null ? centerRect : null));
            Vector2 activeSpeakerOrigin = isGabeSpeaking ? originalLeftPos : (isNpcSpeaking ? originalRightPos : originalCenterPos);
            Vector3 activeSpeakerScale = isGabeSpeaking ? originalLeftScale : (isNpcSpeaking ? originalRightScale : originalCenterScale);

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

                    // Reproducir teletipo al escribir caracteres
                    if (i > 0 && i < text.Length && text[i - 1] != ' ')
                    {
                        if (voiceAudioSource != null && typingAudioClip != null)
                        {
                            voiceAudioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                            voiceAudioSource.PlayOneShot(typingAudioClip, typingAudioVolume);
                        }
                    }

                    yield return new WaitForSeconds(speed);
                }

                dialogueText.text = text;
                isTyping = false;
            }

            if (activeSpeakerRect != null && activePortraitRoutine == null)
            {
                activeSpeakerRect.anchoredPosition = activeSpeakerOrigin;
            }

            yield return new WaitForSeconds(0.08f);
            skipRequested = false;

            if (continueIndicator != null) continueIndicator.SetActive(true);

            if (waitForClick)
            {
                if (activeSpeakerRect != null && activePortraitRoutine == null)
                {
                    if (idleBreathRoutine != null) StopCoroutine(idleBreathRoutine);
                    idleBreathRoutine = StartCoroutine(IdleBreathRoutine(activeSpeakerRect, activeSpeakerScale));
                }

                while (!skipRequested)
                {
                    yield return null;
                }

                if (idleBreathRoutine != null)
                {
                    StopCoroutine(idleBreathRoutine);
                    idleBreathRoutine = null;
                }
                if (activeSpeakerRect != null && activePortraitRoutine == null)
                {
                    activeSpeakerRect.localScale = activeSpeakerScale;
                }
            }

            if (continueIndicator != null) continueIndicator.SetActive(false);
            activeDialogueRoutine = null;
        }

        private bool IsProtagonistSpeaker(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Equals("Gabe", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Protagonista", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Gabriel", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsNarratorSpeaker(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            return name.Equals("Narrador", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Narrator", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateSpeakerStage(string speakerName, Sprite portrait, PortraitEmotion emotion)
        {
            // Si no tenemos soporte de 2 personajes (fallback a 1 solo central)
            if (leftCharacterPortrait == null && rightCharacterPortrait == null)
            {
                if (characterPortrait != null)
                {
                    if (portrait != null)
                    {
                        characterPortrait.sprite = portrait;
                        characterPortrait.color = Color.white;
                        characterPortrait.gameObject.SetActive(true);
                    }
                    else if (!IsNarratorSpeaker(speakerName))
                    {
                        characterPortrait.color = GetSpeakerPlaceholderColor(speakerName);
                        characterPortrait.gameObject.SetActive(true);
                    }
                    else
                    {
                        characterPortrait.gameObject.SetActive(false);
                    }

                    if (characterPortrait.gameObject.activeSelf && emotion != PortraitEmotion.None)
                    {
                        PlayPortraitEmotion(emotion, centerRect, originalCenterPos, originalCenterScale);
                    }
                }
                return;
            }

            bool isGabeSpeaking = IsProtagonistSpeaker(speakerName);
            bool isNarrator = IsNarratorSpeaker(speakerName);
            bool isNpcSpeaking = !isGabeSpeaking && !isNarrator;

            // Sistema Dual Stage (Gabe a la Izquierda, Interlocutor a la Derecha)
            if (isNpcSpeaking)
            {
                currentInterlocutor = speakerName;
                if (portrait != null) currentInterlocutorSprite = portrait;
            }
            else if (isGabeSpeaking && portrait != null)
            {
                gabeSprite = portrait;
            }

            // Configurar Slot Izquierdo (Gabe)
            if (leftCharacterPortrait != null)
            {
                // Gabe aparece siempre si habla él o si está hablando con un NPC
                bool showGabe = isGabeSpeaking || isNpcSpeaking || (!string.IsNullOrEmpty(currentInterlocutor) && isNarrator);
                leftCharacterPortrait.gameObject.SetActive(showGabe);

                if (showGabe)
                {
                    if (gabeSprite != null)
                    {
                        leftCharacterPortrait.sprite = gabeSprite;
                    }
                }
            }

            // Configurar Slot Derecho (NPC)
            if (rightCharacterPortrait != null)
            {
                bool showNpc = !string.IsNullOrEmpty(currentInterlocutor);
                rightCharacterPortrait.gameObject.SetActive(showNpc);

                if (showNpc)
                {
                    if (currentInterlocutorSprite != null)
                    {
                        rightCharacterPortrait.sprite = currentInterlocutorSprite;
                    }
                }
            }

            // Transición suave de Enfoque / Iluminación / Escala
            if (activeFocusRoutine != null) StopCoroutine(activeFocusRoutine);
            activeFocusRoutine = StartCoroutine(TransitionSpeakerFocusRoutine(isGabeSpeaking, isNpcSpeaking, isNarrator));

            // Disparar micro-animación en el personaje activo
            if (emotion != PortraitEmotion.None)
            {
                if (isGabeSpeaking && leftRect != null)
                {
                    PlayPortraitEmotion(emotion, leftRect, originalLeftPos, originalLeftScale);
                }
                else if (isNpcSpeaking && rightRect != null)
                {
                    PlayPortraitEmotion(emotion, rightRect, originalRightPos, originalRightScale);
                }
            }
        }

        private IEnumerator TransitionSpeakerFocusRoutine(bool isGabeSpeaking, bool isNpcSpeaking, bool isNarrator)
        {
            float duration = 0.22f;
            float elapsed = 0f;

            Color targetLeftColor;
            Vector3 targetLeftScale;
            Color targetRightColor;
            Vector3 targetRightScale;

            Color gabeColor = GetSpeakerPlaceholderColor("Gabe");
            Color npcColor = GetSpeakerPlaceholderColor(currentInterlocutor);

            if (isGabeSpeaking)
            {
                // Gabe ACTIVO (brillante y escala completa 1.0)
                targetLeftColor = gabeSprite != null ? Color.white : gabeColor;
                targetLeftScale = Vector3.one;

                // NPC INACTIVO (más oscuro y ligeramente más pequeño ~0.92)
                targetRightColor = (currentInterlocutorSprite != null ? Color.white : npcColor) * inactiveDimColor;
                targetRightScale = Vector3.one * inactiveScale;
            }
            else if (isNpcSpeaking)
            {
                // NPC ACTIVO (brillante y escala completa 1.0)
                targetRightColor = currentInterlocutorSprite != null ? Color.white : npcColor;
                targetRightScale = Vector3.one;

                // Gabe INACTIVO (más oscuro y ligeramente más pequeño ~0.92)
                targetLeftColor = (gabeSprite != null ? Color.white : gabeColor) * inactiveDimColor;
                targetLeftScale = Vector3.one * inactiveScale;
            }
            else // Narrador / Pensamiento
            {
                targetLeftColor = (gabeSprite != null ? Color.white : gabeColor) * new Color(0.65f, 0.65f, 0.7f, 1f);
                targetLeftScale = Vector3.one * 0.95f;

                targetRightColor = (currentInterlocutorSprite != null ? Color.white : npcColor) * new Color(0.65f, 0.65f, 0.7f, 1f);
                targetRightScale = Vector3.one * 0.95f;
            }

            Color startLeftColor = leftCharacterPortrait != null ? leftCharacterPortrait.color : Color.white;
            Vector3 startLeftScale = leftRect != null ? leftRect.localScale : Vector3.one;
            Color startRightColor = rightCharacterPortrait != null ? rightCharacterPortrait.color : Color.white;
            Vector3 startRightScale = rightRect != null ? rightRect.localScale : Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float popT = EaseOutBack(t, 1.4f);

                if (leftCharacterPortrait != null && leftRect != null)
                {
                    leftCharacterPortrait.color = Color.Lerp(startLeftColor, targetLeftColor, t);
                    leftRect.localScale = isGabeSpeaking
                        ? Vector3.LerpUnclamped(startLeftScale, targetLeftScale, popT)
                        : Vector3.Lerp(startLeftScale, targetLeftScale, t);
                }

                if (rightCharacterPortrait != null && rightRect != null)
                {
                    rightCharacterPortrait.color = Color.Lerp(startRightColor, targetRightColor, t);
                    rightRect.localScale = isNpcSpeaking
                        ? Vector3.LerpUnclamped(startRightScale, targetRightScale, popT)
                        : Vector3.Lerp(startRightScale, targetRightScale, t);
                }

                yield return null;
            }

            if (leftCharacterPortrait != null && leftRect != null)
            {
                leftCharacterPortrait.color = targetLeftColor;
                leftRect.localScale = targetLeftScale;
            }
            if (rightCharacterPortrait != null && rightRect != null)
            {
                rightCharacterPortrait.color = targetRightColor;
                rightRect.localScale = targetRightScale;
            }

            activeFocusRoutine = null;
        }

        private static float EaseOutBack(float t, float overshoot = 1.7f)
        {
            t -= 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        private static float EaseOutElastic(float t)
        {
            const float p = 0.3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
        }

        private IEnumerator TalkBobRoutine(RectTransform rect, Vector2 originPos)
        {
            float t = 0f;
            while (t < talkBobDur)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / talkBobDur);
                float y = originPos.y + Mathf.Sin(n * Mathf.PI) * talkBobAmp * (1f - n);
                rect.anchoredPosition = new Vector2(originPos.x, y);
                yield return null;
            }
            rect.anchoredPosition = originPos;
            talkBobRoutine = null;
        }

        private IEnumerator IdleBreathRoutine(RectTransform rect, Vector3 originScale)
        {
            float t = 0f;
            const float period = 2.4f;
            const float amplitude = 0.018f;

            while (true)
            {
                t += Time.deltaTime;
                float breathe = (Mathf.Sin(t * Mathf.PI * 2f / period) + 1f) * 0.5f;
                rect.localScale = new Vector3(originScale.x, originScale.y * (1f + breathe * amplitude), originScale.z);
                yield return null;
            }
        }

        private PortraitEmotion DetectEmotion(ref string text)
        {
            if (string.IsNullOrEmpty(text)) return PortraitEmotion.None;

            if (text.Contains("[shake]"))
            {
                text = text.Replace("[shake]", "");
                return PortraitEmotion.Shake;
            }
            if (text.Contains("[bounce]"))
            {
                text = text.Replace("[bounce]", "");
                return PortraitEmotion.Bounce;
            }
            if (text.Contains("[punch]"))
            {
                text = text.Replace("[punch]", "");
                return PortraitEmotion.Punch;
            }
            if (text.Contains("[tremble]"))
            {
                text = text.Replace("[tremble]", "");
                return PortraitEmotion.Tremble;
            }
            if (text.Contains("[nod]"))
            {
                text = text.Replace("[nod]", "");
                return PortraitEmotion.Nod;
            }

            // Detección contextual sutil por puntuación y semántica
            if (text.Contains("?!") || text.Contains("!?") || text.EndsWith("!!!"))
            {
                return PortraitEmotion.Shake;
            }
            if (text.EndsWith("!"))
            {
                return PortraitEmotion.Punch;
            }
            if (text.EndsWith("?"))
            {
                return PortraitEmotion.Nod;
            }
            if (text.Contains("...") && (text.Contains("afraid") || text.Contains("scared") || text.Contains("dead") || text.Contains("body") || text.Contains("blood") || text.Contains("murder") || text.Contains("kill") || text.Contains("lie") || text.Contains("knife") || text.Contains("glass")))
            {
                return PortraitEmotion.Tremble;
            }

            return PortraitEmotion.None;
        }

        public void PlayPortraitEmotion(PortraitEmotion emotion, RectTransform targetRect, Vector2 originPos, Vector3 originScale)
        {
            if (targetRect == null) return;

            if (activePortraitRoutine != null)
            {
                StopCoroutine(activePortraitRoutine);
            }

            ResetAllPortraitTransforms();

            switch (emotion)
            {
                case PortraitEmotion.Shake:
                    activePortraitRoutine = StartCoroutine(ShakePortraitRoutine(targetRect, originPos));
                    break;
                case PortraitEmotion.Bounce:
                    activePortraitRoutine = StartCoroutine(BouncePortraitRoutine(targetRect, originPos, originScale));
                    break;
                case PortraitEmotion.Punch:
                    activePortraitRoutine = StartCoroutine(PunchPortraitRoutine(targetRect, originScale));
                    break;
                case PortraitEmotion.Tremble:
                    activePortraitRoutine = StartCoroutine(TremblePortraitRoutine(targetRect, originPos));
                    break;
                case PortraitEmotion.Nod:
                    activePortraitRoutine = StartCoroutine(NodPortraitRoutine(targetRect, originPos));
                    break;
            }
        }

        private IEnumerator ShakePortraitRoutine(RectTransform targetRect, Vector2 originPos)
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

                targetRect.anchoredPosition = originPos + new Vector2(offsetX, offsetY);
                targetRect.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                yield return null;
            }

            targetRect.anchoredPosition = originPos;
            targetRect.localRotation = Quaternion.identity;
            activePortraitRoutine = null;
        }

        private IEnumerator BouncePortraitRoutine(RectTransform targetRect, Vector2 originPos, Vector3 originScale)
        {
            float duration = 0.35f;
            float elapsed = 0f;
            float jumpHeight = 26f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

                float scaleY = 1f + Mathf.Sin(t * Mathf.PI) * 0.16f;
                float scaleX = 1f - Mathf.Sin(t * Mathf.PI) * 0.12f;

                targetRect.anchoredPosition = originPos + new Vector2(0f, height);
                targetRect.localScale = new Vector3(originScale.x * scaleX, originScale.y * scaleY, originScale.z);
                yield return null;
            }

            targetRect.anchoredPosition = originPos;
            targetRect.localScale = originScale;
            activePortraitRoutine = null;
        }

        private IEnumerator PunchPortraitRoutine(RectTransform targetRect, Vector3 originScale)
        {
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scaleBoost = (1f - t) * Mathf.Sin(t * Mathf.PI * 3f) * 0.24f;

                targetRect.localScale = originScale * (1f + scaleBoost);
                yield return null;
            }

            targetRect.localScale = originScale;
            activePortraitRoutine = null;
        }

        private IEnumerator TremblePortraitRoutine(RectTransform targetRect, Vector2 originPos)
        {
            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offsetX = (UnityEngine.Random.value - 0.5f) * 6f;
                float offsetY = (UnityEngine.Random.value - 0.5f) * 4f;

                targetRect.anchoredPosition = originPos + new Vector2(offsetX, offsetY);
                yield return null;
            }

            targetRect.anchoredPosition = originPos;
            activePortraitRoutine = null;
        }

        private IEnumerator NodPortraitRoutine(RectTransform targetRect, Vector2 originPos)
        {
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float offsetY = -Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 10f;

                targetRect.anchoredPosition = originPos + new Vector2(0f, offsetY);
                yield return null;
            }

            targetRect.anchoredPosition = originPos;
            activePortraitRoutine = null;
        }

        private void ResetAllPortraitTransforms()
        {
            if (leftRect != null)
            {
                leftRect.anchoredPosition = originalLeftPos;
                leftRect.localRotation = originalLeftRot;
            }
            if (rightRect != null)
            {
                rightRect.anchoredPosition = originalRightPos;
                rightRect.localRotation = originalRightRot;
            }
            if (centerRect != null)
            {
                centerRect.anchoredPosition = originalCenterPos;
                centerRect.localRotation = originalCenterRot;
            }
        }

        private Color GetSpeakerPlaceholderColor(string speakerName)
        {
            if (IsProtagonistSpeaker(speakerName)) return new Color(0.35f, 0.50f, 0.70f, 1f); // Azul detective noir

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
