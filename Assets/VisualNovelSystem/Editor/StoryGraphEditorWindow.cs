using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VisualNovelSystem;

namespace VisualNovelSystem.Editor
{
    public class StoryGraphEditorWindow : EditorWindow
    {
        [SerializeField] private StoryGraph currentGraph;

        private StoryGraphView graphView;
        private MiniMap miniMap;
        private ObjectField graphAssetField;
        private Label graphNameLabel;

        [MenuItem("Window/Visual Novel/Story Graph Editor", false, 100)]
        public static void OpenWindow()
        {
            var window = GetWindow<StoryGraphEditorWindow>("Story Graph Editor");
            window.minSize = new Vector2(800, 500);
        }

        public static void OpenGraph(StoryGraph graph)
        {
            var window = GetWindow<StoryGraphEditorWindow>("Story Graph Editor");
            window.LoadTargetGraph(graph);
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            GenerateMiniMap();

            if (currentGraph != null)
            {
                LoadTargetGraph(currentGraph);
            }

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            if (graphView != null)
            {
                rootVisualElement.Remove(graphView);
            }
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void Update()
        {
            if (Application.isPlaying && graphView != null)
            {
                var runner = UnityEngine.Object.FindAnyObjectByType<StoryRunner>();
                if (runner != null && runner.IsRunning && runner.CurrentNode != null)
                {
                    graphView.HighlightActiveNode(runner.CurrentNode.guid);
                }
                else
                {
                    graphView.HighlightActiveNode(null);
                }
            }
        }

        private void ConstructGraphView()
        {
            graphView = new StoryGraphView
            {
                name = "Story Graph View"
            };

            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            graphView.OnGraphModified += () =>
            {
                if (currentGraph != null)
                {
                    graphView.SaveToGraph(currentGraph);
                }
            };
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            // New Graph Button
            var newBtn = new Button(CreateNewGraph) { text = "New Graph" };
            toolbar.Add(newBtn);

            // Asset Field
            graphAssetField = new ObjectField("Graph Asset:")
            {
                objectType = typeof(StoryGraph),
                value = currentGraph
            };
            graphAssetField.RegisterValueChangedCallback(evt =>
            {
                LoadTargetGraph(evt.newValue as StoryGraph);
            });
            toolbar.Add(graphAssetField);

            // Save Button
            var saveBtn = new Button(SaveGraph) { text = "Save Graph" };
            toolbar.Add(saveBtn);

            // Frame / Center All
            var centerBtn = new Button(() => graphView.FrameAll()) { text = "Center View" };
            toolbar.Add(centerBtn);

            // Create Scene Setup Button
            var sceneSetupBtn = new Button(StorySceneSetupWizard.CreateSceneSetup) { text = "Setup Scene UI & Runner" };
            toolbar.Add(sceneSetupBtn);

            rootVisualElement.Add(toolbar);
        }

        private void GenerateMiniMap()
        {
            miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 30, 200, 140));
            graphView.Add(miniMap);
        }

        public void LoadTargetGraph(StoryGraph targetGraph)
        {
            currentGraph = targetGraph;
            if (graphAssetField != null) graphAssetField.value = targetGraph;

            if (currentGraph != null)
            {
                graphView.PopulateView(currentGraph);
                titleContent = new GUIContent($"{currentGraph.name} - Story Graph");
            }
            else
            {
                graphView.PopulateView(null);
                titleContent = new GUIContent("Story Graph Editor");
            }
        }

        private void SaveGraph()
        {
            if (currentGraph != null)
            {
                graphView.SaveToGraph(currentGraph);
                ShowNotification(new GUIContent("Graph Saved!"));
            }
            else
            {
                ShowNotification(new GUIContent("No Graph Loaded to Save!"));
            }
        }

        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Story Graph", "NewStoryGraph", "asset", "Save new Story Graph asset");
            if (string.IsNullOrEmpty(path)) return;

            var newGraph = ScriptableObject.CreateInstance<StoryGraph>();
            newGraph.graphTitle = System.IO.Path.GetFileNameWithoutExtension(path);

            // Add default start node
            var startNode = new StoryNodeData(StoryNodeType.Start, new Vector2(100, 200));
            newGraph.nodes.Add(startNode);
            newGraph.entryNodeGuid = startNode.guid;

            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.SaveAssets();

            LoadTargetGraph(newGraph);
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && graphView != null)
            {
                graphView.HighlightActiveNode(null);
            }
        }
    }
}
