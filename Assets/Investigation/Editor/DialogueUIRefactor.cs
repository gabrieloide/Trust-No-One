using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VisualNovelSystem;

namespace Investigation.EditorTools
{
    public static class DialogueUIRefactor
    {
        [MenuItem("Tools/Investigation/Refactor Dialogue UI (Half-Body Stage + Clean Textbox)")]
        public static void Refactor()
        {
            var uiController = Object.FindAnyObjectByType<StoryUIController>();
            if (uiController == null)
            {
                Debug.LogError("[DialogueUIRefactor] No se encontró StoryUIController en la escena.");
                return;
            }

            var courier = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Courier_Prime/CourierPrime-Regular SDF.asset");
            var keyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Investigation/Audio/SFX/sfx_typewriter_key.wav");

            Transform uiTrans = uiController.transform;

            // 1. Obtener o crear CharacterStage (medio cuerpo) como hijo de StoryUIController
            var existingStage = uiTrans.Find("CharacterStage");
            GameObject stageGO;
            if (existingStage != null)
            {
                stageGO = existingStage.gameObject;
            }
            else
            {
                stageGO = new GameObject("CharacterStage", typeof(RectTransform), typeof(Image));
                stageGO.transform.SetParent(uiTrans, false);
            }

            // Poner el personaje detrás de la caja de diálogo
            stageGO.transform.SetSiblingIndex(0);

            var stageRect = stageGO.GetComponent<RectTransform>();
            stageRect.anchorMin = new Vector2(0.5f, 0f);
            stageRect.anchorMax = new Vector2(0.5f, 0f);
            stageRect.pivot = new Vector2(0.5f, 0f);
            stageRect.sizeDelta = new Vector2(520f, 750f);
            stageRect.anchoredPosition = new Vector2(0f, 100f);

            var stageImg = stageGO.GetComponent<Image>();
            stageImg.preserveAspect = true;
            stageImg.raycastTarget = false;
            stageGO.SetActive(false);

            // 2. Limpiar / Refactorizar DialogueBox
            var dialogueUI = Object.FindAnyObjectByType<StoryDialogueUI>();
            if (dialogueUI == null)
            {
                Debug.LogError("[DialogueUIRefactor] No se encontró StoryDialogueUI.");
                return;
            }

            var diagGO = dialogueUI.gameObject;
            var diagRect = diagGO.GetComponent<RectTransform>();
            diagRect.anchorMin = new Vector2(0.06f, 0.03f);
            diagRect.anchorMax = new Vector2(0.94f, 0.28f);
            diagRect.offsetMin = Vector2.zero;
            diagRect.offsetMax = Vector2.zero;

            var diagImg = diagGO.GetComponent<Image>();
            if (diagImg != null)
            {
                diagImg.color = new Color(0.06f, 0.06f, 0.08f, 0.95f);
            }

            // Destruir portrait viejo si estaba adentro de DialogueBox
            var oldPortrait = diagGO.transform.Find("CharacterPortrait");
            if (oldPortrait != null && oldPortrait.gameObject != stageGO)
            {
                Object.DestroyImmediate(oldPortrait.gameObject);
            }

            // 3. Ajustar SpeakerName (ancho completo superior)
            var speakerTrans = diagGO.transform.Find("SpeakerName");
            TextMeshProUGUI speakerTMP = null;
            if (speakerTrans != null)
            {
                var sRect = speakerTrans.GetComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.025f, 0.74f);
                sRect.anchorMax = new Vector2(0.975f, 0.95f);
                sRect.offsetMin = Vector2.zero;
                sRect.offsetMax = Vector2.zero;

                speakerTMP = speakerTrans.GetComponent<TextMeshProUGUI>();
                if (speakerTMP != null)
                {
                    if (courier != null) speakerTMP.font = courier;
                    speakerTMP.fontSize = 26f;
                    speakerTMP.fontStyle = FontStyles.Bold;
                    speakerTMP.color = new Color(0.96f, 0.75f, 0.29f);
                }
            }

