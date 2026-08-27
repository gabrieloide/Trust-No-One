using System.Collections;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation
{
    // Controla los eventos cinemáticos y transiciones de días usando StoryGraphs.
    public class GameFlowController : MonoBehaviour
    {
        [Header("Story Graphs de Transición")]
        [SerializeField] private StoryGraph introStoryGraph;
        [SerializeField] private StoryGraph night1StoryGraph;

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
            LocationController.Instance.HideAll();

            if (introStoryGraph != null && StoryRunner.ActiveRunner != null)
            {
                StoryRunner.ActiveRunner.StartStory(introStoryGraph);
            }
            else
            {
                yield return UI.ShowOverlay("DÍA 1", "La carretera de ningún lugar", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
                yield return UI.ShowDialogue("", "El auto se apaga a diez minutos de cualquier cosa. El único cartel en kilómetros dice MOTEL, con una flecha pintada a mano.", null, null, -1f, true);
                yield return UI.ShowDialogue("", "No va a venir ninguna grúa antes de mañana. Voy a tener que quedarme.", null, null, -1f, true);
                LocationController.Instance.RevealStartingLocation();
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
                StartCoroutine(Day1NightSequence());
            }
            else if (day <= 3)
            {
                StartCoroutine(SimpleDayOverlay(day));
            }
        }

        private IEnumerator Day1NightSequence()
        {
            LocationController.Instance.HideAll();

            if (night1StoryGraph != null && StoryRunner.ActiveRunner != null)
            {
                StoryRunner.ActiveRunner.StartStory(night1StoryGraph);
            }
            else
            {
                yield return UI.ShowOverlay("", "Esa noche...", OverlayDisplayMode.TopHeader, OverlayEffect.Fade, 2f, true);
                yield return UI.ShowDialogue("", "Un grito corto, cortado a la mitad. Después, vidrio rompiéndose.", null, null, -1f, true);
                yield return UI.ShowDialogue("", "Cuando salgo al pasillo alcanzo a ver a Elena, corriendo en dirección contraria al ruido.", null, null, -1f, true);
                CaseState.Instance.CollectClue("elena_seen_running");

                yield return UI.ShowDialogue("", "Para cuando llego, ya hay gente alrededor del cuerpo. Alguien fue a buscar a Robert.", null, null, -1f, true);
                yield return UI.ShowDialogue("", "Llega antes de lo que debería tardar cualquiera en despertarse y vestirse. Sin marcas, sin agitación, con la explicación ya lista.", null, null, -1f, true);
                CaseState.Instance.CollectClue("robert_quick_arrival");

                yield return UI.ShowOverlay("", "Fin del Día 1", OverlayDisplayMode.BottomTimestamp, OverlayEffect.Fade, 2f, true);
                yield return UI.ShowOverlay("DÍA 2", "La lista de sospechosos no incluye a Robert Hale", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2.5f, true);

                LocationController.Instance.RevealStartingLocation();
            }
        }

        private IEnumerator SimpleDayOverlay(int day)
        {
            yield return UI.ShowOverlay("DÍA " + day, day >= 3 ? "Última oportunidad para acusar" : "", OverlayDisplayMode.CenterTitleCard, OverlayEffect.Fade, 2f, true);
        }
    }
}
