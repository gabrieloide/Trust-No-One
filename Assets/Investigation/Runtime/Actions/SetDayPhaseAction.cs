using System;
using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    [Serializable]
    public class SetDayPhaseAction : StoryAction
    {
        [SerializeField] public int targetDay = 2;
        [SerializeField] public int targetPhase = 1;
        [SerializeField] public int actionsRemaining = 4;

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (CaseState.Instance != null)
            {
                CaseState.Instance.currentDay = targetDay;
                CaseState.Instance.currentPhase = targetPhase;
                CaseState.Instance.actionsRemainingInPhase = actionsRemaining;
                CaseState.Instance.SetFlag($"d{targetDay}p{targetPhase}_started");
            }
            if (LocationController.Instance != null)
            {
                LocationController.Instance.RefreshAll();
            }
            yield break;
        }

        public override string GetSummary() => $"Establecer: Día {targetDay}, Fase {targetPhase}";
    }
}
