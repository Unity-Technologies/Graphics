using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    internal class MaterialDiffusionProfileReferences : EditorWindow
    {
        static HashSet<string> s_MissingProfiles = new HashSet<string>();

        static class Style
        {
            public static readonly GUIContent title = EditorGUIUtility.TrTextContent("Missing Diffusion Profiles");
        }

        public static void RequireDiffusionProfile(string assetPath)
        {
            if (s_MissingProfiles.Count == 0)
                EditorApplication.update += OpenWindow;

            s_MissingProfiles.Add(assetPath);
        }

        static void OpenWindow()
        {
            EditorApplication.update -= OpenWindow;

            if (HDRenderPipelineGlobalSettings.instance == null || HDRenderPipelineGlobalSettings.instance.diffusionProfileSettingsList == null)
            {
                s_MissingProfiles.Clear();
                return;
            }

            var window = GetWindow<MaterialDiffusionProfileReferences>(Style.title.text);
            window.minSize = new Vector2(500, 450);
        }

        Vector2 scrollView = Vector2.zero;
        ReorderableList uiList;
        bool[] profilesToRegister;
        List<DiffusionProfileSettings> missingProfiles;

        void OnEnable()
        {
            missingProfiles = new List<DiffusionProfileSettings>();
            foreach (var assetPath in s_MissingProfiles)
                missingProfiles.Add(AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(assetPath));
            s_MissingProfiles.Clear();

            profilesToRegister = new bool[missingProfiles.Count];
            uiList = new ReorderableList(missingProfiles, typeof(string), false, false, false, false)
            {
                drawElementCallback = DrawProfileItem,
            };
        }

        void DrawProfileItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (missingProfiles[index] == null)
            {
                EditorGUI.LabelField(rect, "Item has been deleted");
                profilesToRegister[index] = false;
                return;
            }
            if (HDRenderPipelineGlobalSettings.instance.diffusionProfileSettingsList.Any(d => d == missingProfiles[index]))
            {
                EditorGUI.LabelField(rect, "Item has already been added");
                profilesToRegister[index] = false;
                return;
            }

            profilesToRegister[index] = EditorGUI.Toggle(rect, profilesToRegister[index]);
            rect.xMin += 14.0f + 9.0f; // width of the checkbox + padding

            EditorGUI.LabelField(rect, missingProfiles[index].name);
        }

        void OnGUI()
        {
            EditorGUILayout.Space();

            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField("While importing Materials in your project, HDRP detected references to diffusion profile assets that are not registered in the HDRP Global Setting.\n" +
                "If the Diffusion Profile is not referenced in the global settings, HDRP cannot use it.\n" +
                "Select in the list below the missing diffusion profiles you want to add to the HDRP Global Settings asset", textStyle);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Missing Diffusion Profiles", EditorStyles.boldLabel);

            scrollView = EditorGUILayout.BeginScrollView(scrollView, GUILayout.MaxHeight(position.height));
            uiList.ClearSelection();
            uiList.DoLayoutList();
            EditorGUILayout.EndScrollView();

            int maxProfiles = DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1;
            int spaceInSettings = maxProfiles - HDRenderPipelineGlobalSettings.instance.diffusionProfileSettingsList.Length;

            int enabledAmount = profilesToRegister.Count(x => x);
            if (enabledAmount > spaceInSettings)
                EditorGUILayout.HelpBox("HDRP only allows up to " + maxProfiles + " custom profiles. Please unselect at least " + (enabledAmount - spaceInSettings) + ".", MessageType.Error, true);

            using (new EditorGUI.DisabledScope(enabledAmount > spaceInSettings))
            {
                if (GUILayout.Button("Add selected profiles to HDRP"))
                {
                    for (int i = 0; i < profilesToRegister.Length; i++)
                    {
                        if (!profilesToRegister[i]) continue;
                        HDRenderPipelineGlobalSettings.instance.AddDiffusionProfile(missingProfiles[i]);
                        Close();
                    }
                }
            }
        }
    }
}
