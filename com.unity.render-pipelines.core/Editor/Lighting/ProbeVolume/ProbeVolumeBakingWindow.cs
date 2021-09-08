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
        const int k_RightPanelLabelWidth = 200;
        const int k_ProbeVolumeIconSize = 30;
        const string k_RenameFocusKey = "Baking Set Rename Field";

        struct SceneData
        {
            public SceneAsset asset;
            public string path;
            public string guid;
        }

        static class Styles
        {
            public static readonly Texture sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image;
            public static readonly Texture probeVolumeIcon = EditorGUIUtility.IconContent("LightProbeGroup Icon").image; // For now it's not the correct icon, we need to request it

            public static readonly GUIContent sceneLightingSettings = new GUIContent("Light Settings In Use", EditorGUIUtility.IconContent("LightingSettings Icon").image);
            public static readonly GUIContent sceneNotFound = new GUIContent("Scene Not Found!", Styles.sceneIcon);
        }

        SearchField m_SearchField;
        string m_SearchString = "";
        MethodInfo m_DrawHorizontalSplitter;
        [NonSerialized] ReorderableList m_BakingSets = null;
        Vector2 m_LeftScrollPosition;
        Vector2 m_RightScrollPosition;
        ReorderableList m_ScenesInSet;
        GUIStyle m_SubtitleStyle;
        Editor m_ProbeVolumeProfileEditor;
        SerializedObject m_SerializedObject;
        SerializedProperty m_ProbeSceneData;
        bool m_RenameSelectedBakingSet;
        [System.NonSerialized]
        bool m_StyleInitialized;

        List<SceneData> m_ScenesInProject = new List<SceneData>();

        ProbeVolumeSceneData sceneData => ProbeReferenceVolume.instance.sceneData;

        [MenuItem("Window/Rendering/Probe Volume Settings (Experimental)")]
        static void OpenWindow()
        {
            // Get existing open window or if none, make a new one:
            ProbeVolumeBakingWindow window = (ProbeVolumeBakingWindow)EditorWindow.GetWindow(typeof(ProbeVolumeBakingWindow));
            window.Show();
        }

        void OnEnable()
        {
            m_SearchField = new SearchField();
            titleContent = new GUIContent("Probe Volume Settings (Experimental)");

            RefreshSceneAssets();
            m_DrawHorizontalSplitter = typeof(EditorGUIUtility).GetMethod("DrawHorizontalSplitter", BindingFlags.NonPublic | BindingFlags.Static);
        }

        void OnDisable()
        {
            if (m_ProbeVolumeProfileEditor != null)
                Object.DestroyImmediate(m_ProbeVolumeProfileEditor);
        }

        void InitializeStyles()
        {
            if (m_StyleInitialized)
                return;

            m_SubtitleStyle = new GUIStyle(EditorStyles.boldLabel);
            m_SubtitleStyle.fontSize = 20;

            m_StyleInitialized = true;
        }

        void InitializeBakingSetListIfNeeded()
        {
            if (m_BakingSets != null)
                return;

            m_SerializedObject = new SerializedObject(sceneData.parentAsset);
            m_ProbeSceneData = m_SerializedObject.FindProperty(sceneData.parentSceneDataPropertyName);

            m_BakingSets = new ReorderableList(sceneData.bakingSets, typeof(ProbeVolumeSceneData.BakingSet), false, false, true, true);
            m_BakingSets.multiSelect = false;
            m_BakingSets.drawElementCallback = (rect, index, active, focused) =>
            {
                m_SerializedObject.Update();
                // Draw the renamable label for the baking set name
                string key = k_RenameFocusKey + index;
                if (Event.current.type == EventType.MouseDown && GUI.GetNameOfFocusedControl() != key)
                    m_RenameSelectedBakingSet = false;
                if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
                {
                    if (rect.Contains(Event.current.mousePosition))
                        m_RenameSelectedBakingSet = true;
                }
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                    m_RenameSelectedBakingSet = false;

                var set = sceneData.bakingSets[index];

                if (m_RenameSelectedBakingSet)
                {
                    EditorGUI.BeginChangeCheck();
                    GUI.SetNextControlName(key);
                    set.name = EditorGUI.DelayedTextField(rect, set.name, EditorStyles.boldLabel);
                    if (EditorGUI.EndChangeCheck())
                        m_RenameSelectedBakingSet = false;
                }
                else
                    EditorGUI.LabelField(rect, set.name, EditorStyles.boldLabel);
            };
            m_BakingSets.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight;
            m_BakingSets.onSelectCallback = OnBakingSetSelected;

            m_BakingSets.onAddCallback = (list) =>
            {
                sceneData.bakingSets.Add(new ProbeVolumeSceneData.BakingSet
                {
                    name = "New Baking Set",
                });
            };

            m_BakingSets.onRemoveCallback = (list) =>
            {
                if (EditorUtility.DisplayDialog("Delete selected baking set?", $"Do you really want to delete the baking set '{sceneData.bakingSets[list.index].name}'?", "Yes", "Cancel"))
                {
                    Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Deleted baking set");
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };

            OnBakingSetSelected(m_BakingSets);
        }

        void RefreshSceneAssets()
        {
            var sceneAssets = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets/" });

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

        SceneData FindSceneData(string guid)
        {
            // TODO: replace m_ScenesInProject list by a dictionary
            var data = m_ScenesInProject.FirstOrDefault(s => s.guid == guid);

            if (data.asset == null)
            {
                RefreshSceneAssets();
                data = m_ScenesInProject.FirstOrDefault(s => s.guid == guid);
            }

            return data;
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
                var scene = FindSceneData(guid);

                // TODO: Add a label on the first scene to say the the lighting settings of this scene will be used for baking

                if (scene.asset != null)
                    EditorGUI.LabelField(rect, new GUIContent(scene.asset.name, Styles.sceneIcon), EditorStyles.boldLabel);
                else
                    EditorGUI.LabelField(rect, Styles.sceneNotFound, EditorStyles.boldLabel);

                // display the probe volume icon in the scene if it have one
                Rect probeVolumeIconRect = rect;
                probeVolumeIconRect.xMin = rect.xMax - k_ProbeVolumeIconSize;
                if (sceneData.hasProbeVolumes.ContainsKey(scene.path))
                    EditorGUI.LabelField(probeVolumeIconRect, new GUIContent(Styles.probeVolumeIcon));

                // Display the lighting settings of the first scene (it will be used for baking)
                if (index == 0)
                {
                    Rect lightingSettingsRect = rect;
                    var lightingLabel = Styles.sceneLightingSettings;
                    var size = EditorStyles.label.CalcSize(lightingLabel);
                    lightingSettingsRect.xMin = rect.xMax - size.x - probeVolumeIconRect.width;
                    EditorGUI.LabelField(lightingSettingsRect, lightingLabel);
                }
            };
            m_ScenesInSet.onAddCallback = (list) =>
            {
                // TODO: replace this generic menu by a mini-window with a search bar
                var menu = new GenericMenu();

                RefreshSceneAssets();
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
                Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Added scene in baking set");
                var setWithScene = sceneData.bakingSets.FirstOrDefault(s => s.sceneGUIDs.Contains(scene.guid));
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

                SyncBakingSetSettings();
            }
        }

        ProbeVolumeSceneData.BakingSet GetCurrentBakingSet()
        {
            int index = Mathf.Clamp(m_BakingSets.index, 0, sceneData.bakingSets.Count);
            return sceneData.bakingSets[index];
        }

        string GetFirstProbeVolumeSceneGUID(ProbeVolumeSceneData.BakingSet set)
        {
            foreach (var guid in set.sceneGUIDs)
            {
                if (sceneData.sceneBakingSettings.ContainsKey(guid) && sceneData.sceneProfiles.ContainsKey(guid))
                    return guid;
            }
            return null;
        }

        void OnGUI()
        {
            // TODO: add the toolbar with search field for the list
            // DrawToolbar();

            if (ProbeReferenceVolume.instance?.sceneData?.bakingSets == null)
            {
                EditorGUILayout.HelpBox("Probe Volume Data Not Loaded!", MessageType.Error);
                return;
            }

            // The window can load before the APV system
            InitializeStyles();
            InitializeBakingSetListIfNeeded();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawSeparator();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                SyncBakingSetSettings();
        }

        void SyncBakingSetSettings()
        {
            // Sync all the scene settings in the set to avoid config mismatch.
            foreach (var set in sceneData.bakingSets)
            {
                var sceneGUID = GetFirstProbeVolumeSceneGUID(set);

                if (sceneGUID == null)
                    continue;

                var referenceGUID = set.sceneGUIDs[0];
                var referenceSettings = sceneData.sceneBakingSettings[referenceGUID];
                var referenceProfile = sceneData.sceneProfiles[referenceGUID];

                foreach (var guid in set.sceneGUIDs)
                {
                    sceneData.sceneBakingSettings[guid] = referenceSettings;
                    sceneData.sceneProfiles[guid] = referenceProfile;
                }
            }
        }

        void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(k_LeftPanelSize));
            m_LeftScrollPosition = EditorGUILayout.BeginScrollView(m_LeftScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Baking Sets", m_SubtitleStyle);
            EditorGUILayout.Space();
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
            EditorGUIUtility.labelWidth = k_RightPanelLabelWidth;
            EditorGUILayout.BeginVertical();
            m_RightScrollPosition = EditorGUILayout.BeginScrollView(m_RightScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField("Probe Volume Settings", m_SubtitleStyle);
            EditorGUILayout.Space();
            m_ScenesInSet.DoLayoutList();

            var set = GetCurrentBakingSet();
            var sceneGUID = GetFirstProbeVolumeSceneGUID(set);
            if (sceneGUID != null)
            {
                EditorGUILayout.Space();

                // Show only the profile from the first scene of the set (they all should be the same)
                var profile = ProbeReferenceVolume.instance.sceneData.sceneProfiles[sceneGUID];
                if (m_ProbeVolumeProfileEditor == null)
                    m_ProbeVolumeProfileEditor = Editor.CreateEditor(profile);
                if (m_ProbeVolumeProfileEditor.target != profile)
                    Editor.CreateCachedEditor(profile, m_ProbeVolumeProfileEditor.GetType(), ref m_ProbeVolumeProfileEditor);

                EditorGUILayout.LabelField("Probe Volume Profile", EditorStyles.largeLabel);
                m_ProbeVolumeProfileEditor.OnInspectorGUI();

                EditorGUILayout.Space();

                var serializedBakedSettings = m_ProbeSceneData.FindPropertyRelative("serializedBakeSettings");
                var s = serializedBakedSettings.GetArrayElementAtIndex(0);
                var probeVolumeBakingSettings = s.FindPropertyRelative("settings");
                EditorGUILayout.PropertyField(probeVolumeBakingSettings);
            }
            else
            {
                EditorGUILayout.HelpBox("You need to assign at least one scene with probe volumes to configure the baking settings", MessageType.Error, true);
            }

            DrawBakeButton();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawBakeButton()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load All Scenes In Set", GUILayout.ExpandWidth(true)))
            {
                LoadScenesInBakingSet(GetCurrentBakingSet());
            }
            if (Lightmapping.isRunning)
            {
                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true)))
                    Lightmapping.Cancel();
            }
            else
            {
                if (GUILayout.Button("Generate Lighting", GUILayout.ExpandWidth(true)))
                    BakeLightingForSet(GetCurrentBakingSet());
            }
            if (GUILayout.Button("Clear Baked Data"))
            {
                Lightmapping.Clear();
            }
            EditorGUILayout.EndHorizontal();
        }

        void BakeLightingForSet(ProbeVolumeSceneData.BakingSet set)
        {
            // Save current scenes:
            if (!EditorSceneManager.SaveOpenScenes())
            {
                Debug.LogError("Can't bake while a scene is dirty!");
                return;
            }

            var scenesToRestore = new List<string>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                scenesToRestore.Add(EditorSceneManager.GetSceneAt(i).path);

            // First, load all the scenes
            LoadScenesInBakingSet(set);

            // Then we wait 1 frame for HDRP to render and bake
            bool skipFirstFrame = true;
            EditorApplication.update += WaitRenderAndBake;
            void WaitRenderAndBake()
            {
                if (skipFirstFrame)
                {
                    skipFirstFrame = false;
                    return;
                }
                EditorApplication.update -= WaitRenderAndBake;

                UnityEditor.Lightmapping.BakeAsync();

                // Enqueue scene restore operation after bake is finished
                EditorApplication.update += RestoreScenesAfterBake;
            }

            void RestoreScenesAfterBake()
            {
                if (Lightmapping.isRunning)
                    return;

                EditorApplication.update -= RestoreScenesAfterBake;

                LoadScenes(scenesToRestore);
            }
        }

        void LoadScenesInBakingSet(ProbeVolumeSceneData.BakingSet set)
            => LoadScenes(GetCurrentBakingSet().sceneGUIDs.Select(sceneGUID => m_ScenesInProject.FirstOrDefault(s => s.guid == sceneGUID).path));

        void LoadScenes(IEnumerable<string> scenePathes)
        {
            bool loadFirst = true;
            foreach (var scenePath in scenePathes)
            {
                if (loadFirst)
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                else
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                loadFirst = false;
            }
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
