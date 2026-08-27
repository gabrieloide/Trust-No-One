using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VisualNovelSystem;

namespace Investigation.EditorTools
{
    // Arma de una sola pasada las 6 locaciones (placeholders de color + hotspots),
    // la barra de navegación y el HUD de día/fase/acciones, dentro del
    // VisualNovel_Canvas que ya crea StorySceneSetupWizard. Pensado para poder
    // re-ejecutarse (borra WorldRoot si ya existe) mientras se itera el contenido.
    public static class WorldBuilder
    {
        private class HotspotDef
        {
            public string id;
            public string label;
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
            worldRoot.transform.SetSiblingIndex(0); // antes que StoryUIController -> diálogo/choices quedan arriba
            StretchFull(worldRoot.GetComponent<RectTransform>());

            var locationsRoot = new GameObject("LocationsRoot", typeof(RectTransform));
            locationsRoot.transform.SetParent(worldRoot.transform, false);
            StretchFull(locationsRoot.GetComponent<RectTransform>());

            var locations = BuildLocationDefs();

            var locationEntries = new List<(string id, GameObject panel)>();
            foreach (var loc in locations)
            {
                var panel = CreateLocationPanel(locationsRoot.transform, loc);
                locationEntries.Add((loc.id, panel));
            }

            CreateNavBar(worldRoot.transform, locations);
            var hud = CreateHud(worldRoot.transform);
            var evidenceContainer = CreateEvidencePanel(worldRoot.transform);
            CreateAccuseButton(worldRoot.transform);

            var convGO = GameObject.Find("ConversationController");
            if (convGO == null) convGO = new GameObject("ConversationController");
            if (convGO.GetComponent<ConversationController>() == null) convGO.AddComponent<ConversationController>();

            var flowGO = GameObject.Find("GameFlowController");
            if (flowGO == null) flowGO = new GameObject("GameFlowController");
            if (flowGO.GetComponent<GameFlowController>() == null) flowGO.AddComponent<GameFlowController>();

            var accGO = GameObject.Find("AccusationController");
            if (accGO == null) accGO = new GameObject("AccusationController");
            if (accGO.GetComponent<AccusationController>() == null) accGO.AddComponent<AccusationController>();

            var evGO = GameObject.Find("EvidencePanelController");
            if (evGO == null) evGO = new GameObject("EvidencePanelController");
            var evController = evGO.GetComponent<EvidencePanelController>();
            if (evController == null) evController = evGO.AddComponent<EvidencePanelController>();
            var evSo = new SerializedObject(evController);
            evSo.FindProperty("container").objectReferenceValue = evidenceContainer;
            evSo.ApplyModifiedPropertiesWithoutUndo();

            var locGO = GameObject.Find("LocationController");
            if (locGO == null) locGO = new GameObject("LocationController");
            var locController = locGO.GetComponent<LocationController>();
            if (locController == null) locController = locGO.AddComponent<LocationController>();

            var so = new SerializedObject(locController);
            var listProp = so.FindProperty("locations");
            listProp.ClearArray();
            for (int i = 0; i < locationEntries.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                var elem = listProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("id").stringValue = locationEntries[i].id;
                elem.FindPropertyRelative("panelRoot").objectReferenceValue = locationEntries[i].panel;
            }
            so.FindProperty("hudText").objectReferenceValue = hud;
            so.FindProperty("startingLocationId").stringValue = locations[0].id;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[WorldBuilder] Mundo construido: " + locations.Count + " locaciones.");
        }

        private static List<LocationDef> BuildLocationDefs()
        {
            return new List<LocationDef>
            {
                new LocationDef
                {
                    id = "motel", displayName = "Motel — Recepción", color = new Color(0.36f, 0.30f, 0.20f),
                    characters = { new HotspotDef { id = "robert", label = "Robert Hale" }, new HotspotDef { id = "elena", label = "Elena Marchetti" } },
                    investigateSpots = { new HotspotDef { id = "inv_robert_office", label = "Oficina de Robert" } }
                },
                new LocationDef
                {
                    id = "gas_station", displayName = "Gasolinera", color = new Color(0.45f, 0.38f, 0.18f),
                    characters = { new HotspotDef { id = "frank", label = "Frank Doyle" }, new HotspotDef { id = "mark", label = "Mark Doss" } }
                },
                new LocationDef
                {
                    id = "road", displayName = "Camino / Ruta", color = new Color(0.30f, 0.32f, 0.28f),
                    characters = { new HotspotDef { id = "gus", label = "Gus Whitlock" } },
                    investigateSpots = { new HotspotDef { id = "inv_arbustos", label = "Arbustos" } }
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
                    characters = { new HotspotDef { id = "marta", label = "Marta Solís" } }
                },
                new LocationDef
                {
                    id = "crime_scene", displayName = "Zona del Sótano", color = new Color(0.18f, 0.18f, 0.20f),
                    investigateSpots =
                    {
                        new HotspotDef { id = "inv_basement_lock", label = "Puerta del sótano" },
                        new HotspotDef { id = "inv_scene_glass", label = "Perímetro" },
                        new HotspotDef { id = "inv_crime_scene_fiber", label = "Revisión (Día 3)" },
                        new HotspotDef { id = "inv_basement_revisit", label = "Sótano, revisita" },
                        new HotspotDef { id = "inv_near_basement_carla", label = "Rincón junto al sótano" }
                    }
                }
            };
        }

