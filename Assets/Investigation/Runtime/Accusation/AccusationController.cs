using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Pantalla de acusación final (única, solo Día 3): elegir sospechoso + una pista
    // como evidencia. El peso oculto de la pista nunca se muestra al jugador, solo
    // determina cuál de los 4 finales sale.
    public class AccusationController : MonoBehaviour
    {
        public static AccusationController Instance { get; private set; }

        [SerializeField] private StoryUIController uiController;

        private static readonly string[] SuspectIds = { "robert", "ernesto", "mark", "elena" };

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
            StoryInteractable.OnOpenAccusationRequested += BeginAccusation;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                StoryInteractable.OnOpenAccusationRequested -= BeginAccusation;
            }
        }

        public void BeginAccusation()
        {
            if (isBusy) return;
            StartCoroutine(AccusationRoutine());
        }

        private IEnumerator AccusationRoutine()
        {
            isBusy = true;

            if (!PhaseController.Instance.IsCaseOver)
            {
                yield return UI.ShowDialogue("", "I'm not done investigating yet. I can wait a little longer before accusing anyone.", null, null, -1f, true);
                isBusy = false;
                yield break;
            }

            var suspectOptions = SuspectIds
                .Select(id => new StoryChoiceOption { id = id, text = DialogueDatabase.Instance.GetCharacter(id)?.displayName ?? id })
                .ToList();

            int suspectIndex = -1;
            yield return UI.ShowChoices("Who do I accuse?", suspectOptions, idx => suspectIndex = idx);
            if (suspectIndex < 0 || suspectIndex >= suspectOptions.Count)
            {
                isBusy = false;
                yield break;
            }
            string suspectId = suspectOptions[suspectIndex].id;

            var collectedClues = CaseState.Instance.CollectedClues
                .Select(id => DialogueDatabase.Instance.GetClue(id))
                .Where(c => c != null)
                .ToList();

            string evidenceClueId = null;
            if (collectedClues.Count > 0)
            {
                var evidenceOptions = collectedClues
                    .Select(c => new StoryChoiceOption { id = c.id, text = c.displayName })
                    .ToList();

                int evidenceIndex = -1;
                yield return UI.ShowChoices("What do I present as evidence?", evidenceOptions, idx => evidenceIndex = idx);
                if (evidenceIndex >= 0 && evidenceIndex < evidenceOptions.Count)
                {
                    evidenceClueId = evidenceOptions[evidenceIndex].id;
                }
            }

            string suspectDisplayName = DialogueDatabase.Instance.GetCharacter(suspectId)?.displayName ?? suspectId;
            var confirmOptions = new List<StoryChoiceOption>
            {
                new StoryChoiceOption { id = "confirm", text = $"Yes, formally accuse {suspectDisplayName}" },
                new StoryChoiceOption { id = "cancel", text = "Go back" }
            };

            int confirmIndex = -1;
            yield return UI.ShowChoices($"Close the case and accuse {suspectDisplayName}?", confirmOptions, idx => confirmIndex = idx);
            if (confirmIndex != 0)
            {
                UI.HideDialogue();
                isBusy = false;
                yield break;
            }

            yield return ResolveOutcome(suspectId, evidenceClueId);

            isBusy = false;
        }

        private IEnumerator ResolveOutcome(string suspectId, string evidenceClueId)
        {
            bool accusedRobert = suspectId == "robert";

            var evidence = evidenceClueId != null ? DialogueDatabase.Instance.GetClue(evidenceClueId) : null;
            bool hasStrongRelevant = evidence != null
                && evidence.hiddenWeight == ClueWeight.Strong
                && evidence.pointsTo.Contains(suspectId);

            string suspectName = DialogueDatabase.Instance.GetCharacter(suspectId)?.displayName ?? suspectId;

            // ACTO 1: Acusación directa de Gabe
            yield return UI.ShowDialogue("Gabe", $"It was you, {suspectName}. This whole time you've been staging the scene and playing innocent.", null, null, -1f, true);

            // ACTO 2: Réplica del sospechoso
            switch (suspectId)
            {
                case "robert":
                    yield return UI.ShowDialogue("Robert", "And what evidence do you plan to back up such an outrageous claim with, detective? Because so far all you have are guesses and hallway gossip.", null, null, -1f, true);
                    break;
                case "ernesto":
                    yield return UI.ShowDialogue("Ernesto", "You're crazy! I didn't do anything to that woman! You can't prove a single word of what you're saying!", null, null, -1f, true);
                    break;
                case "mark":
                    yield return UI.ShowDialogue("Mark", "No, no, no! It wasn't me! There were noises downstairs, I swear, but I never touched Carla! Don't lock me up again!", null, null, -1f, true);
                    break;
                case "elena":
                    yield return UI.ShowDialogue("Elena", "Me? Why are you looking at me? All I did was run out of fear when I heard the crash... this is insane.", null, null, -1f, true);
                    break;
            }

            // ACTO 3: Presentación de la Evidencia y Reacción
            if (evidence != null)
            {
                yield return UI.ShowOverlay("EVIDENCE PRESENTED", evidence.displayName, OverlayDisplayMode.TopHeader, OverlayEffect.Fade, 1.8f, false);

                if (accusedRobert && hasStrongRelevant)
                {
                    yield return UI.ShowDialogue("Gabe", $"I have this: {evidence.displayName}. {evidence.description}. The padlock forced from the inside, and your exclusive access to the basement. There's no more stories left to invent, Robert.", null, null, -1f, true);
                    yield return UI.ShowDialogue("Robert", "...", null, null, -1f, true);
                }
                else if (accusedRobert)
                {
                    yield return UI.ShowDialogue("Gabe", $"I have this: {evidence.displayName}. {evidence.description}.", null, null, -1f, true);
                    yield return UI.ShowDialogue("Robert", "Is that all you have? A loose thread. I'm afraid you'll need a lot more than that in front of a judge, Mr. Miller.", null, null, -1f, true);
                }
                else if (hasStrongRelevant)
                {
                    yield return UI.ShowDialogue("Gabe", $"I have this: {evidence.displayName}. {evidence.description}. Every piece fits you directly.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "No... it can't be... that's not how it happened...", null, null, -1f, true);
                }
                else
                {
                    yield return UI.ShowDialogue("Gabe", $"I have this: {evidence.displayName}. {evidence.description}.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "That doesn't prove a single thing against me. You're grasping at a culprit in the dark.", null, null, -1f, true);
                }
            }
            else
            {
                yield return UI.ShowDialogue("Gabe", "I don't have hard physical proof... but the facts speak for themselves.", null, null, -1f, true);
                yield return UI.ShowDialogue(suspectName, "Without proof, your words are worth nothing.", null, null, -1f, true);
            }

            // EPÍLOGO / DESENLACE
            string ending;
            if (accusedRobert && hasStrongRelevant)
            {
                ending = "He doesn't confess. He never does. But when they take the evidence away, he doesn't protest, doesn't call a lawyer, doesn't say a word too many. He just stands there staring at the basement like a man looking at something that finally stopped being his. Sometimes that's enough.";
            }
            else if (accusedRobert)
            {
                ending = "I'm right. I know it with the same certainty I know my own name. But certainty isn't proof, and what I'm carrying is too loose to hold up a charge. Robert Hale stays behind the counter, smiling, handing room 4 to some other stranded traveler. Some nights, that's the only thing that keeps me up.";
            }
            else if (hasStrongRelevant)
            {
                ending = $"The case closes. Every piece fits where it should, neat, convincing. {suspectName} has no way to defend against it, and deep down, doesn't even look surprised. It's good police work. It's also, without me knowing it yet, the wrong answer.";
            }
            else
            {
                ending = $"I accuse with what I have, which isn't much. {suspectName} denies it, and for once, has every right to. The file closes badly, with more questions than I arrived with. Somewhere in this town, someone still hasn't paid for what they did.";
            }

            yield return UI.ShowOverlay("", "CASE CLOSED", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
            yield return UI.ShowDialogue("", ending, null, null, -1f, true);
            UI.HideDialogue();
        }
    }
}
