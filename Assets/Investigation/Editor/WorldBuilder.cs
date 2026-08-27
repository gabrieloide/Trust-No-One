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
            var (evidenceBarGO, evidenceContainer) = CreateEvidencePanel(worldRoot.transform);
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

            var evGO = GameObject.Find("EvidencePanelController");
            if (evGO == null) evGO = new GameObject("EvidencePanelController");
            var evController = evGO.GetComponent<EvidencePanelController>();
            if (evController == null) evController = evGO.AddComponent<EvidencePanelController>();
            var evSo = new SerializedObject(evController);
            evSo.FindProperty("panelRoot").objectReferenceValue = evidenceBarGO;
            evSo.FindProperty("container").objectReferenceValue = evidenceContainer;
            evSo.ApplyModifiedPropertiesWithoutUndo();

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
                    id = "road", displayName = "Camino / Ruta", color = new Color(0.30f, 0.32f, 0.28f),
                    characters =
                    {
                        new HotspotDef { id = "gus", label = "Gus Whitlock" },
                        new HotspotDef { id = "mark", label = "Mark Doss" }
                    }
                },
                new LocationDef
                {
                    id = "motel", displayName = "Motel — Recepción", color = new Color(0.36f, 0.30f, 0.20f),
                    characters =
                    {
                        new HotspotDef { id = "elena", label = "Elena Marchetti", storyGraph = elenaSG },
                        new HotspotDef { id = "robert", label = "Robert Hale", storyGraph = robertSG }
                    },
                    investigateSpots = { new HotspotDef { id = "inv_robert_office", label = "Oficina de Robert" } }
                },
                new LocationDef
                {
                    id = "gas_station", displayName = "Gasolinera", color = new Color(0.45f, 0.38f, 0.18f),
                    characters =
                    {
                        new HotspotDef { id = "frank", label = "Frank Doyle" },
                        new HotspotDef { id = "mark", label = "Mark Doss" }
                    }
                },
                new LocationDef
                {
                    id = "ernesto_shop", displayName = "Tienda de Alfombras", color = new Color(0.40f, 0.24f, 0.20f),
                    characters = { new HotspotDef { id = "ernesto", label = "Ernesto Vidal" } },
                    investigateSpots = { new HotspotDef { id = "inv_carpet_shop_receipt", label = "Mostrador" } }
                },
                new LocationDef
                {
                    id = "cafeteria", displayName = "Cafetería", color = new Color(0.42f, 0.34f, 0.24f),
                    characters =
                    {
                        new HotspotDef { id = "marta", label = "Marta Solís" },
                        new HotspotDef { id = "gus", label = "Gus Whitlock" }
                    }
                },
                new LocationDef
                {
                    id = "crime_scene", displayName = "Patio Trasero — Escena", color = new Color(0.22f, 0.22f, 0.24f),
                    investigateSpots =
                    {
                        new HotspotDef { id = "inv_arbustos", label = "Arbustos del cerco" },
                        new HotspotDef { id = "inv_scene_glass", label = "Perímetro / Suelo" },
                        new HotspotDef { id = "inv_basement_lock", label = "Puerta de servicio" },
                        new HotspotDef { id = "inv_crime_scene_fiber", label = "Revisión (Día 3)" },
                        new HotspotDef { id = "inv_basement_revisit", label = "Cerrojo, revisita" },
                        new HotspotDef { id = "inv_near_basement_carla", label = "Rincón de cajas" }
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

                // Mismo hotspot también acepta que le arrastren una pista encima (Confrontar).
                var confrontTarget = hotspot.AddComponent<ClueConfrontTarget>();
                var ctSo = new SerializedObject(confrontTarget);
                ctSo.FindProperty("characterId").stringValue = c.id;
                ctSo.ApplyModifiedPropertiesWithoutUndo();

                characterHotspots.Add((c.id, loc.id, hotspot));
                x += 260f;
            }

            int spotIndex = 0;
            foreach (var s in loc.investigateSpots)
            {
                float spotX = -300f + (spotIndex % 3) * 260f;
                float spotY = spotIndex < 3 ? -160f : -260f;
                var hotspot = CreateHotspot(panel.transform, "[Investigar]\n" + s.label, new Vector2(spotX, spotY), new Vector2(240f, 85f),
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

        private static (GameObject bar, Transform container) CreateEvidencePanel(Transform parent)
        {
            var bar = new GameObject("EvidencePanel", typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(0f, 64f);
            barRect.anchoredPosition = new Vector2(0f, 70f); // arriba de la NavBar

            var bg = bar.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.75f);

            var outerLayout = bar.AddComponent<HorizontalLayoutGroup>();
            outerLayout.childAlignment = TextAnchor.MiddleLeft;
            outerLayout.spacing = 8f;
            outerLayout.padding = new RectOffset(16, 16, 6, 6);
            outerLayout.childForceExpandWidth = false;
            outerLayout.childForceExpandHeight = true;

            var label = CreateLabel(bar.transform, "💼 Pistas:", 18f, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.85f, 0.6f, 0.9f));
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

            var chipsContainer = new GameObject("ChipsContainer", typeof(RectTransform));
            chipsContainer.transform.SetParent(bar.transform, false);
            var chipsLayout = chipsContainer.AddComponent<HorizontalLayoutGroup>();
            chipsLayout.spacing = 8f;
            chipsLayout.childAlignment = TextAnchor.MiddleLeft;
            chipsLayout.childForceExpandWidth = false;
            chipsLayout.childForceExpandHeight = true;
            chipsContainer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            return (bar, chipsContainer.transform);
        }

        private static GameObject CreateAccuseButton(Transform parent)
        {
            var go = CreateHotspot(parent, "⚖️ ACUSAR", Vector2.zero, new Vector2(160f, 54f),
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

            var text = CreateLabel(go.transform, "Día 1 · Fase 1 · Acciones restantes: 4", 22f, TextAlignmentOptions.MidlineLeft, Color.white);
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
