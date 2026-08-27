using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Controla los eventos cinemáticos y transiciones de días usando StoryGraphs y secuencias noir.
    public class GameFlowController : MonoBehaviour
    {
        [Header("Story Graph del Prólogo")]
        [SerializeField] private StoryGraph introStoryGraph;

        private int lastSeenDay = 1;
        private StoryUIController UI => StoryUIController.Instance;

        private void Awake()
        {
            PhaseController.OnActionsChanged += CheckDayTransition;
        }

        private void OnDestroy()
        {
            PhaseController.OnActionsChanged -= CheckDayTransition;
        }

        private void Start()
        {
            StartCoroutine(IntroSequence());
        }

        private IEnumerator IntroSequence()
        {
            if (LocationController.Instance != null)
            {
                LocationController.Instance.HideAll();
            }

            if (introStoryGraph != null && StoryRunner.ActiveRunner != null)
            {
                StoryRunner.ActiveRunner.StartStory(introStoryGraph);
            }
            else
            {
                yield return UI.ShowOverlay("DÍA 1", "La carretera de ningún lugar", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
                yield return UI.ShowDialogue("", "El auto se apaga a diez minutos de cualquier cosa. El único cartel en kilómetros dice MOTEL, con una flecha pintada a mano.", null, null, -1f, true);
                yield return UI.ShowDialogue("", "No va a venir ninguna grúa antes de mañana. Voy a tener que quedarme.", null, null, -1f, true);
                
                if (LocationController.Instance != null)
                {
                    LocationController.Instance.RevealStartingLocation();
                }
            }
        }

        private void CheckDayTransition()
        {
            if (CaseState.Instance == null) return;
            int day = CaseState.Instance.currentDay;
            if (day == lastSeenDay) return;

            int fromDay = lastSeenDay;
            lastSeenDay = day;

            if (fromDay == 1 && day == 2)
            {
                // El prólogo unificado ya reproduce la noche del crimen del Día 1
                // y hace la transición directa al Día 2, por lo que no debe duplicarse.
                return;
            }
            else if (fromDay == 2 && day == 3)
            {
                StartCoroutine(Day2ToDay3TransitionRoutine());
            }
            else if (day > 3)
            {
                StartCoroutine(EndInvestigationOverlay());
            }
        }

        private IEnumerator Day2ToDay3TransitionRoutine()
        {
            if (LocationController.Instance != null)
            {
                LocationController.Instance.SetWorldUIActive(false);
            }

            // 1. Monólogo reflexivo de Gabe al terminar el Día 2
            yield return UI.ShowDialogue("", "La noche se puso demasiado fría y oscura para seguir afuera. El pueblo entero parece haberse apagado.", null, null, -1f, true);
            yield return UI.ShowDialogue("", "Es hora de volver a mi habitación en el Starlight Motel a repasar en mi libreta todo lo que descubrí hoy.", null, null, -1f, true);

            UI.HideDialogue();

            // 2. Fundido a negro (Fade Out)
            yield return UI.FadeScreen(1f, Color.black, 0.8f);

            // 3. Reubicación automática en el Motel
            if (LocationController.Instance != null)
            {
                LocationController.Instance.GoTo("motel");
            }

            // 4. Cartel de Título de Día 3 limpio
            yield return UI.ShowOverlay("DÍA 3", "08:00 AM — Última oportunidad para investigar", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Instant, 2.5f, true);

            // 5. Fundido de entrada (Fade In)
            yield return UI.FadeScreen(0f, Color.black, 0.8f);

            if (LocationController.Instance != null)
            {
                LocationController.Instance.SetWorldUIActive(true);
                LocationController.Instance.RefreshAll();
            }

            // 6. Monólogo de Gabe al amanecer del Día 3
            yield return UI.ShowDialogue("", "Amanece sobre el motel. El sheriff llegará al caer la noche para cerrar el caso. Todo lo que no averigüe hoy, quedará enterrado.", null, null, -1f, true);
            UI.HideDialogue();
        }

        private IEnumerator EndInvestigationOverlay()
        {
            if (LocationController.Instance != null)
            {
                LocationController.Instance.SetWorldUIActive(false);
            }

            yield return UI.ShowOverlay("PLAZO AGOTADO", "El tiempo de investigación ha terminado. Es hora de formular la acusación.", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 3f, true);
            
            if (AccusationController.Instance != null)
            {
                AccusationController.Instance.BeginAccusation();
            }
        }
    }
}
