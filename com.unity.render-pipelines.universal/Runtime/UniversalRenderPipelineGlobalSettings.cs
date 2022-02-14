using System;
using UnityEditor;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Universal Render Pipeline's Global Settings.
    /// Global settings are unique per Render Pipeline type. In URP, Global Settings contain:
    /// - light layer names
    /// </summary>
    [URPHelpURL("URP-Global-Settings")]
    partial class UniversalRenderPipelineGlobalSettings : RenderPipelineGlobalSettings, ISerializationCallbackReceiver
    {
        #region Version system

#pragma warning disable CS0414
        [SerializeField] int k_AssetVersion = 2;
#pragma warning restore CS0414

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (k_AssetVersion != 2)
            {
                EditorApplication.delayCall += () => UpgradeAsset(this.GetInstanceID());
            }
#endif
        }

#if UNITY_EDITOR
        static void UpgradeAsset(int assetInstanceID)
        {
            UniversalRenderPipelineGlobalSettings asset = EditorUtility.InstanceIDToObject(assetInstanceID) as UniversalRenderPipelineGlobalSettings;

            if (asset.k_AssetVersion < 2)
            {
#pragma warning disable 618 // Obsolete warning
                // Renamed supportRuntimeDebugDisplay => stripDebugVariants, which results in inverted logic
                asset.m_StripDebugVariants = !asset.supportRuntimeDebugDisplay;
                asset.k_AssetVersion = 2;
#pragma warning restore 618 // Obsolete warning

                // For old test projects lets keep post processing stripping enabled, as huge chance they did not used runtime profile creating
#if UNITY_INCLUDE_TESTS
                asset.m_StripUnusedPostProcessingVariants = true;
#endif
            }

            EditorUtility.SetDirty(asset);
        }

