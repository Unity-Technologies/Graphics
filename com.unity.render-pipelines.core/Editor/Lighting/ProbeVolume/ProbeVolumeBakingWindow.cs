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
using UnityEditor.SceneManagement;

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
        SerializedObject m_SerializedObject;
        SerializedProperty m_ProbeSceneData;

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
                Object.DestroyImmediate(m_ProbeVolumeProfileEditor);
        }

        void InitializeBakingSetListIfNeeded()
        {
            if (m_BakingSets != null)
                return;

            // Hum, move that somewhere else
            m_SubtitleStyle = new GUIStyle(EditorStyles.boldLabel);
            m_SubtitleStyle.fontSize = 20;

            m_SerializedObject = new SerializedObject(ProbeReferenceVolume.instance.sceneData.parentAsset);
            m_ProbeSceneData = m_SerializedObject.FindProperty("apvScenesData"); // TODO: read this from the scene data object

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

            m_BakingSets.onAddCallback = (list) =>
            {
                ProbeReferenceVolume.instance.sceneData.bakingSets.Add(new ProbeVolumeSceneData.BakingSet
                {
                    name = "New Baking Set",
                });
            };

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
            var set = GetCurrentBakingSet();

            m_ScenesInSet = new ReorderableList(set.sceneGUIDs, typeof(string), true, true, true, true);
            m_ScenesInSet.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Scenes", EditorStyles.largeLabel);
            m_ScenesInSet.multiSelect = true;
            m_ScenesInSet.drawElementCallback = (rect, index, active, focused) =>
            {
                var guid = set.sceneGUIDs[index];
                // Find scene name from GUID:
                var data = m_ScenesInProject.FirstOrDefault(s => s.guid == guid); // TODO: dictionary + update code if the scene is not found
                EditorGUI.LabelField(rect, new GUIContent(data.asset.name, EditorGUIUtility.IconContent("SceneAsset Icon").image), EditorStyles.boldLabel);
            };
            m_ScenesInSet.onAddCallback = (list) =>
            {
                var menu = new GenericMenu();

                foreach (var scene in m_ScenesInProject)
                {
                    if (set.sceneGUIDs.Contains(scene.guid))
                        continue;

                    menu.AddItem(new GUIContent(scene.asset.name), false, () =>
                    {
                        TryAddScene(scene);
                    });
                }

                if (menu.GetItemCount() == 0)
                    menu.AddDisabledItem(new GUIContent("No available scenes"));

                menu.ShowAsContext();
            };

            void TryAddScene(SceneData scene)
            {
                // Don't allow the same scene in two different sets
                var setWithScene = ProbeReferenceVolume.instance.sceneData.bakingSets.FirstOrDefault(s => s.sceneGUIDs.Contains(scene.guid));
                if (setWithScene != null)
                {
                    if (EditorUtility.DisplayDialog("Move Scene to baking set", $"The scene '{scene.asset.name}' was already added in the baking set '{setWithScene.name}'. Do you want to move it to the current set?", "Yes", "Cancel"))
                    {
                        setWithScene.sceneGUIDs.Remove(scene.guid);
                        set.sceneGUIDs.Add(scene.guid);
                    }
                }
                else
                    set.sceneGUIDs.Add(scene.guid);
            }

            // Show only the profile from the first scene of the set (they all should be the same)
            var guid = set.sceneGUIDs.FirstOrDefault();
            var profile = ProbeReferenceVolume.instance.sceneData.sceneProfiles[guid];
            if (m_ProbeVolumeProfileEditor == null)
                m_ProbeVolumeProfileEditor = Editor.CreateEditor(profile);
            if (m_ProbeVolumeProfileEditor.target != profile)
                m_ProbeVolumeProfileEditor.target = profile;
        }

        ProbeVolumeSceneData.BakingSet GetCurrentBakingSet()
        {
            int index = Mathf.Clamp(m_BakingSets.index, 0, ProbeReferenceVolume.instance.sceneData.bakingSets.Count);
            return ProbeReferenceVolume.instance.sceneData.bakingSets[index];
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

            // TODO: code to sync the settings between all scenes inside a set (they must use the same asset / settings)
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
            // EditorGUILayout.LabelField("Probe Volume Authoring Settings", EditorStyles.largeLabel);
            // TODO: show dilation and other settings

            // For now only show a the first scene data, maybe we can display scene by scene?
            var set = GetCurrentBakingSet();
            var sceneGUID = set.sceneGUIDs[0];
            var sceneBakingSettings = ProbeReferenceVolume.instance.sceneData.sceneBakingSettings[sceneGUID];

            var serializedBakedSettings = m_ProbeSceneData.FindPropertyRelative("serializedBakeSettings");
            var s = serializedBakedSettings.GetArrayElementAtIndex(0);
            var probeVolumeBakingSettings = s.FindPropertyRelative("settings");

            EditorGUILayout.PropertyField(probeVolumeBakingSettings);

            DrawBakeButton();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawBakeButton()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Generate Lighting", GUILayout.Width(200)))
            {
                // Save current scenes:
                if (!EditorSceneManager.SaveOpenScenes())
                {
                    Debug.LogError("Can't bake while a scene is dirty!");
                    return;
                }

                var scenesToRestore = new List<Scene>();
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    scenesToRestore.Add(EditorSceneManager.GetSceneAt(i));

                // for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                // EditorSceneManager.UnloadScene(EditorSceneManager.GetSceneAt(0));

                bool loadFirst = true;
                foreach (var sceneGUID in GetCurrentBakingSet().sceneGUIDs)
                {
                    var scene = m_ScenesInProject.FirstOrDefault(s => s.guid == sceneGUID);

                    if (loadFirst)
                        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                    else
                        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                    loadFirst = false;

                    if (scene.asset != null)
                    {
                        // UnityEditor.Experimental.Lightmapping.Bake;
                        UnityEditor.Lightmapping.BakeAsync();
                    }
                }
                // Debug.Log("TODO: Baking!");
            }
            EditorGUILayout.EndHorizontal();
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
