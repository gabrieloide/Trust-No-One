using System;
using System.Collections;
using UnityEngine;

namespace VisualNovelSystem
{
    public enum VariableOperation
    {
        Set,
        Add,
        Subtract,
        ToggleBool
    }

    [Serializable]
    public class SetVariableAction : StoryAction
    {
        public string variableName = "StoryFlag";
        public VariableType variableType = VariableType.Bool;
        public VariableOperation operation = VariableOperation.Set;
        public string stringValue = "true";

        public override IEnumerator Execute(StoryRunner runner)
        {
            var blackboard = runner != null ? runner.Blackboard : null;
            if (blackboard == null) yield break;

            switch (variableType)
            {
                case VariableType.Bool:
                    if (operation == VariableOperation.ToggleBool)
                    {
                        bool current = blackboard.GetBool(variableName, false);
                        blackboard.SetBool(variableName, !current);
                    }
                    else
                    {
                        bool.TryParse(stringValue, out bool bVal);
                        blackboard.SetBool(variableName, bVal);
                    }
                    break;

                case VariableType.Int:
                    int.TryParse(stringValue, out int iVal);
                    if (operation == VariableOperation.Add)
                    {
                        int cur = blackboard.GetInt(variableName, 0);
                        blackboard.SetInt(variableName, cur + iVal);
                    }
                    else if (operation == VariableOperation.Subtract)
                    {
                        int cur = blackboard.GetInt(variableName, 0);
                        blackboard.SetInt(variableName, cur - iVal);
                    }
                    else
                    {
                        blackboard.SetInt(variableName, iVal);
                    }
                    break;

                case VariableType.Float:
                    float.TryParse(stringValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fVal);
                    if (operation == VariableOperation.Add)
                    {
                        float cur = blackboard.GetFloat(variableName, 0f);
                        blackboard.SetFloat(variableName, cur + fVal);
                    }
                    else if (operation == VariableOperation.Subtract)
                    {
                        float cur = blackboard.GetFloat(variableName, 0f);
                        blackboard.SetFloat(variableName, cur - fVal);
                    }
                    else
                    {
                        blackboard.SetFloat(variableName, fVal);
                    }
                    break;

                case VariableType.String:
                    blackboard.SetString(variableName, stringValue);
                    break;
            }

            yield break;
        }

        public override string GetSummary()
        {
            return $"Set Var: '{variableName}' = {stringValue}";
        }
    }
}
