using System;
using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    [Serializable]
    public class CaseFlagAction : StoryAction
    {
        public enum Operation { SetFlag, ClearFlag, IncrementCounter }
        [SerializeField] public Operation operation = Operation.SetFlag;
        [SerializeField] public string key = "";
        [SerializeField] public int amount = 1;

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (string.IsNullOrEmpty(key) || CaseState.Instance == null) yield break;

            switch (operation)
            {
                case Operation.SetFlag:
                    CaseState.Instance.SetFlag(key);
                    break;
                case Operation.ClearFlag:
                    CaseState.Instance.ClearFlag(key);
                    break;
                case Operation.IncrementCounter:
                    CaseState.Instance.IncrementCounter(key, amount);
                    break;
            }
            yield break;
        }

        public override string GetSummary() => $"{operation}: {key}";
    }
}
