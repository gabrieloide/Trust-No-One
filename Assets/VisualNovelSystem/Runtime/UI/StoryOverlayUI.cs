using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    public enum OverlayDisplayMode
    {
        CenterTitleCard,
        TopHeader,
        BottomTimestamp
    }

    public enum OverlayEffect
    {
        Fade,
        Typewriter,
        Instant
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class StoryOverlayUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private RectTransform contentContainer;

        private Coroutine activeOverlayRoutine;
        private bool skipRequested = false;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            HideInstant();
        }

        private void Update()
        {
            if (StoryInput.ContinuePressed())
            {
                skipRequested = true;
            }
        }

        public void HideInstant()
        {
            if (activeOverlayRoutine != null)
            {
                StopCoroutine(activeOverlayRoutine);
                activeOverlayRoutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            if (titleText != null) titleText.text = "";
            if (subtitleText != null) subtitleText.text = "";
        }

        public IEnumerator ShowOverlayRoutine(string title, string subtitle, OverlayDisplayMode mode, OverlayEffect effect, float duration, bool waitForClick, float fadeDuration = 0.5f)
        {
            skipRequested = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = waitForClick;
            }

            // Adjust anchors/positions based on mode
            ApplyDisplayMode(mode);

            if (titleText != null) titleText.text = "";
            if (subtitleText != null) subtitleText.text = "";

            if (effect == OverlayEffect.Instant)
            {
                if (titleText != null) titleText.text = title;
                if (subtitleText != null) subtitleText.text = subtitle;
            }
            else if (effect == OverlayEffect.Typewriter)
            {
                if (titleText != null)
                {
                    for (int i = 0; i <= title.Length; i++)
                    {
                        if (skipRequested)
                        {
                            titleText.text = title;
                            break;
                        }
                        titleText.text = title.Substring(0, i);
                        yield return new WaitForSeconds(0.04f);
                    }
                }

                if (subtitleText != null && !string.IsNullOrEmpty(subtitle))
                {
                    yield return new WaitForSeconds(0.2f);
                    for (int i = 0; i <= subtitle.Length; i++)
                    {
                        if (skipRequested)
                        {
                            subtitleText.text = subtitle;
                            break;
                        }
                        subtitleText.text = subtitle.Substring(0, i);
                        yield return new WaitForSeconds(0.03f);
                    }
                }
            }
            else // Fade
            {
                if (titleText != null) titleText.text = title;
                if (subtitleText != null) subtitleText.text = subtitle;

                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                    if (titleText != null)
                    {
                        Color c = titleText.color;
                        c.a = alpha;
                        titleText.color = c;
                    }
                    if (subtitleText != null)
                    {
                        Color c = subtitleText.color;
                        c.a = alpha;
                        subtitleText.color = c;
                    }
                    yield return null;
                }
            }

            skipRequested = false;

            if (waitForClick)
            {
                // Wait until user clicks or presses space
                while (!skipRequested)
                {
                    yield return null;
                }
            }
            else if (duration > 0f)
            {
                float timer = 0f;
                while (timer < duration && !skipRequested)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            // Fade out overlay
            if (fadeDuration > 0.01f)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                    yield return null;
                }
            }

            HideInstant();
            activeOverlayRoutine = null;
        }

        private void ApplyDisplayMode(OverlayDisplayMode mode)
        {
            if (contentContainer == null) return;

            switch (mode)
            {
                case OverlayDisplayMode.CenterTitleCard:
                    contentContainer.anchorMin = new Vector2(0.5f, 0.5f);
                    contentContainer.anchorMax = new Vector2(0.5f, 0.5f);
                    contentContainer.pivot = new Vector2(0.5f, 0.5f);
                    contentContainer.anchoredPosition = Vector2.zero;
                    if (titleText != null) titleText.alignment = TextAlignmentOptions.Center;
                    if (subtitleText != null) subtitleText.alignment = TextAlignmentOptions.Center;
                    break;

                case OverlayDisplayMode.TopHeader:
                    contentContainer.anchorMin = new Vector2(0.5f, 1f);
                    contentContainer.anchorMax = new Vector2(0.5f, 1f);
                    contentContainer.pivot = new Vector2(0.5f, 1f);
                    contentContainer.anchoredPosition = new Vector2(0, -80);
                    if (titleText != null) titleText.alignment = TextAlignmentOptions.Center;
                    if (subtitleText != null) subtitleText.alignment = TextAlignmentOptions.Center;
                    break;

                case OverlayDisplayMode.BottomTimestamp:
                    contentContainer.anchorMin = new Vector2(1f, 0f);
                    contentContainer.anchorMax = new Vector2(1f, 0f);
                    contentContainer.pivot = new Vector2(1f, 0f);
                    contentContainer.anchoredPosition = new Vector2(-60, 60);
                    if (titleText != null) titleText.alignment = TextAlignmentOptions.Right;
                    if (subtitleText != null) subtitleText.alignment = TextAlignmentOptions.Right;
                    break;
            }
        }
    }
}
