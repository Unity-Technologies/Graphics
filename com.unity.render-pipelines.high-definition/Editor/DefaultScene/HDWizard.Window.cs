using UnityEngine;
using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    partial class HDWizard : EditorWindow
    {
        static class Style
        {
            public static readonly GUIContent hdrpProjectSettingsPath = EditorGUIUtility.TrTextContent("Default Resources Folder", "Resources Folder will be the one where to get project elements related to HDRP as default scene and default settings.");
            public static readonly GUIContent firstTimeInit = EditorGUIUtility.TrTextContent("Populate / Reset", "Populate or override Default Resources Folder content with required assets.");
            public static readonly GUIContent defaultScene = EditorGUIUtility.TrTextContent("Default Scene Prefab", "This prefab contains scene elements that are used when creating a new scene in HDRP.");
            public static readonly GUIContent haveStartPopup = EditorGUIUtility.TrTextContent("Show on start");

            //configuration debugger
            public static readonly GUIContent ok = EditorGUIUtility.TrIconContent(EditorGUIUtility.Load(@"Packages/com.unity.render-pipelines.high-definition/Editor/DefaultScene/WizardResources/OK.png") as Texture2D);
            public static readonly GUIContent fail = EditorGUIUtility.TrIconContent(EditorGUIUtility.Load(@"Packages/com.unity.render-pipelines.high-definition/Editor/DefaultScene/WizardResources/Error.png") as Texture2D);
            public static readonly GUIContent resolve = EditorGUIUtility.TrTextContent("Fix");
            public static readonly GUIContent resolveAll = EditorGUIUtility.TrTextContent("Fix All");
            public static readonly GUIContent resolveAllQuality = EditorGUIUtility.TrTextContent("Fix All Qualities");
            public static readonly GUIContent resolveAllBuildTarget = EditorGUIUtility.TrTextContent("Fix All Platforms");
            public static readonly GUIContent hdrpConfigurationLabel = EditorGUIUtility.TrTextContent("HDRP configuration");
            public static readonly GUIContent vrConfigurationLabel = EditorGUIUtility.TrTextContent("VR additional configuration");
            public static readonly GUIContent dxrConfigurationLabel = EditorGUIUtility.TrTextContent("DXR additional configuration");
            public const string allConfigurationError = "There is issue in your configuration. (See below for detail)";
            public static readonly GUIContent colorSpaceLabel = EditorGUIUtility.TrTextContent("Color space");
            public const string colorSpaceError = "Only linear color space supported!";
            public static readonly GUIContent lightmapLabel = EditorGUIUtility.TrTextContent("Lightmap encoding");
            public const string lightmapError = "Only high quality lightmap supported!";
            public static readonly GUIContent shadowLabel = EditorGUIUtility.TrTextContent("Shadows");
            public const string shadowError = "Shadow must be set to activated! (either on hard or soft)";
            public static readonly GUIContent shadowMaskLabel = EditorGUIUtility.TrTextContent("Shadowmask mode");
            public const string shadowMaskError = "Only distance shadowmask supported at the project level! (You can still change this per light.)";
            public static readonly GUIContent scriptingRuntimeVersionLabel = EditorGUIUtility.TrTextContent("Script runtime version");
            public const string scriptingRuntimeVersionError = "Script runtime version must be .Net 4.x or earlier!";
            public static readonly GUIContent hdrpAssetLabel = EditorGUIUtility.TrTextContent("Asset configuration");
            public const string hdrpAssetError = "There are issues in the HDRP asset configuration. (see below)";
            public static readonly GUIContent hdrpAssetUsedLabel = EditorGUIUtility.TrTextContent("Assigned");
            public const string hdrpAssetUsedError = "There is no HDRP asset assigned to the render pipeline!";
            public static readonly GUIContent hdrpAssetRuntimeResourcesLabel = EditorGUIUtility.TrTextContent("Runtime resources");
            public const string hdrpAssetRuntimeResourcesError = "There is an issue with the runtime resources!";
            public static readonly GUIContent hdrpAssetEditorResourcesLabel = EditorGUIUtility.TrTextContent("Editor resources");
            public const string hdrpAssetEditorResourcesError = "There is an issue with the editor resources!";
            public static readonly GUIContent hdrpAssetDiffusionProfileLabel = EditorGUIUtility.TrTextContent("Diffusion profile");
            public const string hdrpAssetDiffusionProfileError = "There is no diffusion profile assigned in the HDRP asset!";
            public static readonly GUIContent defaultVolumeProfileLabel = EditorGUIUtility.TrTextContent("Default scene prefab");
            public const string defaultVolumeProfileError = "Default scene prefab must be set to create HD templated scene!";
            public static readonly GUIContent vrSupportedLabel = EditorGUIUtility.TrTextContent("VR activated");
            public const string vrSupportedError = "VR need to be enabled in Player Settings!";
            public static readonly GUIContent dxrAutoGraphicsAPILabel = EditorGUIUtility.TrTextContent("Auto graphics API");
            public const string dxrAutoGraphicsAPIError = "Auto Graphics API is not supported!";
            public static readonly GUIContent dxrDirect3D12Label = EditorGUIUtility.TrTextContent("Direct3D 12");
            public const string dxrDirect3D12Error = "Direct3D 12 is needed!";
            public static readonly GUIContent dxrSymbolLabel = EditorGUIUtility.TrTextContent("Scripting symbols");
            public const string dxrSymbolError = "REALTIME_RAYTRACING_SUPPORT must be defined!";
            public static readonly GUIContent dxrResourcesLabel = EditorGUIUtility.TrTextContent("DXR resources");
            public const string dxrResourcesError = "There is an issue with the DXR resources!";
            public static readonly GUIContent dxrActivatedLabel = EditorGUIUtility.TrTextContent("DXR activated");
            public const string dxrActivatedError = "DXR is not activated!";
            public const string dxrWindowOnly = "DXR is only available for window on specific version of Unity";

            public const string hdrpAssetDisplayDialogTitle = "Create or Load HDRenderPipelineAsset";
            public const string hdrpAssetDisplayDialogContent = "Do you want to create a fresh HDRenderPipelineAsset in the default resource folder and automatically assign it?";
            public const string diffusionProfileSettingsDisplayDialogTitle = "Create or Load DiffusionProfileSettings";
            public const string diffusionProfileSettingsDisplayDialogContent = "Do you want to create a fresh DiffusionProfileSettings in the default resource folder and automatically assign it?";
            public const string scenePrefabTitle = "Create or Load HD default scene";
            public const string scenePrefabContent = "Do you want to create a fresh HD default scene in the default resource folder and automatically assign it?";
            public const string displayDialogCreate = "Create One";
            public const string displayDialogLoad = "Load One";
        }
        
        [MenuItem("Window/Render Pipeline/HD Render Pipeline Wizard", priority = 10000)]
        static void OpenWindow()
            => GetWindow<HDWizard>("Render Pipeline Wizard");

        #region SCRIPT_RELOADING

        static int frameToWait;

        static void OpenWindowDelayed()
        {
            if (frameToWait > 0)
                --frameToWait;
            else
            {
                EditorApplication.update -= OpenWindowDelayed;

                //Application.isPlaying cannot be called in constructor. Do it here
                if (Application.isPlaying)
                    return;

                OpenWindow();
            }
        }

        [Callbacks.DidReloadScripts]
        static void ResetDelayed()
        {
            //remove it from domain reload but keep it in editor opening
            frameToWait = 0;
            EditorApplication.update -= OpenWindowDelayed;
        }

        #endregion

        #region DRAWERS

        Vector2 scrollPos;

        void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            DrawFolderData();
            DrawDefaultScene();

            EditorGUILayout.Space();
            DrawHDRPConfigInfo();

            EditorGUILayout.Space();
            DrawVRConfigInfo();

            EditorGUILayout.Space();
            DrawDXRConfigInfo();

            GUILayout.EndScrollView();
        }

        void DrawFolderData()
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string changedProjectSettingsFolderPath = EditorGUILayout.DelayedTextField(Style.hdrpProjectSettingsPath, HDProjectSettings.projectSettingsFolderPath);
            if (EditorGUI.EndChangeCheck())
            {
                HDProjectSettings.projectSettingsFolderPath = changedProjectSettingsFolderPath;
            }
            if (GUILayout.Button(Style.firstTimeInit, EditorStyles.miniButton, GUILayout.Width(110), GUILayout.ExpandWidth(false)))
            {
                if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                    AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

                var hdrpAsset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                hdrpAsset.name = "HDRenderPipelineAsset";

                int index = 0;
                hdrpAsset.diffusionProfileSettingsList = new DiffusionProfileSettings[hdrpAsset.renderPipelineEditorResources.defaultDiffusionProfileSettingsList.Length];
                foreach (var defaultProfile in hdrpAsset.renderPipelineEditorResources.defaultDiffusionProfileSettingsList)
                {
                    string defaultDiffusionProfileSettingsPath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + defaultProfile.name + ".asset";
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(defaultProfile), defaultDiffusionProfileSettingsPath);

                    DiffusionProfileSettings defaultDiffusionProfile = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(defaultDiffusionProfileSettingsPath);

                    hdrpAsset.diffusionProfileSettingsList[index++] = defaultDiffusionProfile;
                }

                AssetDatabase.CreateAsset(hdrpAsset, "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + hdrpAsset.name + ".asset");

                GraphicsSettings.renderPipelineAsset = hdrpAsset;
                if (!IsHdrpAssetRuntimeResourcesCorrect())
                    FixHdrpAssetRuntimeResources();
                if (!IsHdrpAssetEditorResourcesCorrect())
                    FixHdrpAssetEditorResources();

                CreateDefaultSceneFromPackageAnsAssignIt();
            }
            GUILayout.EndHorizontal();
        }

        void DrawDefaultScene()
        {
            EditorGUI.BeginChangeCheck();
            GameObject changedDefaultScene = EditorGUILayout.ObjectField(Style.defaultScene, HDProjectSettings.defaultScenePrefab, typeof(GameObject), allowSceneObjects: false) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                //only affect on change as it will write the guid on disk
                HDProjectSettings.defaultScenePrefab = changedDefaultScene;
            }
        }

        void DrawWizardBehaviour()
        {
            EditorGUI.BeginChangeCheck();
            bool changedHasStatPopup = EditorGUILayout.Toggle(Style.haveStartPopup, HDProjectSettings.hasStartPopup);
            if (EditorGUI.EndChangeCheck())
                HDProjectSettings.hasStartPopup = changedHasStatPopup;
        }

        void DrawHDRPConfigInfo()
        {
            DrawTitleConfigInfo(Style.hdrpConfigurationLabel, Style.resolveAll, IsHDRPAllCorrect, FixHDRPAll);

            ++EditorGUI.indentLevel;
            DrawConfigInfoLine(Style.colorSpaceLabel, Style.colorSpaceError, Style.ok, Style.resolve, IsColorSpaceCorrect, FixColorSpace);
            DrawConfigInfoLine(Style.lightmapLabel, Style.lightmapError, Style.ok, Style.resolveAllBuildTarget, IsLightmapCorrect, FixLightmap);
            DrawConfigInfoLine(Style.shadowMaskLabel, Style.shadowMaskError, Style.ok, Style.resolveAllQuality, IsShadowmaskCorrect, FixShadowmask);
            DrawConfigInfoLine(Style.hdrpAssetLabel, Style.hdrpAssetError, Style.ok, Style.resolveAll, IsHdrpAssetCorrect, FixHdrpAsset);
            ++EditorGUI.indentLevel;
            DrawConfigInfoLine(Style.hdrpAssetUsedLabel, Style.hdrpAssetUsedError, Style.ok, Style.resolve, IsHdrpAssetUsedCorrect, () => FixHdrpAssetUsed(async: false));
            DrawConfigInfoLine(Style.hdrpAssetRuntimeResourcesLabel, Style.hdrpAssetRuntimeResourcesError, Style.ok, Style.resolve, IsHdrpAssetRuntimeResourcesCorrect, FixHdrpAssetRuntimeResources);
            DrawConfigInfoLine(Style.hdrpAssetEditorResourcesLabel, Style.hdrpAssetEditorResourcesError, Style.ok, Style.resolve, IsHdrpAssetEditorResourcesCorrect, FixHdrpAssetEditorResources);
            DrawConfigInfoLine(Style.hdrpAssetDiffusionProfileLabel, Style.hdrpAssetDiffusionProfileError, Style.ok, Style.resolve, IsHdrpAssetDiffusionProfileCorrect, FixHdrpAssetDiffusionProfile);
            --EditorGUI.indentLevel;
            DrawConfigInfoLine(Style.defaultVolumeProfileLabel, Style.defaultVolumeProfileError, Style.ok, Style.resolve, IsDefaultSceneCorrect, () => FixDefaultScene(async: false));
            --EditorGUI.indentLevel;
        }

        void DrawVRConfigInfo()
        {
            DrawTitleConfigInfo(Style.vrConfigurationLabel, Style.resolveAll, IsVRAllCorrect, FixVRAll);

            ++EditorGUI.indentLevel;
            DrawConfigInfoLine(Style.vrSupportedLabel, Style.vrSupportedError, Style.ok, Style.resolve, IsVRSupportedForCurrentBuildTargetGroupCorrect, FixVRSupportedForCurrentBuildTargetGroup);
            --EditorGUI.indentLevel;
        }
        void DrawDXRConfigInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                DrawDXRConfigInfoWindow();
            else
                DrawDXRConfigInfoNotWindow();
        }

        void DrawDXRConfigInfoNotWindow()
        {
            EditorGUILayout.LabelField(Style.dxrConfigurationLabel, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Style.dxrWindowOnly, MessageType.Info);
        }

        void DrawDXRConfigInfoWindow()
        {
            DrawTitleConfigInfo(Style.dxrConfigurationLabel, Style.resolveAll, IsDXRAllCorrect, FixDXRAll);

            ++EditorGUI.indentLevel;
            DrawConfigInfoLine(Style.dxrAutoGraphicsAPILabel, Style.dxrAutoGraphicsAPIError, Style.ok, Style.resolve, IsDXRAutoGraphicsAPICorrect, FixDXRAutoGraphicsAPI);
            DrawConfigInfoLine(Style.dxrDirect3D12Label, Style.dxrDirect3D12Error, Style.ok, Style.resolve, IsDXRDirect3D12Correct, () => FixDXRDirect3D12(fromAsync: false));
            DrawConfigInfoLine(Style.dxrSymbolLabel, Style.dxrSymbolError, Style.ok, Style.resolve, IsDXRCSharpKeyWordCorrect, FixDXRCSharpKeyWord);
            DrawConfigInfoLine(Style.dxrActivatedLabel, Style.dxrActivatedError, Style.ok, Style.resolve, IsDXRActivationCorrect, FixDXRActivation);
            DrawConfigInfoLine(Style.dxrResourcesLabel, Style.dxrResourcesError, Style.ok, Style.resolve, IsDXRAssetCorrect, FixDXRAsset);
            --EditorGUI.indentLevel;
        }

        void DrawTitleConfigInfo(GUIContent title, GUIContent buttonName, Func<bool> checker, Action solver)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (!checker() && GUILayout.Button(buttonName, EditorStyles.miniButton, GUILayout.Width(110), GUILayout.ExpandWidth(false)))
                solver();
            EditorGUILayout.EndHorizontal();
        }

        void DrawConfigInfoLine(GUIContent label, string error, GUIContent ok, GUIContent resolverButtonLabel, Func<bool> tester, Action resolver, GUIContent AdditionalCheckButtonLabel = null, Func<bool> additionalTester = null)
        {
            bool wellConfigured = tester();
            EditorGUILayout.LabelField(label, wellConfigured ? Style.ok : Style.fail);
            if (wellConfigured)
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(error, MessageType.Error);
            EditorGUILayout.BeginVertical(GUILayout.Width(114), GUILayout.ExpandWidth(false));
            EditorGUILayout.Space();
            if (GUILayout.Button(resolverButtonLabel, EditorStyles.miniButton))
                resolver();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        #endregion
        
        #region OBJECT_SELECTOR

        //utility class to show only non scene object selection
        static class ObjectSelector
        {
            static Action<UnityEngine.Object, Type, Action<UnityEngine.Object>> ShowObjectSelector;
            static Func<UnityEngine.Object> GetCurrentObject;
            static Func<int> GetSelectorID;
            static Action<int> SetSelectorID;

            const string ObjectSelectorUpdatedCommand = "ObjectSelectorUpdated";

            static int id;

            static int selectorID { get => GetSelectorID(); set => SetSelectorID(value); }

            public static bool opened
                => Resources.FindObjectsOfTypeAll(typeof(PlayerSettings).Assembly.GetType("UnityEditor.ObjectSelector")).Length > 0;

            static ObjectSelector()
            {
                Type playerSettingsType = typeof(PlayerSettings);
                Type objectSelectorType = playerSettingsType.Assembly.GetType("UnityEditor.ObjectSelector");
                var instanceObjectSelectorInfo = objectSelectorType.GetProperty("get", BindingFlags.Static | BindingFlags.Public);
                var showInfo = objectSelectorType.GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(UnityEngine.Object), typeof(Type), typeof(SerializedProperty), typeof(bool), typeof(List<int>), typeof(Action<UnityEngine.Object>), typeof(Action<UnityEngine.Object>) }, null);
                var objectSelectorVariable = Expression.Variable(objectSelectorType, "objectSelector");
                var objectParameter = Expression.Parameter(typeof(UnityEngine.Object), "unityObject");
                var typeParameter = Expression.Parameter(typeof(Type), "type");
                var onClosedParameter = Expression.Parameter(typeof(Action<UnityEngine.Object>), "onClosed");
                var onChangedObjectParameter = Expression.Parameter(typeof(Action<UnityEngine.Object>), "onChangedObject");
                var showObjectSelectorBlock = Expression.Block(
                    new[] { objectSelectorVariable },
                    Expression.Assign(objectSelectorVariable, Expression.Call(null, instanceObjectSelectorInfo.GetGetMethod())),
                    Expression.Call(objectSelectorVariable, showInfo, objectParameter, typeParameter, Expression.Constant(null, typeof(SerializedProperty)), Expression.Constant(false), Expression.Constant(null, typeof(List<int>)), Expression.Constant(null, typeof(Action<UnityEngine.Object>)), onChangedObjectParameter)
                    );
                var showObjectSelectorLambda = Expression.Lambda<Action<UnityEngine.Object, Type, Action<UnityEngine.Object>>>(showObjectSelectorBlock, objectParameter, typeParameter, onChangedObjectParameter);
                ShowObjectSelector = showObjectSelectorLambda.Compile();

                var instanceCall = Expression.Call(null, instanceObjectSelectorInfo.GetGetMethod());
                var objectSelectorIDField = Expression.Field(instanceCall, "objectSelectorID");
                var getSelectorIDLambda = Expression.Lambda<Func<int>>(objectSelectorIDField);
                GetSelectorID = getSelectorIDLambda.Compile();

                var inSelectorIDParam = Expression.Parameter(typeof(int), "value");
                var setSelectorIDLambda = Expression.Lambda<Action<int>>(Expression.Assign(objectSelectorIDField, inSelectorIDParam), inSelectorIDParam);
                SetSelectorID = setSelectorIDLambda.Compile();

                var getCurrentObjectInfo = objectSelectorType.GetMethod("GetCurrentObject");
                var getCurrentObjectLambda = Expression.Lambda<Func<UnityEngine.Object>>(Expression.Call(null, getCurrentObjectInfo));
                GetCurrentObject = getCurrentObjectLambda.Compile();
            }

            public static void Show(UnityEngine.Object obj, Type type, Action<UnityEngine.Object> onChangedObject)
            {
                id = GUIUtility.GetControlID("s_ObjectFieldHash".GetHashCode(), FocusType.Keyboard);
                GUIUtility.keyboardControl = id;
                ShowObjectSelector(obj, type, onChangedObject);
                selectorID = id;
            }

            public static void CheckAssignationEvent<T>(Action<T> assignator)
                where T : UnityEngine.Object
            {
                Event evt = Event.current;
                if (evt.type != EventType.ExecuteCommand)
                    return;
                string commandName = evt.commandName;
                if (commandName != ObjectSelectorUpdatedCommand || selectorID != id)
                    return;
                T current = GetCurrentObject() as T;
                if (current == null)
                    return;
                assignator(current);
                GUI.changed = true;
                evt.Use();
            }
        }
        
        void CreateDefaultSceneFromPackageAnsAssignIt()
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

            var hdrpAssetEditorResources = HDRenderPipeline.defaultAsset.renderPipelineEditorResources;

            string defaultScenePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + hdrpAssetEditorResources.defaultScene.name + ".prefab";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(hdrpAssetEditorResources.defaultScene), defaultScenePath);
            string defaultSkyAndFogProfilePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + hdrpAssetEditorResources.defaultSkyAndFogProfile.name + ".asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(hdrpAssetEditorResources.defaultSkyAndFogProfile), defaultSkyAndFogProfilePath);
            string defaultPostProcessingProfilePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + hdrpAssetEditorResources.defaultPostProcessingProfile.name + ".asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(hdrpAssetEditorResources.defaultPostProcessingProfile), defaultPostProcessingProfilePath);

            GameObject defaultScene = AssetDatabase.LoadAssetAtPath<GameObject>(defaultScenePath);
            VolumeProfile defaultSkyAndFogProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultSkyAndFogProfilePath);
            VolumeProfile defaultPostProcessingProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultPostProcessingProfilePath);

            foreach (var volume in defaultScene.GetComponentsInChildren<Volume>())
            {
                if (volume.sharedProfile.name.StartsWith(hdrpAssetEditorResources.defaultSkyAndFogProfile.name))
                    volume.sharedProfile = defaultSkyAndFogProfile;
                else if (volume.sharedProfile.name.StartsWith(hdrpAssetEditorResources.defaultPostProcessingProfile.name))
                    volume.sharedProfile = defaultPostProcessingProfile;
            }

            HDProjectSettings.defaultScenePrefab = defaultScene;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void CreateOrLoad<T>(Action onCancel, Action<T> onObjectChanged)
            where T : ScriptableObject
        {
            string title;
            string content;
            UnityEngine.Object target;
            if (typeof(T) == typeof(HDRenderPipelineAsset))
            {
                title = Style.hdrpAssetDisplayDialogTitle;
                content = Style.hdrpAssetDisplayDialogContent;
                target = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            }
            else
                throw new ArgumentException("Unknown type used");

            switch (EditorUtility.DisplayDialogComplex(title, content, Style.displayDialogCreate, "Cancel", Style.displayDialogLoad))
            {
                case 0: //create
                    if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                        AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);
                    var asset = ScriptableObject.CreateInstance<T>();
                    asset.name = typeof(T).Name;
                    AssetDatabase.CreateAsset(asset, "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + asset.name + ".asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    if (typeof(T) == typeof(HDRenderPipelineAsset))
                        GraphicsSettings.renderPipelineAsset = asset as HDRenderPipelineAsset;
                    break;
                case 1: //cancel
                    onCancel?.Invoke();
                    break;
                case 2: //Load
                    ObjectSelector.Show(target, typeof(T), o => onObjectChanged?.Invoke((T)o));
                    break;
                default:
                    throw new ArgumentException("Unrecognized option");
            }
        }

        void CreateOrLoadDefaultScene(Action onCancel, Action<GameObject> onObjectChanged)
        {
            switch (EditorUtility.DisplayDialogComplex(Style.scenePrefabTitle, Style.scenePrefabContent, Style.displayDialogCreate, "Cancel", Style.displayDialogLoad))
            {
                case 0: //create
                    CreateDefaultSceneFromPackageAnsAssignIt();
                    break;
                case 1: //cancel
                    onCancel?.Invoke();
                    break;
                case 2: //Load
                    ObjectSelector.Show(HDProjectSettings.defaultScenePrefab, typeof(GameObject), o => onObjectChanged?.Invoke((GameObject)o));
                    break;
                default:
                    throw new ArgumentException("Unrecognized option");
            }
        }

        #endregion
    }
}

