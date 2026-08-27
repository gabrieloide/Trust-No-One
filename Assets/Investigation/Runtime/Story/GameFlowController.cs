using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Controla los eventos cinemáticos y transiciones de días usando StoryGraphs.
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
            else if (day == 3)
            {
                StartCoroutine(SimpleDayOverlay(3, "Última oportunidad para investigar"));
            }
            else if (day > 3)
            {
                StartCoroutine(EndInvestigationOverlay());
            }
        }

        private IEnumerator SimpleDayOverlay(int day, string subtitle)
        {
            yield return UI.ShowOverlay("DÍA " + day, subtitle, OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2.5f, true);
        }

        private IEnumerator EndInvestigationOverlay()
        {
            yield return UI.ShowOverlay("PLAZO AGOTADO", "El tiempo de investigación ha terminado. Es hora de formular la acusación.", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 3f, true);
            
            if (AccusationController.Instance != null)
            {
                AccusationController.Instance.BeginAccusation();
            }
        }
    }
}
