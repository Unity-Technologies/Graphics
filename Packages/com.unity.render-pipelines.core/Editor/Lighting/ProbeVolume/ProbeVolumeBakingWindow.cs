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
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    class ProbeVolumeBakingWindow : EditorWindow
    {
        const int k_LeftPanelSize = 300; // TODO: resizable panel
        const int k_RightPanelLabelWidth = 200;
        const int k_ProbeVolumeIconSize = 30;
        const int k_TitleTextHeight = 30;
        const string k_SelectedBakingSetKey = "Selected Baking Set";
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
            public static readonly GUIContent bakingSetsTitle = new GUIContent("Baking Sets", Styles.sceneIcon);
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
        bool m_Initialized;

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

            Undo.undoRedoPerformed -= RefreshAfterUndo;
            Undo.undoRedoPerformed += RefreshAfterUndo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= RefreshAfterUndo;
            if (m_ProbeVolumeProfileEditor != null)
                Object.DestroyImmediate(m_ProbeVolumeProfileEditor);
        }

        void Initialize()
        {
            if (m_Initialized)
                return;

            m_SubtitleStyle = new GUIStyle(EditorStyles.boldLabel);
            m_SubtitleStyle.fontSize = 20;

            m_SerializedObject = new SerializedObject(sceneData.parentAsset);
            m_ProbeSceneData = m_SerializedObject.FindProperty(sceneData.parentSceneDataPropertyName);

            InitializeBakingSetList();

            m_Initialized = true;
        }

        void InitializeBakingSetList()
        {
            m_BakingSets = new ReorderableList(sceneData.bakingSets, typeof(ProbeVolumeSceneData.BakingSet), false, false, true, true);
            m_BakingSets.multiSelect = false;
            m_BakingSets.drawElementCallback = (rect, index, active, focused) =>
            {
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
                    {
                        m_RenameSelectedBakingSet = false;

                        // Rename profile asset to match name:
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(set.profile), set.name);
                        set.profile.name = set.name;
                    }
                }
                else
                    EditorGUI.LabelField(rect, set.name, EditorStyles.boldLabel);
            };
            m_BakingSets.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight;
            m_BakingSets.onSelectCallback = OnBakingSetSelected;

            m_BakingSets.onAddCallback = (list) =>
            {
                Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Added new baking set");
                sceneData.CreateNewBakingSet("New Baking Set");
                m_SerializedObject.Update();
                OnBakingSetSelected(list);
            };

            m_BakingSets.onRemoveCallback = (list) =>
            {
                if (m_BakingSets.count == 1)
                {
                    EditorUtility.DisplayDialog("Can't delete baking set", "You can't delete the last Baking set. You need to have at least one.", "Ok");
                    return;
                }
                if (EditorUtility.DisplayDialog("Delete the selected baking set?", $"Deleting the baking set will also delete it's profile asset on disk.\nDo you really want to delete the baking set '{sceneData.bakingSets[list.index].name}'?\n\nYou cannot undo the delete assets action.", "Yes", "Cancel"))
                {
                    var pathToDelete = AssetDatabase.GetAssetPath(sceneData.bakingSets[list.index].profile);
                    if (!String.IsNullOrEmpty(pathToDelete))
                        AssetDatabase.DeleteAsset(pathToDelete);
                    Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Deleted baking set");
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    // A new set will be selected automatically, so we perform the same operations as if we did the selection explicitly.
                    OnBakingSetSelected(m_BakingSets);

                }
            };

            m_BakingSets.index = Mathf.Clamp(EditorPrefs.GetInt(k_SelectedBakingSetKey, 0), 0, m_BakingSets.count - 1);

            OnBakingSetSelected(m_BakingSets);
        }

        void RefreshAfterUndo()
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                // Feature not enabled, nothing to do.
                return;
            }

            InitializeBakingSetList();

            OnBakingSetSelected(m_BakingSets);

            Repaint();
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
            EditorPrefs.SetInt(k_SelectedBakingSetKey, list.index);
            var set = GetCurrentBakingSet();

            m_ScenesInSet = new ReorderableList(set.sceneGUIDs, typeof(string), true, true, true, true);
            m_ScenesInSet.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Scenes", EditorStyles.largeLabel);
            m_ScenesInSet.multiSelect = true;
            m_ScenesInSet.drawElementCallback = (rect, index, active, focused) =>
            {
                var guid = set.sceneGUIDs[index];
                // Find scene name from GUID:
                var scene = FindSceneData(guid);

                if (scene.asset != null)
                    EditorGUI.LabelField(rect, new GUIContent(scene.asset.name, Styles.sceneIcon), EditorStyles.boldLabel);
                else
                    EditorGUI.LabelField(rect, Styles.sceneNotFound, EditorStyles.boldLabel);

                // display the probe volume icon in the scene if it have one
                Rect probeVolumeIconRect = rect;
                probeVolumeIconRect.xMin = rect.xMax - k_ProbeVolumeIconSize;
                if (sceneData.hasProbeVolumes.ContainsKey(scene.guid))
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
            m_ScenesInSet.onRemoveCallback = (list) =>
            {
                Undo.RegisterCompleteObjectUndo(sceneData.parentAsset, "Deleted scene in baking set");
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
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

                sceneData.SyncBakingSetSettings();
                m_SerializedObject.Update();
            }
        }

        ProbeVolumeSceneData.BakingSet GetCurrentBakingSet()
        {
            int index = Mathf.Clamp(m_BakingSets.index, 0, sceneData.bakingSets.Count - 1);
            return sceneData.bakingSets[index];
        }

        void OnGUI()
        {
            // TODO: add the toolbar with search field for the list
            // DrawToolbar();

            string apvDisabledErrorMsg = "The Probe Volume is not enabled.";
            var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "HDRenderPipelineAsset")
            {
                apvDisabledErrorMsg += " Make sure it is enabled in the HDRP Global Settings and in the HDRP asset in use.";
            }

            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                EditorGUILayout.HelpBox(apvDisabledErrorMsg, MessageType.Error);
                return;
            }

            if (ProbeReferenceVolume.instance.sceneData?.bakingSets == null)
            {
                EditorGUILayout.HelpBox("Probe Volume Data Not Loaded!", MessageType.Error);
                return;
            }

            // The window can load before the APV system
            Initialize();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawSeparator();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                sceneData.SyncBakingSetSettings();
        }

        void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(k_LeftPanelSize));
            m_LeftScrollPosition = EditorGUILayout.BeginScrollView(m_LeftScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            var titleRect = EditorGUILayout.GetControlRect(true, k_TitleTextHeight);
            EditorGUI.LabelField(titleRect, "Baking Sets", m_SubtitleStyle);
            EditorGUILayout.Space();
            m_BakingSets.DoLayoutList();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawSeparator()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            m_DrawHorizontalSplitter?.Invoke(null, new object[] { new Rect(k_LeftPanelSize, 20, 2, position.height) });
            EditorGUILayout.EndVertical();
        }

        void SanitizeScenes()
        {
            // Remove entries in the list pointing to deleted scenes
            foreach (var set in sceneData.bakingSets)
                set.sceneGUIDs.RemoveAll(guid => FindSceneData(guid).asset == null);
        }

        void DrawRightPanel()
        {
            EditorGUIUtility.labelWidth = k_RightPanelLabelWidth;
            EditorGUILayout.BeginVertical();
            m_RightScrollPosition = EditorGUILayout.BeginScrollView(m_RightScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            var titleRect = EditorGUILayout.GetControlRect(true, k_TitleTextHeight);
            EditorGUI.LabelField(titleRect, "Probe Volume Settings", m_SubtitleStyle);
            EditorGUILayout.Space();
            SanitizeScenes();
            m_ScenesInSet.DoLayoutList();

            var set = GetCurrentBakingSet();
            var sceneGUID = sceneData.GetFirstProbeVolumeSceneGUID(set);
            if (sceneGUID != null)
            {
                EditorGUILayout.Space();

                // Show only the profile from the first scene of the set (they all should be the same)
                if (set.profile == null)
                {
                    EditorUtility.DisplayDialog("Missing Probe Volume Profile Asset!", $"We couldn't find the asset profile associated with the Baking Set '{set.name}'.\nDo you want to create a new one?", "Yes");
                    set.profile = ScriptableObject.CreateInstance<ProbeReferenceVolumeProfile>();

                    // Delay asset creation, workaround to avoid creating assets while importing another one (SRP can be called from asset import).
                    EditorApplication.update += DelayCreateAsset;
                    void DelayCreateAsset()
                    {
                        EditorApplication.update -= DelayCreateAsset;
                        ProjectWindowUtil.CreateAsset(set.profile, set.name + ".asset");
                    }
                }
                if (m_ProbeVolumeProfileEditor == null)
                    m_ProbeVolumeProfileEditor = Editor.CreateEditor(set.profile);
                if (m_ProbeVolumeProfileEditor.target != set.profile)
                    Editor.CreateCachedEditor(set.profile, m_ProbeVolumeProfileEditor.GetType(), ref m_ProbeVolumeProfileEditor);

                EditorGUILayout.LabelField("Probe Volume Profile", EditorStyles.largeLabel);
                m_ProbeVolumeProfileEditor.OnInspectorGUI();

                EditorGUILayout.Space();

                var serializedSets = m_ProbeSceneData.FindPropertyRelative("serializedBakingSets");
                var serializedSet = serializedSets.GetArrayElementAtIndex(m_BakingSets.index);
                var probeVolumeBakingSettings = serializedSet.FindPropertyRelative("settings");
                EditorGUILayout.PropertyField(probeVolumeBakingSettings);

                // Clamp to make sure minimum we set for dilation distance is min probe distance
                set.settings.dilationSettings.dilationDistance = Mathf.Max(set.profile.minDistanceBetweenProbes, set.settings.dilationSettings.dilationDistance);
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
            EditorGUI.BeginDisabledGroup(Lightmapping.isRunning);
            if (GUILayout.Button("Load All Scenes In Set", GUILayout.ExpandWidth(true)))
                LoadScenesInBakingSet(GetCurrentBakingSet());
            if (GUILayout.Button("Clear Loaded Scene Data"))
                Lightmapping.Clear();
            EditorGUI.EndDisabledGroup();
            if (Lightmapping.isRunning)
            {
                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true)))
                    Lightmapping.Cancel();
            }
            else
            {
                if (GUILayout.Button("Generate Lighting", GUILayout.ExpandWidth(true)))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Bake the set"), false, () => BakeLightingForSet(GetCurrentBakingSet()));
                    menu.AddItem(new GUIContent("Bake loaded scenes"), false, () => Lightmapping.BakeAsync());
                    menu.ShowAsContext();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void BakeLightingForSet(ProbeVolumeSceneData.BakingSet set)
        {
            // Save current scenes:
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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
