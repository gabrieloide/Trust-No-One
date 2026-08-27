using System.IO;
using RetroSoundSynthesizer.Editor;
using RetroSoundSynthesizer.Runtime;
using UnityEditor;
using UnityEngine;
using VisualNovelSystem;

namespace Investigation.EditorTools
{
    public static class InvestigationSFXGenerator
    {
        private const string SfxFolder = "Assets/Investigation/Audio/SFX";

        [MenuItem("Tools/Investigation/Generate Noir Sound FX")]
        public static void GenerateAllNoirSFX()
        {
            if (!Directory.Exists(SfxFolder))
            {
                Directory.CreateDirectory(SfxFolder);
            }
            else
            {
                // Limpiar archivos anteriores para no crear sufijos numéricos (_1, _2)
                var existingFiles = Directory.GetFiles(SfxFolder, "*.wav");
                foreach (var f in existingFiles)
                {
                    File.Delete(f);
                }
            }
            AssetDatabase.Refresh();

            // 1. Typewriter Key (Golpe de tecla seco y mecánico para el teletipo)
            GenerateTypewriterKey();

            // 2. Typewriter Bell / Enter (Campana mecánica sutil al terminar línea)
            GenerateTypewriterBell();

            // 3. UI Click (Clic de botón vintage)
            GenerateUIClick();

            // 4. UI Hover (Blip sutil al pasar cursor)
            GenerateUIHover();

            // 5. Clue Found / Eureka (Sting de misterio al descubrir pista)
            GenerateClueFound();

            // 6. Confrontation Slam / Impact (Impacto dramático al presentar prueba reina)
            GenerateConfrontationSlam();

            // 7. Evidence Paper Slide (Ruido de libreta de notas)
            GenerateNotebookSlide();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Asignar los sonidos a los componentes de la escena
            WireSFXToScene();

            Debug.Log("[InvestigationSFXGenerator] ¡Todos los efectos de sonido Noir fueron sintetizados y exportados exitosamente en " + SfxFolder + "!");
        }

        private static void GenerateTypewriterKey()
        {
            var p = new SoundParameters
            {
                soundName = "sfx_typewriter_key",
                waveType = WaveType.Noise,
                sampleRate = SampleRateOption.Rate44k,
                sampleSize = SampleSizeOption.Bit16,
                masterGain = 0.55f,
                attackTime = 0.0f,
                sustainTime = 0.02f,
                sustainPunch = 0.6f,
                decayTime = 0.04f,
                startFrequency = 0.65f,
                minFrequencyCutoff = 0.1f,
                slide = -0.25f,
                lpCutoffFrequency = 0.8f
            };

            float[] buffer = SynthEngine.Synthesize(p);
            WavExporter.ExportToWav(buffer, p.sampleRate, p.sampleSize, "sfx_typewriter_key", SfxFolder);
        }

        private static void GenerateTypewriterBell()
        {
            var cs = new CompositeSound();
            cs.baseSound = new SoundParameters
            {
                soundName = "sfx_typewriter_bell",
                waveType = WaveType.Sine,
                sampleRate = SampleRateOption.Rate44k,
                sampleSize = SampleSizeOption.Bit16,
                masterGain = 0.6f,
                attackTime = 0.01f,
                sustainTime = 0.08f,
                sustainPunch = 0.4f,
                decayTime = 0.45f,
                startFrequency = 0.82f,
                minFrequencyCutoff = 0.0f,
                slide = 0.0f,
                hpCutoffFrequency = 0.3f
            };

            var subLayer = new SoundParameters
            {
                waveType = WaveType.Sine,
                masterGain = 0.4f,
                attackTime = 0.02f,
                sustainTime = 0.05f,
                decayTime = 0.3f,
                startFrequency = 0.95f,
                delay = 0.01f
            };
            cs.layers.Add(subLayer);

            float[] buffer = SynthEngine.Synthesize(cs);
            WavExporter.ExportToWav(buffer, cs.baseSound.sampleRate, cs.baseSound.sampleSize, "sfx_typewriter_bell", SfxFolder);
        }

        private static void GenerateUIClick()
        {
            var p = new SoundParameters
            {
                soundName = "sfx_ui_click",
                waveType = WaveType.Square,
                sampleRate = SampleRateOption.Rate44k,
                sampleSize = SampleSizeOption.Bit16,
                masterGain = 0.5f,
                attackTime = 0.0f,
                sustainTime = 0.03f,
                sustainPunch = 0.5f,
                decayTime = 0.06f,
                startFrequency = 0.45f,
                slide = -0.3f,
                dutyCycle = 0.3f,
                lpCutoffFrequency = 0.7f
            };

            float[] buffer = SynthEngine.Synthesize(p);
            WavExporter.ExportToWav(buffer, p.sampleRate, p.sampleSize, "sfx_ui_click", SfxFolder);
        }

        private static void GenerateUIHover()
        {
            var p = new SoundParameters
            {
                soundName = "sfx_ui_hover",
                waveType = WaveType.Sine,
                sampleRate = SampleRateOption.Rate44k,
                sampleSize = SampleSizeOption.Bit16,
                masterGain = 0.35f,
                attackTime = 0.01f,
                sustainTime = 0.02f,
                decayTime = 0.04f,
                startFrequency = 0.6f,
                slide = 0.1f
            };

            float[] buffer = SynthEngine.Synthesize(p);
            WavExporter.ExportToWav(buffer, p.sampleRate, p.sampleSize, "sfx_ui_hover", SfxFolder);
        }

