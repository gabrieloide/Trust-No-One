using System;
using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    [Serializable]
    public class SetWorldUIAction : StoryAction
    {
        [SerializeField] public bool active = true;

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (LocationController.Instance != null)
            {
                LocationController.Instance.SetWorldUIActive(active);
            }
            yield break;
        }

        public override string GetSummary() => $"World UI: {(active ? "Activo" : "Oculto")}";
    }
}
