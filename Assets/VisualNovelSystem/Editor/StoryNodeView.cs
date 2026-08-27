using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VisualNovelSystem;

namespace VisualNovelSystem.Editor
{
    public class StoryNodeView : Node
    {
        public StoryNodeData NodeData { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }
        public Dictionary<string, Port> ChoicePorts { get; private set; } = new Dictionary<string, Port>();
        public Port TruePort { get; private set; }
        public Port FalsePort { get; private set; }

        private VisualElement actionListContainer;
        private VisualElement customBodyContainer;
        private Action onNodeModified;

        public StoryNodeView(StoryNodeData nodeData, Action onModifiedCallback = null)
        {
            NodeData = nodeData;
            onNodeModified = onModifiedCallback;
            viewDataKey = nodeData.guid;

            style.left = nodeData.position.x;
            style.top = nodeData.position.y;

            AddToClassList("story-node");
            SetupHeader();
            SetupPorts();
            SetupBody();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void SetupHeader()
        {
            title = $"{NodeData.nodeType}: {NodeData.title}";

            string headerClass = NodeData.nodeType switch
            {
                StoryNodeType.Start => "node-header-start",
                StoryNodeType.ActionSequence => "node-header-action",
                StoryNodeType.Choice => "node-header-choice",
                StoryNodeType.Condition => "node-header-condition",
                StoryNodeType.Wait => "node-header-wait",
                StoryNodeType.Exploration => "node-header-exploration",
                StoryNodeType.End => "node-header-end",
                _ => "node-header-action"
            };

            titleContainer.AddToClassList(headerClass);
        }

        private void SetupPorts()
        {
            // Input Port (all except Start)
            if (NodeData.nodeType != StoryNodeType.Start)
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                InputPort.portName = "In";
                inputContainer.Add(InputPort);
            }

            // Output Ports
            switch (NodeData.nodeType)
            {
                case StoryNodeType.Start:
                case StoryNodeType.ActionSequence:
                case StoryNodeType.Wait:
                case StoryNodeType.Exploration:
                    OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    OutputPort.portName = "Out";
                    outputContainer.Add(OutputPort);
                    break;

                case StoryNodeType.Choice:
                    SetupChoicePorts();
                    break;

                case StoryNodeType.Condition:
                    TruePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    TruePort.portName = "True";
                    outputContainer.Add(TruePort);

                    FalsePort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    FalsePort.portName = "False";
                    outputContainer.Add(FalsePort);
                    break;

                case StoryNodeType.End:
                    // No output ports
                    break;
            }
        }

        private void SetupChoicePorts()
        {
            ChoicePorts.Clear();
            outputContainer.Clear();

            if (NodeData.choices == null) NodeData.choices = new List<StoryChoiceOption>();

            for (int i = 0; i < NodeData.choices.Count; i++)
            {
                var choice = NodeData.choices[i];
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                port.portName = string.IsNullOrEmpty(choice.text) ? $"Option {i + 1}" : choice.text;
                ChoicePorts[choice.id] = port;
                outputContainer.Add(port);
            }
        }

