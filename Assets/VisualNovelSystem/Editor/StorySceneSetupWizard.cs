using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VisualNovelSystem;

namespace VisualNovelSystem.Editor
{
    public static class StorySceneSetupWizard
    {
        [MenuItem("GameObject/Visual Novel/Setup Visual Novel in Scene", false, 10)]
        public static void CreateSceneSetup()
        {
            // 1. Create or Find Canvas
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            GameObject canvasObj = null;

            if (canvas == null)
            {
                canvasObj = new GameObject("VisualNovel_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                canvasObj = canvas.gameObject;
            }

            Undo.RegisterCreatedObjectUndo(canvasObj, "Setup Visual Novel in Scene");

            // 2. Create StoryUIController Root
            // RectTransform es obligatorio acá: sin él, este objeto queda con un Transform
            // común y todos los hijos con anclaje "stretch" (DialogueBox, ChoicesPanel...)
            // colapsan a tamaño 0 porque no tienen un rect de padre del que estirarse,
            // aunque sus propios anchors/sizeDelta se vean normales en el Inspector.
            var uiControllerObj = new GameObject("StoryUIController", typeof(RectTransform), typeof(StoryUIController));
            uiControllerObj.transform.SetParent(canvasObj.transform, false);
            var uiControllerRect = uiControllerObj.GetComponent<RectTransform>();
            uiControllerRect.anchorMin = Vector2.zero;
            uiControllerRect.anchorMax = Vector2.one;
            uiControllerRect.offsetMin = Vector2.zero;
            uiControllerRect.offsetMax = Vector2.zero;
            var uiController = uiControllerObj.GetComponent<StoryUIController>();

            // 3. Create Dialogue UI
            var dialogueObj = new GameObject("DialogueBox", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(StoryDialogueUI));
            dialogueObj.transform.SetParent(uiControllerObj.transform, false);
            var diagRect = dialogueObj.GetComponent<RectTransform>();
            diagRect.anchorMin = new Vector2(0.1f, 0.05f);
            diagRect.anchorMax = new Vector2(0.9f, 0.32f);
            diagRect.offsetMin = Vector2.zero;
            diagRect.offsetMax = Vector2.zero;

            var diagImage = dialogueObj.GetComponent<Image>();
            diagImage.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

            // Portrait
            var portraitObj = new GameObject("CharacterPortrait", typeof(RectTransform), typeof(Image));
            portraitObj.transform.SetParent(dialogueObj.transform, false);
            var portRect = portraitObj.GetComponent<RectTransform>();
            portRect.anchorMin = new Vector2(0.02f, 0.1f);
            portRect.anchorMax = new Vector2(0.18f, 0.9f);
            portRect.offsetMin = Vector2.zero;
            portRect.offsetMax = Vector2.zero;
            portraitObj.GetComponent<Image>().preserveAspect = true;

            // Speaker Name
            var speakerObj = new GameObject("SpeakerName", typeof(RectTransform), typeof(TextMeshProUGUI));
            speakerObj.transform.SetParent(dialogueObj.transform, false);
            var speakerRect = speakerObj.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0.2f, 0.72f);
            speakerRect.anchorMax = new Vector2(0.95f, 0.92f);
            speakerRect.offsetMin = Vector2.zero;
            speakerRect.offsetMax = Vector2.zero;
            var speakerTMP = speakerObj.GetComponent<TextMeshProUGUI>();
            speakerTMP.text = "Nombre del Personaje";
            speakerTMP.fontSize = 28;
            speakerTMP.fontStyle = FontStyles.Bold;
            speakerTMP.color = new Color(0.95f, 0.75f, 0.3f);

            // Dialogue Text
            var textObj = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(dialogueObj.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.2f, 0.1f);
            textRect.anchorMax = new Vector2(0.95f, 0.7f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textTMP = textObj.GetComponent<TextMeshProUGUI>();
            textTMP.text = "Este es el texto del diálogo...";
            textTMP.fontSize = 24;
            textTMP.color = Color.white;

            // Continue Indicator
            var indicatorObj = new GameObject("ContinueIndicator", typeof(RectTransform), typeof(TextMeshProUGUI));
            indicatorObj.transform.SetParent(dialogueObj.transform, false);
            var indRect = indicatorObj.GetComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0.94f, 0.05f);
            indRect.anchorMax = new Vector2(0.98f, 0.2f);
            indRect.offsetMin = Vector2.zero;
            indRect.offsetMax = Vector2.zero;
            var indTMP = indicatorObj.GetComponent<TextMeshProUGUI>();
            indTMP.text = "▼";
            indTMP.fontSize = 22;
            indTMP.alignment = TextAlignmentOptions.Center;
            indTMP.color = new Color(0.95f, 0.75f, 0.3f);

            // AudioSource for voice
            var voiceSource = dialogueObj.AddComponent<AudioSource>();

            // Assign Dialogue UI serialized fields
            var dialogueUI = dialogueObj.GetComponent<StoryDialogueUI>();
            SetPrivateField(dialogueUI, "canvasGroup", dialogueObj.GetComponent<CanvasGroup>());
            SetPrivateField(dialogueUI, "speakerNameText", speakerTMP);
            SetPrivateField(dialogueUI, "dialogueText", textTMP);
            SetPrivateField(dialogueUI, "characterPortrait", portraitObj.GetComponent<Image>());
            SetPrivateField(dialogueUI, "continueIndicator", indicatorObj);
            SetPrivateField(dialogueUI, "voiceAudioSource", voiceSource);

            // 4. Create Choices UI
            var choicesObj = new GameObject("ChoicesPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(StoryChoiceUI));
            choicesObj.transform.SetParent(uiControllerObj.transform, false);
            var choicesRect = choicesObj.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0.2f, 0.35f);
            choicesRect.anchorMax = new Vector2(0.8f, 0.75f);
            choicesRect.offsetMin = Vector2.zero;
            choicesRect.offsetMax = Vector2.zero;

            var promptObj = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            promptObj.transform.SetParent(choicesObj.transform, false);
            var promptRect = promptObj.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0f, 0.8f);
            promptRect.anchorMax = new Vector2(1f, 1f);
            promptRect.offsetMin = Vector2.zero;
            promptRect.offsetMax = Vector2.zero;
            var promptTMP = promptObj.GetComponent<TextMeshProUGUI>();
            promptTMP.text = "¿Qué deseas hacer?";
            promptTMP.fontSize = 28;
            promptTMP.alignment = TextAlignmentOptions.Center;

