using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    /// <summary>
    /// Base class for all modular story actions.
    /// To create custom actions, inherit from this class and implement Execute().
    /// </summary>
    [Serializable]
    public abstract class StoryAction
    {
        [SerializeField] public bool enabled = true;

        /// <summary>
        /// Executes the action. Yield return within the coroutine to pause sequence until complete.
        /// </summary>
        public abstract IEnumerator Execute(StoryRunner runner);

        /// <summary>
        /// Human-readable title or summary of this action shown in the node editor.
        /// </summary>
        public virtual string GetSummary()
        {
            return GetType().Name;
        }
    }
}
