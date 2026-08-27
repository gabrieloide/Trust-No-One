using System;
using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    [Serializable]
    public class SpendActionAction : StoryAction
    {
        public override IEnumerator Execute(StoryRunner runner)
        {
            if (PhaseController.Instance != null)
            {
                PhaseController.Instance.SpendAction();
            }
            yield break;
        }

        public override string GetSummary() => "Gastar 1 Acción del Caso";
    }
}