            var containerObj = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            containerObj.transform.SetParent(choicesObj.transform, false);
            var contRect = containerObj.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.1f, 0.05f);
            contRect.anchorMax = new Vector2(0.9f, 0.75f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;

            var layout = containerObj.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            var choiceUI = choicesObj.GetComponent<StoryChoiceUI>();
            SetPrivateField(choiceUI, "canvasGroup", choicesObj.GetComponent<CanvasGroup>());
            SetPrivateField(choiceUI, "promptText", promptTMP);
            SetPrivateField(choiceUI, "buttonsContainer", containerObj.transform);

            // 5. Create Fader UI (on top of dialogue)
            var faderObj = new GameObject("ScreenFader", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(StoryFader));
            faderObj.transform.SetParent(uiControllerObj.transform, false);
            var faderRect = faderObj.GetComponent<RectTransform>();
            faderRect.anchorMin = Vector2.zero;
            faderRect.anchorMax = Vector2.one;
            faderRect.offsetMin = Vector2.zero;
            faderRect.offsetMax = Vector2.zero;
            var faderImg = faderObj.GetComponent<Image>();
            faderImg.color = Color.black;
            var fader = faderObj.GetComponent<StoryFader>();
            SetPrivateField(fader, "fadeImage", faderImg);
            SetPrivateField(fader, "canvasGroup", faderObj.GetComponent<CanvasGroup>());

            // 6. Create Overlay UI (for Title Cards and chapter names)
            var overlayObj = new GameObject("OverlayUI", typeof(RectTransform), typeof(CanvasGroup), typeof(StoryOverlayUI));
            overlayObj.transform.SetParent(uiControllerObj.transform, false);
            var overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var overlayContentObj = new GameObject("ContentContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            overlayContentObj.transform.SetParent(overlayObj.transform, false);
            var ovContRect = overlayContentObj.GetComponent<RectTransform>();
            ovContRect.sizeDelta = new Vector2(800, 200);

            var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(overlayContentObj.transform, false);
            var ovTitleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            ovTitleTMP.text = "Capítulo 1";
            ovTitleTMP.fontSize = 42;
            ovTitleTMP.fontStyle = FontStyles.Bold;
            ovTitleTMP.alignment = TextAlignmentOptions.Center;

            var subTitleObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTitleObj.transform.SetParent(overlayContentObj.transform, false);
            var ovSubTMP = subTitleObj.GetComponent<TextMeshProUGUI>();
            ovSubTMP.text = "El Comienzo";
            ovSubTMP.fontSize = 26;
            ovSubTMP.fontStyle = FontStyles.Italic;
            ovSubTMP.alignment = TextAlignmentOptions.Center;
            ovSubTMP.color = new Color(0.85f, 0.85f, 0.85f);

            var overlayUI = overlayObj.GetComponent<StoryOverlayUI>();
            SetPrivateField(overlayUI, "canvasGroup", overlayObj.GetComponent<CanvasGroup>());
            SetPrivateField(overlayUI, "titleText", ovTitleTMP);
            SetPrivateField(overlayUI, "subtitleText", ovSubTMP);
            SetPrivateField(overlayUI, "contentContainer", ovContRect);

            // Connect sub-controllers to StoryUIController
            SetPrivateField(uiController, "dialogueUI", dialogueUI);
            SetPrivateField(uiController, "choiceUI", choiceUI);
            SetPrivateField(uiController, "fader", fader);
            SetPrivateField(uiController, "overlayUI", overlayUI);

            // 7. Create Core Managers & Runner in scene
            var managerObj = new GameObject("VisualNovel_Manager", typeof(StoryAudioManager), typeof(StoryCursorManager), typeof(StorySceneEvents), typeof(StoryRunner));
            Undo.RegisterCreatedObjectUndo(managerObj, "Setup Visual Novel in Scene");

            var runner = managerObj.GetComponent<StoryRunner>();
            SetPrivateField(runner, "uiController", uiController);

            Selection.activeGameObject = managerObj;
            EditorUtility.DisplayDialog("Setup Completo", "Se ha creado la estructura completa de Visual Novel + Point & Click (Canvas UI, Fader, Overlay Text, Audio Manager, Cursor Manager, Scene Events y Story Runner) en la escena.", "Aceptar");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
