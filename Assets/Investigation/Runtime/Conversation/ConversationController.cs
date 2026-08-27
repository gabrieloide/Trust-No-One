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
                mainOptions.Add(new StoryChoiceOption { id = ConfrontOptionId, text = "🔍 Confront..." });
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
                string label = seen ? $"{t.displayName} [✓]" : t.displayName;
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
                string label = tried ? $"{c.displayName} [✓]" : c.displayName;
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
                    yield return UI.ShowDialogue(line.speaker, line.text, null, null, -1f, true);
                }

                if (firstAttempt && PhaseController.Instance != null)
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

            switch (characterId)
            {
                case "ernesto":
                    if (clueId == "carpet_fiber")
                    {
                        lines.Add(("Gabe", "I found this synthetic olive-green fiber near where Carla died. It's identical to the roll on your counter."));
                        lines.Add(("Ernesto", "Half the town has that carpet, detective! It proves nothing... nothing!"));
                        lines.Add(("Gabe", "Then why are your hands shaking, Ernesto? Where's that cut on your wrist from?"));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("ernesto_fiber_trapped");
                            CaseState.Instance.CollectClue("ernesto_false_alibi");
                            CaseState.Instance.CollectClue("ernesto_hand_cut");
                        };
                        return true;
                    }
                    if (clueId == "carpet_shop_receipt")
                    {
                        lines.Add(("Gabe", "Three sales in two months, Ernesto. This place is falling apart. Where's the money for rent coming from?"));
                        lines.Add(("Ernesto", "Now you're going through my trash? Robert gave me an extension... I just do him some maintenance favors, that's all."));
                        lines.Add(("Gabe", "Night favors in the motel basement?"));
                        lines.Add(("Ernesto", "I didn't say that! Shut up!"));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("ernesto_debt_revealed");
                            CaseState.Instance.SetFlag("robert_leverage_ernesto");
                        };
                        return true;
                    }
                    lines.Add(("Ernesto", "And what do you want me to do with that? I don't have time to play detective. Buy something or leave."));
                    return false;

                case "robert":
                    if (clueId == "basement_lock")
                    {
                        lines.Add(("Gabe", "The basement padlock wasn't forced from outside: it gave way from a hard impact from inside."));
                        lines.Add(("Robert", "It's an old building, detective. Damp swells the beams and the locks give. Or maybe some intruder tried to take shelter from the storm."));
                        lines.Add(("Gabe", "You have the only key. And the door was locked from the outside. Makes no sense for an intruder to lock himself in."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("robert_cornered_basement");
                            CaseState.Instance.CollectClue("basement_exclusive_access");
                        };
                        return true;
                    }
                    if (clueId == "robert_quick_arrival")
                    {
                        lines.Add(("Gabe", "The first witnesses took a while to come over after the scream. You got there in under ninety seconds, dressed and groomed."));
                        lines.Add(("Robert", "It's my motel, Miller. I'm a light sleeper when it comes to my guests' safety, and I keep my work clothes handy in case of emergencies. Looking after my guests is my duty."));
                        onFinish = () => CaseState.Instance.SetFlag("robert_timing_doubt");
                        return true;
                    }
                    if (clueId == "carla_belongings")
                    {
                        lines.Add(("Gabe", "I found Carla's purse and papers hidden behind some crates near the basement. They weren't in her room."));
                        lines.Add(("Robert", "Someone must have moved them... some dishonest employee, or the attacker himself. I'll look into this with the sheriff."));
                        onFinish = () => CaseState.Instance.SetFlag("robert_belongings_exposed");
                        return true;
                    }
                    if (clueId == "basement_noises_match")
                    {
                        lines.Add(("Gabe", "There were noises coming from the basement at odd hours of the night."));
                        lines.Add(("Robert", "I do the heavy maintenance work in the early hours so I don't bother guests with the water pumps. If you're looking for suspects, ask the vagrant who hangs around the gas station."));
                        onFinish = () => CaseState.Instance.SetFlag("robert_excuse_locked");
                        return true;
                    }
                    lines.Add(("Robert", "A curious object, detective. But I don't see what it has to do with running the Starlight."));
                    return false;

                case "elena":
                    if (clueId == "elena_master_keys")
                    {
                        lines.Add(("Gabe", "There are only two master keys in the whole complex: Robert's and yours. Nobody can get into the rooms without them."));
                        lines.Add(("Elena", "I didn't open Carla's door that night... I swear it. But I saw Robert leaving the office with his set of keys close to midnight."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("elena_implicates_robert");
                            CaseState.Instance.CollectClue("robert_quick_arrival");
                        };
                        return true;
                    }
                    if (clueId == "elena_seen_running")
                    {
                        lines.Add(("Gabe", "A witness saw you bolt down the back hallway right after the scream."));
                        lines.Add(("Elena", "I was terrified! I heard Robert arguing with her... heard a hard thud, and I ran to lock myself in my room. I didn't want to be next."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("elena_confession_full");
                            CaseState.Instance.CollectClue("elena_alibi_gap");
                        };
                        return true;
                    }
                    if (clueId == "carla_belongings")
                    {
                        lines.Add(("Elena", "That's the bag Carla left at the front desk... my God. Robert flat-out forbade me from going into the basement or touching anything."));
                        onFinish = () => CaseState.Instance.SetFlag("elena_confirmed_coverup");
                        return true;
                    }
                    lines.Add(("Elena", "I don't know what that is, Mr. Miller... please, don't put me in any more trouble than I already have."));
                    return false;

                case "mark":
                    if (clueId == "bottle_was_marks" || clueId == "glass_matches_bottle")
                    {
                        lines.Add(("Gabe", "The broken glass next to the body matches the gin you bought at the gas station last night."));
                        lines.Add(("Mark", "No... no, I didn't do anything to her! I tripped on the curb... the bottle slipped out of my hands and shattered against the edge. Carla screamed and ran off toward the back of the motel."));
                        lines.Add(("Gabe", "Did you see anyone else near her?"));
                        lines.Add(("Mark", "Saw a tall shape... someone stepped out of the shadows in the hallway. I got scared and hid behind the sign."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("mark_cleared_murder");
                            CaseState.Instance.CollectClue("mark_no_memory");
                        };
                        return true;
                    }
                    if (clueId == "frank_saw_mark_and_carla")
                    {
                        lines.Add(("Gabe", "Frank saw you two cross paths near the highway before midnight."));
                        lines.Add(("Mark", "I asked her for some change... she gave me a crumpled bill and said she had to leave town before sunrise. That's all it was!"));
                        onFinish = () => CaseState.Instance.SetFlag("mark_carla_warning_revealed");
                        return true;
                    }
                    lines.Add(("Mark", "Lights and shadows... that doesn't tell me anything. The noises come from the night..."));
                    return false;

                case "frank":
                    if (clueId == "frank_saw_mark_and_carla")
                    {
                        lines.Add(("Gabe", "Frank, when you saw Mark with Carla, was anyone else watching from the shadows?"));
                        lines.Add(("Frank", "Well... now that I think back... Ernesto's truck rolled by slow on the shoulder with the lights off, two minutes later."));
                        onFinish = () =>
                        {
                            CaseState.Instance.SetFlag("frank_saw_ernesto_truck");
                            CaseState.Instance.CollectClue("ernesto_false_alibi");
                        };
                        return true;
                    }
                    if (clueId == "glass_matches_bottle")
                    {
                        lines.Add(("Gabe", "This broken bottle neck matches the gin you sell at your station."));
                        lines.Add(("Frank", "That's right. Sold it to Mark at eight at night. He staggered off toward the shoulder by the motel."));
                        onFinish = () => CaseState.Instance.CollectClue("bottle_was_marks");
                        return true;
                    }
                    lines.Add(("Frank", "No idea what that is, friend. Gasoline, I know all about; town gossip, just enough to get by."));
                    return false;

                case "marta":
                    if (clueId == "carla_belongings")
                    {
                        lines.Add(("Marta", "Poor Carla... she told me she was saving up money to leave on the bus to Seattle. Said Robert wouldn't leave her alone and that she was scared of him."));
                        onFinish = () => CaseState.Instance.SetFlag("marta_carla_seattle_confirmed");
                        return true;
                    }
                    lines.Add(("Marta", "I don't know what that is, sir. Around here people come in for coffee and to keep their mouths shut."));
                    return false;

                default:
                    lines.Add((characterId, "I have nothing to say about that object."));
                    return false;
            }
        }

        private string GetAmbientGreeting(string charId, int day, int phase)
        {
            switch (charId)
            {
                case "elena":
                    if (day == 2)
                    {
                        if (phase == 1) return "What... what do you need, Mr. Miller? I couldn't sleep a wink after everything that happened last night.";
                        if (phase == 2) return "Tell me quickly, please... I don't want trouble with Robert.";
                        return "Things get tense around here at night... what are you after now?";
                    }
                    return "Still poking around here... find out anything about Carla?";

                case "robert":
                    if (day == 2)
                    {
                        if (phase == 1) return "Good morning, detective. Trying to keep things calm at the motel after the tragedy. How can I help you?";
                        if (phase == 2) return "Mr. Miller. Find anything useful to clear this up?";
                        return "It's getting late, detective. Need anything before I close up the front desk?";
                    }
                    return "Last day around here, I understand. I hope your conclusions are fair and professional.";

                case "gus":
                    if (day == 2)
                    {
                        if (phase == 1) return "Quite a mess at the motel last night, huh? Some night.";
                        if (phase == 2) return "My truck's engine is almost good to go. What's the detective got?";
                        return "Can't see a thing out here at night. Except what a man would rather not see.";
                    }
                    return "Soon as I load the trailer I'm leaving this town. Gives me a bad feeling.";

                case "ernesto":
                    if (day == 2)
                    {
                        if (phase == 1) return "You again? I'm busy stocking orders, make it quick.";
                        if (phase == 2) return "Carpets don't sell themselves. What do you want now?";
                        return "About to close up. If you're not buying, don't bother me.";
                    }
                    return "Still hanging around? Already told the police everything I had to say.";

                case "marta":
                    if (day == 2)
                    {
                        if (phase == 1) return "Cold day to be investigating crimes, sir. Coffee, or are you after answers?";
                        if (phase == 2) return "Carla used to come sit by the window around this hour. Such a shame about that girl...";
                        return "Closing up soon. Be careful if you're out wandering at night.";
                    }
                    return "I hope you find whoever did this to Carla. This town needs that wound closed.";

                case "frank":
                    if (day == 2)
                    {
                        if (phase <= 2) return "Hey, the broken-down-car guy! Terrible business at the motel. Good thing Robert's keeping a level head.";
                        return "The station sees all sorts at night. What can I do for you?";
                    }
                    return "Don't like the mood in this town today. Hope it all gets sorted out soon.";

                case "mark":
                    if (day == 2)
                    {
                        return "Noises... footsteps in the night... don't look at me, I didn't do anything...";
                    }
                    return "The basement... nobody believes me, but they know what's down there...";

                default:
                    return "Yes? How can I help you?";
            }
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
                    yield return UI.ShowDialogue(line.speaker, line.text, null, null, -1f, true);
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
                    yield return UI.ShowDialogue(line.speaker, line.text, null, null, -1f, true);
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
