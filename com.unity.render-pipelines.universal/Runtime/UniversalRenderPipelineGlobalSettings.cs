using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Universal Render Pipeline's Global Settings.
    /// Global settings are unique per Render Pipeline type. In URP, Global Settings contain:
    /// - light layer names
    /// </summary>
    partial class UniversalRenderPipelineGlobalSettings : RenderPipelineGlobalSettings
    {
        #region Version system
        [SerializeField] int k_AssetVersion = 1;
        [SerializeField] int k_AssetPreviousVersion = 1;

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (k_AssetPreviousVersion != k_AssetVersion)
            {
                EditorApplication.delayCall += () => UpgradeAsset(this);
            }
#endif
        }

#if UNITY_EDITOR
        static void UpgradeAsset(UniversalRenderPipelineGlobalSettings asset)
        {
            EditorUtility.SetDirty(asset);
        }

#endif
        #endregion

        private static UniversalRenderPipelineGlobalSettings cachedInstance = null;
        public static UniversalRenderPipelineGlobalSettings instance
        {
            get
            {
                if (cachedInstance == null)
                    cachedInstance = GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>() as UniversalRenderPipelineGlobalSettings;
                return cachedInstance;
            }
        }

        static internal void UpdateGraphicsSettings(UniversalRenderPipelineGlobalSettings newSettings)
        {
            if (newSettings == null || newSettings == cachedInstance)
                return;
            GraphicsSettings.RegisterRenderPipelineSettings<UniversalRenderPipeline>(newSettings as RenderPipelineGlobalSettings);
            cachedInstance = newSettings;
        }

        public static readonly string defaultAssetName = "UniversalRenderPipelineGlobalSettings";

