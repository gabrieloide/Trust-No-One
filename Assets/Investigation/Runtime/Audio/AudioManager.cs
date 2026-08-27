using UnityEngine;

namespace Investigation
{
    public enum SFXType
    {
        TypewriterKey,
        TypewriterBell,
        UIClick,
        UIHover,
        ClueFound,
        ConfrontationSlam,
        NotebookSlide
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;

        [Header("Noir Sound Effects")]
        [SerializeField] private AudioClip typewriterKey;
        [SerializeField] private AudioClip typewriterBell;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip uiHover;
        [SerializeField] private AudioClip clueFound;
        [SerializeField] private AudioClip confrontationSlam;
        [SerializeField] private AudioClip notebookSlide;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            LoadDefaultClipsIfEmpty();
        }

        private void OnEnable()
        {
            VisualNovelSystem.StoryChoiceButton.OnAnyChoiceClicked += HandleUIClick;
            VisualNovelSystem.StoryChoiceButton.OnAnyChoiceHovered += HandleUIHover;
            VisualNovelSystem.StoryInteractable.OnAnyInteractClicked += HandleUIClick;
            VisualNovelSystem.StoryInteractable.OnAnyInteractHovered += HandleUIHover;
        }

        private void OnDisable()
        {
            VisualNovelSystem.StoryChoiceButton.OnAnyChoiceClicked -= HandleUIClick;
            VisualNovelSystem.StoryChoiceButton.OnAnyChoiceHovered -= HandleUIHover;
            VisualNovelSystem.StoryInteractable.OnAnyInteractClicked -= HandleUIClick;
            VisualNovelSystem.StoryInteractable.OnAnyInteractHovered -= HandleUIHover;
        }

        private void HandleUIClick() => PlayInternal(SFXType.UIClick, 0.6f);
        private void HandleUIHover() => PlayInternal(SFXType.UIHover, 0.35f, 0.05f);

        private void LoadDefaultClipsIfEmpty()
        {
            if (typewriterKey == null) typewriterKey = Resources.Load<AudioClip>("SFX/sfx_typewriter_key");
            if (uiClick == null) uiClick = Resources.Load<AudioClip>("SFX/sfx_ui_click");
            if (uiHover == null) uiHover = Resources.Load<AudioClip>("SFX/sfx_ui_hover");
            if (clueFound == null) clueFound = Resources.Load<AudioClip>("SFX/sfx_clue_found");
            if (confrontationSlam == null) confrontationSlam = Resources.Load<AudioClip>("SFX/sfx_confrontation_slam");
            if (notebookSlide == null) notebookSlide = Resources.Load<AudioClip>("SFX/sfx_notebook_slide");
        }

        public static void Play(SFXType type, float volume = 1f, float pitchVariation = 0f)
        {
            if (Instance != null)
            {
                Instance.PlayInternal(type, volume, pitchVariation);
            }
        }

        public void PlayInternal(SFXType type, float volume = 1f, float pitchVariation = 0f)
        {
            if (sfxSource == null) return;

            AudioClip clip = null;
            switch (type)
            {
                case SFXType.TypewriterKey: clip = typewriterKey; break;
                case SFXType.TypewriterBell: clip = typewriterBell; break;
                case SFXType.UIClick: clip = uiClick; break;
                case SFXType.UIHover: clip = uiHover; break;
                case SFXType.ClueFound: clip = clueFound; break;
                case SFXType.ConfrontationSlam: clip = confrontationSlam; break;
                case SFXType.NotebookSlide: clip = notebookSlide; break;
            }

            if (clip != null)
            {
                if (pitchVariation > 0f)
                {
                    sfxSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
                }
                else
                {
                    sfxSource.pitch = 1f;
                }

                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
