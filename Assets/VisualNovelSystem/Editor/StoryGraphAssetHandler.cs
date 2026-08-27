using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using VisualNovelSystem;

namespace VisualNovelSystem.Editor
{
    public static class StoryGraphAssetHandler
    {
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var selected = Selection.activeObject as StoryGraph;
            if (selected != null)
            {
                StoryGraphEditorWindow.OpenGraph(selected);
                return true;
            }

            return false;
        }
    }
}