#endif
        #endregion

        private static UniversalRenderPipelineGlobalSettings cachedInstance = null;
        /// <summary>
        /// Active URP Global Settings asset. If the value is null then no UniversalRenderPipelineGlobalSettings has been registered to the Graphics Settings with the UniversalRenderPipeline.
        /// </summary>
        public static UniversalRenderPipelineGlobalSettings instance
        {
            get
            {
#if !UNITY_EDITOR
                // The URP Global Settings could have been changed by script, undo/redo (case 1342987), or file update - file versioning, let us make sure we display the correct one
                // In a Player, we do not need to worry about those changes as we only support loading one
                if (cachedInstance == null)
#endif
                    cachedInstance = GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>() as UniversalRenderPipelineGlobalSettings;
                return cachedInstance;
            }
        }

        static internal void UpdateGraphicsSettings(UniversalRenderPipelineGlobalSettings newSettings)
        {
            if (newSettings == cachedInstance)
                return;
            if (newSettings != null)
                GraphicsSettings.RegisterRenderPipelineSettings<UniversalRenderPipeline>(newSettings as RenderPipelineGlobalSettings);
            else
                GraphicsSettings.UnregisterRenderPipelineSettings<UniversalRenderPipeline>();
            cachedInstance = newSettings;
        }

        /// <summary>Default name when creating an URP Global Settings asset.</summary>
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

                    // TODO: Reenable after next urp template is published
                    //Debug.LogWarning("No URP Global Settings Asset is assigned. One will be created for you. If you want to modify it, go to Project Settings > Graphics > URP Settings.");
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
                if (assetCreated != null)
                {
                    assetCreated.name = System.IO.Path.GetFileName(path);
                }
                AssetDatabase.CreateAsset(assetCreated, path);
                Debug.Assert(assetCreated);
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
                    UpdateRenderingLayerNames();
                return m_RenderingLayerNames;
            }
        }
        [System.NonSerialized]
        string[] m_PrefixedRenderingLayerNames;
        string[] prefixedRenderingLayerNames
        {
            get
            {
                if (m_PrefixedRenderingLayerNames == null)
                    UpdateRenderingLayerNames();
                return m_PrefixedRenderingLayerNames;
            }
        }
        /// <summary>Names used for display of rendering layer masks.</summary>
        public string[] renderingLayerMaskNames => renderingLayerNames;
        /// <summary>Names used for display of rendering layer masks with a prefix.</summary>
        public string[] prefixedRenderingLayerMaskNames => prefixedRenderingLayerNames;

        /// <summary>Regenerate Rendering Layer names and their prefixed versions.</summary>
        internal void UpdateRenderingLayerNames()
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

            // Update prefixed
            if (m_PrefixedRenderingLayerNames == null)
                m_PrefixedRenderingLayerNames = new string[32];
            if (m_PrefixedLightLayerNames == null)
                m_PrefixedLightLayerNames = new string[8];
            for (int i = 0; i < m_PrefixedRenderingLayerNames.Length; ++i)
            {
                m_PrefixedRenderingLayerNames[i] = string.Format("{0}: {1}", i, m_RenderingLayerNames[i]);
                if (i < 8)
                    m_PrefixedLightLayerNames[i] = m_PrefixedRenderingLayerNames[i];
            }
        }

        [System.NonSerialized]
        string[] m_PrefixedLightLayerNames = null;
        /// <summary>
        /// Names used for display of light layers with Layer's index as prefix.
        /// For example: "0: Light Layer Default"
        /// </summary>
        public string[] prefixedLightLayerNames
        {
            get
            {
                if (m_PrefixedLightLayerNames == null)
                    UpdateRenderingLayerNames();
                return m_PrefixedLightLayerNames;
            }
        }

        #region Light Layer Names [3D]

        static readonly string[] k_DefaultLightLayerNames = { "Light Layer default", "Light Layer 1", "Light Layer 2", "Light Layer 3", "Light Layer 4", "Light Layer 5", "Light Layer 6", "Light Layer 7" };

        /// <summary>Name for light layer 0.</summary>
        public string lightLayerName0 = k_DefaultLightLayerNames[0];
        /// <summary>Name for light layer 1.</summary>
        public string lightLayerName1 = k_DefaultLightLayerNames[1];
        /// <summary>Name for light layer 2.</summary>
        public string lightLayerName2 = k_DefaultLightLayerNames[2];
        /// <summary>Name for light layer 3.</summary>
        public string lightLayerName3 = k_DefaultLightLayerNames[3];
        /// <summary>Name for light layer 4.</summary>
        public string lightLayerName4 = k_DefaultLightLayerNames[4];
        /// <summary>Name for light layer 5.</summary>
        public string lightLayerName5 = k_DefaultLightLayerNames[5];
        /// <summary>Name for light layer 6.</summary>
        public string lightLayerName6 = k_DefaultLightLayerNames[6];
        /// <summary>Name for light layer 7.</summary>
        public string lightLayerName7 = k_DefaultLightLayerNames[7];

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

        internal void ResetRenderingLayerNames()
        {
            lightLayerName0 = k_DefaultLightLayerNames[0];
            lightLayerName1 = k_DefaultLightLayerNames[1];
            lightLayerName2 = k_DefaultLightLayerNames[2];
            lightLayerName3 = k_DefaultLightLayerNames[3];
            lightLayerName4 = k_DefaultLightLayerNames[4];
            lightLayerName5 = k_DefaultLightLayerNames[5];
            lightLayerName6 = k_DefaultLightLayerNames[6];
            lightLayerName7 = k_DefaultLightLayerNames[7];
        }

        #endregion

        #region Misc Settings

        [SerializeField] bool m_StripDebugVariants = true;

        [SerializeField] bool m_StripUnusedPostProcessingVariants = false;

        [SerializeField] bool m_StripUnusedVariants = true;

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        [Obsolete("Please use stripRuntimeDebugShaders instead.", false)]
        public bool supportRuntimeDebugDisplay = false;

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        public bool stripDebugVariants { get => m_StripDebugVariants; set { m_StripDebugVariants = value; } }

        /// <summary>
        /// Controls whether strips automatically post processing shader variants based on <see cref="VolumeProfile"/> components.
        /// It strips based on VolumeProfiles in project and not scenes that actually uses it.
        /// </summary>
        public bool stripUnusedPostProcessingVariants { get => m_StripUnusedPostProcessingVariants; set { m_StripUnusedPostProcessingVariants = value; } }

        /// <summary>
        /// Controls whether strip off variants if the feature is enabled.
        /// </summary>
        public bool stripUnusedVariants { get => m_StripUnusedVariants; set { m_StripUnusedVariants = value; } }

        #endregion
    }
}
