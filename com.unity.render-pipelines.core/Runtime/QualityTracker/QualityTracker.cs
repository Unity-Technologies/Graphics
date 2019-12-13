using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{

    [ExecuteInEditMode]
    public class QualityTracker : MonoBehaviour
    {
        public static RenderPipelineAsset GetCurrentRenderPipeline()
        {
            return QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) is RenderPipelineAsset qualityAsset ? qualityAsset : GraphicsSettings.renderPipelineAsset;
        }

#if UNITY_EDITOR
        private int lastQualityLevel = -1;

        public QualityLevel[] Levels;


        public GameObject[] MaterialSceneRoots;

        public enum QualityModes
        {
            EnableOnThisLevel = 0,
            DisableOnThisLevel,
            IgnoreThisLevel
        }

        [System.Serializable]
        public struct QualityLevel
        {
            public string Name;
            public int Level;
            public GameObject[] EnableOnLevel;
            public GameObject[] DisableOnLevel;
            public MaterialRoot[] MaterialAssetRoots;
            public Material Skybox;
        }

        void Update()
        {
            int currentQualityLevel = QualitySettings.GetQualityLevel();

            if (lastQualityLevel < 0 || currentQualityLevel != lastQualityLevel)
            {
                // quality changed

                Debug.Log("New Quality level: " + currentQualityLevel);
                lastQualityLevel = currentQualityLevel;

                ActivateChildrenBasedOnLevel(currentQualityLevel);

            }
        }

        public void ActivateChildrenBasedOnLevel(int qualityLevel = -1)
        {


            if (qualityLevel < 0)
                qualityLevel = lastQualityLevel;



            foreach (var level in Levels)
            {
                bool isCurrentLevel = level.Level == qualityLevel;

                if (level.EnableOnLevel == null && level.DisableOnLevel == null)
                    return;

                foreach (var enableElement in level.EnableOnLevel)
                {
                    enableElement.SetActive(isCurrentLevel);
                }

                if (isCurrentLevel)
                {
                    foreach (var disbleElement in level.DisableOnLevel)
                    {
                        disbleElement.SetActive(false);
                    }

                    if (level.Skybox != null)
                        RenderSettings.skybox = level.Skybox;

                    List<string> materialRootFolders = new List<string>();
                    foreach (var materialRoot in level.MaterialAssetRoots)
                    {
                        string assetPathRoot = materialRoot.GetAssetPath();
                        if (string.IsNullOrEmpty(assetPathRoot))
                            continue;

                        string materialRootFolder = System.IO.Path.GetDirectoryName(assetPathRoot);
                        materialRootFolder = materialRootFolder.Replace("\\", "/");
                        materialRootFolders.Add(materialRootFolder);
                    }

                    Dictionary<string, Material> loadedMaterials = new Dictionary<string, Material>();
                    foreach (var materialSceneRoot in MaterialSceneRoots)
                    {
                        List<Renderer> allRenderer = new List<Renderer>(materialSceneRoot.GetComponentsInChildren<Renderer>(true));
                        foreach (var renderer in allRenderer)
                        {
                            List<int> subIDs = new List<int>();
                            Material[] currentMaterials = renderer.sharedMaterials;
                            for (int i = 0; i < currentMaterials.Length; i++)
                            {
                                Material currentMaterial = currentMaterials[i];
                                if (currentMaterial != null)
                                {
                                    string materialName = renderer.sharedMaterials[i].name.Replace("(Instance)", "").Trim();
                                    Material newMaterial = null;
                                    if (loadedMaterials.ContainsKey(materialName))
                                    {
                                        newMaterial = loadedMaterials[materialName];
                                    }
                                    else
                                    {
                                        string[] guids = AssetDatabase.FindAssets(materialName, materialRootFolders.ToArray());
                                        foreach (var guid in guids)
                                        {
                                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                                            newMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                                            if (newMaterial != null)
                                            {
                                                loadedMaterials.Add(materialName, newMaterial);
                                                break;
                                            }
                                        }

                                    }
                                    if (newMaterial != null)
                                        currentMaterials[i] = newMaterial;
                                }
                            }
                            renderer.sharedMaterials = currentMaterials;
                        }
                    }
                }
            }
        }
#endif
    }
}
