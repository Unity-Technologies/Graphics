using System.Collections.Generic;
using Unity.Collections;
using System;
using UnityEditor;
using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;
using UnityEditor.IMGUI.Controls;
using System.Reflection;
using UnityEditorInternal;
using System.Linq;

namespace UnityEngine.Experimental.Rendering
{
    class ProbeVolumeBakingWindow : EditorWindow
    {
        const int k_LeftPanelSize = 300; // TODO: resizable panel

        struct SceneData
        {
            public SceneAsset asset;
            public string path;
            public string guid;
        }

        SearchField m_SearchField;
        string m_SearchString = "";
        MethodInfo m_DrawHorizontalSplitter;
        [NonSerialized] ReorderableList m_BakingSets = null;
        Vector2 m_LeftScrollPosition;
        Vector2 m_RightScrollPosition;
        // ProbeVolumeSceneData m_BakingSettings;
        ReorderableList m_ScenesInSet;
        GUIStyle m_SubtitleStyle;
        Editor m_ProbeVolumeProfileEditor;

        List<SceneData> m_ScenesInProject = new List<SceneData>();

        static PropertyInfo s_SceneGUID = typeof(Scene).GetProperty("guid", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);

        [MenuItem("Window/ProbeVolumeBakingWindow")]
        static void OpenWindow()
        {
            // Get existing open window or if none, make a new one:
            ProbeVolumeBakingWindow window = (ProbeVolumeBakingWindow)EditorWindow.GetWindow(typeof(ProbeVolumeBakingWindow));
            window.Show();
        }

        void OnEnable()
        {
            m_SearchField = new SearchField();

            RefreshSceneAssets();
            m_DrawHorizontalSplitter = typeof(EditorGUIUtility).GetMethod("DrawHorizontalSplitter", BindingFlags.NonPublic | BindingFlags.Static);
        }

        void OnDisable()
        {
            if (m_ProbeVolumeProfileEditor != null)
                Object.Destroy(m_ProbeVolumeProfileEditor);
        }

        void InitializeBakingSetListIfNeeded()
        {
            if (m_BakingSets != null)
                return;

            // Hum, move that somewhere else
            m_SubtitleStyle = new GUIStyle(EditorStyles.boldLabel);
            m_SubtitleStyle.fontSize = 20;

            m_BakingSets = new ReorderableList(ProbeReferenceVolume.instance.sceneData.bakingSets, typeof(ProbeVolumeSceneData.BakingSet), false, true, true, true);

            m_BakingSets.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Baking Sets", EditorStyles.largeLabel);
            m_BakingSets.multiSelect = false;

            m_BakingSets.drawElementCallback = (rect, index, active, focused) =>
            {
                var set = ProbeReferenceVolume.instance.sceneData.bakingSets[index];
                EditorGUI.LabelField(rect, set.name, EditorStyles.boldLabel);
            };
            m_BakingSets.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight;
            m_BakingSets.onSelectCallback = OnBakingSetSelected;

            OnBakingSetSelected(m_BakingSets);
        }

        void RefreshSceneAssets()
        {
            var sceneAssets = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets/" });

            // Debug.Log("HEHEO: " + sceneAssets.Length);
            // Debug.Log("A: " + m_BakingSettings.sceneProfiles.Count);
            // foreach (var a in m_BakingSettings.sceneProfiles)
            // {
            //     Debug.Log(a.Key);
            // }

            m_ScenesInProject = sceneAssets.Select(s =>
            {
                var path = AssetDatabase.GUIDToAssetPath(s);
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                return new SceneData
                {
                    asset = asset,
                    path = path,
                    guid = s
                };
            }).ToList();
        }

        void OnBakingSetSelected(ReorderableList list)
        {
            // Update left panel data
            int index = Mathf.Clamp(list.index, 0, ProbeReferenceVolume.instance.sceneData.bakingSets.Count);
            var set = ProbeReferenceVolume.instance.sceneData.bakingSets[index];

            m_ScenesInSet = new ReorderableList(set.sceneGUIDs, typeof(string), true, true, true, true);
            m_ScenesInSet.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Scenes", EditorStyles.largeLabel);
            m_ScenesInSet.multiSelect = true;
            m_ScenesInSet.drawElementCallback = (rect, index, active, focused) =>
            {
                Debug.Log(set.sceneGUIDs);
                var guid = set.sceneGUIDs[index];
                Debug.Log(guid);
                // Find scene name from GUID:
                var data = m_ScenesInProject.FirstOrDefault(s => s.guid == guid); // TODO: dictionary + update code if the scene is not found
                Debug.Log(data.asset.name);
                EditorGUI.LabelField(rect, new GUIContent(data.asset.name, EditorGUIUtility.IconContent("SceneAsset Icon").image), EditorStyles.boldLabel);
            };

            // Show only the profile from the first scene of the set (they all should be the same)
            var guid = set.sceneGUIDs.FirstOrDefault();
            var profile = ProbeReferenceVolume.instance.sceneData.sceneProfiles[guid];
            if (m_ProbeVolumeProfileEditor == null)
                m_ProbeVolumeProfileEditor = Editor.CreateEditor(profile);
            if (m_ProbeVolumeProfileEditor.target != profile)
                m_ProbeVolumeProfileEditor.target = profile;
        }

        void OnGUI()
        {
            DrawToolbar();

            if (ProbeReferenceVolume.instance?.sceneData?.bakingSets == null)
            {
                EditorGUILayout.HelpBox("No Probe Volume Data", MessageType.Error);
                return;
            }

            // The window can load before the APV system
            InitializeBakingSetListIfNeeded();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawSeparator();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(k_LeftPanelSize));
            m_LeftScrollPosition = EditorGUILayout.BeginScrollView(m_LeftScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            m_BakingSets.DoLayoutList();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawSeparator()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            m_DrawHorizontalSplitter?.Invoke(null, new object[] { new Rect(k_LeftPanelSize, 20, 2, position.height) }); // TODO: remove magic numbers
            EditorGUILayout.EndVertical();
        }

        void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();
            m_RightScrollPosition = EditorGUILayout.BeginScrollView(m_RightScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Probe Volume Settings", m_SubtitleStyle);

            EditorGUILayout.Space();

            m_ScenesInSet.DoLayoutList();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Probe Volume Profile", EditorStyles.largeLabel);
            m_ProbeVolumeProfileEditor.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Probe Volume Authoring Settings", EditorStyles.largeLabel);
            // TODO: show dilation and other settings

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawToolbar()
        {
            // Gameobject popup dropdown
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.FlexibleSpace();

            //Search field GUI
            GUILayout.Space(6);
            var searchRect = EditorGUILayout.GetControlRect(false, GUILayout.MaxWidth(300));
            m_SearchString = m_SearchField.OnToolbarGUI(searchRect, m_SearchString);

            GUILayout.EndHorizontal();
        }
    }
}
