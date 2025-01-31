using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.Rendering.HighDefinition
{
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class HDLightingWindowEnvironmentSectionEditor : LightingWindowEnvironmentSection
    {
        class Styles
        {
            public static readonly GUIStyle inspectorTitle = "IN Title";
        }

        class SerializedStaticLightingSky
        {
            SerializedObject serializedObject;
            public SerializedProperty skyUniqueID;
            public SerializedProperty cloudUniqueID;
            public SerializedProperty volumetricCloudsToggle;
            public SerializedProperty numberOfBounces;

            public VolumeProfile volumeProfile
            {
                get => (serializedObject.targetObject as StaticLightingSky).profile;
                set => (serializedObject.targetObject as StaticLightingSky).profile = value;
            }

            public SerializedStaticLightingSky(StaticLightingSky staticLightingSky)
            {
                serializedObject = new SerializedObject(staticLightingSky);
                skyUniqueID = serializedObject.FindProperty("m_StaticLightingSkyUniqueID");
                cloudUniqueID = serializedObject.FindProperty("m_StaticLightingCloudsUniqueID");
                volumetricCloudsToggle = serializedObject.FindProperty("m_StaticLightingVolumetricClouds");
                numberOfBounces = serializedObject.FindProperty("bounces");
            }

            public void Apply() => serializedObject.ApplyModifiedProperties();

            public void Update() => serializedObject.Update();

            public bool valid
                => serializedObject != null
                && !serializedObject.Equals(null)
                && serializedObject.targetObject != null
                && !serializedObject.targetObject.Equals(null);
        }

        SerializedStaticLightingSky m_SerializedActiveSceneLightingSky;

        List<GUIContent> m_SkyClassNames = null;
        List<int> m_SkyUniqueIDs = null;
        List<GUIContent> m_CloudClassNames = null;
        List<int> m_CloudUniqueIDs = null;

        const string k_ToggleValueKey = "HDRP:LightingWindowEnvironemntSection:Header";
        bool m_ToggleValue = true;
        bool toggleValue
        {
            get => m_ToggleValue;
            set
            {
                m_ToggleValue = value;
                EditorPrefs.SetBool(k_ToggleValueKey, value);
            }
        }

        static MethodInfo k_FoldoutTitlebar;

        public override void OnEnable()
        {
            m_SerializedActiveSceneLightingSky = new SerializedStaticLightingSky(GetStaticLightingSkyForScene(EditorSceneManager.GetActiveScene()));

            if (EditorPrefs.HasKey(k_ToggleValueKey))
                m_ToggleValue = EditorPrefs.GetBool(k_ToggleValueKey);

            EditorSceneManager.activeSceneChanged += OnActiveSceneChange;
        }

        public override void OnDisable()
            => EditorSceneManager.activeSceneChanged -= OnActiveSceneChange;

        void OnActiveSceneChange(Scene current, Scene next)
            => m_SerializedActiveSceneLightingSky = new SerializedStaticLightingSky(GetStaticLightingSkyForScene(next));

        static internal StaticLightingSky GetStaticLightingSkyForScene(Scene scene)
        {
            StaticLightingSky result = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.TryGetComponent<StaticLightingSky>(out result))
                    break;
            }

            //Perhaps it is an old scene. Search everywhere
            if (result == null)
            {
                var candidates = GameObject.FindObjectsByType<StaticLightingSky>(FindObjectsSortMode.InstanceID).Where(sls => sls.gameObject.scene == scene);
                if (candidates.Count() > 0)
                    result = candidates.First();
            }

            //Perhaps it not exist yet
            if (result == null)
            {
                var go = new GameObject("StaticLightingSky", new[] { typeof(StaticLightingSky) });
                go.hideFlags = HideFlags.HideInHierarchy;
                result = go.GetComponent<StaticLightingSky>();
            }

            return result;
        }

        public override void OnInspectorGUI()
        {
            WorkarroundWhileActiveSceneChangedHookIsNotCalled();
            m_SerializedActiveSceneLightingSky.Update();

            EditorGUI.BeginChangeCheck();

            //Volume can have changed. Check available sky
            UpdateIntPopupData(ref m_SkyClassNames, ref m_SkyUniqueIDs, SkyManager.skyTypesDict, m_SerializedActiveSceneLightingSky.skyUniqueID);
            UpdateIntPopupData(ref m_CloudClassNames, ref m_CloudUniqueIDs, SkyManager.cloudTypesDict, m_SerializedActiveSceneLightingSky.cloudUniqueID);

            DrawGUI();
            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedActiveSceneLightingSky.Apply();
                var hdrp = HDRenderPipeline.currentPipeline;
                if (hdrp != null)
                {
                    hdrp.RequestStaticSkyUpdate();
                    SceneView.RepaintAll();
                }
            }
        }

        void DrawGUI()
        {
            if (k_FoldoutTitlebar == null)
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Static;
                Type[] args = new Type[] { typeof(Rect), typeof(GUIContent), typeof(bool), typeof(bool) };
                k_FoldoutTitlebar = typeof(EditorGUI).GetMethod("FoldoutTitlebar", flags, null, args, null);
            }

            var labelRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.inspectorTitle, GUILayout.ExpandWidth(true));
            var label = EditorGUIUtility.TrTextContent("Environment (HDRP)", "Sky lighting environment for active Scene");

            toggleValue = (bool)k_FoldoutTitlebar.Invoke(null, new object[] { labelRect, label, toggleValue, true });

            if (m_ToggleValue)
            {
                ++EditorGUI.indentLevel;

                //cannot use SerializeProperty due to logic in the property
                var profile = m_SerializedActiveSceneLightingSky.volumeProfile;
                var newProfile = EditorGUILayout.ObjectField(EditorGUIUtility.TrTextContent("Profile"), profile, typeof(VolumeProfile), allowSceneObjects: false) as VolumeProfile;
                if (profile != newProfile)
                {
                    m_SerializedActiveSceneLightingSky.volumeProfile = newProfile;
                }

                using (new EditorGUI.DisabledScope(m_SkyClassNames.Count == 1)) // Only "None"
                {
                    EditorGUILayout.IntPopup(m_SerializedActiveSceneLightingSky.skyUniqueID, m_SkyClassNames.ToArray(), m_SkyUniqueIDs.ToArray(), EditorGUIUtility.TrTextContent("Static Lighting Sky", "Specify which kind of sky you want to use for static ambient in the referenced profile for active scene."));
                }

                if (m_SkyClassNames.Count > 1 && m_SerializedActiveSceneLightingSky.skyUniqueID.intValue > 0)
                    EditorGUILayout.HelpBox("Note that depending on the static lighting sky type, you might need to regenerate the lighting after changing the properties of the main directional light.", MessageType.Info);

                using (new EditorGUI.DisabledScope(m_CloudClassNames.Count == 1)) // Only "None"
                {
                    EditorGUILayout.IntPopup(m_SerializedActiveSceneLightingSky.cloudUniqueID, m_CloudClassNames.ToArray(), m_CloudUniqueIDs.ToArray(), EditorGUIUtility.TrTextContent("Static Lighting Background Clouds", "Specify which kind of background clouds you want to use for static ambient in the referenced profile for active scene."));
                }

                using (new EditorGUI.DisabledScope(m_SerializedActiveSceneLightingSky.volumeProfile == null))
                {
                    EditorGUILayout.PropertyField(m_SerializedActiveSceneLightingSky.volumetricCloudsToggle, EditorGUIUtility.TrTextContent("Static Lighting Volumetric Clouds", "Specify if volumetric clouds should be used for static ambient in the referenced profile for active scene."));
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Reflection Probes");
                using (new EditorGUI.IndentLevelScope())
                    EditorGUILayout.PropertyField(m_SerializedActiveSceneLightingSky.numberOfBounces);

                --EditorGUI.indentLevel;
            }
        }

        void UpdateIntPopupData(ref List<GUIContent> classNames, ref List<int> uniqueIds, Dictionary<int, Type> typesDict, SerializedProperty idProperty)
        {
            if (classNames == null)
            {
                classNames = new List<GUIContent>();
                uniqueIds = new List<int>();
            }

            // We always reinit because the content can change depending on the volume and we are not always notified when this happens (like for undo/redo for example)
            classNames.Clear();
            uniqueIds.Clear();

            // Add special "None" case.
            classNames.Add(new GUIContent("None"));
            uniqueIds.Add(0);

            VolumeProfile profile = m_SerializedActiveSceneLightingSky.volumeProfile;
            if (profile != null)
            {
                var currentID = idProperty.intValue;
                bool foundID = currentID == 0;

                foreach (KeyValuePair<int, Type> kvp in typesDict)
                {
                    if (profile.TryGet(kvp.Value, out VolumeComponent comp) && comp.active)
                    {
                        classNames.Add(new GUIContent(kvp.Value.Name.ToString()));
                        uniqueIds.Add(kvp.Key);
                        foundID |= (currentID == kvp.Key);
                    }
                }

                if (!foundID) // Selected volume component has been deleted
                {
                    idProperty.intValue = 0;
                    GUI.changed = true;
                }
            }
        }

        Scene m_ActiveScene;
        void WorkarroundWhileActiveSceneChangedHookIsNotCalled()
        {
            Scene currentActiveScene = EditorSceneManager.GetActiveScene();

            if (m_ActiveScene != currentActiveScene
                || !(m_SerializedActiveSceneLightingSky?.valid ?? false))
            {
                m_SerializedActiveSceneLightingSky = new SerializedStaticLightingSky(GetStaticLightingSkyForScene(currentActiveScene));
                m_ActiveScene = currentActiveScene;
            }
        }
    }
}