        private void SetupBody()
        {
            customBodyContainer = new VisualElement();
            customBodyContainer.style.paddingLeft = 8;
            customBodyContainer.style.paddingRight = 8;
            customBodyContainer.style.paddingTop = 6;
            customBodyContainer.style.paddingBottom = 6;

            // Title field
            var titleField = new TextField("Node Title") { value = NodeData.title };
            titleField.RegisterValueChangedCallback(evt =>
            {
                NodeData.title = evt.newValue;
                title = $"{NodeData.nodeType}: {NodeData.title}";
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(titleField);

            switch (NodeData.nodeType)
            {
                case StoryNodeType.ActionSequence:
                    SetupActionSequenceUI();
                    break;

                case StoryNodeType.Choice:
                    SetupChoiceUI();
                    break;

                case StoryNodeType.Condition:
                    SetupConditionUI();
                    break;

                case StoryNodeType.Wait:
                    SetupWaitUI();
                    break;

                case StoryNodeType.Exploration:
                    SetupExplorationUI();
                    break;
            }

            extensionContainer.Add(customBodyContainer);
        }

        private void SetupActionSequenceUI()
        {
            actionListContainer = new VisualElement();
            customBodyContainer.Add(actionListContainer);
            RebuildActionList();

            var addActionBtn = new Button(ShowAddActionMenu) { text = "+ Add Action" };
            addActionBtn.AddToClassList("add-action-btn");
            customBodyContainer.Add(addActionBtn);
        }

        private void RebuildActionList()
        {
            actionListContainer.Clear();

            if (NodeData.actions == null) NodeData.actions = new List<StoryAction>();

            for (int i = 0; i < NodeData.actions.Count; i++)
            {
                int index = i;
                var action = NodeData.actions[i];
                if (action == null) continue;

                var card = new VisualElement();
                card.AddToClassList("action-card");

                // Header row
                var headerRow = new VisualElement();
                headerRow.AddToClassList("action-card-header");

                var toggle = new Toggle { value = action.enabled };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    action.enabled = evt.newValue;
                    onNodeModified?.Invoke();
                });
                headerRow.Add(toggle);

                var titleLabel = new Label(action.GetSummary());
                titleLabel.AddToClassList("action-card-title");
                headerRow.Add(titleLabel);

                // Move Up
                if (index > 0)
                {
                    var upBtn = new Button(() =>
                    {
                        var temp = NodeData.actions[index];
                        NodeData.actions[index] = NodeData.actions[index - 1];
                        NodeData.actions[index - 1] = temp;
                        RebuildActionList();
                        onNodeModified?.Invoke();
                    }) { text = "▲" };
                    upBtn.AddToClassList("action-button");
                    headerRow.Add(upBtn);
                }

                // Move Down
                if (index < NodeData.actions.Count - 1)
                {
                    var downBtn = new Button(() =>
                    {
                        var temp = NodeData.actions[index];
                        NodeData.actions[index] = NodeData.actions[index + 1];
                        NodeData.actions[index + 1] = temp;
                        RebuildActionList();
                        onNodeModified?.Invoke();
                    }) { text = "▼" };
                    downBtn.AddToClassList("action-button");
                    headerRow.Add(downBtn);
                }

                // Delete
                var deleteBtn = new Button(() =>
                {
                    NodeData.actions.RemoveAt(index);
                    RebuildActionList();
                    onNodeModified?.Invoke();
                }) { text = "✕" };
                deleteBtn.AddToClassList("action-button");
                deleteBtn.AddToClassList("action-button-danger");
                headerRow.Add(deleteBtn);

                card.Add(headerRow);

                // Inspector Foldout for Action Fields
                var foldout = new Foldout { text = "Parameters", value = false };
                var imguiInspector = new IMGUIContainer(() =>
                {
                    DrawActionIMGUI(action, titleLabel);
                });
                foldout.Add(imguiInspector);
                card.Add(foldout);

                actionListContainer.Add(card);
            }
        }

