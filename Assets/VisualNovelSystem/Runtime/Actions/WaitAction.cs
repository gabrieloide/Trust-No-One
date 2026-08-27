using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    [Serializable]
    public class WaitAction : StoryAction
    {
        public float duration = 1.0f;

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
        }

        public override string GetSummary()
        {
            return $"Wait {duration}s";
        }
    }
}
