using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Menú de temas + Presionar/Confrontar, reproducidos con StoryUIController.ShowDialogue
    // y StoryUIController.ShowChoices (StoryChoiceOption ya es genérico y no depende de
    // StoryGraph/StoryRunner, así que se reutiliza tal cual). No pasa por StoryGraph.
    public class ConversationController : MonoBehaviour
    {
        public static ConversationController Instance { get; private set; }

        [SerializeField] private StoryUIController uiController;

        private const string LeaveOptionId = "__leave";
        private bool isBusy;

        private StoryUIController UI => uiController != null ? uiController : StoryUIController.Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            StoryInteractable.OnOpenConversationRequested += Open;
            StoryInteractable.OnInvestigateRequested += Investigate;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                StoryInteractable.OnOpenConversationRequested -= Open;
                StoryInteractable.OnInvestigateRequested -= Investigate;
            }
        }

        public void Open(string characterId)
        {
            if (isBusy) return;
            StartCoroutine(OpenRoutine(characterId));
        }

        public void Confront(string characterId, string clueId)
        {
            if (isBusy) return;
            StartCoroutine(ConfrontRoutine(characterId, clueId));
        }

        public void Investigate(string spotId)
        {
            if (isBusy) return;
            StartCoroutine(InvestigateRoutine(spotId));
        }

        private IEnumerator OpenRoutine(string characterId)
        {
            isBusy = true;

            var character = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetCharacter(characterId) : null;
            if (character == null)
            {
                Debug.LogWarning($"[ConversationController] No hay datos de diálogo para '{characterId}'.");
                isBusy = false;
                yield break;
            }

            int day = CaseState.Instance != null ? CaseState.Instance.currentDay : 2;
            int phase = CaseState.Instance != null ? CaseState.Instance.currentPhase : 1;

            // 1. Saludo / Diálogo ambiental del personaje antes del menú de temas
            string greeting = GetAmbientGreeting(character.id, day, phase);
            if (!string.IsNullOrEmpty(greeting))
            {
                yield return UI.ShowDialogue(character.displayName, greeting, null, null, -1f, true);
            }

            // 2. Obtener temas disponibles para la fase actual
            var visibleTopics = GetVisibleTopics(character);

            if (visibleTopics.Count == 0)
            {
                yield return UI.ShowDialogue(character.displayName, "No tengo nada más que decir por ahora.", null, null, -1f, true);
                UI.HideDialogue();
                isBusy = false;
                yield break;
            }

            var options = visibleTopics.Select(t =>
            {
                bool seen = CaseState.Instance != null && CaseState.Instance.HasSeenTopic(character.id, t.id);
                string label = seen ? $"{t.displayName} [✓]" : t.displayName;
                return new StoryChoiceOption { id = t.id, text = label };
            }).ToList();
            options.Add(new StoryChoiceOption { id = LeaveOptionId, text = "Dejar de hablar por ahora" });

            int selected = -1;
            yield return UI.ShowChoices($"¿De qué hablar con {character.displayName}?", options, idx => selected = idx);

            if (selected >= 0 && selected < options.Count && options[selected].id != LeaveOptionId)
            {
                var topic = visibleTopics.First(t => t.id == options[selected].id);
                yield return PlayTopic(character, topic);
            }
            else
            {
                UI.HideDialogue();
            }

            isBusy = false;
        }

        private string GetAmbientGreeting(string charId, int day, int phase)
        {
            switch (charId)
            {
                case "elena":
                    if (day == 2)
                    {
                        if (phase == 1) return "¿Qué... qué necesita, señor Miller? No pude pegar un ojo con todo lo que pasó anoche.";
                        if (phase == 2) return "Dígame rápido, por favor... no quiero tener problemas con Robert.";
                        return "La noche se pone tensa acá... ¿qué busca ahora?";
                    }
                    return "Sigue dando vueltas por acá... ¿averiguó algo sobre Carla?";

                case "robert":
                    if (day == 2)
                    {
                        if (phase == 1) return "Buen día, detective. Intento mantener la calma en el motel tras la tragedia. ¿En qué lo puedo ayudar?";
                        if (phase == 2) return "Señor Miller. ¿Encontró algo de utilidad para esclarecer el asunto?";
                        return "Se hace tarde, detective. ¿Necesita algo antes de que cierre la recepción?";
                    }
                    return "Último día por acá, entiendo. Espero que sus conclusiones sean justas y profesionales.";

                case "gus":
                    if (day == 2)
                    {
                        if (phase == 1) return "Lindo lío se armó en el motel anoche, ¿no? Qué nochecita.";
                        if (phase == 2) return "El motor de mi camión ya casi está a punto. ¿Qué cuenta el detective?";
                        return "Por acá de noche no se ve nada. Salvo lo que uno preferiría no ver.";
                    }
                    return "Apenas cargue el acoplado me voy de este pueblo. Me da mala espina.";

                case "ernesto":
                    if (day == 2)
                    {
                        if (phase == 1) return "¿Usted otra vez? Estoy ocupado acomodando pedidos, sea breve.";
                        if (phase == 2) return "Las alfombras no se van a vender solas. ¿Qué quiere ahora?";
                        return "Ya es hora de cerrar. Si no va a comprar nada, no moleste.";
                    }
                    return "¿Todavía dando vueltas? Ya le dije a la policía todo lo que tenía para decir.";

                case "marta":
                    if (day == 2)
                    {
                        if (phase == 1) return "Día frío para estar investigando crímenes, señor. ¿Le sirvo un café o busca respuestas?";
                        if (phase == 2) return "Carla solía venir a esta hora a sentarse junto al ventanal. Qué pena de chica...";
                        return "Cierro en un rato. Tenga cuidado si anda dando vueltas afuera de noche.";
                    }
                    return "Espero que encuentre al culpable de lo de Carla. Este pueblo necesita cerrar esa herida.";

                case "frank":
                    if (day == 2)
                    {
                        if (phase <= 2) return "¡Eh, el del auto roto! Terrible lo del motel. Menos mal que Robert tiene la cabeza fría.";
                        return "La noche trae de todo por la estación de servicio. ¿Qué se le ofrece?";
                    }
                    return "No me gusta el clima que hay en el pueblo hoy. Ojalá todo se aclare pronto.";

                case "mark":
                    if (day == 2)
                    {
                        return "Ruidos... pasos en la noche... no me miren a mí, yo no hice nada...";
                    }
                    return "El sótano... nadie me cree, pero ellos saben lo que hay ahí abajo...";

                default:
                    return "¿Sí? ¿En qué puedo ayudarlo?";
            }
        }

        private IEnumerator ConfrontRoutine(string characterId, string clueId)
        {
            isBusy = true;

            var character = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetCharacter(characterId) : null;
            if (character != null)
            {
                CaseState.Instance.RecordConfrontation(characterId, clueId);

                var topic = character.topics.FirstOrDefault(t => t.kind == TopicKind.Confrontar);
                if (topic == null)
                {
                    Debug.LogWarning($"[ConversationController] '{characterId}' no tiene un tema de tipo Confrontar definido.");
                }
                else if (CaseState.Instance.EvaluateAll(topic.unlockConditions))
                {
                    yield return PlayTopic(character, topic);
                }
                else
                {
                    yield return UI.ShowDialogue(character.displayName, "Todavía no tengo nada que decir sobre eso.", null, null, -1f, true);
                    UI.HideDialogue();
                }
            }

            isBusy = false;
        }

        private IEnumerator InvestigateRoutine(string spotId)
        {
            isBusy = true;

            var spot = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetInvestigateSpot(spotId) : null;
            if (spot == null)
            {
                Debug.LogWarning($"[ConversationController] No hay datos para el punto de investigación '{spotId}'.");
                isBusy = false;
                yield break;
            }

            if (!CaseState.Instance.EvaluateAll(spot.unlockConditions))
            {
                isBusy = false;
                yield break;
            }

            var variant = ResolveVariant(spot.variants);
            if (variant != null)
            {
                foreach (var line in variant.lines)
                {
                    yield return UI.ShowDialogue(line.speaker, line.text, null, null, -1f, true);
                }

                CaseState.Instance.ApplyAll(variant.effects);

                // Investigar consume 1 acción
                PhaseController.Instance.SpendAction();
                UI.HideDialogue();
            }

            isBusy = false;
        }

        private List<TopicData> GetVisibleTopics(CharacterData character)
        {
            // Confrontar también aparece en el menú (no exige arrastrar una pista física):
            // se lo trata como cualquier otro tema, gateado por sus propias unlockConditions.
            var result = new List<TopicData>();
            foreach (var topic in character.topics)
            {
                if (CaseState.Instance.EvaluateAll(topic.unlockConditions)) result.Add(topic);
            }
            return result;
        }

        private IEnumerator PlayTopic(CharacterData character, TopicData topic)
        {
            var variant = ResolveVariant(topic.variants);
            if (variant == null) yield break;

            foreach (var line in variant.lines)
            {
                yield return UI.ShowDialogue(line.speaker, line.text, null, null, -1f, true);
            }

            CaseState.Instance.ApplyAll(variant.effects);

            // Relectura gratuita: solo gasta acción si el tema se escucha por primera vez
            bool isFirstTime = !CaseState.Instance.HasSeenTopic(character.id, topic.id);
            if (isFirstTime)
            {
                CaseState.Instance.MarkTopicSeen(character.id, topic.id);
                PhaseController.Instance.SpendAction();
            }

            UI.HideDialogue();
        }

        private DialogueVariantData ResolveVariant(List<DialogueVariantData> variants)
        {
            foreach (var variant in variants.OrderByDescending(v => v.priority))
            {
                if (CaseState.Instance.EvaluateAll(variant.conditions)) return variant;
            }
            return null;
        }
    }
}
