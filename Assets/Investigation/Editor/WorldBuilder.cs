using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VisualNovelSystem;

namespace Investigation.EditorTools
{
    // Genera la escena de investigación completa, vinculando las locaciones, los StoryGraphs del Día 1
    // y el sistema dinámico de navegación y HUD.
    public static class WorldBuilder
    {
        private class HotspotDef
        {
            public string id;
            public string label;
            public StoryGraph storyGraph;
        }

        private class LocationDef
        {
            public string id;
            public string displayName;
            public Color color;
            public List<HotspotDef> characters = new List<HotspotDef>();
            public List<HotspotDef> investigateSpots = new List<HotspotDef>();
        }

        [MenuItem("Tools/Investigation/Build World Scene")]
        public static void Build()
        {
            var canvasGO = GameObject.Find("VisualNovel_Canvas");
            if (canvasGO == null)
            {
                Debug.LogError("[WorldBuilder] No se encontró 'VisualNovel_Canvas'. Corré primero GameObject > Visual Novel > Setup Visual Novel in Scene.");
                return;
            }

            var canvas = canvasGO.transform;

            var existingRoot = canvas.Find("WorldRoot");
            if (existingRoot != null) Object.DestroyImmediate(existingRoot.gameObject);

            var worldRoot = new GameObject("WorldRoot", typeof(RectTransform));
            worldRoot.transform.SetParent(canvas, false);
            worldRoot.transform.SetSiblingIndex(0); // antes que StoryUIController
            StretchFull(worldRoot.GetComponent<RectTransform>());

            var locationsRoot = new GameObject("LocationsRoot", typeof(RectTransform));
            locationsRoot.transform.SetParent(worldRoot.transform, false);
            StretchFull(locationsRoot.GetComponent<RectTransform>());

            // Cargar StoryGraph del Prólogo Completo
            var introSG = AssetDatabase.LoadAssetAtPath<StoryGraph>("Assets/Investigation/Stories/Dia1/SG_D1_Prologo_Completo.asset");

            var locations = BuildLocationDefs(null, null, null);

            var locationEntries = new List<(string id, GameObject panel)>();
            var investigateHotspotEntries = new List<(string id, GameObject go)>();
            var characterHotspotEntries = new List<(string charId, string locId, GameObject go)>();

            foreach (var loc in locations)
            {
                var panel = CreateLocationPanel(locationsRoot.transform, loc, investigateHotspotEntries, characterHotspotEntries);
                locationEntries.Add((loc.id, panel));
            }

            var navButtonEntries = new List<(string id, GameObject go)>();
            var navBarGO = CreateNavBar(worldRoot.transform, locations, navButtonEntries);
            var (hudGO, hudText) = CreateHud(worldRoot.transform);
            var (boardPanelGO, pinArea, detailText) = CreateClueBoardPanel(worldRoot.transform);
            CreateClueBoardButton(worldRoot.transform);
            var accuseButtonGO = CreateAccuseButton(worldRoot.transform);

            // Controllers Setup
            var convGO = GameObject.Find("ConversationController");
            if (convGO == null) convGO = new GameObject("ConversationController");
            if (convGO.GetComponent<ConversationController>() == null) convGO.AddComponent<ConversationController>();

            var flowGO = GameObject.Find("GameFlowController");
            if (flowGO == null) flowGO = new GameObject("GameFlowController");
            var flowController = flowGO.GetComponent<GameFlowController>();
            if (flowController == null) flowController = flowGO.AddComponent<GameFlowController>();
            var flowSo = new SerializedObject(flowController);
            flowSo.FindProperty("introStoryGraph").objectReferenceValue = introSG;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            var accGO = GameObject.Find("AccusationController");
            if (accGO == null) accGO = new GameObject("AccusationController");
            if (accGO.GetComponent<AccusationController>() == null) accGO.AddComponent<AccusationController>();

            var boardControllerGO = GameObject.Find("ClueBoardController");
            if (boardControllerGO == null) boardControllerGO = new GameObject("ClueBoardController");
            var boardController = boardControllerGO.GetComponent<ClueBoardController>();
            if (boardController == null) boardController = boardControllerGO.AddComponent<ClueBoardController>();
            var boardSo = new SerializedObject(boardController);
            boardSo.FindProperty("boardPanel").objectReferenceValue = boardPanelGO;
            boardSo.FindProperty("pinArea").objectReferenceValue = pinArea;
            boardSo.FindProperty("detailText").objectReferenceValue = detailText;
            boardSo.ApplyModifiedPropertiesWithoutUndo();

            var locGO = GameObject.Find("LocationController");
            if (locGO == null) locGO = new GameObject("LocationController");
            var locController = locGO.GetComponent<LocationController>();
            if (locController == null) locController = locGO.AddComponent<LocationController>();

            var so = new SerializedObject(locController);
            
            // Locations
            var listProp = so.FindProperty("locations");
            listProp.ClearArray();
            for (int i = 0; i < locationEntries.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                var elem = listProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("id").stringValue = locationEntries[i].id;
                elem.FindPropertyRelative("panelRoot").objectReferenceValue = locationEntries[i].panel;
            }

            // NavButtons
            var navProp = so.FindProperty("navButtons");
            navProp.ClearArray();
            for (int i = 0; i < navButtonEntries.Count; i++)
            {
                navProp.InsertArrayElementAtIndex(i);
                var elem = navProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("locationId").stringValue = navButtonEntries[i].id;
                elem.FindPropertyRelative("buttonGO").objectReferenceValue = navButtonEntries[i].go;
            }

            // Investigate Hotspots
            var hotProp = so.FindProperty("investigateHotspots");
            hotProp.ClearArray();
            for (int i = 0; i < investigateHotspotEntries.Count; i++)
            {
                hotProp.InsertArrayElementAtIndex(i);
                var elem = hotProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("spotId").stringValue = investigateHotspotEntries[i].id;
                elem.FindPropertyRelative("hotspotGO").objectReferenceValue = investigateHotspotEntries[i].go;
            }

            // Character Hotspots
            var charProp = so.FindProperty("characterHotspots");
            charProp.ClearArray();
            for (int i = 0; i < characterHotspotEntries.Count; i++)
            {
                charProp.InsertArrayElementAtIndex(i);
                var elem = charProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("characterId").stringValue = characterHotspotEntries[i].charId;
                elem.FindPropertyRelative("locationId").stringValue = characterHotspotEntries[i].locId;
                elem.FindPropertyRelative("hotspotGO").objectReferenceValue = characterHotspotEntries[i].go;
            }

            // UI Elements
            so.FindProperty("worldUIRoot").objectReferenceValue = worldRoot;
            so.FindProperty("hudRoot").objectReferenceValue = hudGO;
            so.FindProperty("hudText").objectReferenceValue = hudText;
            so.FindProperty("navBarRoot").objectReferenceValue = navBarGO;
            so.FindProperty("accuseButtonRoot").objectReferenceValue = accuseButtonGO;
            
            // Inicio en la Carretera (según diseño de Notion)
            so.FindProperty("startingLocationId").stringValue = "road";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Configurar Audio y SFX
            InvestigationSFXGenerator.WireSFXToScene();

            // Asegurar layout de Diálogo con Stage de medio cuerpo y caja limpia
            DialogueUIRefactor.Refactor();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[WorldBuilder] Mundo construido con StoryGraphs: 6 locaciones, inicio en 'road' (Carretera).");
        }

