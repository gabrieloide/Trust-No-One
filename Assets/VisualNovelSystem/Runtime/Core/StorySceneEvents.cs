using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VisualNovelSystem
{
    [Serializable]
    public class NamedStoryEvent
    {
        public string eventName;
        public UnityEvent onTrigger;
    }

    /// <summary>
    /// Attach this MonoBehaviour to any GameObject in the scene to bind named UnityEvents to your story graph.
    /// </summary>
    public class StorySceneEvents : MonoBehaviour
    {
        public static StorySceneEvents Instance { get; private set; }

        [SerializeField]
        private List<NamedStoryEvent> events = new List<NamedStoryEvent>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void TriggerEvent(string eventName)
        {
            var evt = events.Find(e => string.Equals(e.eventName, eventName, StringComparison.OrdinalIgnoreCase));
            if (evt != null)
            {
                evt.onTrigger?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[StorySceneEvents] Event '{eventName}' was not found in the scene.");
            }
        }
    }
}
