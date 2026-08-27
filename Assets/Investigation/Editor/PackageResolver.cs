using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Investigation.EditorTools
{
    public static class PackageResolver
    {
        [MenuItem("Tools/Investigation/Resolve Packages")]
        public static void Resolve()
        {
            Client.Resolve();
            AssetDatabase.Refresh();
            Debug.Log("[PackageResolver] Package resolution triggered!");
        }
    }
}
