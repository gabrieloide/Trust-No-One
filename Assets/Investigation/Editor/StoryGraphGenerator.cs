using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation.EditorTools
{
    public static class StoryGraphGenerator
    {
        private const string StoriesFolder = "Assets/Investigation/Stories/Dia1";

        [MenuItem("Tools/Investigation/Generate Day 1 StoryGraphs")]
        public static void GenerateDay1Graphs()
        {
            if (!Directory.Exists(StoriesFolder))
            {
                Directory.CreateDirectory(StoriesFolder);
                AssetDatabase.Refresh();
            }

            CreateIntroGraph();
            CreateErnestoGraph();
            CreateElenaGraph();
            CreateRobertGraph();
            CreateNight1Graph();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StoryGraphGenerator] ¡Todos los StoryGraphs del Día 1 fueron generados exitosamente en " + StoriesFolder + "!");
        }

        private static void CreateIntroGraph()
        {
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.graphTitle = "SG_D1_Intro_Carretera";

            var startNode = new StoryNodeData(StoryNodeType.Start, new Vector2(100, 200));
            var seqNode = new StoryNodeData(StoryNodeType.ActionSequence, new Vector2(350, 200))
            {
                title = "Secuencia Intro"
            };

            seqNode.actions.Add(new OverlayTextAction
            {
                titleText = "DÍA 1",
                subtitleText = "La carretera de ningún lugar",
                displayMode = OverlayDisplayMode.CenterTitleCard,
                effect = OverlayEffect.Fade,
                duration = 2.0f,
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "El auto se apaga a diez minutos de cualquier cosa. El único cartel en kilómetros dice MOTEL, con una flecha pintada a mano.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "No va a venir ninguna grúa antes de mañana. Voy a tener que quedarme.",
                waitForClick = true
            });

            seqNode.actions.Add(new TravelLocationAction
            {
                targetLocationId = "road"
            });

            var endNode = new StoryNodeData(StoryNodeType.End, new Vector2(700, 200));

            graph.nodes.Add(startNode);
            graph.nodes.Add(seqNode);
            graph.nodes.Add(endNode);

            graph.entryNodeGuid = startNode.guid;
            graph.nodeLinks.Add(new NodeLinkData(startNode.guid, "output", seqNode.guid));
            graph.nodeLinks.Add(new NodeLinkData(seqNode.guid, "output", endNode.guid));

            SaveGraph(graph, "SG_D1_Intro.asset");
        }

        private static void CreateErnestoGraph()
        {
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.graphTitle = "SG_D1_Ernesto_Ruta";

            var startNode = new StoryNodeData(StoryNodeType.Start, new Vector2(100, 200));
            var seqNode = new StoryNodeData(StoryNodeType.ActionSequence, new Vector2(350, 200))
            {
                title = "Encuentro con Ernesto"
            };

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Un hombre carga cajas hacia una camioneta. Se frena en seco al verme, como si hubiera memorizado cada cara del pueblo y la mía no encajara.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Ernesto",
                dialogueText = "¿Usted es nuevo por acá? No... no suelo ver caras nuevas seguido.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "Se me rompió el auto en la ruta. Voy a quedarme una noche en el motel.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Ernesto",
                dialogueText = "El motel. Claro. Buena suerte con eso.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Lo dice como quien repite un chiste privado que no piensa explicar. Sigue cargando cajas sin volver a mirarme.",
                waitForClick = true
            });

            seqNode.actions.Add(new SpendActionAction());
            seqNode.actions.Add(new CaseFlagAction { operation = CaseFlagAction.Operation.SetFlag, key = "d1_ernesto_talked" });

            var endNode = new StoryNodeData(StoryNodeType.End, new Vector2(700, 200));

            graph.nodes.Add(startNode);
            graph.nodes.Add(seqNode);
            graph.nodes.Add(endNode);

            graph.entryNodeGuid = startNode.guid;
            graph.nodeLinks.Add(new NodeLinkData(startNode.guid, "output", seqNode.guid));
            graph.nodeLinks.Add(new NodeLinkData(seqNode.guid, "output", endNode.guid));

            SaveGraph(graph, "SG_D1_Ernesto_Ruta.asset");
        }

        private static void CreateElenaGraph()
        {
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.graphTitle = "SG_D1_Elena_CheckIn";

            var startNode = new StoryNodeData(StoryNodeType.Start, new Vector2(100, 200));
            var seqNode = new StoryNodeData(StoryNodeType.ActionSequence, new Vector2(350, 200))
            {
                title = "Check-In con Elena"
            };

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Una mujer joven atiende el mostrador sin levantar mucho la vista.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Elena",
                dialogueText = "¿Cuántas noches?",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "Una, con suerte.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Elena",
                dialogueText = "Acá nadie se queda una sola noche por suerte.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Lo dice sin ánimo de broma. Me da la llave y vuelve a lo suyo antes de que pueda preguntar algo más.",
                waitForClick = true
            });

            seqNode.actions.Add(new SpendActionAction());
            seqNode.actions.Add(new CaseFlagAction { operation = CaseFlagAction.Operation.SetFlag, key = "d1_elena_talked" });

            var endNode = new StoryNodeData(StoryNodeType.End, new Vector2(700, 200));

            graph.nodes.Add(startNode);
            graph.nodes.Add(seqNode);
            graph.nodes.Add(endNode);

            graph.entryNodeGuid = startNode.guid;
            graph.nodeLinks.Add(new NodeLinkData(startNode.guid, "output", seqNode.guid));
            graph.nodeLinks.Add(new NodeLinkData(seqNode.guid, "output", endNode.guid));

            SaveGraph(graph, "SG_D1_Elena_CheckIn.asset");
        }

        private static void CreateRobertGraph()
        {
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.graphTitle = "SG_D1_Robert_Bienvenida";

            var startNode = new StoryNodeData(StoryNodeType.Start, new Vector2(100, 200));
            var seqNode = new StoryNodeData(StoryNodeType.ActionSequence, new Vector2(350, 200))
            {
                title = "Bienvenida de Robert"
            };

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Un hombre de sonrisa fácil sale a recibirme antes de que termine de acercarme.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Robert",
                dialogueText = "¡Bienvenido, bienvenido! No todos los días se le rompe el auto justo frente a un motel. Tiene suerte, en el sentido más raro posible.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "Necesito un cuarto para esta noche.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Robert",
                dialogueText = "Por supuesto. Le doy el número 4, es el más tranquilo. Si necesita algo, cualquier hora, golpee mi puerta.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Todo en él es cortesía calculada al milímetro. El tipo de anfitrión que uno recuerda por lo perfecto, no por lo cálido.",
                waitForClick = true
            });

            seqNode.actions.Add(new SpendActionAction());
            seqNode.actions.Add(new CaseFlagAction { operation = CaseFlagAction.Operation.SetFlag, key = "d1_robert_talked" });

            var endNode = new StoryNodeData(StoryNodeType.End, new Vector2(700, 200));

            graph.nodes.Add(startNode);
            graph.nodes.Add(seqNode);
            graph.nodes.Add(endNode);

            graph.entryNodeGuid = startNode.guid;
            graph.nodeLinks.Add(new NodeLinkData(startNode.guid, "output", seqNode.guid));
            graph.nodeLinks.Add(new NodeLinkData(seqNode.guid, "output", endNode.guid));

            SaveGraph(graph, "SG_D1_Robert_Bienvenida.asset");
        }

        private static void CreateNight1Graph()
        {
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.graphTitle = "SG_D1_Noche_Crimen";

            var startNode = new StoryNodeData(StoryNodeType.Start, new Vector2(100, 200));
            var seqNode = new StoryNodeData(StoryNodeType.ActionSequence, new Vector2(350, 200))
            {
                title = "Noche del Crimen"
            };

            seqNode.actions.Add(new OverlayTextAction
            {
                titleText = "",
                subtitleText = "Esa noche...",
                displayMode = OverlayDisplayMode.TopHeader,
                effect = OverlayEffect.Fade,
                duration = 2.0f,
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Un grito corto, cortado a la mitad. Después, vidrio rompiéndose.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Cuando salgo al pasillo alcanzo a ver a Elena, corriendo en dirección contraria al ruido.",
                waitForClick = true
            });

            seqNode.actions.Add(new CollectClueAction
            {
                clueId = "elena_seen_running"
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Para cuando llego, ya hay gente alrededor del cuerpo. Alguien fue a buscar a Robert.",
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Llega antes de lo que debería tardar cualquiera en despertarse y vestirse. Sin marcas, sin agitación, con la explicación ya lista.",
                waitForClick = true
            });

            seqNode.actions.Add(new CollectClueAction
            {
                clueId = "robert_quick_arrival"
            });

            seqNode.actions.Add(new OverlayTextAction
            {
                titleText = "",
                subtitleText = "Fin del Día 1",
                displayMode = OverlayDisplayMode.BottomTimestamp,
                effect = OverlayEffect.Fade,
                duration = 2.0f,
                waitForClick = true
            });

            seqNode.actions.Add(new OverlayTextAction
            {
                titleText = "DÍA 2",
                subtitleText = "La lista de sospechosos no incluye a Robert Hale",
                displayMode = OverlayDisplayMode.CenterTitleCard,
                effect = OverlayEffect.Fade,
                duration = 2.5f,
                waitForClick = true
            });

            seqNode.actions.Add(new TravelLocationAction
            {
                targetLocationId = "motel"
            });

            var endNode = new StoryNodeData(StoryNodeType.End, new Vector2(700, 200));

            graph.nodes.Add(startNode);
            graph.nodes.Add(seqNode);
            graph.nodes.Add(endNode);

            graph.entryNodeGuid = startNode.guid;
            graph.nodeLinks.Add(new NodeLinkData(startNode.guid, "output", seqNode.guid));
            graph.nodeLinks.Add(new NodeLinkData(seqNode.guid, "output", endNode.guid));

            SaveGraph(graph, "SG_D1_Noche_Crimen.asset");
        }

        private static void SaveGraph(StoryGraph graph, string fileName)
        {
            string path = Path.Combine(StoriesFolder, fileName).Replace("\\", "/");
            var existing = AssetDatabase.LoadAssetAtPath<StoryGraph>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(graph, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(graph, path);
            }
        }
    }
}
