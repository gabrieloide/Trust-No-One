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

            // Una visita = un tema (= una acción del presupuesto de la fase). Elegir entre
            // varios temas disponibles es la decisión; "Terminar conversación" no gasta acción.
            var visibleTopics = GetVisibleTopics(character);

            if (visibleTopics.Count == 0)
            {
                yield return UI.ShowDialogue(character.displayName, "No tengo nada más que decir por ahora.", null, null, -1f, true);
                isBusy = false;
                yield break;
            }

            var options = visibleTopics.Select(t => new StoryChoiceOption { id = t.id, text = t.displayName }).ToList();
            options.Add(new StoryChoiceOption { id = LeaveOptionId, text = "Terminar conversación" });

            int selected = -1;
            yield return UI.ShowChoices($"Hablando con {character.displayName}", options, idx => selected = idx);

            if (selected >= 0 && selected < options.Count && options[selected].id != LeaveOptionId)
            {
                var topic = visibleTopics.First(t => t.id == options[selected].id);
                yield return PlayTopic(character, topic);
            }

            isBusy = false;
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
                PhaseController.Instance.SpendAction();
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
            CaseState.Instance.MarkTopicSeen(character.id, topic.id);
            PhaseController.Instance.SpendAction();
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
