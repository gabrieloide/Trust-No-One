using System;
using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    [Serializable]
    public class CollectClueAction : StoryAction
    {
        [SerializeField] public string clueId = "";
        [SerializeField] public bool playSound = true;

        public override IEnumerator Execute(StoryRunner runner)
        {
            if (!string.IsNullOrEmpty(clueId) && CaseState.Instance != null)
            {
                CaseState.Instance.CollectClue(clueId, playSound);
            }
            yield break;
        }

        public override string GetSummary() => $"Recolectar Pista: {clueId}";
    }
}
