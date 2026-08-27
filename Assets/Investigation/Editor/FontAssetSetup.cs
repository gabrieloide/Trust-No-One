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
            var bebas = GetOrCreateFontAsset(BebasTtfPath, BebasAssetPath);
            var courier = GetOrCreateFontAsset(CourierTtfPath, CourierAssetPath);
            var spaceMono = GetOrCreateFontAsset(SpaceMonoTtfPath, SpaceMonoAssetPath);
            var specialElite = GetOrCreateFontAsset(SpecialEliteTtfPath, SpecialEliteAssetPath);

            ApplyFontsToActiveScene(bebas, courier, spaceMono, specialElite);

            // Reconstruir la escena para asegurar que todos los prefabs y builders usen las nuevas fuentes
            WorldBuilder.Build();

            // Guardar cambios
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FontAssetSetup] ¡Todas las fuentes TMP fueron generadas y aplicadas a toda la UI exitosamente!");
        }

        public static TMP_FontAsset GetOrCreateFontAsset(string fontPath, string targetAssetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError($"[FontAssetSetup] No se encontró Font en {fontPath}");
                return null;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            if (fontAsset != null)
            {
                string dir = Path.GetDirectoryName(targetAssetPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                AssetDatabase.CreateAsset(fontAsset, targetAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[FontAssetSetup] Creado TMP_FontAsset en: {targetAssetPath}");
            }
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

                // 1. Títulos, Carteles y Overlays -> Bebas Neue
                if (name.Contains("title") || name.Contains("overlay") || name.Contains("header") ||
                    parentName.Contains("overlay") || parentName.Contains("accuse") ||
                    (parentName.StartsWith("location_") && name.Contains("label")))
                {
                    tmp.font = bebas != null ? bebas : courier;
                    if (tmp.fontMaterial != null && bebas != null) tmp.fontSharedMaterial = bebas.material;
                }
                // 2. Diálogos, Textos largos y Nombres de personajes -> Courier Prime
                else if (name.Contains("dialogue") || name.Contains("speaker") || name.Contains("subtitle"))
                {
                    tmp.font = courier;
                    if (tmp.fontMaterial != null && courier != null) tmp.fontSharedMaterial = courier.material;
                }
                // 3. Botones, HUD, Pistas, Opciones y Prompt -> Space Mono
                else
                {
                    tmp.font = spaceMono != null ? spaceMono : courier;
                    if (tmp.fontMaterial != null && spaceMono != null) tmp.fontSharedMaterial = spaceMono.material;
                }

                EditorUtility.SetDirty(tmp);
                count++;
            }

            Debug.Log($"[FontAssetSetup] Se asignaron las nuevas fuentes a {count} componentes TextMeshPro en la escena.");
        }
    }
}
