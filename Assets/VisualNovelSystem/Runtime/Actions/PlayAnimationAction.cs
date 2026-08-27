using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum AnimationActionType
    {
        SetTrigger,
        SetBool,
        SetInteger,
        SetFloat,
        PlayState
    }

    [Serializable]
    public class PlayAnimationAction : StoryAction
    {
        public string targetObjectName = "Character";
        public AnimationActionType actionType = AnimationActionType.SetTrigger;
        public string parameterOrStateName = "Idle";
        public bool boolValue = true;
        public int intValue = 0;
        public float floatValue = 0f;

        public bool waitForSeconds = false;
        public float waitDuration = 1.0f;

        public override IEnumerator Execute(StoryRunner runner)
        {
            GameObject target = GameObject.Find(targetObjectName);
            if (target != null)
            {
                var animator = target.GetComponent<Animator>();
                if (animator != null)
                {
                    switch (actionType)
                    {
                        case AnimationActionType.SetTrigger:
                            animator.SetTrigger(parameterOrStateName);
                            break;
                        case AnimationActionType.SetBool:
                            animator.SetBool(parameterOrStateName, boolValue);
                            break;
                        case AnimationActionType.SetInteger:
                            animator.SetInteger(parameterOrStateName, intValue);
                            break;
                        case AnimationActionType.SetFloat:
                            animator.SetFloat(parameterOrStateName, floatValue);
                            break;
                        case AnimationActionType.PlayState:
                            animator.Play(parameterOrStateName);
                            break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlayAnimationAction] No Animator component found on '{targetObjectName}'.");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayAnimationAction] Target GameObject '{targetObjectName}' not found in scene.");
            }

            if (waitForSeconds && waitDuration > 0)
            {
                yield return new WaitForSeconds(waitDuration);
            }
        }

        public override string GetSummary()
        {
            return $"Animate '{targetObjectName}' -> {actionType} '{parameterOrStateName}'";
        }
    }
}