        private static List<LocationDef> BuildLocationDefs(StoryGraph ernestoSG, StoryGraph elenaSG, StoryGraph robertSG)
        {
            return new List<LocationDef>
            {
                new LocationDef
                {
                    id = "road", displayName = "Road / Highway", color = new Color(0.30f, 0.32f, 0.28f),
                    characters =
                    {
                        new HotspotDef { id = "gus", label = "Gus Whitlock" },
                        new HotspotDef { id = "mark", label = "Mark Doss" }
                    }
                },
                new LocationDef
                {
                    id = "motel", displayName = "Motel — Front Desk", color = new Color(0.36f, 0.30f, 0.20f),
                    characters =
                    {
                        new HotspotDef { id = "elena", label = "Elena Marchetti", storyGraph = elenaSG },
                        new HotspotDef { id = "robert", label = "Robert Hale", storyGraph = robertSG }
                    },
                    investigateSpots = { new HotspotDef { id = "inv_robert_office", label = "Robert's Office" } }
                },
                new LocationDef
                {
                    id = "gas_station", displayName = "Gas Station", color = new Color(0.45f, 0.38f, 0.18f),
                    characters =
                    {
                        new HotspotDef { id = "frank", label = "Frank Doyle" },
                        new HotspotDef { id = "mark", label = "Mark Doss" }
                    }
                },
                new LocationDef
                {
                    id = "ernesto_shop", displayName = "Carpet Shop", color = new Color(0.40f, 0.24f, 0.20f),
                    characters = { new HotspotDef { id = "ernesto", label = "Ernesto Vidal" } },
                    investigateSpots = { new HotspotDef { id = "inv_carpet_shop_receipt", label = "Counter" } }
                },
                new LocationDef
                {
                    id = "cafeteria", displayName = "Diner", color = new Color(0.42f, 0.34f, 0.24f),
                    characters =
                    {
                        new HotspotDef { id = "marta", label = "Marta Solís" },
                        new HotspotDef { id = "gus", label = "Gus Whitlock" }
                    }
                },
                new LocationDef
                {
                    id = "crime_scene", displayName = "Backyard — Crime Scene", color = new Color(0.22f, 0.22f, 0.24f),
                    investigateSpots =
                    {
                        new HotspotDef { id = "inv_arbustos", label = "Bushes by the Fence" },
                        new HotspotDef { id = "inv_scene_glass", label = "Perimeter / Ground" },
                        new HotspotDef { id = "inv_basement_lock", label = "Service Door" },
                        new HotspotDef { id = "inv_crime_scene_fiber", label = "Re-examination (Day 3)" },
                        new HotspotDef { id = "inv_basement_revisit", label = "Padlock, Second Look" },
                        new HotspotDef { id = "inv_near_basement_carla", label = "Corner of Crates" }
                    }
                }
            };
        }

