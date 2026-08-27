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
                LocationController.Instance.GoTo(targetLocationId);
            }
            yield break;
        }

        public override string GetSummary() => $"Viajar a: {targetLocationId}";
    }
}