        private void DrawActionIMGUI(StoryAction action, Label titleLabel)
        {
            EditorGUI.BeginChangeCheck();

            switch (action)
            {
                case FadeScreenAction fade:
                    fade.fadeType = (FadeType)EditorGUILayout.EnumPopup("Fade Type", fade.fadeType);
                    fade.fadeColor = EditorGUILayout.ColorField("Color", fade.fadeColor);
                    fade.duration = EditorGUILayout.FloatField("Duration (s)", fade.duration);
                    fade.curve = EditorGUILayout.CurveField("Curve", fade.curve);
                    fade.waitForCompletion = EditorGUILayout.Toggle("Wait Complete", fade.waitForCompletion);
                    break;

                case ChangeSceneAction scene:
                    scene.changeType = (SceneChangeType)EditorGUILayout.EnumPopup("Change Type", scene.changeType);
                    if (scene.changeType == SceneChangeType.UnityScene)
                    {
                        scene.sceneName = EditorGUILayout.TextField("Scene Name", scene.sceneName);
                    }
                    else
                    {
                        scene.newBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("BG Sprite", scene.newBackgroundSprite, typeof(Sprite), false);
                        scene.backgroundObjectName = EditorGUILayout.TextField("Target Object", scene.backgroundObjectName);
                    }
                    scene.fadeOut = EditorGUILayout.Toggle("Fade Out", scene.fadeOut);
                    if (scene.fadeOut) scene.fadeOutDuration = EditorGUILayout.FloatField("Fade Out (s)", scene.fadeOutDuration);
                    scene.fadeIn = EditorGUILayout.Toggle("Fade In", scene.fadeIn);
                    if (scene.fadeIn) scene.fadeInDuration = EditorGUILayout.FloatField("Fade In (s)", scene.fadeInDuration);
                    scene.fadeColor = EditorGUILayout.ColorField("Fade Color", scene.fadeColor);
                    break;

                case OverlayTextAction overlay:
                    EditorGUILayout.LabelField("Title Text:");
                    overlay.titleText = EditorGUILayout.TextArea(overlay.titleText, GUILayout.MinHeight(35));
                    EditorGUILayout.LabelField("Subtitle Text:");
                    overlay.subtitleText = EditorGUILayout.TextArea(overlay.subtitleText, GUILayout.MinHeight(25));
                    overlay.displayMode = (OverlayDisplayMode)EditorGUILayout.EnumPopup("Position", overlay.displayMode);
                    overlay.effect = (OverlayEffect)EditorGUILayout.EnumPopup("Effect", overlay.effect);
                    overlay.duration = EditorGUILayout.FloatField("Duration (s)", overlay.duration);
                    overlay.waitForClick = EditorGUILayout.Toggle("Wait Click", overlay.waitForClick);
                    overlay.fadeDuration = EditorGUILayout.FloatField("Fade Speed (s)", overlay.fadeDuration);
                    break;

                case DialogueAction diag:
                    diag.speakerName = EditorGUILayout.TextField("Speaker", diag.speakerName);
                    EditorGUILayout.LabelField("Dialogue Text:");
                    diag.dialogueText = EditorGUILayout.TextArea(diag.dialogueText, GUILayout.MinHeight(45));
                    diag.characterPortrait = (Sprite)EditorGUILayout.ObjectField("Portrait", diag.characterPortrait, typeof(Sprite), false);
                    diag.voiceClip = (AudioClip)EditorGUILayout.ObjectField("Voice Clip", diag.voiceClip, typeof(AudioClip), false);
                    diag.typewriterSpeed = EditorGUILayout.FloatField("Typewriter Speed", diag.typewriterSpeed);
                    diag.waitForClick = EditorGUILayout.Toggle("Wait for Click", diag.waitForClick);
                    diag.hideAfterFinished = EditorGUILayout.Toggle("Hide Box After", diag.hideAfterFinished);
                    break;

                case PlayAnimationAction anim:
                    anim.targetObjectName = EditorGUILayout.TextField("Target Object", anim.targetObjectName);
                    anim.actionType = (AnimationActionType)EditorGUILayout.EnumPopup("Action Type", anim.actionType);
                    anim.parameterOrStateName = EditorGUILayout.TextField("Param/State", anim.parameterOrStateName);
                    if (anim.actionType == AnimationActionType.SetBool) anim.boolValue = EditorGUILayout.Toggle("Bool Value", anim.boolValue);
                    if (anim.actionType == AnimationActionType.SetInteger) anim.intValue = EditorGUILayout.IntField("Int Value", anim.intValue);
                    if (anim.actionType == AnimationActionType.SetFloat) anim.floatValue = EditorGUILayout.FloatField("Float Value", anim.floatValue);
                    anim.waitForSeconds = EditorGUILayout.Toggle("Wait Duration", anim.waitForSeconds);
                    if (anim.waitForSeconds) anim.waitDuration = EditorGUILayout.FloatField("Duration (s)", anim.waitDuration);
                    break;

                case TweenTransformAction tween:
                    tween.targetObjectName = EditorGUILayout.TextField("Target Object", tween.targetObjectName);
                    tween.tweenType = (TweenType)EditorGUILayout.EnumPopup("Tween Type", tween.tweenType);
                    if (tween.tweenType == TweenType.FadeAlpha)
                        tween.targetAlpha = EditorGUILayout.Slider("Target Alpha", tween.targetAlpha, 0f, 1f);
                    else
                        tween.targetVector = EditorGUILayout.Vector3Field("Target Value", tween.targetVector);
                    tween.duration = EditorGUILayout.FloatField("Duration (s)", tween.duration);
                    tween.curve = EditorGUILayout.CurveField("Easing Curve", tween.curve);
                    tween.waitForCompletion = EditorGUILayout.Toggle("Wait Complete", tween.waitForCompletion);
                    break;

                case UnityEventAction evt:
                    evt.triggerMode = (EventTriggerMode)EditorGUILayout.EnumPopup("Trigger Mode", evt.triggerMode);
                    if (evt.triggerMode == EventTriggerMode.NamedSceneEvent)
                    {
                        evt.eventName = EditorGUILayout.TextField("Event Name", evt.eventName);
                    }
                    else
                    {
                        evt.targetObjectName = EditorGUILayout.TextField("Target Object", evt.targetObjectName);
                        evt.methodName = EditorGUILayout.TextField("Method Name", evt.methodName);
                        evt.methodParameter = EditorGUILayout.TextField("Parameter (opt)", evt.methodParameter);
                    }
                    break;

                case PlayAudioAction audio:
                    audio.audioType = (StoryAudioType)EditorGUILayout.EnumPopup("Audio Type", audio.audioType);
                    if (audio.audioType != StoryAudioType.StopBGM)
                    {
                        audio.audioClip = (AudioClip)EditorGUILayout.ObjectField("Clip", audio.audioClip, typeof(AudioClip), false);
                        audio.volume = EditorGUILayout.Slider("Volume", audio.volume, 0f, 1f);
                    }
                    if (audio.audioType == StoryAudioType.BGM)
                    {
                        audio.loop = EditorGUILayout.Toggle("Loop", audio.loop);
                        audio.bgmFadeDuration = EditorGUILayout.FloatField("Fade Time (s)", audio.bgmFadeDuration);
                    }
                    break;

                case WaitAction wait:
                    wait.duration = EditorGUILayout.FloatField("Wait Seconds", wait.duration);
                    break;

                case SetVariableAction setVar:
                    setVar.variableName = EditorGUILayout.TextField("Variable Name", setVar.variableName);
                    setVar.variableType = (VariableType)EditorGUILayout.EnumPopup("Type", setVar.variableType);
                    setVar.operation = (VariableOperation)EditorGUILayout.EnumPopup("Operation", setVar.operation);
                    setVar.stringValue = EditorGUILayout.TextField("Value", setVar.stringValue);
                    break;

                case SetInteractableStateAction inter:
                    inter.targetObjectName = EditorGUILayout.TextField("Target Object", inter.targetObjectName);
                    inter.setInteractable = EditorGUILayout.Toggle("Set Interactable", inter.setInteractable);
                    inter.modifyGameObjectActive = EditorGUILayout.Toggle("Modify Active State", inter.modifyGameObjectActive);
                    if (inter.modifyGameObjectActive) inter.isGameObjectActive = EditorGUILayout.Toggle("Is Active", inter.isGameObjectActive);
                    break;

                case SetCursorAction cursor:
                    cursor.cursorType = (CursorIconType)EditorGUILayout.EnumPopup("Cursor Type", cursor.cursorType);
                    if (cursor.cursorType == CursorIconType.Custom)
                        cursor.customTexture = (Texture2D)EditorGUILayout.ObjectField("Custom Texture", cursor.customTexture, typeof(Texture2D), false);
                    break;

                default:
                    // Generic reflection drawing for custom user actions
                    DrawGenericActionFields(action);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (titleLabel != null) titleLabel.text = action.GetSummary();
                onNodeModified?.Invoke();
            }
        }

