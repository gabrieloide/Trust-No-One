using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum EventTriggerMode
    {
        NamedSceneEvent,
        SendMessageToGameObject
    }

    [Serializable]
    public class UnityEventAction : StoryAction
    {
        public EventTriggerMode triggerMode = EventTriggerMode.NamedSceneEvent;

        [Header("Named Scene Event Settings")]
        public string eventName = "MySceneEvent";

        [Header("SendMessage Settings")]
        public string targetObjectName = "GameManager";
        public string methodName = "OnStoryTrigger";
        public string methodParameter = "";

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (triggerMode == EventTriggerMode.NamedSceneEvent)
            {
                if (StorySceneEvents.Instance != null)
                {
                    StorySceneEvents.Instance.TriggerEvent(eventName);
                }
                else
                {
                    var sceneEvents = UnityEngine.Object.FindAnyObjectByType<StorySceneEvents>();
                    if (sceneEvents != null)
                    {
                        sceneEvents.TriggerEvent(eventName);
                    }
                    else
                    {
                        Debug.LogWarning($"[UnityEventAction] No StorySceneEvents component found in scene for event '{eventName}'.");
                    }
                }
            }
            else // SendMessageToGameObject
            {
                GameObject obj = GameObject.Find(targetObjectName);
                if (obj != null)
                {
                    if (!string.IsNullOrEmpty(methodParameter))
                    {
                        obj.SendMessage(methodName, methodParameter, SendMessageOptions.DontRequireReceiver);
                    }
                    else
                    {
                        obj.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
                    }
                }
                else
                {
                    Debug.LogWarning($"[UnityEventAction] GameObject '{targetObjectName}' not found for SendMessage '{methodName}'.");
                }
            }

            yield break;
        }

        public override string GetSummary()
        {
            if (triggerMode == EventTriggerMode.NamedSceneEvent)
                return $"Trigger Event: '{eventName}'";
            return $"Call '{methodName}()' on '{targetObjectName}'";
        }
    }
}
