using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VisualNovelSystem;

namespace VisualNovelSystem.Editor
{
    public class StorySearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private StoryGraphView graphView;
        private Texture2D indentationIcon;

        public void Initialize(StoryGraphView view)
        {
            graphView = view;
            indentationIcon = new Texture2D(1, 1);
            indentationIcon.SetPixel(0, 0, Color.clear);
            indentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Story Node"), 0),
                new SearchTreeGroupEntry(new GUIContent("Story Flow"), 1),

                new SearchTreeEntry(new GUIContent("Start Node", indentationIcon))
                {
                    level = 2,
                    userData = StoryNodeType.Start
                },
                new SearchTreeEntry(new GUIContent("Action Sequence Node (Events/Animations/Dialogue)", indentationIcon))
                {
                    level = 2,
                    userData = StoryNodeType.ActionSequence
                },
                new SearchTreeEntry(new GUIContent("Choice / Branch Node", indentationIcon))
                {
                    level = 2,
                    userData = StoryNodeType.Choice
                },
                new SearchTreeEntry(new GUIContent("Condition / Logic Node", indentationIcon))
                {
                    level = 2,
                    userData = StoryNodeType.Condition
                },
                new SearchTreeEntry(new GUIContent("Wait Node", indentationIcon))
                {
                    level = 2,
                    userData = StoryNodeType.Wait
                },
                new SearchTreeEntry(new GUIContent("Exploration Mode Node (Point & Click / Hotspots)", indentationIcon))
                {
                    level = 2,
                    userData = StoryNodeType.Exploration
                },
                new SearchTreeEntry(new GUIContent("End Node", indentationIcon))
                {
                    level = 2,
                    userData = StoryNodeType.End
                }
            };

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is StoryNodeType nodeType)
            {
                var worldMousePos = context.screenMousePosition;
                var windowRoot = graphView.panel.visualTree;
                var windowMousePos = windowRoot.WorldToLocal(worldMousePos);
                var graphMousePos = graphView.contentViewContainer.WorldToLocal(windowMousePos);

                graphView.CreateNode(nodeType, graphMousePos);
                return true;
            }

            return false;
        }
    }
}
