using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    [Serializable]
    public class SetInteractableStateAction : StoryAction
    {
        public string targetObjectName = "Hotspot_Object";
        public bool modifyGameObjectActive = false;
        public bool isGameObjectActive = true;
        public bool setInteractable = true;

        public override IEnumerator Execute(StoryRunner runner)
        {
            GameObject obj = GameObject.Find(targetObjectName);
            if (obj != null)
            {
                if (modifyGameObjectActive)
                {
                    obj.SetActive(isGameObjectActive);
                }

                var interactable = obj.GetComponent<StoryInteractable>();
                if (interactable != null)
                {
                    interactable.IsInteractable = setInteractable;
                }
            }
            else
            {
                Debug.LogWarning($"[SetInteractableStateAction] Object '{targetObjectName}' not found in scene.");
            }

            yield break;
        }

        public override string GetSummary()
        {
            return $"Set Interactable '{targetObjectName}' -> {(setInteractable ? "Enabled" : "Disabled")}";
        }
    }
}