        private void DrawGenericActionFields(StoryAction action)
        {
            var fields = action.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.Name == "enabled") continue;
                object val = f.GetValue(action);

                if (f.FieldType == typeof(string))
                {
                    string strVal = EditorGUILayout.TextField(ObjectNames.NicifyVariableName(f.Name), (string)val);
                    f.SetValue(action, strVal);
                }
                else if (f.FieldType == typeof(int))
                {
                    int iVal = EditorGUILayout.IntField(ObjectNames.NicifyVariableName(f.Name), (int)(val ?? 0));
                    f.SetValue(action, iVal);
                }
                else if (f.FieldType == typeof(float))
                {
                    float flVal = EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(f.Name), (float)(val ?? 0f));
                    f.SetValue(action, flVal);
                }
                else if (f.FieldType == typeof(bool))
                {
                    bool bVal = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(f.Name), (bool)(val ?? false));
                    f.SetValue(action, bVal);
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                {
                    var objVal = EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(f.Name), (UnityEngine.Object)val, f.FieldType, false);
                    f.SetValue(action, objVal);
                }
            }
        }

        private void ShowAddActionMenu()
        {
            GenericMenu menu = new GenericMenu();

            var actionTypes = TypeCache.GetTypesDerivedFrom<StoryAction>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name);

            foreach (var type in actionTypes)
            {
                string category = "Actions";
                if (type.Name.Contains("Fade") || type.Name.Contains("Scene") || type.Name.Contains("Overlay"))
                    category = "Transitions & Screen";
                else if (type.Name.Contains("Dialogue"))
                    category = "Dialogue";
                else if (type.Name.Contains("Animation") || type.Name.Contains("Tween"))
                    category = "Animation & Visuals";
                else if (type.Name.Contains("Audio"))
                    category = "Audio";
                else if (type.Name.Contains("Interactable") || type.Name.Contains("Cursor") || type.Name.Contains("Drag"))
                    category = "Point & Click";
                else if (type.Name.Contains("Event") || type.Name.Contains("Variable"))
                    category = "Logic & Events";

                string menuPath = $"{category}/{ObjectNames.NicifyVariableName(type.Name)}";

                menu.AddItem(new GUIContent(menuPath), false, () =>
                {
                    var newAction = (StoryAction)Activator.CreateInstance(type);
                    if (NodeData.actions == null) NodeData.actions = new List<StoryAction>();
                    NodeData.actions.Add(newAction);
                    RebuildActionList();
                    onNodeModified?.Invoke();
                });
            }

            menu.ShowAsContext();
        }

        private void SetupChoiceUI()
        {
            var promptField = new TextField("Prompt Text") { value = NodeData.promptText };
            promptField.RegisterValueChangedCallback(evt =>
            {
                NodeData.promptText = evt.newValue;
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(promptField);

            var choicesContainer = new VisualElement();
            customBodyContainer.Add(choicesContainer);

            Action rebuildChoices = () =>
            {
                choicesContainer.Clear();
                for (int i = 0; i < NodeData.choices.Count; i++)
                {
                    int index = i;
                    var choice = NodeData.choices[i];

                    var choiceRow = new VisualElement();
                    choiceRow.style.flexDirection = FlexDirection.Row;
                    choiceRow.style.marginTop = 2;
                    choiceRow.style.marginBottom = 2;

                    var textField = new TextField($"Choice {index + 1}") { value = choice.text };
                    textField.style.flexGrow = 1;
                    textField.RegisterValueChangedCallback(evt =>
                    {
                        choice.text = evt.newValue;
                        if (ChoicePorts.TryGetValue(choice.id, out Port p))
                        {
                            p.portName = evt.newValue;
                        }
                        onNodeModified?.Invoke();
                    });
                    choiceRow.Add(textField);

                    var removeBtn = new Button(() =>
                    {
                        NodeData.choices.RemoveAt(index);
                        SetupChoicePorts();
                        RefreshPorts();
                        onNodeModified?.Invoke();
                        // Re-trigger rebuild
                        SetupBody();
                    }) { text = "✕" };
                    removeBtn.AddToClassList("action-button-danger");
                    choiceRow.Add(removeBtn);

                    choicesContainer.Add(choiceRow);
                }
            };

            rebuildChoices();

            var addChoiceBtn = new Button(() =>
            {
                if (NodeData.choices == null) NodeData.choices = new List<StoryChoiceOption>();
                NodeData.choices.Add(new StoryChoiceOption { text = $"Option {NodeData.choices.Count + 1}" });
                SetupChoicePorts();
                RefreshPorts();
                onNodeModified?.Invoke();
                rebuildChoices();
            }) { text = "+ Add Choice Option" };
            addChoiceBtn.AddToClassList("add-action-btn");
            customBodyContainer.Add(addChoiceBtn);
        }

        private void SetupConditionUI()
        {
            var varField = new TextField("Variable Name") { value = NodeData.conditionVariableName };
            varField.RegisterValueChangedCallback(evt =>
            {
                NodeData.conditionVariableName = evt.newValue;
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(varField);

            var compField = new EnumField("Comparison", NodeData.conditionComparison);
            compField.RegisterValueChangedCallback(evt =>
            {
                NodeData.conditionComparison = (ConditionComparison)evt.newValue;
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(compField);

            var valField = new TextField("Compare Value") { value = NodeData.conditionCompareValue };
            valField.RegisterValueChangedCallback(evt =>
            {
                NodeData.conditionCompareValue = evt.newValue;
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(valField);
        }

        private void SetupWaitUI()
        {
            var durField = new FloatField("Duration (s)") { value = NodeData.waitDuration };
            durField.RegisterValueChangedCallback(evt =>
            {
                NodeData.waitDuration = evt.newValue;
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(durField);
        }

        private void SetupExplorationUI()
        {
            var promptField = new TextField("Exploration Prompt") { value = NodeData.explorationPrompt };
            promptField.RegisterValueChangedCallback(evt =>
            {
                NodeData.explorationPrompt = evt.newValue;
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(promptField);

            var exitVarField = new TextField("Exit Condition Var (opt)") { value = NodeData.exitConditionVariable };
            exitVarField.RegisterValueChangedCallback(evt =>
            {
                NodeData.exitConditionVariable = evt.newValue;
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(exitVarField);

            var timeoutField = new FloatField("Timeout (s, -1 infinite)") { value = NodeData.explorationTimeout };
            timeoutField.RegisterValueChangedCallback(evt =>
            {
                NodeData.explorationTimeout = evt.newValue;
                onNodeModified?.Invoke();
            });
            customBodyContainer.Add(timeoutField);
        }

        public void SetActiveHighlight(bool active)
        {
            if (active)
            {
                AddToClassList("node-active");
            }
            else
            {
                RemoveFromClassList("node-active");
            }
        }
    }
}
