using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    /// <summary>
    /// Example of how you can create any custom script/action to animate or trigger things in your scenes.
    /// Just inherit from StoryAction, implement Execute(), and it will automatically be available in the Node Editor!
    /// </summary>
    [Serializable]
    public class CustomSampleAction : StoryAction
    {
        public string customMessage = "¡Evento personalizado ejecutado!";
        public float delay = 0.5f;

        public override IEnumerator Execute(StoryRunner runner)
        {
            Debug.Log($"[CustomSampleAction] {customMessage}");

            // Example: Do your custom logic here (e.g. call a special script, spawn VFX, trigger custom camera move, etc.)
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        public override string GetSummary()
        {
            return $"Custom Action: \"{customMessage}\"";
        }
    }
}
