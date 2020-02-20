using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using VirtualTexturingSettings = UnityEngine.Rendering.HighDefinition.VirtualTexturingSettings;

#if ENABLE_VIRTUALTEXTURES
namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(VirtualTexturingSettings))]
    partial class VirtualTexturingSettingsEditor : HDBaseEditor<VirtualTexturingSettings>
    {
        sealed class Settings
        {
            internal SerializedProperty self;
            internal UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings objReference;

            internal SerializedProperty cpuCacheSize;
            internal SerializedProperty gpuCacheSize;
            internal SerializedProperty gpuCacheSizeOverrides;
        }

        Settings m_Settings;

        private bool m_Dirty = false;

        protected override void OnEnable()
        {
            base.OnEnable();

            var serializedSettings = properties.Find(x => x.settings);

            var rp = new RelativePropertyFetcher<UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings>(serializedSettings);

            m_Settings = new Settings
            {
                self = serializedSettings,
                objReference = m_Target.settings,

                cpuCacheSize = rp.Find(x => x.cpuCache.sizeInMegaBytes),
                gpuCacheSize = rp.Find(x => x.gpuCache.sizeInMegaBytes),
                gpuCacheSizeOverrides = rp.Find(x => x.gpuCache.sizeOverrides),
            };

            Undo.undoRedoPerformed += UpdateSettings;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UpdateSettings;
        }

        void ApplyChanges()
        {
            UnityEngine.Rendering.VirtualTexturing.System.ApplyVirtualTexturingSettings(m_Settings.objReference);
        }

        public override void OnInspectorGUI()
        {
            CheckStyles();

            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUI.indentLevel++;

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_Settings.cpuCacheSize, s_Styles.cpuCacheSize);
                EditorGUILayout.PropertyField(m_Settings.gpuCacheSize, s_Styles.gpuCacheSize);

                serializedObject.ApplyModifiedProperties();

                if (scope.changed)
                {
                    m_Dirty = true;
                }
            }

            EditorGUILayout.Space();

            if (m_Dirty)
            {
                if (GUILayout.Button("Apply"))
                {
                    ApplyChanges();
                    m_Dirty = false;
                }
            }

            EditorGUILayout.Space();
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        void UpdateSettings()
        {
            //m_Target.settings.Validate();
        }
        sealed class Styles
        {
            public readonly GUIContent cpuCacheSize = new GUIContent("CPU Cache Size");
            public readonly GUIContent gpuCacheSize = new GUIContent("GPU Cache Size");

            public Styles()
            {

            }
        }

        static Styles s_Styles;

        // Can't use a static initializer in case we need to create GUIStyle in the Styles class as
        // these can only be created with an active GUI rendering context
        void CheckStyles()
        {
            if (s_Styles == null)
                s_Styles = new Styles();
        }
    }
}
#endif