        private static GameObject CreateLocationPanel(Transform parent, LocationDef loc)
        {
            var panel = new GameObject("Location_" + loc.id, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());

            var bg = panel.AddComponent<Image>();
            bg.color = loc.color;

            var label = CreateLabel(panel.transform, "FONDO: " + loc.displayName, 28f, TextAlignmentOptions.Top, new Color(1f, 1f, 1f, 0.45f));
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(700f, 50f);
            labelRect.anchoredPosition = new Vector2(0f, -16f);

            float x = -300f;
            foreach (var c in loc.characters)
            {
                var hotspot = CreateHotspot(panel.transform, c.label, new Vector2(x, 40f), new Vector2(220f, 140f),
                    new Color(0.55f, 0.2f, 0.2f, 0.9f), InteractType.OpenConversation, c.id);

                // Mismo hotspot también acepta que le arrastren una pista encima (Confrontar).
                var confrontTarget = hotspot.AddComponent<ClueConfrontTarget>();
                var ctSo = new SerializedObject(confrontTarget);
                ctSo.FindProperty("characterId").stringValue = c.id;
                ctSo.ApplyModifiedPropertiesWithoutUndo();

                x += 260f;
            }

            x = -300f;
            foreach (var s in loc.investigateSpots)
            {
                CreateHotspot(panel.transform, "[Investigar]\n" + s.label, new Vector2(x, -160f), new Vector2(200f, 90f),
                    new Color(0.2f, 0.35f, 0.45f, 0.9f), InteractType.InvestigateSpot, s.id);
                x += 220f;
            }

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateHotspot(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Color color, InteractType type, string payload)
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

        private static void CreateNavBar(Transform parent, List<LocationDef> locations)
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
            bg.color = new Color(0f, 0f, 0f, 0.6f);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach (var loc in locations)
            {
                CreateHotspot(bar.transform, loc.displayName, Vector2.zero, new Vector2(150f, 54f),
                    new Color(0.25f, 0.25f, 0.25f, 0.95f), InteractType.GoToLocation, loc.id);
            }
        }

        private static Transform CreateEvidencePanel(Transform parent)
        {
            var bar = new GameObject("EvidencePanel", typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.sizeDelta = new Vector2(0f, 64f);
            barRect.anchoredPosition = new Vector2(0f, 70f); // justo arriba de la NavBar

            var bg = bar.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.55f);

            var outerLayout = bar.AddComponent<HorizontalLayoutGroup>();
            outerLayout.childAlignment = TextAnchor.MiddleLeft;
            outerLayout.spacing = 8f;
            outerLayout.padding = new RectOffset(12, 12, 6, 6);
            outerLayout.childForceExpandWidth = false;
            outerLayout.childForceExpandHeight = true;

            var label = CreateLabel(bar.transform, "Pistas:", 18f, TextAlignmentOptions.MidlineLeft, new Color(1f, 1f, 1f, 0.6f));
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 70f;

            var chipsContainer = new GameObject("ChipsContainer", typeof(RectTransform));
            chipsContainer.transform.SetParent(bar.transform, false);
            var chipsLayout = chipsContainer.AddComponent<HorizontalLayoutGroup>();
            chipsLayout.spacing = 6f;
            chipsLayout.childAlignment = TextAnchor.MiddleLeft;
            chipsLayout.childForceExpandWidth = false;
            chipsLayout.childForceExpandHeight = true;
            chipsContainer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            return chipsContainer.transform;
        }

        private static void CreateAccuseButton(Transform parent)
        {
            var go = CreateHotspot(parent, "ACUSAR", Vector2.zero, new Vector2(160f, 54f),
                new Color(0.5f, 0.1f, 0.1f, 0.95f), InteractType.OpenAccusation, "");

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -16f);
        }

        private static TextMeshProUGUI CreateHud(Transform parent)
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
            return text;
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
