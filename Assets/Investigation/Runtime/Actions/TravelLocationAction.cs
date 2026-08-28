using System;
using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    [Serializable]
    public class TravelLocationAction : StoryAction
    {
        [SerializeField] public string targetLocationId = "";

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (!string.IsNullOrEmpty(targetLocationId) && LocationController.Instance != null)
            {
                var fader = runner != null && runner.UIController != null ? runner.UIController.Fader : null;
                bool isAlreadyBlack = fader != null && fader.IsBlack;
                LocationController.Instance.GoTo(targetLocationId, isAlreadyBlack);
            }
            yield break;
        }

        public override string GetSummary() => $"Viajar a: {targetLocationId}";
    }
}