            // 4. Ajustar DialogueText (ancho completo cuerpo)
            var textTrans = diagGO.transform.Find("DialogueText");
            TextMeshProUGUI textTMP = null;
            if (textTrans != null)
            {
                var tRect = textTrans.GetComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0.025f, 0.08f);
                tRect.anchorMax = new Vector2(0.975f, 0.72f);
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;

                textTMP = textTrans.GetComponent<TextMeshProUGUI>();
                if (textTMP != null)
                {
                    if (courier != null) textTMP.font = courier;
                    textTMP.fontSize = 22f;
                    textTMP.color = Color.white;
                }
            }

            // 5. Ajustar OverlayUI (Carteles de título de capítulo amplios y sin cortes)
            var overlayUI = Object.FindAnyObjectByType<StoryOverlayUI>();
            if (overlayUI != null)
            {
                var ovTrans = overlayUI.transform;
                var contentCont = ovTrans.Find("ContentContainer");
                if (contentCont != null)
                {
                    var cRect = contentCont.GetComponent<RectTransform>();
                    cRect.anchorMin = new Vector2(0f, 0.5f);
                    cRect.anchorMax = new Vector2(1f, 0.5f);
                    cRect.pivot = new Vector2(0.5f, 0.5f);
                    cRect.sizeDelta = new Vector2(0f, 300f);
                    cRect.anchoredPosition = Vector2.zero;

                    var vlg = contentCont.GetComponent<VerticalLayoutGroup>();
                    if (vlg == null) vlg = contentCont.gameObject.AddComponent<VerticalLayoutGroup>();
                    vlg.childAlignment = TextAnchor.MiddleCenter;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 14f;

                    var tTrans = contentCont.Find("TitleText");
                    if (tTrans != null)
                    {
                        var tTMP = tTrans.GetComponent<TextMeshProUGUI>();
                        if (tTMP != null)
                        {
                            var bebas = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bebas_Neue/BebasNeue-Regular SDF.asset");
                            if (bebas != null) tTMP.font = bebas;
                            tTMP.fontSize = 46f;
                            tTMP.enableAutoSizing = true;
                            tTMP.fontSizeMin = 28f;
                            tTMP.fontSizeMax = 52f;
                            tTMP.textWrappingMode = TextWrappingModes.NoWrap;
                            tTMP.overflowMode = TextOverflowModes.Overflow;
                            tTMP.alignment = TextAlignmentOptions.Center;
                            tTMP.color = new Color(0.96f, 0.85f, 0.45f);
                        }
                    }

                    var sTrans = contentCont.Find("SubtitleText");
                    if (sTrans != null)
                    {
                        var sTMP = sTrans.GetComponent<TextMeshProUGUI>();
                        if (sTMP != null)
                        {
                            if (courier != null) sTMP.font = courier;
                            sTMP.fontSize = 24f;
                            sTMP.enableAutoSizing = true;
                            sTMP.fontSizeMin = 18f;
                            sTMP.fontSizeMax = 28f;
                            sTMP.textWrappingMode = TextWrappingModes.NoWrap;
                            sTMP.overflowMode = TextOverflowModes.Overflow;
                            sTMP.alignment = TextAlignmentOptions.Center;
                            sTMP.color = new Color(0.9f, 0.9f, 0.9f);
                        }
                    }
                }
                EditorUtility.SetDirty(overlayUI);
            }

            // 6. Vincular al componente StoryDialogueUI
            var so = new SerializedObject(dialogueUI);
            so.FindProperty("characterPortrait").objectReferenceValue = stageImg;
            if (speakerTMP != null) so.FindProperty("speakerNameText").objectReferenceValue = speakerTMP;
            if (textTMP != null) so.FindProperty("dialogueText").objectReferenceValue = textTMP;
            if (keyClip != null) so.FindProperty("typingAudioClip").objectReferenceValue = keyClip;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Guardar cambios
            EditorUtility.SetDirty(diagGO);
            EditorUtility.SetDirty(stageGO);
            EditorUtility.SetDirty(dialogueUI);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("[DialogueUIRefactor] ¡UI de Diálogo refactorizada exitosamente con Stage de medio cuerpo y caja de texto limpia!");
        }
    }
}
