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
                UI.HideDialogue();
                isBusy = false;
                yield break;
            }

            yield return GatheringPreamble();

            var suspectOptions = SuspectIds
                .Select(id => new StoryChoiceOption { id = id, text = DialogueDatabase.Instance.GetCharacter(id)?.displayName ?? id })
                .ToList();

            // "Go back" en la confirmación vuelve acá en vez de cortar la corrutina: si no,
            // el jugador queda parado en la escena de la reunión sin nada clickeable.
            string suspectId = null;
            string evidenceClueId = null;
            bool confirmed = false;

            while (!confirmed)
            {
                int suspectIndex = -1;
                yield return UI.ShowChoices("Who do I accuse?", suspectOptions, idx => suspectIndex = idx);
                if (suspectIndex < 0 || suspectIndex >= suspectOptions.Count)
                {
                    isBusy = false;
                    yield break;
                }
                suspectId = suspectOptions[suspectIndex].id;

                var collectedClues = CaseState.Instance.CollectedClues
                    .Select(id => DialogueDatabase.Instance.GetClue(id))
                    .Where(c => c != null)
                    .ToList();

                evidenceClueId = null;
                if (collectedClues.Count > 0)
                {
                    yield return AccusationEvidenceBoardUI.Instance.SelectEvidenceRoutine(collectedClues, id => evidenceClueId = id);
                }

                string suspectDisplayName = DialogueDatabase.Instance.GetCharacter(suspectId)?.displayName ?? suspectId;
                var confirmOptions = new List<StoryChoiceOption>
                {
                    new StoryChoiceOption { id = "confirm", text = $"Yes, formally accuse {suspectDisplayName}" },
                    new StoryChoiceOption { id = "cancel", text = "Go back" }
                };

                int confirmIndex = -1;
                yield return UI.ShowChoices($"Close the case and accuse {suspectDisplayName}?", confirmOptions, idx => confirmIndex = idx);
                confirmed = confirmIndex == 0;
            }

            yield return ResolveOutcome(suspectId, evidenceClueId);

            isBusy = false;
        }

        // Reúne al sheriff y a los 4 sospechosos antes del menú de acusación: paga la promesa
        // del monólogo del amanecer del Día 3 ("the sheriff will arrive at nightfall to close
        // the case"), que hasta ahora no tenía ninguna escena asociada.
        private IEnumerator GatheringPreamble()
        {
            yield return UI.ShowOverlay("NIGHTFALL", "The sheriff's cruiser pulls into the lot.", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);

            yield return UI.ShowDialogue("", "By the time he steps out, everyone's already gathered under the buzzing neon sign — Robert, Elena, Ernesto, Mark, herded together like it's the only thing left to do tonight.", null, null, -1f, true);

            yield return UI.ShowDialogue("Sheriff", "Alright. I'm told somebody here's got a story worth hearing. Last chance before I start writing names down myself.", null, null, -1f, true);

            var db = DialogueDatabase.Instance;
            yield return UI.ShowDialogue(db.ResolveSpeakerDisplayName("ernesto"), "Ask him what he keeps in that basement! Ask him why nobody else gets a key!", null, null, -1f, true);
            yield return UI.ShowDialogue(db.ResolveSpeakerDisplayName("robert"), "Ernesto, please. This isn't the time for wild stories.", null, null, -1f, true);
            yield return UI.ShowDialogue(db.ResolveSpeakerDisplayName("elena"), "Wild stories? A girl is dead, Robert.", null, null, -1f, true);
            yield return UI.ShowDialogue(db.ResolveSpeakerDisplayName("mark"), "I didn't do nothing... I didn't do nothing...", null, null, -1f, true);

            yield return UI.ShowDialogue("Sheriff", "That's enough, all of you. Quiet.", null, null, -1f, true);
            yield return UI.ShowDialogue("Sheriff", "Detective. You've had three days. Who was it?", null, null, -1f, true);
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

            // ACTO 2: Réplica del sospechoso — un round completo de pelea, no una sola línea.
            switch (suspectId)
            {
                case "robert":
                    yield return UI.ShowDialogue(suspectName, "And what evidence do you plan to back up such an outrageous claim with, detective? Because so far all you have are guesses and hallway gossip.", null, null, -1f, true);
                    yield return UI.ShowDialogue("Gabe", "Then let's talk about that basement, Robert. The one padlock in this whole motel that only you ever touch.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "A man's allowed to lock up his own supplies. That's not a confession, detective, that's a hardware store receipt.", null, null, -1f, true);
                    break;
                case "ernesto":
                    yield return UI.ShowDialogue(suspectName, "You're crazy! I didn't do anything to that woman! You can't prove a single word of what you're saying!", null, null, -1f, true);
                    yield return UI.ShowDialogue("Gabe", "Nobody said proof yet, Ernesto. I'm just asking why your hands were shaking every time her name came up.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "Because I liked her, you idiot! Is that a crime now, liking somebody?!", null, null, -1f, true);
                    break;
                case "mark":
                    yield return UI.ShowDialogue(suspectName, "No, no, no! It wasn't me! There were noises downstairs, I swear, but I never touched Carla! Don't lock me up again!", null, null, -1f, true);
                    yield return UI.ShowDialogue("Gabe", "Nobody's locking anybody up over noises, Mark. But you were the only one who heard them and never told a soul. Why sit on that?", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "Because nobody ever believes me! Not then, not now! You think a badge changes that?!", null, null, -1f, true);
                    break;
                case "elena":
                    yield return UI.ShowDialogue(suspectName, "Me?! Of course, blame the motel clerk! That woman conned me out of my life savings, and I wanted to tear her apart, yes! But I didn't murder her!", null, null, -1f, true);
                    yield return UI.ShowDialogue("Gabe", "Wanting to and doing it aren't the same thing, Elena. But you've got the best motive of anyone in this parking lot.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "Motive isn't a knife, detective. Try again when you've got something that actually cuts.", null, null, -1f, true);
                    break;
            }

            // ACTO 3: Presentación de la Evidencia y Reacción — el sospechoso pelea antes de ceder.
            if (evidence != null)
            {
                yield return UI.ShowOverlay("EVIDENCE PRESENTED", evidence.displayName, OverlayDisplayMode.TopHeader, OverlayEffect.Fade, 1.8f, false);

                if (accusedRobert && hasStrongRelevant)
                {
                    yield return UI.ShowDialogue("Gabe", $"I have this: {evidence.displayName}. {evidence.description}. The padlock forced from the inside, and your exclusive access to the basement.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "That could mean anything. Broken locks happen.", null, null, -1f, true);
                    yield return UI.ShowDialogue("Gabe", "Not locks forced from the inside, Robert. Not when you're the only one with a key. You're not explaining a break-in. You're explaining an escape.", null, null, -1f, true);

                    AudioManager.Play(SFXType.ConfrontationSlam);
                    yield return UI.ShowOverlay("", "He says nothing.", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 1.6f, false);

                    yield return UI.ShowDialogue(suspectName, "...", null, null, -1f, true);
                }
                else if (accusedRobert)
                {
                    yield return UI.ShowDialogue("Gabe", $"I have this: {evidence.displayName}. {evidence.description}.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "Is that all you have? A loose thread. I'm afraid you'll need a lot more than that in front of a judge, detective.", null, null, -1f, true);
                    yield return UI.ShowDialogue("Gabe", "It's a start.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "Starts don't hold up in court. Neither do hunches, detective, however good yours might be.", null, null, -1f, true);
                }
                else if (hasStrongRelevant)
                {
                    yield return UI.ShowDialogue("Gabe", $"I have this: {evidence.displayName}. {evidence.description}. Every piece fits you directly.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "That's not... that doesn't mean I—", null, null, -1f, true);
                    yield return UI.ShowDialogue("Gabe", "It means exactly what it looks like.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "No... it can't be... that's not how it happened...", null, null, -1f, true);
                }
                else
                {
                    yield return UI.ShowDialogue("Gabe", $"I have this: {evidence.displayName}. {evidence.description}.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "That doesn't prove a single thing against me. You're grasping at a culprit in the dark.", null, null, -1f, true);
                    yield return UI.ShowDialogue("Gabe", "Maybe.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "Not maybe. Ask anyone in this town — you're chasing the wrong shadow.", null, null, -1f, true);
                }
            }
            else
            {
                yield return UI.ShowDialogue("Gabe", "I don't have hard physical proof... but the facts speak for themselves.", null, null, -1f, true);
                yield return UI.ShowDialogue(suspectName, "Without proof, your words are worth nothing.", null, null, -1f, true);
            }

            // ACTO 4: El sheriff se pronuncia — acá se decide, en voz alta, si le da la razón a Gabe.
            if (hasStrongRelevant)
            {
                yield return UI.ShowDialogue("Sheriff", $"That's enough for me. {suspectName}, hands where I can see them.", null, null, -1f, true);
            }
            else if (accusedRobert)
            {
                yield return UI.ShowDialogue("Sheriff", "Detective, I believe you believe it. But I can't walk him out of here on a feeling. Bring me something that holds, and I'll be the first one at his door.", null, null, -1f, true);
            }
            else
            {
                yield return UI.ShowDialogue("Sheriff", "That's not enough to hang a man's name on. Not tonight.", null, null, -1f, true);
            }

            // EPÍLOGO / DESENLACE
            string ending;
            string endingLabel;
            if (accusedRobert && hasStrongRelevant)
            {
                endingLabel = "THE TRUTH COMES OUT";
                ending = "He doesn't confess. He never does. But when they take the evidence away, he doesn't protest, doesn't call a lawyer, doesn't say a word too many. He just stands there staring at the basement like a man looking at something that finally stopped being his. Sometimes that's enough. Three days later, the state makes it official: Robert Hale, for the murder of Carla Whitmore. She gets her name back on a headstone instead of a missing-persons file. I did my job. For once, that's the whole story.";
            }
            else if (accusedRobert)
            {
                endingLabel = "NOT ENOUGH";
                ending = "I'm right. I know it with the same certainty I know my own name. But certainty isn't proof, and what I'm carrying is too loose to hold up a charge. Robert Hale stays behind the counter, smiling, handing room 4 to some other stranded traveler. Some nights, that's the only thing that keeps me up. The file goes in a drawer marked open. I keep driving past the motel some nights, certain and useless in equal measure.";
            }
            else if (hasStrongRelevant)
            {
                endingLabel = "WRONG MAN";
                ending = $"The case closes. Every piece fits where it should, neat, convincing. {suspectName} has no way to defend against it, and deep down, doesn't even look surprised. It's good police work. It's also, without me knowing it yet, the wrong answer. {suspectName} goes away for something they didn't do. Three counties over, Robert Hale reads about it in the paper over coffee. I don't find out for months. By the time I do, it's someone else's case to fix.";
            }
            else
            {
                endingLabel = "UNRESOLVED";
                ending = $"I accuse with what I have, which isn't much. {suspectName} denies it, and for once, has every right to. The file closes badly, with more questions than I arrived with. Somewhere in this town, someone still hasn't paid for what they did. His name is Robert Hale, and he'll go on renting out room 4 long after I've stopped asking questions about it.";
            }

            yield return UI.ShowOverlay("CASE CLOSED", endingLabel, OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
            yield return UI.ShowDialogue("", ending, null, null, -1f, true);
            UI.HideDialogue();

            yield return UI.FadeScreen(1f, Color.black, 1.2f);
            yield return EndingCreditsUI.Instance.PlayRoutine();
        }
    }
}
