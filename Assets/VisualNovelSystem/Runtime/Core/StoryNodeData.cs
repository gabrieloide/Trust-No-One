using System;
using System.Collections.Generic;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum StoryNodeType
    {
        Start,
        ActionSequence,
        Choice,
        Condition,
        Wait,
        Exploration,
        End
    }

    public enum ConditionComparison
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }

    [Serializable]
    public class StoryChoiceOption
    {
        public string id = Guid.NewGuid().ToString();
        public string text = "Option";
        public string conditionVariable = ""; // Optional condition flag to show this choice
    }

    [Serializable]
    public class StoryNodeData
    {
        public string guid = Guid.NewGuid().ToString();
        public string title = "Node";
        public StoryNodeType nodeType = StoryNodeType.ActionSequence;
        public Vector2 position;

        // For ActionSequence nodes
        [SerializeReference]
        public List<StoryAction> actions = new List<StoryAction>();

        // For Choice nodes
        public string promptText = "¿Qué deseas hacer?";
        public List<StoryChoiceOption> choices = new List<StoryChoiceOption>();

        // For Condition nodes
        public string conditionVariableName = "";
        public ConditionComparison conditionComparison = ConditionComparison.Equal;
        public string conditionCompareValue = "true";

        // For Wait nodes
        public float waitDuration = 1f;

        // For Exploration (Point & Click) nodes
        public string explorationPrompt = "Modo Exploración: Haz clic o interactúa con objetos del mapa.";
        public string exitConditionVariable = ""; // Variable that automatically exits exploration when true
        public float explorationTimeout = -1f; // -1 for indefinite

        // Custom note / comment
        [TextArea(2, 4)]
        public string comment = "";

        public StoryNodeData()
        {
            guid = Guid.NewGuid().ToString();
        }

        public StoryNodeData(StoryNodeType type, Vector2 pos)
        {
            guid = Guid.NewGuid().ToString();
            nodeType = type;
            position = pos;
            title = type.ToString();
        }
    }
}
