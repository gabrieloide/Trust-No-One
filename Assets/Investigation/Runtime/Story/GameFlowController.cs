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
                yield return UI.ShowOverlay("DAY 1", "The road to nowhere", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
                yield return UI.ShowDialogue("", "The car dies ten minutes from anything. The only sign for miles says MOTEL, with a hand-painted arrow.", null, null, -1f, true);
                yield return UI.ShowDialogue("", "No tow truck's coming before morning. I'm going to have to stay.", null, null, -1f, true);
                
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
            // Esperar a que la conversación o interacción activa cierre y libere completamente la UI
            while (ConversationController.Instance != null && ConversationController.Instance.IsBusy)
            {
                yield return null;
            }

            // Esperar al final del frame para garantizar que cualquier llamada a HideDialogue() del caller haya finalizado
            yield return new WaitForEndOfFrame();

            if (LocationController.Instance != null)
            {
                LocationController.Instance.SetWorldUIActive(false);
            }

            // 1. Monólogo reflexivo de Gabe al terminar el Día 2
            yield return UI.ShowDialogue("", "The night's gotten too cold and dark to stay out any longer. The whole town seems to have shut down.", null, null, -1f, true);
            yield return UI.ShowDialogue("", "Time to head back to my room at the Starlight Motel and go over everything I found out today in my notebook.", null, null, -1f, true);

            UI.HideDialogue();

            // 2. Fundido a negro (Fade Out)
            yield return UI.FadeScreen(1f, Color.black, 0.8f);

            // 3. Reubicación automática en la Habitación 4 (su habitación al despertar)
            if (LocationController.Instance != null)
            {
                LocationController.Instance.GoTo("room4");
            }

            // 4. Cartel de Título de Día 3 limpio
            yield return UI.ShowOverlay("DAY 3", "08:00 AM - Last chance to investigate", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Instant, 2.5f, true);

            // 5. Fundido de entrada (Fade In)
            yield return UI.FadeScreen(0f, Color.black, 0.8f);

            // 6. Monólogo de Gabe al amanecer del Día 3
            yield return UI.ShowDialogue("", "Dawn breaks over the motel. The sheriff will arrive at nightfall to close the case. Whatever I don't find out today stays buried.", null, null, -1f, true);
            UI.HideDialogue();

            if (LocationController.Instance != null)
            {
                LocationController.Instance.SetWorldUIActive(true);
                LocationController.Instance.RefreshAll();
            }
        }

        private IEnumerator EndInvestigationOverlay()
        {
            while (ConversationController.Instance != null && ConversationController.Instance.IsBusy)
            {
                yield return null;
            }
            yield return new WaitForEndOfFrame();

            if (LocationController.Instance != null)
            {
                LocationController.Instance.SetWorldUIActive(false);
            }

            yield return UI.ShowOverlay("TIME'S UP", "The investigation is over. Time to make an accusation.", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 3f, true);
            
            if (AccusationController.Instance != null)
            {
                AccusationController.Instance.BeginAccusation();
            }
        }
    }
}