        private static GameObject CreateLocationPanel(Transform parent, LocationDef loc, List<(string id, GameObject go)> investigateHotspots, List<(string charId, string locId, GameObject go)> characterHotspots)
        {
            var panel = new GameObject("Location_" + loc.id, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());

            var bg = panel.AddComponent<Image>();
            bg.color = loc.color;

            var label = CreateLabel(panel.transform, loc.displayName.ToUpper(), 28f, TextAlignmentOptions.Top, new Color(1f, 1f, 1f, 0.45f));
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(700f, 50f);
            labelRect.anchoredPosition = new Vector2(0f, -16f);

            float x = -300f;
            foreach (var c in loc.characters)
            {
                var type = c.storyGraph != null ? InteractType.TriggerStoryGraph : InteractType.OpenConversation;
                var hotspot = CreateHotspot(panel.transform, c.label, new Vector2(x, 40f), new Vector2(220f, 140f),
                    new Color(0.55f, 0.2f, 0.2f, 0.9f), type, c.id, c.storyGraph);

                characterHotspots.Add((c.id, loc.id, hotspot));
                x += 260f;
            }

            int spotIndex = 0;
            foreach (var s in loc.investigateSpots)
            {
                float spotX = -300f + (spotIndex % 3) * 260f;
                float spotY = spotIndex < 3 ? -160f : -260f;
                var hotspot = CreateHotspot(panel.transform, "[Investigate]\n" + s.label, new Vector2(spotX, spotY), new Vector2(240f, 85f),
                    new Color(0.2f, 0.35f, 0.45f, 0.9f), InteractType.InvestigateSpot, s.id);
                investigateHotspots.Add((s.id, hotspot));
                spotIndex++;
            }

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateHotspot(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Color color, InteractType type, string payload, StoryGraph storyGraph = null)
        {
            var go = new GameObject("Hotspot_" + label.Replace("\n", " "), typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = color;

            var lbl = CreateLabel(go.transform, label, 20f, TextAlignmentOptions.Center, Color.white);
            var lblRect = lbl.GetComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = new Vector2(6f, 6f);
            lblRect.offsetMax = new Vector2(-6f, -6f);

            var interactable = go.AddComponent<StoryInteractable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("interactType").enumValueIndex = (int)type;
            if (storyGraph != null)
            {
                so.FindProperty("targetStoryGraph").objectReferenceValue = storyGraph;
            }

            switch (type)
            {
                case InteractType.OpenConversation:
                    so.FindProperty("conversationCharacterId").stringValue = payload;
                    break;
                case InteractType.InvestigateSpot:
                    so.FindProperty("investigateSpotId").stringValue = payload;
                    break;
                case InteractType.GoToLocation:
                    so.FindProperty("targetLocationId").stringValue = payload;
                    break;
            }
            so.FindProperty("uiGraphic").objectReferenceValue = img;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject CreateNavBar(Transform parent, List<LocationDef> locations, List<(string id, GameObject go)> navButtons)
        {
            var bar = new GameObject("NavBar", typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(0f, 70f);
            barRect.anchoredPosition = Vector2.zero;

            var bg = bar.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach (var loc in locations)
            {
                var btn = CreateHotspot(bar.transform, "📍 " + loc.displayName, Vector2.zero, new Vector2(150f, 54f),
                    new Color(0.20f, 0.22f, 0.24f, 0.95f), InteractType.GoToLocation, loc.id);
                navButtons.Add((loc.id, btn));
            }

            return bar;
        }

        private static GameObject CreateClueBoardButton(Transform parent)
        {
            var go = CreateHotspot(parent, "🗂 Clue Board", Vector2.zero, new Vector2(170f, 54f),
                new Color(0.35f, 0.3f, 0.15f, 0.95f), InteractType.OpenClueBoard, "");

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(16f, 86f); // arriba de la NavBar

            return go;
        }

        private static (GameObject root, RectTransform pinArea, TextMeshProUGUI detailText) CreateClueBoardPanel(Transform parent)
        {
            var root = new GameObject("ClueBoardOverlay", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            StretchFull(root.GetComponent<RectTransform>());

            // Scrim de fondo: oscurece la escena y cierra la pizarra al clickearlo (afuera del panel).
            var scrimGO = CreateHotspot(root.transform, "", Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.6f), InteractType.OpenClueBoard, "");
            StretchFull(scrimGO.GetComponent<RectTransform>());
            foreach (Transform child in scrimGO.transform) Object.DestroyImmediate(child.gameObject); // sin label

            var boardGO = new GameObject("BoardPanel", typeof(RectTransform));
            boardGO.transform.SetParent(root.transform, false);
            var boardRect = boardGO.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.08f, 0.12f);
            boardRect.anchorMax = new Vector2(0.92f, 0.88f);
            boardRect.offsetMin = Vector2.zero;
            boardRect.offsetMax = Vector2.zero;

            var boardBg = boardGO.AddComponent<Image>();
            boardBg.color = new Color(0.32f, 0.22f, 0.15f, 0.97f); // placeholder "corcho"

            var title = CreateLabel(boardGO.transform, "PISTAS", 26f, TextAlignmentOptions.Center, new Color(0.95f, 0.9f, 0.75f, 0.9f));
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 48f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);

            var closeGO = CreateHotspot(boardGO.transform, "✕", Vector2.zero, new Vector2(44f, 44f),
                new Color(0.5f, 0.15f, 0.15f, 0.95f), InteractType.OpenClueBoard, "");
            var closeRect = closeGO.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-10f, -10f);

            var pinAreaGO = new GameObject("PinArea", typeof(RectTransform));
            pinAreaGO.transform.SetParent(boardGO.transform, false);
            var pinAreaRect = pinAreaGO.GetComponent<RectTransform>();
            pinAreaRect.anchorMin = new Vector2(0.03f, 0.2f);
            pinAreaRect.anchorMax = new Vector2(0.97f, 0.88f);
            pinAreaRect.offsetMin = Vector2.zero;
            pinAreaRect.offsetMax = Vector2.zero;

            var detailBarGO = new GameObject("DetailBar", typeof(RectTransform));
            detailBarGO.transform.SetParent(boardGO.transform, false);
            var detailBarRect = detailBarGO.GetComponent<RectTransform>();
            detailBarRect.anchorMin = new Vector2(0f, 0f);
            detailBarRect.anchorMax = new Vector2(1f, 0.18f);
            detailBarRect.offsetMin = Vector2.zero;
            detailBarRect.offsetMax = Vector2.zero;
            var detailBg = detailBarGO.AddComponent<Image>();
            detailBg.color = new Color(0f, 0f, 0f, 0.35f);

            var detailText = CreateLabel(detailBarGO.transform, "Click a pinned clue to read it.", 16f, TextAlignmentOptions.TopLeft, Color.white);
            var detailRect = detailText.GetComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = new Vector2(14f, 8f);
            detailRect.offsetMax = new Vector2(-14f, -8f);

            return (root, pinAreaRect, detailText);
        }

        private static GameObject CreateAccuseButton(Transform parent)
        {
            var go = CreateHotspot(parent, "⚖️ ACCUSE", Vector2.zero, new Vector2(160f, 54f),
                new Color(0.6f, 0.12f, 0.12f, 0.95f), InteractType.OpenAccusation, "");

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -16f);

            return go;
        }

        private static (GameObject go, TextMeshProUGUI text) CreateHud(Transform parent)
        {
            var go = new GameObject("Hud", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(500f, 40f);
            rect.anchoredPosition = new Vector2(16f, -16f);

            var text = CreateLabel(go.transform, "Day 1 · Phase 1 · Actions left: 4", 22f, TextAlignmentOptions.MidlineLeft, Color.white);
            text.transform.SetParent(go.transform, false);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return (go, text);
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;

            // Tipografía contextual según el rol del texto
            if (fontSize >= 26f || (parent != null && parent.name.StartsWith("Location_")))
            {
                var bebas = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bebas_Neue/BebasNeue-Regular SDF.asset");
                if (bebas != null) tmp.font = bebas;
            }
            else
            {
                var spaceMono = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Space_Mono/SpaceMono-Regular SDF.asset");
                if (spaceMono != null) tmp.font = spaceMono;
            }

            return tmp;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
