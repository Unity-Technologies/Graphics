using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VFXViewPreference
    {
        private static bool m_Loaded = false;
        private static bool m_GenerateOutputContextWithShaderGraph;
        private static bool m_DisplayExperimentalOperator = false;
        private static bool m_AllowShaderExternalization = false;
        private static bool m_DisplayExtraDebugInfo = false;
        private static bool m_ForceEditionCompilation = false;
        private static bool m_AdvancedLogs = false;
        private static VFXMainCameraBufferFallback m_CameraBuffersFallback = VFXMainCameraBufferFallback.PreferMainCamera;
        private static bool m_MultithreadUpdateEnabled = true;

        public static bool generateOutputContextWithShaderGraph
        {
            get
            {
                LoadIfNeeded();
                return m_GenerateOutputContextWithShaderGraph;
            }
        }

        public static bool displayExperimentalOperator
        {
            get
            {
                LoadIfNeeded();
                return m_DisplayExperimentalOperator;
            }
        }

        public static bool displayExtraDebugInfo
        {
            get
            {
                LoadIfNeeded();
                return m_DisplayExtraDebugInfo;
            }
        }

        public static bool advancedLogs
        {
            get
            {
                LoadIfNeeded();
                return m_AdvancedLogs;
            }
        }

        public static bool forceEditionCompilation
        {
            get
            {
                LoadIfNeeded();
                return m_ForceEditionCompilation;
            }
        }

        public static VFXMainCameraBufferFallback cameraBuffersFallback
        {
            get
            {
                LoadIfNeeded();
                return m_CameraBuffersFallback;
            }
        }

        public static bool multithreadUpdateEnabled
        {
            get
            {
                LoadIfNeeded();
                return m_MultithreadUpdateEnabled;
            }
        }

        public const string experimentalOperatorKey = "VFX.displayExperimentalOperatorKey";
        public const string extraDebugInfoKey = "VFX.ExtraDebugInfo";
        public const string forceEditionCompilationKey = "VFX.ForceEditionCompilation";
        public const string allowShaderExternalizationKey = "VFX.allowShaderExternalization";
        public const string advancedLogsKey = "VFX.AdvancedLogs";
        public const string cameraBuffersFallbackKey = "VFX.CameraBuffersFallback";
        public const string multithreadUpdateEnabledKey = "VFX.MultithreadUpdateEnabled";

        private static void LoadIfNeeded()
        {
            if (!m_Loaded)
            {
                m_GenerateOutputContextWithShaderGraph = true;
                m_DisplayExperimentalOperator = EditorPrefs.GetBool(experimentalOperatorKey, false);
                m_DisplayExtraDebugInfo = EditorPrefs.GetBool(extraDebugInfoKey, false);
                m_ForceEditionCompilation = EditorPrefs.GetBool(forceEditionCompilationKey, false);
                m_AllowShaderExternalization = EditorPrefs.GetBool(allowShaderExternalizationKey, false);
                m_AdvancedLogs = EditorPrefs.GetBool(advancedLogsKey, false);
                m_CameraBuffersFallback = (VFXMainCameraBufferFallback)EditorPrefs.GetInt(cameraBuffersFallbackKey, (int)VFXMainCameraBufferFallback.PreferMainCamera);
                m_MultithreadUpdateEnabled = EditorPrefs.GetBool(multithreadUpdateEnabledKey, true);
                m_Loaded = true;
            }
        }

        class VFXSettingsProvider : SettingsProvider
        {
            public VFXSettingsProvider() : base("Preferences/Visual Effects", SettingsScope.User)
            {
                hasSearchInterestHandler = HasSearchInterestHandler;
            }

            bool HasSearchInterestHandler(string searchContext)
            {
                return true;
            }

            public override void OnGUI(string searchContext)
            {
                using (new SettingsWindow.GUIScope())
                {
                    LoadIfNeeded();
                    m_GenerateOutputContextWithShaderGraph = EditorGUILayout.Toggle(new GUIContent("Improved Shader Graph Generation", "When enabled, any shadergraph shaders can be assigned to Visual Effect outputs as long as ‘Support VFX Graph’ is enabled in the ShaderGraph’s Graph Inspector."), m_GenerateOutputContextWithShaderGraph);
                    m_DisplayExperimentalOperator = EditorGUILayout.Toggle(new GUIContent("Experimental Operators/Blocks", "When enabled, operators and blocks which are still in an experimental state become available to use within the Visual Effect Graph."), m_DisplayExperimentalOperator);
                    m_DisplayExtraDebugInfo = EditorGUILayout.Toggle(new GUIContent("Show Additional Debug info", "When enabled, additional information becomes available in the inspector when selecting blocks, such as the attributes they use and their shader code."), m_DisplayExtraDebugInfo);
                    m_AdvancedLogs = EditorGUILayout.Toggle(new GUIContent("Verbose Mode for compilation", "When enabled, additional information about the data, expressions, and generated shaders is displayed in the console whenever a graph is compiled."), m_AdvancedLogs);
                    m_AllowShaderExternalization = EditorGUILayout.Toggle(new GUIContent("Experimental shader externalization", "When enabled, the generated shaders are stored alongside the Visual Effect asset, enabling their direct modification."), m_AllowShaderExternalization);

                    bool oldForceEditionCompilation = m_ForceEditionCompilation;
                    m_ForceEditionCompilation = EditorGUILayout.Toggle(new GUIContent("Force Compilation in Edition Mode", "When enabled, the unoptimized edit version of the Visual Effect is compiled even when the effect is not being edited. Otherwise, an optimized runtime version is compiled."), m_ForceEditionCompilation);
                    if (m_ForceEditionCompilation != oldForceEditionCompilation)
                    {
                        // TODO Factorize that somewhere
                        var vfxAssets = new HashSet<VisualEffectAsset>();
                        var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset");
                        foreach (var guid in vfxAssetsGuid)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                            if (vfxAsset != null)
                                vfxAssets.Add(vfxAsset);
                        }

                        foreach (var vfxAsset in vfxAssets)
                            vfxAsset.GetResource().GetOrCreateGraph().SetCompilationMode(m_ForceEditionCompilation ? VFXCompilationMode.Edition : VFXCompilationMode.Runtime);
                    }

#if UNITY_2022_1_OR_NEWER
                    if (Unsupported.IsDeveloperMode())
                    {
                        m_MultithreadUpdateEnabled = EditorGUILayout.Toggle(new GUIContent("Multithread Update Enabled", "When enabled, visual effects will be updated in parallel when possible."), m_MultithreadUpdateEnabled);
                    }
#endif

                    m_CameraBuffersFallback = (VFXMainCameraBufferFallback)EditorGUILayout.EnumPopup(new GUIContent("Main Camera fallback", "Specifies the camera source for the color and depth buffer that MainCamera Operators use when in the editor."), m_CameraBuffersFallback);

                    var userTemplateDirectory = EditorGUILayout.DelayedTextField(new GUIContent("User Systems", "Directory for user-generated VFX templates (e.g. Assets/VFX/Templates)"), VFXResources.defaultResources.userTemplateDirectory);

                    if (GUI.changed)
                    {
                        EditorPrefs.SetBool(experimentalOperatorKey, m_DisplayExperimentalOperator);
                        EditorPrefs.SetBool(extraDebugInfoKey, m_DisplayExtraDebugInfo);
                        EditorPrefs.SetBool(forceEditionCompilationKey, m_ForceEditionCompilation);
                        EditorPrefs.SetBool(advancedLogsKey, m_AdvancedLogs);
                        EditorPrefs.SetBool(allowShaderExternalizationKey, m_AllowShaderExternalization);
                        EditorPrefs.SetInt(cameraBuffersFallbackKey, (int)m_CameraBuffersFallback);
                        EditorPrefs.SetBool(multithreadUpdateEnabledKey, m_MultithreadUpdateEnabled);
                        userTemplateDirectory = userTemplateDirectory.Replace('\\', '/');
                        userTemplateDirectory = userTemplateDirectory.TrimEnd(new char[] { '/' });
                        userTemplateDirectory = userTemplateDirectory.TrimStart(new char[] { '/' });
                        VFXResources.defaultResources.userTemplateDirectory = userTemplateDirectory;
                    }
                }

                if ((VFXResources.defaultResources.userTemplateDirectory.Length > 0) && (!System.IO.Directory.Exists(VFXResources.defaultResources.userTemplateDirectory)))
                    EditorGUILayout.HelpBox("The specified User Systems directory does not exist in the project.", MessageType.Warning);

                base.OnGUI(searchContext);
            }
        }

        [SettingsProvider]
        public static SettingsProvider PreferenceSettingsProvider()
        {
            return new VFXSettingsProvider();
        }

        public static void PreferencesGUI()
        {
        }
    }
}
