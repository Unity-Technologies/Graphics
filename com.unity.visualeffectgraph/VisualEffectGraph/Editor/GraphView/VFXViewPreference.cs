using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public static class VFXViewPreference
    {
        private static bool m_Loaded = false;
        private static bool m_DisplayExperimentalOperator = false;
        private static bool m_DisplayExtraDebugInfo = false;
        private static bool m_ForceEditionCompilation = false;

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

        public static bool forceEditionCompilation { get { return m_ForceEditionCompilation; } }

        public const string experimentalOperatorKey = "VFX.displayExperimentalOperatorKey";
        public const string extraDebugInfoKey = "VFX.ExtraDebugInfo";
        public const string forceEditionCompilationKey = "VFX.ForceEditionCompilation";

        private static void LoadIfNeeded()
        {
            if (!m_Loaded)
            {
                m_DisplayExperimentalOperator = EditorPrefs.GetBool(experimentalOperatorKey, false);
                m_DisplayExtraDebugInfo = EditorPrefs.GetBool(extraDebugInfoKey, false);
                m_ForceEditionCompilation = EditorPrefs.GetBool(forceEditionCompilationKey, false);

                m_Loaded = true;
            }
        }

        [PreferenceItem("Visual Effects")]
        public static void PreferencesGUI()
        {
            LoadIfNeeded();
            m_DisplayExperimentalOperator = EditorGUILayout.Toggle("Experimental Operators/Blocks", m_DisplayExperimentalOperator);
            m_DisplayExtraDebugInfo = EditorGUILayout.Toggle("Show Additional DebugInfo", m_DisplayExtraDebugInfo);

            bool oldForceEditionCompilation = m_ForceEditionCompilation;
            m_ForceEditionCompilation = EditorGUILayout.Toggle("Force Compilation in Edition Mode", m_ForceEditionCompilation);
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

            if (GUI.changed)
            {
                EditorPrefs.SetBool(experimentalOperatorKey, m_DisplayExperimentalOperator);
                EditorPrefs.SetBool(extraDebugInfoKey, m_DisplayExtraDebugInfo);
                EditorPrefs.SetBool(forceEditionCompilationKey, m_ForceEditionCompilation);
            }
        }
    }
}
