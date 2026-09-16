using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Maneja conversaciones interactivas, selección de temas, saludos ambientales y confrontación con evidencias.
    public class ConversationController : MonoBehaviour
    {
        public static ConversationController Instance { get; private set; }

        private const string LeaveOptionId = "__leave__";
        private const string TalkOptionId = "__talk__";
        private const string ConfrontOptionId = "__confront__";
        private bool isBusy = false;
        public bool IsBusy => isBusy;

        private StoryUIController UI => StoryUIController.Instance;

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
                Debug.LogWarning($"[ConversationController] No dialogue data for '{characterId}'.");
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

            // 2. Menú principal: Hablar / Confrontar (si hay pistas) / Salir
            var visibleTopics = GetVisibleTopics(character);

            var mainOptions = new List<StoryChoiceOption>
            {
                new StoryChoiceOption { id = TalkOptionId, text = "Talk" }
            };

            if (CaseState.Instance != null && CaseState.Instance.CollectedClues.Count > 0)
            {
                mainOptions.Add(new StoryChoiceOption { id = ConfrontOptionId, text = "Confront..." });
            }

            mainOptions.Add(new StoryChoiceOption { id = LeaveOptionId, text = "Stop talking for now" });

            int selectedMain = -1;
            yield return UI.ShowChoices($"What do you want to do with {character.displayName}?", mainOptions, idx => selectedMain = idx);

            if (selectedMain >= 0 && selectedMain < mainOptions.Count)
            {
                string chosenId = mainOptions[selectedMain].id;
                if (chosenId == TalkOptionId)
                {
                    yield return TalkTopicsRoutine(character, visibleTopics);
                }
                else if (chosenId == ConfrontOptionId)
                {
                    yield return PresentEvidenceRoutine(character);
                }
                else
                {
                    UI.HideDialogue();
                }
            }
            else
            {
                UI.HideDialogue();
            }

            isBusy = false;
        }

        private IEnumerator TalkTopicsRoutine(CharacterData character, List<TopicData> visibleTopics)
        {
            var options = visibleTopics.Select(t =>
            {
                bool seen = CaseState.Instance != null && CaseState.Instance.HasSeenTopic(character.id, t.id);
                string label = seen ? $"{t.displayName} [seen]" : t.displayName;
                return new StoryChoiceOption { id = t.id, text = label };
            }).ToList();

            options.Add(new StoryChoiceOption { id = LeaveOptionId, text = "Never mind" });

            int selected = -1;
            yield return UI.ShowChoices($"What to talk about with {character.displayName}?", options, idx => selected = idx);

            if (selected >= 0 && selected < options.Count && options[selected].id != LeaveOptionId)
            {
                var topic = visibleTopics.First(t => t.id == options[selected].id);
                yield return PlayTopic(character, topic);
            }
            else
            {
                UI.HideDialogue();
            }
        }

        private IEnumerator PresentEvidenceRoutine(CharacterData character)
        {
            var clues = CaseState.Instance.CollectedClues
                .Select(id => DialogueDatabase.Instance.GetClue(id))
                .Where(c => c != null)
                .ToList();

            if (clues.Count == 0)
            {
                yield return UI.ShowDialogue(character.displayName, "You don't have any clues to show me.", null, null, -1f, true);
                UI.HideDialogue();
                yield break;
            }

            var clueOptions = clues.Select(c =>
            {
                bool tried = CaseState.Instance != null && CaseState.Instance.WasConfrontedWith(character.id, c.id);
                string label = tried ? $"{c.displayName} [used]" : c.displayName;
                return new StoryChoiceOption { id = c.id, text = label };
            }).ToList();
            clueOptions.Add(new StoryChoiceOption { id = "__cancel__", text = "Back to the questions" });

            int clueIndex = -1;
            yield return UI.ShowChoices($"What evidence to present to {character.displayName}?", clueOptions, idx => clueIndex = idx);

            if (clueIndex >= 0 && clueIndex < clueOptions.Count && clueOptions[clueIndex].id != "__cancel__")
            {
                string clueId = clueOptions[clueIndex].id;
                yield return ConfrontRoutine(character.id, clueId);
            }
            else
            {
                UI.HideDialogue();
            }
        }

        public IEnumerator ConfrontRoutine(string characterId, string clueId)
        {
            isBusy = true;

            var character = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetCharacter(characterId) : null;
            var clue = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetClue(clueId) : null;

            if (character != null && clue != null)
            {
                // Cuesta 1 acción la primera vez que se prueba este par (personaje, pista),
                // acierte o no — repetir el mismo par ya intentado no vuelve a cobrar.
                bool firstAttempt = CaseState.Instance != null && !CaseState.Instance.WasConfrontedWith(characterId, clueId);

                CaseState.Instance.RecordConfrontation(characterId, clueId);

                bool isRelevant = ExecuteConfrontationReaction(characterId, clueId, out List<(string speaker, string text)> lines, out Action onFinish);

                foreach (var line in lines)
                {
                    yield return UI.ShowDialogue(DialogueDatabase.Instance.ResolveSpeakerDisplayName(line.speaker), line.text, null, null, -1f, true);
                }

                // Solo gasta acción si la confrontación fue incorrecta/irrelevante en su primer intento (para evitar spam).
                // Las confrontaciones acertadas (isRelevant == true) recompensan al jugador no gastando acción.
                if (!isRelevant && firstAttempt && PhaseController.Instance != null)
                {
                    PhaseController.Instance.SpendAction();
                }

                if (isRelevant)
                {
                    onFinish?.Invoke();
                }

                UI.HideDialogue();
            }

            isBusy = false;
        }

        private bool ExecuteConfrontationReaction(string characterId, string clueId, out List<(string speaker, string text)> lines, out Action onFinish)
        {
            lines = new List<(string speaker, string text)>();
            onFinish = null;

            if (DialogueDatabase.Instance != null)
            {
                var reaction = DialogueDatabase.Instance.GetConfrontation(characterId, clueId);
                if (reaction != null)
                {
                    if (reaction.lines != null)
                    {
                        foreach (var line in reaction.lines)
                        {
                            lines.Add((line.speaker, line.text));
                        }
                    }

                    onFinish = () =>
                    {
                        if (reaction.effects != null && CaseState.Instance != null)
                        {
                            CaseState.Instance.ApplyAll(reaction.effects);
                        }
                    };
                    return true;
                }

                string defaultRejection = DialogueDatabase.Instance.GetDefaultRejection(characterId);
                lines.Add((characterId, defaultRejection));
                return false;
            }

            lines.Add((characterId, "I have nothing to say about that object."));
            return false;
        }

        private string GetAmbientGreeting(string charId, int day, int phase)
        {
            if (DialogueDatabase.Instance != null)
            {
                return DialogueDatabase.Instance.GetAmbientGreeting(charId, day, phase);
            }
            return "Yes? How can I help you?";
        }

        private IEnumerator InvestigateRoutine(string spotId)
        {
            isBusy = true;

            var spot = DialogueDatabase.Instance != null ? DialogueDatabase.Instance.GetInvestigateSpot(spotId) : null;
            if (spot == null)
            {
                Debug.LogWarning($"[ConversationController] No investigate-spot data for '{spotId}'.");
                isBusy = false;
                yield break;
            }

            var variant = PickVariant(spot.variants);
            if (variant != null)
            {
                foreach (var line in variant.lines)
                {
                    yield return UI.ShowDialogue(DialogueDatabase.Instance.ResolveSpeakerDisplayName(line.speaker), line.text, null, null, -1f, true);
                }

                // Solo gasta acción si es la primera vez que se inspecciona
                string spotKey = $"spot_seen_{spot.id}";
                if (!CaseState.Instance.HasFlag(spotKey))
                {
                    CaseState.Instance.SetFlag(spotKey);
                    if (PhaseController.Instance != null)
                    {
                        PhaseController.Instance.SpendAction();
                    }
                }

                ApplyEffects(variant.effects);
            }

            UI.HideDialogue();
            isBusy = false;
        }

        private List<TopicData> GetVisibleTopics(CharacterData character)
        {
            if (character == null || character.topics == null) return new List<TopicData>();
            var cs = CaseState.Instance;
            return character.topics
                .Where(t => t.kind != TopicKind.Confrontar && (cs == null || cs.EvaluateAll(t.unlockConditions)))
                .ToList();
        }

        private IEnumerator PlayTopic(CharacterData character, TopicData topic)
        {
            var variant = PickVariant(topic.variants);
            if (variant != null)
            {
                foreach (var line in variant.lines)
                {
                    yield return UI.ShowDialogue(DialogueDatabase.Instance.ResolveSpeakerDisplayName(line.speaker), line.text, null, null, -1f, true);
                }

                // Solo gasta acción si es la primera vez que se lee este tema
                if (!CaseState.Instance.HasSeenTopic(character.id, topic.id))
                {
                    CaseState.Instance.MarkTopicSeen(character.id, topic.id);
                    if (PhaseController.Instance != null)
                    {
                        PhaseController.Instance.SpendAction();
                    }
                }

                ApplyEffects(variant.effects);
            }

            UI.HideDialogue();
        }

        private DialogueVariantData PickVariant(List<DialogueVariantData> variants)
        {
            if (variants == null || variants.Count == 0) return null;
            var cs = CaseState.Instance;

            var matching = variants
                .Where(v => cs == null || cs.EvaluateAll(v.conditions))
                .OrderByDescending(v => v.priority)
                .ToList();

            return matching.Count > 0 ? matching[0] : null;
        }

        private void ApplyEffects(List<EffectData> effects)
        {
            if (effects == null || CaseState.Instance == null) return;

            foreach (var effect in effects)
            {
                switch (effect.kind)
                {
                    case EffectKind.SetFlag:
                        CaseState.Instance.SetFlag(effect.a);
                        break;
                    case EffectKind.ClearFlag:
                        CaseState.Instance.ClearFlag(effect.a);
                        break;
                    case EffectKind.UnlockTopic:
                        CaseState.Instance.UnlockTopic(effect.a, effect.b);
                        break;
                    case EffectKind.CollectClue:
                        CaseState.Instance.CollectClue(effect.a);
                        break;
                    case EffectKind.IncrementCounter:
                        int inc = int.TryParse(effect.b, out var parsedInc) ? parsedInc : 1;
                        CaseState.Instance.IncrementCounter(effect.a, inc);
                        break;
                }
            }
        }
    }
}
