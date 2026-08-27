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
                yield return UI.ShowDialogue("", "Todavía no terminé de investigar. Puedo esperar un poco más antes de acusar a alguien.", null, null, -1f, true);
                isBusy = false;
                yield break;
            }

            var suspectOptions = SuspectIds
                .Select(id => new StoryChoiceOption { id = id, text = DialogueDatabase.Instance.GetCharacter(id)?.displayName ?? id })
                .ToList();

            int suspectIndex = -1;
            yield return UI.ShowChoices("¿A quién acuso?", suspectOptions, idx => suspectIndex = idx);
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
                yield return UI.ShowChoices("¿Qué presento como evidencia?", evidenceOptions, idx => evidenceIndex = idx);
                if (evidenceIndex >= 0 && evidenceIndex < evidenceOptions.Count)
                {
                    evidenceClueId = evidenceOptions[evidenceIndex].id;
                }
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
            string ending;

            if (accusedRobert && hasStrongRelevant)
            {
                ending = "No confiesa. Nunca lo hace. Pero cuando se llevan las pruebas, no protesta, no llama a un abogado, no dice una palabra de más. Se queda mirando el sótano como quien mira algo que por fin dejó de ser suyo. A veces alcanza con eso.";
            }
            else if (accusedRobert)
            {
                ending = "Tengo razón. Lo sé con la misma certeza con la que sé mi propio nombre. Pero certeza no es prueba, y lo que llevo es demasiado suelto para sostener un cargo. Robert Hale sigue del otro lado del mostrador, sonriendo, dándole la habitación 4 a otro que se quedó varado en la ruta. Algunas noches, eso es lo único que me despierta.";
            }
            else if (hasStrongRelevant)
            {
                ending = $"El caso cierra. Cada pieza encaja donde debería, prolija, convincente. {suspectName} no tiene cómo defenderse, y en el fondo, tampoco parece sorprendido. Es un buen trabajo policial. Es también, sin que yo lo sepa todavía, la respuesta equivocada.";
            }
            else
            {
                ending = $"Acuso con lo que tengo, que no es mucho. {suspectName} lo niega, y por una vez, tiene toda la razón para hacerlo. El expediente se cierra mal, con más preguntas que las que tenía al llegar. En algún lugar de este pueblo, alguien sigue sin pagar por lo que hizo.";
            }

            yield return UI.ShowDialogue("", ending, null, null, -1f, true);
        }
    }
}
