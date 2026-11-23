using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>Rebuilds every playable floor and exits, so a single batch run refreshes all scenes.</summary>
    public static class BuildAllFloors
    {
        [MenuItem("Misterio/Build All Playable Floors")]
        public static void BuildAll()
        {
            Floor1SceneBuilder.Build();
            Floor2SceneBuilder.Build();
            Floor3SceneBuilder.Build();
            Floor4SceneBuilder.Build();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Floors] all four playable floors built");
            EditorApplication.Exit(0);
        }
    }
}
