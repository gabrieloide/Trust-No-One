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

            CreateCompletePrologueGraph();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StoryGraphGenerator] ¡Prólogo completo del Día 1 generado exitosamente en " + StoriesFolder + "!");
        }

        private static void CreateCompletePrologueGraph()
        {
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            graph.graphTitle = "SG_D1_Prologo_Completo";

            var startNode = new StoryNodeData(StoryNodeType.Start, new Vector2(100, 200));
            var seqNode = new StoryNodeData(StoryNodeType.ActionSequence, new Vector2(350, 200))
            {
                title = "Prólogo Día 1 (Completo)"
            };

            // ==========================================
            // 1. APERTURA: CARRETERA (DÍA 1)
            // ==========================================
            seqNode.actions.Add(new SetWorldUIAction { active = false });
            seqNode.actions.Add(new FadeScreenAction { fadeType = FadeType.FadeOut, duration = 0.1f, waitForCompletion = true });
            seqNode.actions.Add(new TravelLocationAction { targetLocationId = "road" });
            seqNode.actions.Add(new OverlayTextAction
            {
                titleText = "NO ONE IS INNOCENT",
                subtitleText = "Día 1 — La carretera de ningún lugar",
                displayMode = OverlayDisplayMode.CenterTitleCard,
                effect = OverlayEffect.Fade,
                duration = 2.5f,
                waitForClick = true
            });
            seqNode.actions.Add(new FadeScreenAction { fadeType = FadeType.FadeIn, duration = 0.6f, waitForCompletion = true });

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

            // Encuentro con Ernesto en la ruta
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "A unos metros, un hombre carga cajas hacia una camioneta. Se frena en seco al verme, como si hubiera memorizado cada cara del pueblo y la mía no encajara.",
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
            seqNode.actions.Add(new CaseFlagAction { operation = CaseFlagAction.Operation.SetFlag, key = "d1_ernesto_talked" });

            // ==========================================
            // 2. RECEPCIÓN DEL MOTEL (CHECK-IN)
            // ==========================================
            seqNode.actions.Add(new FadeScreenAction { fadeType = FadeType.FadeOut, duration = 0.4f, waitForCompletion = true });
            seqNode.actions.Add(new TravelLocationAction { targetLocationId = "motel" });
            seqNode.actions.Add(new FadeScreenAction { fadeType = FadeType.FadeIn, duration = 0.4f, waitForCompletion = true });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Camino hasta la recepción del motel. Una mujer joven atiende el mostrador sin levantar mucho la vista.",
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
                dialogueText = "Me entrega una llave de bronce y vuelve a sus papeles antes de que pueda agregar una sola palabra.",
                waitForClick = true
            });
            seqNode.actions.Add(new CaseFlagAction { operation = CaseFlagAction.Operation.SetFlag, key = "d1_elena_talked" });

            // Bienvenida de Robert
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Desde el pasillo sale un hombre de mediana edad, impecable y con una sonrisa amplia que contrasta con la recepcionista.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Robert",
                dialogueText = "¡Bienvenido, bienvenido! No todos los días se le rompe el auto a alguien justo frente a nuestro motel. Tiene suerte, en el sentido más raro posible.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "Solo necesito pasar la noche hasta que llegue el auxilio mañana.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Robert",
                dialogueText = "Por supuesto. Le toca la habitación 4, es la más tranquila de todas. Si necesita cualquier cosa, a cualquier hora, golpee mi puerta sin dudar.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Todo en él es cortesía calculada al milímetro. El tipo de anfitrión que uno recuerda por lo perfecto, no por lo cálido.",
                waitForClick = true
            });
            seqNode.actions.Add(new CaseFlagAction { operation = CaseFlagAction.Operation.SetFlag, key = "d1_robert_talked" });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "Entro a la habitación 4 y cierro la puerta. Me tiro sobre la cama a esperar que pase la noche.",
                waitForClick = true
            });

            // ==========================================
            // 3. LA NOCHE DEL CRIMEN (02:15 AM)
            // ==========================================
            seqNode.actions.Add(new FadeScreenAction { fadeType = FadeType.FadeOut, duration = 0.8f, waitForCompletion = true });
            seqNode.actions.Add(new OverlayTextAction
            {
                titleText = "",
                subtitleText = "Esa misma noche... 02:15 AM",
                displayMode = OverlayDisplayMode.CenterTitleCard,
                effect = OverlayEffect.Fade,
                duration = 2.0f,
                waitForClick = true
            });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Un grito desgarrador corta la madrugada. Después, el sonido seco de vidrio rompiéndose contra el suelo.",
                waitForClick = true
            });

            seqNode.actions.Add(new FadeScreenAction { fadeType = FadeType.FadeIn, duration = 0.5f, waitForCompletion = true });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Salgo al pasillo alertado por el estruendo. Apenas alcanzo a ver la silueta de Elena corriendo despavorida hacia la salida trasera.",
                waitForClick = true
            });
            seqNode.actions.Add(new CollectClueAction { clueId = "elena_seen_running" });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "Sigo el ruido hasta la parte trasera del motel, cerca del acceso al sótano. Hay una mujer inmóvil en el suelo, rodeada de vidrios rotos.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "",
                dialogueText = "En cuestión de segundos aparece Robert, perfectamente peinado y vestido, como si nunca se hubiera acostado.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "¿Qué fue ese grito? ¿Quién es la mujer del suelo?",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Robert",
                dialogueText = "Es Carla... Carla Rossi. Pobrecita, Dios mío... parece que tropezó con una botella o alguien la atacó en la oscuridad.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "¿Llegó usted muy rápido, no? Apenas se escuchó el golpe.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Robert",
                dialogueText = "Tengo el sueño ligero cuando se trata de la seguridad de mi motel, señor Miller. Ya avisé al sheriff del condado, estarán aquí a primera hora.",
                waitForClick = true
            });
            seqNode.actions.Add(new CollectClueAction { clueId = "robert_quick_arrival" });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Robert",
                dialogueText = "Le sugiero volver a su habitación y cerrar con llave. Esto no es algo que un huésped deba presenciar.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "Vuelvo a mi habitación. Pero en este motel, ya nadie va a poder dormir.",
                waitForClick = true
            });

            // ==========================================
            // 4. AMANECER DEL DÍA 2 (INVESTIGACIÓN LIBRE)
            // ==========================================
            seqNode.actions.Add(new FadeScreenAction { fadeType = FadeType.FadeOut, duration = 0.8f, waitForCompletion = true });
            seqNode.actions.Add(new OverlayTextAction
            {
                titleText = "DÍA 2",
                subtitleText = "08:00 AM — El motel bajo sospecha",
                displayMode = OverlayDisplayMode.CenterTitleCard,
                effect = OverlayEffect.Fade,
                duration = 2.5f,
                waitForClick = true
            });

            seqNode.actions.Add(new SetDayPhaseAction { targetDay = 2, targetPhase = 1, actionsRemaining = 4 });
            seqNode.actions.Add(new SetWorldUIAction { active = true });
            seqNode.actions.Add(new TravelLocationAction { targetLocationId = "motel" });
            seqNode.actions.Add(new FadeScreenAction { fadeType = FadeType.FadeIn, duration = 0.6f, waitForCompletion = true });

            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "A la mañana siguiente, Frank me avisa desde la gasolinera que el repuesto de mi auto tardará dos días en llegar desde la ciudad.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "Tras el crimen de Carla Rossi, Robert me ofreció extender mi estadía en la habitación 4 sin cargo mientras colabore como testigo. Tengo dos días antes de que llegue la grúa y el sheriff cierre el caso.",
                waitForClick = true
            });
            seqNode.actions.Add(new DialogueAction
            {
                speakerName = "Gabe",
                dialogueText = "Todos en el pueblo tienen secretos o motivos. Voy a tener que investigar por mi cuenta.",
                waitForClick = true
            });

            var endNode = new StoryNodeData(StoryNodeType.End, new Vector2(700, 200));

            graph.nodes.Add(startNode);
            graph.nodes.Add(seqNode);
            graph.nodes.Add(endNode);

            graph.entryNodeGuid = startNode.guid;
            graph.nodeLinks.Add(new NodeLinkData(startNode.guid, "output", seqNode.guid));
            graph.nodeLinks.Add(new NodeLinkData(seqNode.guid, "output", endNode.guid));

            SaveGraph(graph, "SG_D1_Prologo_Completo.asset");
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
