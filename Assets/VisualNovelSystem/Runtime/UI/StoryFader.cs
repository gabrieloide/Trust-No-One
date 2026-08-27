using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovelSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class StoryFader : MonoBehaviour
    {
        [SerializeField] private Image fadeImage;
        [SerializeField] private CanvasGroup canvasGroup;

        private Coroutine currentFadeRoutine;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (fadeImage == null) fadeImage = GetComponentInChildren<Image>();

            // Default transparent
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public bool IsFading => currentFadeRoutine != null;

        public void SetInstant(Color color, float alpha)
        {
            if (currentFadeRoutine != null)
            {
                StopCoroutine(currentFadeRoutine);
                currentFadeRoutine = null;
            }

            if (fadeImage != null) fadeImage.color = color;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                canvasGroup.blocksRaycasts = alpha > 0.01f;
            }
        }

        public IEnumerator FadeRoutine(float targetAlpha, Color targetColor, float duration, AnimationCurve curve = null)
        {
            if (duration <= 0.001f)
            {
                SetInstant(targetColor, targetAlpha);
                yield break;
            }

            if (fadeImage != null) fadeImage.color = targetColor;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = targetAlpha > 0.01f;

            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
            float elapsed = 0f;

            if (curve == null) curve = AnimationCurve.Linear(0, 0, 1, 1);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float evaluatedT = curve.Evaluate(t);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, evaluatedT);
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
                canvasGroup.blocksRaycasts = targetAlpha > 0.01f;
            }

            currentFadeRoutine = null;
        }

        public void StartFade(float targetAlpha, Color targetColor, float duration, AnimationCurve curve = null)
        {
            if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
            currentFadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, targetColor, duration, curve));
        }
    }
}
