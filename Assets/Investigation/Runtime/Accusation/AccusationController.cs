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

            // ACTO 1: Acusación directa de Gabe
            yield return UI.ShowDialogue("Gabe", $"Fuiste vos, {suspectName}. Todo este tiempo estuviste armando la escena y jugando a que no tenías nada que ver.", null, null, -1f, true);

            // ACTO 2: Réplica del sospechoso
            switch (suspectId)
            {
                case "robert":
                    yield return UI.ShowDialogue("Robert", "¿Y con qué pruebas piensa sostener semejante disparate, detective? Porque hasta ahora todo lo que tiene son conjeturas y sospechas de pasillo.", null, null, -1f, true);
                    break;
                case "ernesto":
                    yield return UI.ShowDialogue("Ernesto", "¡Estás loco! ¡Yo no le hice nada a esa mujer! ¡No podés probar una sola palabra de lo que estás diciendo!", null, null, -1f, true);
                    break;
                case "mark":
                    yield return UI.ShowDialogue("Mark", "¡No, no, no! ¡Yo no fui! ¡Había ruidos abajo, se lo juro, pero yo no la toqué a Carla! ¡No me encierren de nuevo!", null, null, -1f, true);
                    break;
                case "elena":
                    yield return UI.ShowDialogue("Elena", "¿Yo? ¿Por qué me mira a mí? Todo lo que hice fue correr del miedo cuando escuché el golpe... esto es una locura.", null, null, -1f, true);
                    break;
            }

            // ACTO 3: Presentación de la Evidencia y Reacción
            if (evidence != null)
            {
                yield return UI.ShowOverlay("EVIDENCIA PRESENTADA", evidence.displayName, OverlayDisplayMode.TopHeader, OverlayEffect.Fade, 1.8f, false);

                if (accusedRobert && hasStrongRelevant)
                {
                    yield return UI.ShowDialogue("Gabe", $"Tengo esto: {evidence.displayName}. {evidence.description}. El candado forzado desde adentro y tu acceso exclusivo al sótano. Ya no hay más versiones que inventar, Robert.", null, null, -1f, true);
                    yield return UI.ShowDialogue("Robert", "...", null, null, -1f, true);
                }
                else if (accusedRobert)
                {
                    yield return UI.ShowDialogue("Gabe", $"Tengo esto: {evidence.displayName}. {evidence.description}.", null, null, -1f, true);
                    yield return UI.ShowDialogue("Robert", "¿Eso es todo lo que tiene? Un indicio suelto. Me temo que va a necesitar mucho más que eso ante un juez, señor Miller.", null, null, -1f, true);
                }
                else if (hasStrongRelevant)
                {
                    yield return UI.ShowDialogue("Gabe", $"Tengo esto: {evidence.displayName}. {evidence.description}. Cada pieza encaja directamente con vos.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "No... no puede ser... no fue así...", null, null, -1f, true);
                }
                else
                {
                    yield return UI.ShowDialogue("Gabe", $"Tengo esto: {evidence.displayName}. {evidence.description}.", null, null, -1f, true);
                    yield return UI.ShowDialogue(suspectName, "Eso no prueba absolutamente nada contra mí. Está buscando un culpable a ciegas.", null, null, -1f, true);
                }
            }
            else
            {
                yield return UI.ShowDialogue("Gabe", "No tengo una prueba física contundente... pero los hechos hablan por sí solos.", null, null, -1f, true);
                yield return UI.ShowDialogue(suspectName, "Sin pruebas, sus palabras no valen nada.", null, null, -1f, true);
            }

            // EPÍLOGO / DESENLACE
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

            yield return UI.ShowOverlay("", "EXPEDIENTE FINAL", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
            yield return UI.ShowDialogue("", ending, null, null, -1f, true);
            UI.HideDialogue();
        }
    }
}
