using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VisualNovelSystem;

namespace Investigation.EditorTools
{
    public static class FontAssetSetup
    {
        public const string BebasTtfPath = "Assets/Fonts/Bebas_Neue/BebasNeue-Regular.ttf";
        public const string BebasAssetPath = "Assets/Fonts/Bebas_Neue/BebasNeue-Regular SDF.asset";

        public const string CourierTtfPath = "Assets/Fonts/Courier_Prime/CourierPrime-Regular.ttf";
        public const string CourierAssetPath = "Assets/Fonts/Courier_Prime/CourierPrime-Regular SDF.asset";

        public const string SpaceMonoTtfPath = "Assets/Fonts/Space_Mono/SpaceMono-Regular.ttf";
        public const string SpaceMonoAssetPath = "Assets/Fonts/Space_Mono/SpaceMono-Regular SDF.asset";

        public const string SpecialEliteTtfPath = "Assets/Fonts/Special_Elite/SpecialElite-Regular.ttf";
        public const string SpecialEliteAssetPath = "Assets/Fonts/Special_Elite/SpecialElite-Regular SDF.asset";

        [MenuItem("Tools/Investigation/Generate TMP Font Assets and Apply to Scene")]
        public static void GenerateAndApplyAll()
        {
            var courier = CreateProperFontAsset(CourierTtfPath, CourierAssetPath);
            var bebas = CreateProperFontAsset(BebasTtfPath, BebasAssetPath);
            var spaceMono = CreateProperFontAsset(SpaceMonoTtfPath, SpaceMonoAssetPath);
            var specialElite = CreateProperFontAsset(SpecialEliteTtfPath, SpecialEliteAssetPath);

            var libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

            // Configurar Courier y LiberationSans como fallbacks universales
            if (bebas != null)
            {
                var fallbacks = new List<TMP_FontAsset>();
                if (courier != null) fallbacks.Add(courier);
                if (libSans != null) fallbacks.Add(libSans);
                bebas.fallbackFontAssetTable = fallbacks;
                EditorUtility.SetDirty(bebas);
            }

            if (courier != null && libSans != null)
            {
                courier.fallbackFontAssetTable = new List<TMP_FontAsset> { libSans };
                EditorUtility.SetDirty(courier);
            }

            if (spaceMono != null && libSans != null)
            {
                spaceMono.fallbackFontAssetTable = new List<TMP_FontAsset> { libSans };
                EditorUtility.SetDirty(spaceMono);
            }

            ApplyFontsToActiveScene(bebas, courier, spaceMono, specialElite);

            // Reconstruir la escena y UI para asegurar consistencia
            WorldBuilder.Build();

            // Guardar cambios
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FontAssetSetup] ¡Fuentes TMP recreadas con Materiales y Texturas sub-asset completas!");
        }

        public static TMP_FontAsset CreateProperFontAsset(string fontPath, string targetAssetPath)
        {
            if (File.Exists(targetAssetPath))
            {
                AssetDatabase.DeleteAsset(targetAssetPath);
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError($"[FontAssetSetup] No se encontró Font en {fontPath}");
                return null;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            if (fontAsset == null)
            {
                Debug.LogError($"[FontAssetSetup] Falló CreateFontAsset para {fontPath}");
                return null;
            }

            string dir = Path.GetDirectoryName(targetAssetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Shader shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader == null) shader = Shader.Find("TextMeshPro/Mobile/Distance Field");

            Material material = new Material(shader);
            material.name = fontAsset.name + " Material";

            if (fontAsset.atlasTexture != null)
            {
                material.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTexture);
                material.SetFloat(ShaderUtilities.ID_TextureWidth, fontAsset.atlasTexture.width);
                material.SetFloat(ShaderUtilities.ID_TextureHeight, fontAsset.atlasTexture.height);
            }
            material.SetFloat(ShaderUtilities.ID_GradientScale, fontAsset.atlasPadding + 1);
            material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
            material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);

            fontAsset.material = material;

            AssetDatabase.CreateAsset(fontAsset, targetAssetPath);

            if (fontAsset.atlasTextures != null)
            {
                foreach (var tex in fontAsset.atlasTextures)
                {
                    if (tex != null) AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }
            else if (fontAsset.atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            AssetDatabase.AddObjectToAsset(material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            return fontAsset;
        }

        public static void ApplyFontsToActiveScene(TMP_FontAsset bebas, TMP_FontAsset courier, TMP_FontAsset spaceMono, TMP_FontAsset specialElite)
        {
            if (courier == null) courier = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CourierAssetPath);
            if (bebas == null) bebas = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BebasAssetPath);
            if (spaceMono == null) spaceMono = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SpaceMonoAssetPath);

            var allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            int count = 0;

            foreach (var tmp in allTMPs)
            {
                string name = tmp.gameObject.name.ToLower();
                Transform parent = tmp.transform.parent;
                string parentName = parent != null ? parent.name.ToLower() : "";

                // 1. Títulos de Carteles principales -> Courier o Bebas
                if (name.Contains("title") || (parentName.Contains("overlay") && name.Contains("title")))
                {
                    tmp.font = courier != null ? courier : bebas;
                    if (tmp.font != null) tmp.fontSharedMaterial = tmp.font.material;
                }
                // 2. Diálogos, Textos de personajes y subtítulos -> Courier Prime
                else if (name.Contains("dialogue") || name.Contains("speaker") || name.Contains("subtitle"))
                {
                    tmp.font = courier;
                    if (courier != null) tmp.fontSharedMaterial = courier.material;
                }
                // 3. Botones, HUD, Pistas, Opciones y Navegación -> Space Mono
                else
                {
                    tmp.font = spaceMono != null ? spaceMono : courier;
                    if (tmp.font != null) tmp.fontSharedMaterial = tmp.font.material;
                }

                EditorUtility.SetDirty(tmp);
                count++;
            }

            Debug.Log($"[FontAssetSetup] Se asignaron las nuevas fuentes correctamente con materiales a {count} componentes TextMeshPro en la escena.");
        }
    }
}
