using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Helper class to display a window listing diffusion profiles not present in the HDRP Global Settings.
    /// </summary>
    [InitializeOnLoad]
    public class MaterialDiffusionProfileReferences : EditorWindow
    {
        static List<DiffusionProfileSettings> s_MissingProfiles = new List<DiffusionProfileSettings>();
        static List<bool> s_ProfilesToRegister = new List<bool>();

        static ReorderableList uiList;
        static Vector2 scrollView = Vector2.zero;

        static class Style
        {
            public static readonly GUIContent title = EditorGUIUtility.TrTextContent("Missing Diffusion Profiles");
            public static readonly GUIContent showWindow = EditorGUIUtility.TrTextContent("Show this window on import", "Uncheck this box to prevent HDRP from opening this window when it imports a Material with a missing diffusion profile.\nYou can also change this setting in the Miscellaneous section of the HDRP Global Settings.\nNote that the window can still be opened by a script.");
            public static readonly GUIContent profileOverride = new GUIContent("To use more than 15 Diffusion Profiles in a Scene, you can use the Diffusion Profile Override inside a Volume.", CoreEditorStyles.iconHelp);

            public static readonly GUIStyle text = new GUIStyle(EditorStyles.label) { wordWrap = true };
        }

        /// <summary>
        /// Notify the user that a Diffusion Profile Asset is required by a material but is not present in the Global Settings.
        /// </summary>
        /// <param name="assetPath">The path of the Diffusion Profile Asset</param>
        public static void RequireDiffusionProfile(string assetPath)
        {
            if (Application.isBatchMode)
                return;

            var diffusionProfile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(assetPath);
            if (diffusionProfile == null ||
                s_MissingProfiles.Any(d => d == diffusionProfile) ||
                HDRenderPipelineGlobalSettings.instance.diffusionProfileSettingsList.Any(d => d == diffusionProfile))
                return;

            if (s_MissingProfiles.Count == 0)
                EditorApplication.update += OpenWindow;

            s_MissingProfiles.Add(diffusionProfile);
            s_ProfilesToRegister.Add(true);
        }

        static void OpenWindow()
        {
            EditorApplication.update -= OpenWindow;

            var window = GetWindow<MaterialDiffusionProfileReferences>(Style.title.text);
            window.minSize = new Vector2(500, 450);
        }

        static void DrawProfileItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            var boxRect = new Rect(rect) { width = 14.0f };
            s_ProfilesToRegister[index] = EditorGUI.Toggle(boxRect, s_ProfilesToRegister[index]);

            using (new EditorGUI.DisabledScope(true))
            {
                // Padding around the field
                rect.xMin += boxRect.width + 9.0f;
                rect.y += 1;
                rect.height = 20;

                EditorGUI.ObjectField(rect, s_MissingProfiles[index], typeof(DiffusionProfileSettings), false);
            }
        }

        void RefreshList()
        {
            for (int i = 0; i < s_MissingProfiles.Count; i++)
            {
                if (s_MissingProfiles[i] == null || HDRenderPipelineGlobalSettings.instance.diffusionProfileSettingsList.Any(d => d == s_MissingProfiles[i]))
                {
                    s_MissingProfiles.RemoveAt(i);
                    s_ProfilesToRegister.RemoveAt(i);
                    i--;
                }
            }
            if (s_MissingProfiles.Count == 0)
                Close();

            if (uiList == null)
            {
                uiList = new ReorderableList(s_MissingProfiles, typeof(DiffusionProfileSettings), false, false, false, false)
                {
                    drawElementCallback = DrawProfileItem,
                };
            }
            else
                uiList.list = s_MissingProfiles;
        }

        void OnGUI()
        {
            int maxProfileCount = DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1;

            var globalSettings = HDRenderPipelineGlobalSettings.instance;
            if (globalSettings == null || globalSettings.diffusionProfileSettingsList == null)
                return;

            RefreshList();

            var style = new GUIStyle(EditorStyles.inspectorDefaultMargins) { padding = new RectOffset(4, 4, 0, 0) };
            EditorGUILayout.BeginVertical(style);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("While importing Materials into your project, HDRP detected references to diffusion profile assets that are not registered in the HDRP Global Settings.", Style.text);
            EditorGUILayout.LabelField("Make sure these Diffusion Profiles are referenced in either a Diffusion Profile Override or the HDRP Global Settings. If a Diffusion Profile is not referenced in one of these places, HDRP cannot use it.", Style.text);
            EditorGUILayout.LabelField("Select which Diffusion Profiles to add in the HDRP Global Settings from the Missing Diffusion Profiles list below.", Style.text);

            EditorGUILayout.Space();

            if (GUILayout.Button(Style.profileOverride, Style.text))
                Application.OpenURL(DocumentationInfo.GetPageLink(Documentation.packageName, "Override-Diffusion-Profile"));

            EditorGUILayout.Space();

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 165.0f;
            globalSettings.showMissingDiffusionProfiles = EditorGUILayout.Toggle(Style.showWindow, globalSettings.showMissingDiffusionProfiles);
            EditorGUIUtility.labelWidth = prevLabelWidth;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Missing Diffusion Profiles", EditorStyles.boldLabel);

            scrollView = EditorGUILayout.BeginScrollView(scrollView, GUILayout.MaxHeight(position.height));
            uiList.ClearSelection();
            uiList.DoLayoutList();
            EditorGUILayout.EndScrollView();

            int spaceInSettings = maxProfileCount - globalSettings.diffusionProfileSettingsList.Length;

            int enabledAmount = s_ProfilesToRegister.Count(x => x);
            if (enabledAmount > spaceInSettings)
                EditorGUILayout.HelpBox("HDRP only allows up to " + maxProfileCount + " custom profiles in the Global Settings. Please unselect at least " + (enabledAmount - spaceInSettings) + ".", MessageType.Error, true);

            using (new EditorGUI.DisabledScope(enabledAmount > spaceInSettings))
            {
                if (GUILayout.Button("Add selected profiles to HDRP Global Settings"))
                {
                    for (int i = 0; i < s_ProfilesToRegister.Count; i++)
                    {
                        if (!s_ProfilesToRegister[i]) continue;
                        globalSettings.AddDiffusionProfile(s_MissingProfiles[i]);
                    }
                    Close();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void OnDestroy()
        {
            uiList = null;
            s_MissingProfiles.Clear();
            s_ProfilesToRegister.Clear();
        }
    }
}
