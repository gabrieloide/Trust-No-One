using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum FadeType
    {
        FadeOut, // From transparent to color (darken)
        FadeIn   // From color to transparent (reveal)
    }

    [Serializable]
    public class FadeScreenAction : StoryAction
    {
        public FadeType fadeType = FadeType.FadeOut;
        public Color fadeColor = Color.black;
        public float duration = 1.0f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool waitForCompletion = true;

        public override IEnumerator Execute(StoryRunner runner)
        {
            float targetAlpha = (fadeType == FadeType.FadeOut) ? 1.0f : 0.0f;
            var fader = runner != null && runner.UIController != null ? runner.UIController.Fader : null;

            if (fader != null)
            {
                if (waitForCompletion)
                {
                    yield return fader.FadeRoutine(targetAlpha, fadeColor, duration, curve);
                }
                else
                {
                    fader.StartFade(targetAlpha, fadeColor, duration, curve);
                }
            }
            else if (waitForCompletion)
            {
                yield return new WaitForSeconds(duration);
            }

            // Al oscurecer por completo (Fade Out), limpiamos el diálogo y los personajes viejos
            // para que al hacer Fade In posterior nunca aparezca el personaje anterior.
            if (fadeType == FadeType.FadeOut && runner != null && runner.UIController != null)
            {
                runner.UIController.HideDialogue();
            }
        }

        public override string GetSummary()
        {
            return $"Fade Screen ({fadeType}, {duration}s)";
        }
    }
}
