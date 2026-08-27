using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VisualNovelSystem
{
    public class StoryRunner : MonoBehaviour
    {
        [Header("Graph Setup")]
        [SerializeField] private StoryGraph storyGraph;
        [SerializeField] private bool playOnStart = true;

        [Header("UI & Systems References")]
        [SerializeField] private StoryUIController uiController;

        public StoryGraph CurrentGraph => storyGraph;
        public StoryUIController UIController => uiController;
        public StoryBlackboard Blackboard => storyGraph != null ? storyGraph.blackboard : null;

        public static StoryRunner ActiveRunner { get; private set; }

        public bool IsRunning { get; private set; }
        public StoryNodeData CurrentNode { get; private set; }

        public event Action<StoryNodeData> OnNodeStarted;
        public event Action<StoryNodeData> OnNodeFinished;
        public event Action OnGraphCompleted;

        private Coroutine runnerRoutine;
        private bool explorationCompleted = false;

        private void Awake()
        {
            ActiveRunner = this;

            if (uiController == null)
            {
                uiController = StoryUIController.Instance;
                if (uiController == null)
                {
                    uiController = UnityEngine.Object.FindAnyObjectByType<StoryUIController>();
                }
            }
        }

        public void CompleteExploration()
        {
            explorationCompleted = true;
        }

        private void Start()
        {
            if (playOnStart && storyGraph != null)
            {
                StartStory(storyGraph);
            }
        }

        public void StartStory(StoryGraph graph = null)
        {
            if (graph != null) storyGraph = graph;

            if (storyGraph == null)
            {
                Debug.LogWarning("[StoryRunner] Cannot start story: No StoryGraph assigned!");
                return;
            }

            if (runnerRoutine != null)
            {
                StopCoroutine(runnerRoutine);
            }

            runnerRoutine = StartCoroutine(RunGraphRoutine());
        }

        public void StopStory()
        {
            if (runnerRoutine != null)
            {
                StopCoroutine(runnerRoutine);
                runnerRoutine = null;
            }
            if (uiController != null) uiController.HideDialogue();
            IsRunning = false;
            CurrentNode = null;
        }

        private IEnumerator RunGraphRoutine()
        {
            IsRunning = true;

            if (storyGraph.blackboard != null)
            {
                storyGraph.blackboard.Initialize();
            }

            StoryNodeData currentNode = storyGraph.GetStartNode();
            if (currentNode == null)
            {
                Debug.LogError("[StoryRunner] StoryGraph contains no Start node or Entry point!");
                IsRunning = false;
                yield break;
            }

            while (currentNode != null && IsRunning)
            {
                CurrentNode = currentNode;
                OnNodeStarted?.Invoke(currentNode);

                string nextNodeGuid = null;

                switch (currentNode.nodeType)
                {
                    case StoryNodeType.Start:
                        // Move to next node immediately
                        nextNodeGuid = storyGraph.GetNextNodeGuid(currentNode.guid, "output");
                        break;

                    case StoryNodeType.ActionSequence:
                        // Execute all actions in order
                        if (currentNode.actions != null)
                        {
                            foreach (var action in currentNode.actions)
                            {
                                if (action != null && action.enabled)
                                {
                                    yield return action.Execute(this);
                                }
                            }
                        }
                        nextNodeGuid = storyGraph.GetNextNodeGuid(currentNode.guid, "output");
                        break;

                    case StoryNodeType.Choice:
                        // Filter visible choices
                        List<StoryChoiceOption> visibleChoices = new List<StoryChoiceOption>();
                        foreach (var choice in currentNode.choices)
                        {
                            if (string.IsNullOrEmpty(choice.conditionVariable) ||
                                (Blackboard != null && Blackboard.GetBool(choice.conditionVariable, true)))
                            {
                                visibleChoices.Add(choice);
                            }
                        }

                        if (visibleChoices.Count == 0)
                        {
                            Debug.LogWarning($"[StoryRunner] Choice node '{currentNode.title}' has no valid options to display.");
                            nextNodeGuid = storyGraph.GetNextNodeGuid(currentNode.guid, "output");
                        }
                        else
                        {
                            int selectedIndex = -1;
                            if (uiController != null)
                            {
                                yield return uiController.ShowChoices(currentNode.promptText, visibleChoices, (idx) =>
                                {
                                    selectedIndex = idx;
                                });
                            }
                            else
                            {
                                selectedIndex = 0;
                            }

                            if (selectedIndex >= 0 && selectedIndex < visibleChoices.Count)
                            {
                                var selectedChoice = visibleChoices[selectedIndex];
                                // Check connection by choice.id or choice index
                                nextNodeGuid = storyGraph.GetNextNodeGuid(currentNode.guid, selectedChoice.id);
                                if (string.IsNullOrEmpty(nextNodeGuid))
                                {
                                    nextNodeGuid = storyGraph.GetNextNodeGuid(currentNode.guid, $"choice_{selectedIndex}");
                                }
                            }
                        }
                        break;

                    case StoryNodeType.Condition:
                        bool conditionMet = false;
                        if (Blackboard != null)
                        {
                            conditionMet = Blackboard.EvaluateCondition(
                                currentNode.conditionVariableName,
                                currentNode.conditionComparison,
                                currentNode.conditionCompareValue
                            );
                        }

                        string portId = conditionMet ? "true" : "false";
                        nextNodeGuid = storyGraph.GetNextNodeGuid(currentNode.guid, portId);
                        break;

                    case StoryNodeType.Wait:
                        if (currentNode.waitDuration > 0)
                        {
                            yield return new WaitForSeconds(currentNode.waitDuration);
                        }
                        nextNodeGuid = storyGraph.GetNextNodeGuid(currentNode.guid, "output");
                        break;

                    case StoryNodeType.Exploration:
                        explorationCompleted = false;

                        // Hide dialogue box while in exploration mode
                        if (uiController != null) uiController.HideDialogue();

                        // Show prompt banner if specified
                        if (!string.IsNullOrEmpty(currentNode.explorationPrompt) && uiController != null && uiController.OverlayUI != null)
                        {
                            StartCoroutine(uiController.ShowOverlay("", currentNode.explorationPrompt, OverlayDisplayMode.TopHeader, OverlayEffect.Fade, 2.5f, false));
                        }

                        float exploreTimer = 0f;
                        while (!explorationCompleted)
                        {
                            // Check exit condition variable in blackboard
                            if (!string.IsNullOrEmpty(currentNode.exitConditionVariable) && Blackboard != null)
                            {
                                if (Blackboard.GetBool(currentNode.exitConditionVariable, false))
                                {
                                    explorationCompleted = true;
                                    break;
                                }
                            }

                            // Check timeout if enabled
                            if (currentNode.explorationTimeout > 0f)
                            {
                                exploreTimer += Time.deltaTime;
                                if (exploreTimer >= currentNode.explorationTimeout)
                                {
                                    explorationCompleted = true;
                                    break;
                                }
                            }

                            yield return null;
                        }

                        nextNodeGuid = storyGraph.GetNextNodeGuid(currentNode.guid, "output");
                        break;

                    case StoryNodeType.End:
                        IsRunning = false;
                        if (uiController != null) uiController.HideDialogue();
                        OnNodeFinished?.Invoke(currentNode);
                        OnGraphCompleted?.Invoke();
                        yield break;
                }

                OnNodeFinished?.Invoke(currentNode);

                if (string.IsNullOrEmpty(nextNodeGuid))
                {
                    // Reached end of connected path
                    break;
                }

                currentNode = storyGraph.GetNodeByGuid(nextNodeGuid);
            }

            if (uiController != null) uiController.HideDialogue();
            IsRunning = false;
            CurrentNode = null;
            OnGraphCompleted?.Invoke();
        }
    }
}
