using System;
using System.Collections.Generic;
using UnityEngine;

namespace VisualNovelSystem
{
    [Serializable]
    public class BlackboardVariable
    {
        public string key;
        public string value; // Stored as string for generic serialization, parsed as bool/int/float/string
        public VariableType type;
    }

    public enum VariableType
    {
        Bool,
        Int,
        Float,
        String
    }

    [Serializable]
    public class StoryBlackboard
    {
        [SerializeField]
        private List<BlackboardVariable> initialVariables = new List<BlackboardVariable>();

        private Dictionary<string, string> runtimeVariables = new Dictionary<string, string>();

        public void Initialize()
        {
            runtimeVariables.Clear();
            foreach (var v in initialVariables)
            {
                if (!string.IsNullOrEmpty(v.key))
                {
                    runtimeVariables[v.key] = v.value;
                }
            }
        }

        public void SetBool(string key, bool value)
        {
            runtimeVariables[key] = value.ToString().ToLower();
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (runtimeVariables.TryGetValue(key, out string val))
            {
                if (bool.TryParse(val, out bool result)) return result;
            }
            return defaultValue;
        }

        public void SetInt(string key, int value)
        {
            runtimeVariables[key] = value.ToString();
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (runtimeVariables.TryGetValue(key, out string val))
            {
                if (int.TryParse(val, out int result)) return result;
            }
            return defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            runtimeVariables[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (runtimeVariables.TryGetValue(key, out string val))
            {
                if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result)) return result;
            }
            return defaultValue;
        }

        public void SetString(string key, string value)
        {
            runtimeVariables[key] = value;
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (runtimeVariables.TryGetValue(key, out string val))
            {
                return val;
            }
            return defaultValue;
        }

        public bool HasVariable(string key)
        {
            return runtimeVariables.ContainsKey(key);
        }

        public bool EvaluateCondition(string variableName, ConditionComparison comparison, string compareValue)
        {
            if (string.IsNullOrEmpty(variableName)) return true;

            string currentValue = GetString(variableName, "false");

            // Check bool
            if (bool.TryParse(currentValue, out bool currentBool) && bool.TryParse(compareValue, out bool compareBool))
            {
                if (comparison == ConditionComparison.Equal) return currentBool == compareBool;
                if (comparison == ConditionComparison.NotEqual) return currentBool != compareBool;
            }

            // Check number
            if (float.TryParse(currentValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float currentNum) &&
                float.TryParse(compareValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float compareNum))
            {
                switch (comparison)
                {
                    case ConditionComparison.Equal: return Mathf.Approximately(currentNum, compareNum);
                    case ConditionComparison.NotEqual: return !Mathf.Approximately(currentNum, compareNum);
                    case ConditionComparison.GreaterThan: return currentNum > compareNum;
                    case ConditionComparison.LessThan: return currentNum < compareNum;
                    case ConditionComparison.GreaterOrEqual: return currentNum >= compareNum;
                    case ConditionComparison.LessOrEqual: return currentNum <= compareNum;
                }
            }

            // Default string comparison
            switch (comparison)
            {
                case ConditionComparison.Equal: return string.Equals(currentValue, compareValue, StringComparison.OrdinalIgnoreCase);
                case ConditionComparison.NotEqual: return !string.Equals(currentValue, compareValue, StringComparison.OrdinalIgnoreCase);
                default: return string.Equals(currentValue, compareValue, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
