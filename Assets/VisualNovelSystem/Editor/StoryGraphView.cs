using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VisualNovelSystem;

namespace VisualNovelSystem.Editor
{
    public class StoryGraphView : GraphView
    {
        public readonly Vector2 DefaultNodeSize = new Vector2(300, 200);
        public StoryGraph CurrentGraph { get; private set; }

        private StorySearchWindow searchWindow;
        private List<StoryNodeView> nodeViews = new List<StoryNodeView>();

        public Action OnGraphModified;

        public StoryGraphView()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/VisualNovelSystem/Editor/Styles/StoryGraphStyles.uss"));

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddSearchWindow();
        }

        private void AddSearchWindow()
        {
            searchWindow = ScriptableObject.CreateInstance<StorySearchWindow>();
            searchWindow.Initialize(this);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }

        public StoryNodeView CreateNode(StoryNodeType nodeType, Vector2 position, StoryNodeData existingData = null)
        {
            StoryNodeData data = existingData;
            if (data == null)
            {
                data = new StoryNodeData(nodeType, position);
                if (nodeType == StoryNodeType.Choice)
                {
                    data.choices.Add(new StoryChoiceOption { text = "Opción 1" });
                    data.choices.Add(new StoryChoiceOption { text = "Opción 2" });
                }
                else if (nodeType == StoryNodeType.ActionSequence)
                {
                    data.actions.Add(new DialogueAction());
                }

                if (CurrentGraph != null)
                {
                    CurrentGraph.nodes.Add(data);
                    EditorUtility.SetDirty(CurrentGraph);
                }
            }

            var nodeView = new StoryNodeView(data, () =>
            {
                if (CurrentGraph != null) EditorUtility.SetDirty(CurrentGraph);
                OnGraphModified?.Invoke();
            });

            nodeView.SetPosition(new Rect(position, DefaultNodeSize));
            AddElement(nodeView);
            nodeViews.Add(nodeView);

            return nodeView;
        }

        public void PopulateView(StoryGraph graph)
        {
            CurrentGraph = graph;

            // Clear existing elements
            graphElements.ForEach(RemoveElement);
            nodeViews.Clear();

            if (CurrentGraph == null) return;

            // 1. Create Node Views
            Dictionary<string, StoryNodeView> guidToViewMap = new Dictionary<string, StoryNodeView>();

            foreach (var nodeData in CurrentGraph.nodes)
            {
                var view = CreateNode(nodeData.nodeType, nodeData.position, nodeData);
                guidToViewMap[nodeData.guid] = view;
            }

            // 2. Create Connections / Edges
            foreach (var link in CurrentGraph.nodeLinks)
            {
                if (guidToViewMap.TryGetValue(link.baseNodeGuid, out var baseView) &&
                    guidToViewMap.TryGetValue(link.targetNodeGuid, out var targetView))
                {
                    Port outputPort = GetOutputPort(baseView, link.portIdentifier);
                    Port inputPort = targetView.InputPort;

                    if (outputPort != null && inputPort != null)
                    {
                        var edge = outputPort.ConnectTo(inputPort);
                        AddElement(edge);
                    }
                }
            }
        }

        private Port GetOutputPort(StoryNodeView nodeView, string portIdentifier)
        {
            if (nodeView == null) return null;

            if (portIdentifier == "output" || portIdentifier == "Out")
            {
                return nodeView.OutputPort;
            }
            else if (portIdentifier == "true" || portIdentifier == "True")
            {
                return nodeView.TruePort;
            }
            else if (portIdentifier == "false" || portIdentifier == "False")
            {
                return nodeView.FalsePort;
            }
            else if (nodeView.ChoicePorts.TryGetValue(portIdentifier, out var choicePort))
            {
                return choicePort;
            }
            else if (portIdentifier.StartsWith("choice_"))
            {
                int idx = 0;
                if (int.TryParse(portIdentifier.Substring(7), out idx) && idx < nodeView.ChoicePorts.Count)
                {
                    return nodeView.ChoicePorts.Values.ElementAt(idx);
                }
            }

            return nodeView.OutputPort;
        }

        public void SaveToGraph(StoryGraph targetGraph)
        {
            if (targetGraph == null) return;

            Undo.RecordObject(targetGraph, "Save Story Graph");

            targetGraph.nodes.Clear();
            targetGraph.nodeLinks.Clear();

            // Save nodes & positions
            foreach (var view in nodeViews)
            {
                view.NodeData.position = view.GetPosition().position;
                targetGraph.nodes.Add(view.NodeData);

                if (view.NodeData.nodeType == StoryNodeType.Start)
                {
                    targetGraph.entryNodeGuid = view.NodeData.guid;
                }
            }

            // Save edges
            var allEdges = edges.ToList();
            foreach (var edge in allEdges)
            {
                var outputNode = edge.output.node as StoryNodeView;
                var inputNode = edge.input.node as StoryNodeView;

                if (outputNode != null && inputNode != null)
                {
                    string portId = GetPortIdentifier(outputNode, edge.output);
                    targetGraph.nodeLinks.Add(new NodeLinkData(outputNode.NodeData.guid, portId, inputNode.NodeData.guid));
                }
            }

            EditorUtility.SetDirty(targetGraph);
            AssetDatabase.SaveAssets();
        }

        private string GetPortIdentifier(StoryNodeView nodeView, Port port)
        {
            if (port == nodeView.OutputPort) return "output";
            if (port == nodeView.TruePort) return "true";
            if (port == nodeView.FalsePort) return "false";

            foreach (var kvp in nodeView.ChoicePorts)
            {
                if (kvp.Value == port) return kvp.Key;
            }

            return "output";
        }

        public void HighlightActiveNode(string activeNodeGuid)
        {
            foreach (var view in nodeViews)
            {
                bool isActive = view.NodeData != null && view.NodeData.guid == activeNodeGuid;
                view.SetActiveHighlight(isActive);
            }
        }
    }
}
