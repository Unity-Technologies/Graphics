using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VFXViewPreference
    {
        private static readonly int kAuthoringPrewarmStepCountPerSecondsDefault = 20;
        private static readonly float kAuthoringPrewarmMaxTimeDefault = 3.0f;

        private static readonly int kAuthoringPrewarmStepCountPerSecondsMax = 200;
        private static readonly float kAuthoringPrewarmMaxTimeMax = 60.0f;

        private static bool m_Loaded = false;
        private static bool m_DisplayExperimentalOperator = false;
        private static bool m_DisplayExtraDebugInfo = false;
        private static bool m_AdvancedLogs = false;
        private static VFXMainCameraBufferFallback m_CameraBuffersFallback = VFXMainCameraBufferFallback.PreferMainCamera;
        private static bool m_MultithreadUpdateEnabled = true;
        private static bool m_InstancingEnabled = true;
        private static bool m_ShowPackageIndexingBanner = true;
        private static int m_AuthoringPrewarmStepCountPerSeconds = kAuthoringPrewarmStepCountPerSecondsDefault;
        private static float m_AuthoringPrewarmMaxTime = kAuthoringPrewarmMaxTimeDefault;
        private static bool m_VisualEffectTargetListed = false;

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

        public static bool generateShadersWithDebugSymbols => VFXGlobalImportSettings.instance.generateShadersWithDebugSymbols;

        public static bool advancedLogs
        {
            get
            {
                LoadIfNeeded();
                return m_AdvancedLogs;
            }
        }

        public static bool forceEditionCompilation => VFXGlobalImportSettings.instance.forceEditionCompilation;

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

        public static bool instancingEnabled
        {
            get
            {
                LoadIfNeeded();
                return m_InstancingEnabled;
            }
        }

        internal static bool useNewCompiler => VFXGlobalImportSettings.instance.useNewCompiler;

        public static bool showPackageIndexingBanner
        {
            get
            {
                LoadIfNeeded();
                return m_ShowPackageIndexingBanner;
            }
            set
            {
                m_ShowPackageIndexingBanner = value;
                EditorPrefs.SetBool(showPackageIndexingBannerKey, m_ShowPackageIndexingBanner);
            }
        }

        public static int authoringPrewarmStepCountPerSeconds
        {
            get
            {
                LoadIfNeeded();
                return m_AuthoringPrewarmStepCountPerSeconds;
            }
        }

        public static float authoringPrewarmMaxTime
        {
            get
            {
                LoadIfNeeded();
                return m_AuthoringPrewarmMaxTime;
            }
        }

        public static bool visualEffectTargetListed
        {
            get
            {
                LoadIfNeeded();
                return m_VisualEffectTargetListed;
            }
        }

        public static void SetDirty()
        {
            m_Loaded = false;
            VFXGlobalImportSettings.Reload();
        }

        public const string experimentalOperatorKey = "VFX.displayExperimentalOperatorKey";
        public const string extraDebugInfoKey = "VFX.ExtraDebugInfo";
        public const string advancedLogsKey = "VFX.AdvancedLogs";
        public const string cameraBuffersFallbackKey = "VFX.CameraBuffersFallback";
        public const string multithreadUpdateEnabledKey = "VFX.MultithreadUpdateEnabled";
        public const string instancingEnabledKey = "VFX.InstancingEnabled";
        public const string showPackageIndexingBannerKey = "VFX.ShowPackageIndexingBanner";
        public const string authoringPrewarmStepCountPerSecondsKey = "VFX.AuthoringPrewarmStepCountPerSeconds";
        public const string authoringPrewarmMaxTimeKey = "VFX.AuthoringPrewarmMaxTimeKey";
        public const string visualEffectTargetListedKey = UnityEditor.ShaderGraph.VFXTarget.kVisualEffectTargetListedKey;

        private static void LoadIfNeeded()
        {
            if (!m_Loaded)
            {
                m_DisplayExperimentalOperator = EditorPrefs.GetBool(experimentalOperatorKey, false);
                m_DisplayExtraDebugInfo = EditorPrefs.GetBool(extraDebugInfoKey, false);
                m_AdvancedLogs = EditorPrefs.GetBool(advancedLogsKey, false);
                m_CameraBuffersFallback = (VFXMainCameraBufferFallback)EditorPrefs.GetInt(cameraBuffersFallbackKey, (int)VFXMainCameraBufferFallback.PreferMainCamera);

                m_MultithreadUpdateEnabled = EditorPrefs.GetBool(multithreadUpdateEnabledKey, true);
                m_InstancingEnabled = EditorPrefs.GetBool(instancingEnabledKey, true);

                m_ShowPackageIndexingBanner = EditorPrefs.GetBool(showPackageIndexingBannerKey, true);

                m_AuthoringPrewarmStepCountPerSeconds = EditorPrefs.GetInt(authoringPrewarmStepCountPerSecondsKey, kAuthoringPrewarmStepCountPerSecondsDefault);
                m_AuthoringPrewarmStepCountPerSeconds = Mathf.Clamp(m_AuthoringPrewarmStepCountPerSeconds, 0, kAuthoringPrewarmStepCountPerSecondsMax);

                m_AuthoringPrewarmMaxTime = EditorPrefs.GetFloat(authoringPrewarmMaxTimeKey, kAuthoringPrewarmMaxTimeDefault);
                m_AuthoringPrewarmMaxTime = Mathf.Clamp(m_AuthoringPrewarmMaxTime, 0.0f, kAuthoringPrewarmMaxTimeMax);

                m_VisualEffectTargetListed = EditorPrefs.GetBool(visualEffectTargetListedKey, false);

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

            private static bool DisplayReimportPopup()
            {
                return EditorUtility.DisplayDialog("Recompile all VFX?", "This setting is part of how Visual Effect assets are compiled, so changing it recompiles every Visual Effect asset in the project. (This may take some time)\n\nDo you want to apply the change?", "Apply", "Cancel");
            }

            public override void OnGUI(string searchContext)
            {
                using (new SettingsWindow.GUIScope())
                {
                    LoadIfNeeded();
                    var importSettings = VFXGlobalImportSettings.instance;
                    m_DisplayExperimentalOperator = EditorGUILayout.Toggle(new GUIContent("Experimental Operators/Blocks", "When enabled, operators and blocks which are still in an experimental state become available to use within the Visual Effect Graph."), m_DisplayExperimentalOperator);
                    m_DisplayExtraDebugInfo = EditorGUILayout.Toggle(new GUIContent("Show Additional Debug info", "When enabled, additional information becomes available in the inspector when selecting blocks, such as the attributes they use and their shader code."), m_DisplayExtraDebugInfo);
                    m_AdvancedLogs = EditorGUILayout.Toggle(new GUIContent("Verbose Mode for compilation", "When enabled, additional information about the data, expressions, and generated shaders is displayed in the console whenever a graph is compiled."), m_AdvancedLogs);

                    // Assigning any of these reimports every VFX asset in the project, so the new values are only
                    // committed to the settings once the user has accepted that cost below.
                    var generateShadersWithDebugSymbols = EditorGUILayout.Toggle(new GUIContent("Generate Shaders with Debug Symbols", "When enabled, the VFX shaders are generated with debug symbols."), importSettings.generateShadersWithDebugSymbols);
                    var forceEditionCompilation = EditorGUILayout.Toggle(new GUIContent("Force Compilation in Edition Mode", "When enabled, the unoptimized edit version of the Visual Effect is compiled even when the effect is not being edited. Otherwise, an optimized runtime version is compiled."), importSettings.forceEditionCompilation);

#if UNITY_2022_1_OR_NEWER
                    if (Unsupported.IsDeveloperMode())
                    {
                        m_MultithreadUpdateEnabled = EditorGUILayout.Toggle(new GUIContent("Multithread Update Enabled", "When enabled, visual effects will be updated in parallel when possible."), m_MultithreadUpdateEnabled);
                        m_InstancingEnabled = EditorGUILayout.Toggle(new GUIContent("Instancing Enabled", "When enabled, visual effects will be processed in batches when possible."), m_InstancingEnabled);
                        importSettings.useNewCompiler = EditorGUILayout.Toggle(new GUIContent("Use new VFX Graph compiler", "When enabled, the new VFX Graph compiler will be used for any newly compiled VFX."), importSettings.useNewCompiler);
                    }
#endif

                    m_ShowPackageIndexingBanner = EditorGUILayout.Toggle(new GUIContent("Show package indexing banner", "When enabled, a banner will be displayed in the template window if packages are not indexed"), m_ShowPackageIndexingBanner);

                    m_CameraBuffersFallback = (VFXMainCameraBufferFallback)EditorGUILayout.EnumPopup(new GUIContent("Main Camera fallback", "Specifies the camera source for MainCamera Operators and Blocks to use when in the editor."), m_CameraBuffersFallback);
                    m_VisualEffectTargetListed = EditorGUILayout.Toggle(new GUIContent("Show Target in Shader Graph (deprecated)", "When enabled, the Visual Effect Target is listed in Active Targets dropdown in Shader Graph."), m_VisualEffectTargetListed);

                    m_AuthoringPrewarmStepCountPerSeconds = EditorGUILayout.IntField(new GUIContent("Authoring Prewarm Step Count Per Second", "Specifies the step count per second for prewarming during VFX authoring. High values may impact performance."), m_AuthoringPrewarmStepCountPerSeconds);
                    m_AuthoringPrewarmMaxTime = EditorGUILayout.FloatField(new GUIContent("Authoring Prewarm Maximum Time", "Specifies the maximum prewarming time allowed during VFX authoring in seconds. High values may impact performance."), m_AuthoringPrewarmMaxTime);

                    if (GUI.changed)
                    {
                        EditorPrefs.SetBool(experimentalOperatorKey, m_DisplayExperimentalOperator);
                        EditorPrefs.SetBool(extraDebugInfoKey, m_DisplayExtraDebugInfo);
                        EditorPrefs.SetBool(advancedLogsKey, m_AdvancedLogs);
                        EditorPrefs.SetInt(cameraBuffersFallbackKey, (int)m_CameraBuffersFallback);
                        EditorPrefs.SetBool(multithreadUpdateEnabledKey, m_MultithreadUpdateEnabled);
                        EditorPrefs.SetBool(instancingEnabledKey, m_InstancingEnabled);

                        EditorPrefs.SetBool(showPackageIndexingBannerKey, m_ShowPackageIndexingBanner);

                        m_AuthoringPrewarmStepCountPerSeconds = Mathf.Clamp(m_AuthoringPrewarmStepCountPerSeconds, 0, kAuthoringPrewarmStepCountPerSecondsMax);
                        EditorPrefs.SetInt(authoringPrewarmStepCountPerSecondsKey, m_AuthoringPrewarmStepCountPerSeconds);

                        m_AuthoringPrewarmMaxTime = Mathf.Clamp(m_AuthoringPrewarmMaxTime, 0.0f, kAuthoringPrewarmMaxTimeMax);
                        EditorPrefs.SetFloat(authoringPrewarmMaxTimeKey, m_AuthoringPrewarmMaxTime);

                        EditorPrefs.SetBool(visualEffectTargetListedKey, m_VisualEffectTargetListed);
                    }

                    if (generateShadersWithDebugSymbols != importSettings.generateShadersWithDebugSymbols
                        || forceEditionCompilation != importSettings.forceEditionCompilation)
                    {
                        if (DisplayReimportPopup())
                        {
                            importSettings.generateShadersWithDebugSymbols = generateShadersWithDebugSymbols;
                            importSettings.forceEditionCompilation = forceEditionCompilation;
                        }
                    }
                }

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