        private static void GenerateClueFound()
        {
            var cs = new CompositeSound();
            // Capa 1: Sub-bajo profundo y redondo (cero agudos)
            cs.baseSound = new SoundParameters
            {
                soundName = "sfx_clue_found",
                waveType = WaveType.Sine,
                sampleRate = SampleRateOption.Rate44k,
                sampleSize = SampleSizeOption.Bit16,
                masterGain = 0.85f,
                attackTime = 0.01f,
                sustainTime = 0.18f,
                sustainPunch = 0.45f,
                decayTime = 0.75f,
                startFrequency = 0.16f,
                slide = -0.04f,
                lpCutoffFrequency = 0.35f
            };

            // Capa 2: Armónico grave misterioso
            var layer2 = new SoundParameters
            {
                waveType = WaveType.Sine,
                masterGain = 0.45f,
                attackTime = 0.03f,
                sustainTime = 0.14f,
                decayTime = 0.65f,
                startFrequency = 0.24f,
                slide = -0.02f,
                lpCutoffFrequency = 0.40f,
                delay = 0.02f
            };
            cs.layers.Add(layer2);

            // Capa 3: Golpe sordo de fondo (thump misterioso de película noir)
            var layer3 = new SoundParameters
            {
                waveType = WaveType.Noise,
                masterGain = 0.35f,
                attackTime = 0.0f,
                sustainTime = 0.02f,
                sustainPunch = 0.5f,
                decayTime = 0.16f,
                startFrequency = 0.08f,
                slide = -0.5f,
                lpCutoffFrequency = 0.22f
            };
            cs.layers.Add(layer3);

            float[] buffer = SynthEngine.Synthesize(cs);
            WavExporter.ExportToWav(buffer, cs.baseSound.sampleRate, cs.baseSound.sampleSize, "sfx_clue_found", SfxFolder);
        }

        private static void GenerateConfrontationSlam()
        {
            var cs = new CompositeSound();
            cs.baseSound = new SoundParameters
            {
                soundName = "sfx_confrontation_slam",
                waveType = WaveType.Noise,
                sampleRate = SampleRateOption.Rate44k,
                sampleSize = SampleSizeOption.Bit16,
                masterGain = 0.85f,
                attackTime = 0.0f,
                sustainTime = 0.08f,
                sustainPunch = 0.8f,
                decayTime = 0.65f,
                startFrequency = 0.4f,
                slide = -0.55f,
                lpCutoffFrequency = 0.65f
            };

            var layer2 = new SoundParameters
            {
                waveType = WaveType.Square,
                masterGain = 0.7f,
                attackTime = 0.01f,
                sustainTime = 0.06f,
                decayTime = 0.4f,
                startFrequency = 0.2f,
                slide = -0.4f,
                delay = 0.02f
            };
            cs.layers.Add(layer2);

            float[] buffer = SynthEngine.Synthesize(cs);
            WavExporter.ExportToWav(buffer, cs.baseSound.sampleRate, cs.baseSound.sampleSize, "sfx_confrontation_slam", SfxFolder);
        }

        private static void GenerateNotebookSlide()
        {
            var p = new SoundParameters
            {
                soundName = "sfx_notebook_slide",
                waveType = WaveType.Noise,
                sampleRate = SampleRateOption.Rate44k,
                sampleSize = SampleSizeOption.Bit16,
                masterGain = 0.45f,
                attackTime = 0.04f,
                sustainTime = 0.08f,
                decayTime = 0.12f,
                startFrequency = 0.35f,
                slide = -0.1f,
                lpCutoffFrequency = 0.45f
            };

            float[] buffer = SynthEngine.Synthesize(p);
            WavExporter.ExportToWav(buffer, p.sampleRate, p.sampleSize, "sfx_notebook_slide", SfxFolder);
        }

        public static void WireSFXToScene()
        {
            var keyClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + "/sfx_typewriter_key.wav");
            var bellClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + "/sfx_typewriter_bell.wav");
            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + "/sfx_ui_click.wav");
            var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + "/sfx_ui_hover.wav");
            var clueClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + "/sfx_clue_found.wav");
            var slamClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + "/sfx_confrontation_slam.wav");
            var noteClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxFolder + "/sfx_notebook_slide.wav");

            // Configurar AudioManager en la escena
            var audioMgr = Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr == null)
            {
                var go = new GameObject("AudioManager");
                audioMgr = go.AddComponent<AudioManager>();
            }

            var audioSo = new SerializedObject(audioMgr);
            audioSo.FindProperty("typewriterKey").objectReferenceValue = keyClip;
            audioSo.FindProperty("typewriterBell").objectReferenceValue = bellClip;
            audioSo.FindProperty("uiClick").objectReferenceValue = clickClip;
            audioSo.FindProperty("uiHover").objectReferenceValue = hoverClip;
            audioSo.FindProperty("clueFound").objectReferenceValue = clueClip;
            audioSo.FindProperty("confrontationSlam").objectReferenceValue = slamClip;
            audioSo.FindProperty("notebookSlide").objectReferenceValue = noteClip;
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            // Configurar sonido de teletipo en StoryDialogueUI
            var dialogueUI = Object.FindAnyObjectByType<StoryDialogueUI>();
            if (dialogueUI != null && keyClip != null)
            {
                var so = new SerializedObject(dialogueUI);
                var typingClipProp = so.FindProperty("typingAudioClip");
                if (typingClipProp != null)
                {
                    typingClipProp.objectReferenceValue = keyClip;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