#if UNITY_EDITOR
        //Making sure there is at least one UniversalRenderPipelineGlobalSettings instance in the project
        internal static UniversalRenderPipelineGlobalSettings Ensure(string folderPath = "", bool canCreateNewAsset = true)
        {
            if (UniversalRenderPipelineGlobalSettings.instance)
                return UniversalRenderPipelineGlobalSettings.instance;

            UniversalRenderPipelineGlobalSettings assetCreated = null;
            string path = $"Assets/{folderPath}/{defaultAssetName}.asset";
            assetCreated = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineGlobalSettings>(path);
            if (assetCreated == null)
            {
                var guidGlobalSettingsAssets = AssetDatabase.FindAssets("t:UniversalRenderPipelineGlobalSettings");
                //If we could not find the asset at the default path, find the first one
                if (guidGlobalSettingsAssets.Length > 0)
                {
                    var curGUID = guidGlobalSettingsAssets[0];
                    path = AssetDatabase.GUIDToAssetPath(curGUID);
                    assetCreated = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineGlobalSettings>(path);
                }
                else if (canCreateNewAsset)// or create one altogether
                {
                    if (!AssetDatabase.IsValidFolder("Assets/" + folderPath))
                        AssetDatabase.CreateFolder("Assets", folderPath);
                    assetCreated = Create(path);

                    Debug.LogWarning("No URP Global Settings Asset is assigned. One will be created for you. If you want to modify it, go to Project Settings > Graphics > URP Settings.");
                }
                else
                {
                    Debug.LogError("If you are building a Player, make sure to save an URP Global Settings asset by opening the project in the Editor first.");
                    return null;
                }
            }
            Debug.Assert(assetCreated, "Could not create URP's Global Settings - URP may not work correctly - Open  Project Settings > Graphics > URP Settings for additional help.");
            UpdateGraphicsSettings(assetCreated);
            return UniversalRenderPipelineGlobalSettings.instance;
        }

        internal static UniversalRenderPipelineGlobalSettings Create(string path, UniversalRenderPipelineGlobalSettings src = null)
        {
            UniversalRenderPipelineGlobalSettings assetCreated = null;

            // make sure the asset does not already exists
            assetCreated = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineGlobalSettings>(path);
            if (assetCreated == null)
            {
                assetCreated = ScriptableObject.CreateInstance<UniversalRenderPipelineGlobalSettings>();
                AssetDatabase.CreateAsset(assetCreated, path);
                if (assetCreated != null)
                {
                    assetCreated.name = System.IO.Path.GetFileName(path);
                }
            }

            if (assetCreated)
            {
                if (src != null)
                {
                    assetCreated.lightLayerName0 = System.String.Copy(src.lightLayerName0);
                    assetCreated.lightLayerName1 = System.String.Copy(src.lightLayerName1);
                    assetCreated.lightLayerName2 = System.String.Copy(src.lightLayerName2);
                    assetCreated.lightLayerName3 = System.String.Copy(src.lightLayerName3);
                    assetCreated.lightLayerName4 = System.String.Copy(src.lightLayerName4);
                    assetCreated.lightLayerName5 = System.String.Copy(src.lightLayerName5);
                    assetCreated.lightLayerName6 = System.String.Copy(src.lightLayerName6);
                    assetCreated.lightLayerName7 = System.String.Copy(src.lightLayerName7);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return assetCreated;
        }

#endif

        void Reset()
        {
            UpdateRenderingLayerNames();
        }

        [System.NonSerialized]
        string[] m_RenderingLayerNames;
        string[] renderingLayerNames
        {
            get
            {
                if (m_RenderingLayerNames == null)
                {
                    UpdateRenderingLayerNames();
                }

                return m_RenderingLayerNames;
            }
        }
        public string[] renderingLayerMaskNames => renderingLayerNames;

        void UpdateRenderingLayerNames()
        {
            if (m_RenderingLayerNames == null)
                m_RenderingLayerNames = new string[32];

            int index = 0;
            m_RenderingLayerNames[index++] = lightLayerName0;
            m_RenderingLayerNames[index++] = lightLayerName1;
            m_RenderingLayerNames[index++] = lightLayerName2;
            m_RenderingLayerNames[index++] = lightLayerName3;
            m_RenderingLayerNames[index++] = lightLayerName4;
            m_RenderingLayerNames[index++] = lightLayerName5;
            m_RenderingLayerNames[index++] = lightLayerName6;
            m_RenderingLayerNames[index++] = lightLayerName7;

            // Unused
            for (int i = index; i < m_RenderingLayerNames.Length; ++i)
            {
                m_RenderingLayerNames[i] = string.Format("Unused {0}", i);
            }
        }

        #region Light Layer Names [3D]

        /// <summary>Name for light layer 0.</summary>
        public string lightLayerName0 = "Light Layer default";
        /// <summary>Name for light layer 1.</summary>
        public string lightLayerName1 = "Light Layer 1";
        /// <summary>Name for light layer 2.</summary>
        public string lightLayerName2 = "Light Layer 2";
        /// <summary>Name for light layer 3.</summary>
        public string lightLayerName3 = "Light Layer 3";
        /// <summary>Name for light layer 4.</summary>
        public string lightLayerName4 = "Light Layer 4";
        /// <summary>Name for light layer 5.</summary>
        public string lightLayerName5 = "Light Layer 5";
        /// <summary>Name for light layer 6.</summary>
        public string lightLayerName6 = "Light Layer 6";
        /// <summary>Name for light layer 7.</summary>
        public string lightLayerName7 = "Light Layer 7";


        [System.NonSerialized]
        string[] m_LightLayerNames = null;
        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        public string[] lightLayerNames
        {
            get
            {
                if (m_LightLayerNames == null)
                {
                    m_LightLayerNames = new string[8];
                }

                m_LightLayerNames[0] = lightLayerName0;
                m_LightLayerNames[1] = lightLayerName1;
                m_LightLayerNames[2] = lightLayerName2;
                m_LightLayerNames[3] = lightLayerName3;
                m_LightLayerNames[4] = lightLayerName4;
                m_LightLayerNames[5] = lightLayerName5;
                m_LightLayerNames[6] = lightLayerName6;
                m_LightLayerNames[7] = lightLayerName7;

                return m_LightLayerNames;
            }
        }

        #endregion
    }
}
