using UnityEngine;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal.VR;
using UnityEditor.SceneManagement;
using System.Runtime.InteropServices;

namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    class HDWizard : EditorWindow
    {
        const string k_DXRSupport_Token = "REALTIME_RAYTRACING_SUPPORT";

        //reflect internal legacy enum
        enum LightmapEncodingQualityCopy
        {
            Low = 0,
            Normal = 1,
            High = 2
        }

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

        //utility class to show only non scene object selection
        static class ObjectSelector
        {
            static Action<UnityEngine.Object, Type> ShowObjectSelector;
            static Func<UnityEngine.Object> GetCurrentObject;
            static Func<int> GetSelectorID;
            static Action<int> SetSelectorID;

            const string ObjectSelectorUpdatedCommand = "ObjectSelectorUpdated";

            static int id;

            static int selectorID { get => GetSelectorID(); set => SetSelectorID(value); }

            static ObjectSelector()
            {
                Type playerSettingsType = typeof(PlayerSettings);
                Type objectSelectorType = playerSettingsType.Assembly.GetType("UnityEditor.ObjectSelector");
                var instanceObjectSelectorInfo = objectSelectorType.GetProperty("get", BindingFlags.Static | BindingFlags.Public);
                var showInfo = objectSelectorType.GetMethod("Show", new[] { typeof(UnityEngine.Object), typeof(Type), typeof(SerializedProperty), typeof(bool) });
                var objectSelectorVariable = Expression.Variable(objectSelectorType, "objectSelector");
                var objectParameter = Expression.Parameter(typeof(UnityEngine.Object), "unityObject");
                var typeParameter = Expression.Parameter(typeof(Type), "type");
                var showObjectSelectorBlock = Expression.Block(
                    new[] { objectSelectorVariable },
                    Expression.Assign(objectSelectorVariable, Expression.Call(null, instanceObjectSelectorInfo.GetGetMethod())),
                    Expression.Call(objectSelectorVariable, showInfo, objectParameter, typeParameter, Expression.Constant(null, typeof(SerializedProperty)), Expression.Constant(false))
                    );
                var showObjectSelectorLambda = Expression.Lambda<Action<UnityEngine.Object, Type>>(showObjectSelectorBlock, objectParameter, typeParameter);
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

            public static void Show(UnityEngine.Object obj, Type type)
            {
                id = GUIUtility.GetControlID("s_ObjectFieldHash".GetHashCode(), FocusType.Keyboard);
                GUIUtility.keyboardControl = id;
                ShowObjectSelector(obj, type);
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

        static VolumeProfile s_DefaultVolumeProfile;

        Vector2 scrollPos;
        Rect lastVolumeRect;

        VolumeProfile defaultVolumeProfile;

        static Func<BuildTargetGroup, LightmapEncodingQualityCopy> GetLightmapEncodingQualityForPlatformGroup;
        static Action<BuildTargetGroup, LightmapEncodingQualityCopy> SetLightmapEncodingQualityForPlatformGroup;
        static Func<BuildTarget> CalculateSelectedBuildTarget;
        static Func<BuildTarget, GraphicsDeviceType[]> GetSupportedGraphicsAPIs;
        static Func<BuildTarget, bool> WillEditorUseFirstGraphicsAPI;
        static Action RequestCloseAndRelaunchWithCurrentArguments;

        static HDWizard()
        {
            Type playerSettingsType = typeof(PlayerSettings);
            Type playerSettingsEditorType = playerSettingsType.Assembly.GetType("UnityEditor.PlayerSettingsEditor");
            Type lightEncodingQualityType = playerSettingsType.Assembly.GetType("UnityEditor.LightmapEncodingQuality");
            Type editorUserBuildSettingsUtilsType = playerSettingsType.Assembly.GetType("UnityEditor.EditorUserBuildSettingsUtils");
            var qualityVariable = Expression.Variable(lightEncodingQualityType, "quality_internal");
            var buildTargetGroupParameter = Expression.Parameter(typeof(BuildTargetGroup), "platformGroup");
            var buildTargetParameter = Expression.Parameter(typeof(BuildTarget), "platform");
            var qualityParameter = Expression.Parameter(typeof(LightmapEncodingQualityCopy), "quality");
            var getLightmapEncodingQualityForPlatformGroupInfo = playerSettingsType.GetMethod("GetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            var setLightmapEncodingQualityForPlatformGroupInfo = playerSettingsType.GetMethod("SetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            var calculateSelectedBuildTargetInfo = editorUserBuildSettingsUtilsType.GetMethod("CalculateSelectedBuildTarget", BindingFlags.Static | BindingFlags.Public);
            var getSupportedGraphicsAPIsInfo = playerSettingsType.GetMethod("GetSupportedGraphicsAPIs", BindingFlags.Static | BindingFlags.NonPublic);
            var willEditorUseFirstGraphicsAPIInfo = playerSettingsEditorType.GetMethod("WillEditorUseFirstGraphicsAPI", BindingFlags.Static | BindingFlags.NonPublic);
            var requestCloseAndRelaunchWithCurrentArgumentsInfo = typeof(EditorApplication).GetMethod("RequestCloseAndRelaunchWithCurrentArguments", BindingFlags.Static | BindingFlags.NonPublic);
            var getLightmapEncodingQualityForPlatformGroupBlock = Expression.Block(
                new[] { qualityVariable },
                Expression.Assign(qualityVariable, Expression.Call(getLightmapEncodingQualityForPlatformGroupInfo, buildTargetGroupParameter)),
                Expression.Convert(qualityVariable, typeof(LightmapEncodingQualityCopy))
                );
            var setLightmapEncodingQualityForPlatformGroupBlock = Expression.Block(
                new[] { qualityVariable },
                Expression.Assign(qualityVariable, Expression.Convert(qualityParameter, lightEncodingQualityType)),
                Expression.Call(setLightmapEncodingQualityForPlatformGroupInfo, buildTargetGroupParameter, qualityVariable)
                );
            var getLightmapEncodingQualityForPlatformGroupLambda = Expression.Lambda<Func<BuildTargetGroup, LightmapEncodingQualityCopy>>(getLightmapEncodingQualityForPlatformGroupBlock, buildTargetGroupParameter);
            var setLightmapEncodingQualityForPlatformGroupLambda = Expression.Lambda<Action<BuildTargetGroup, LightmapEncodingQualityCopy>>(setLightmapEncodingQualityForPlatformGroupBlock, buildTargetGroupParameter, qualityParameter);
            var calculateSelectedBuildTargetLambda = Expression.Lambda<Func<BuildTarget>>(Expression.Call(null, calculateSelectedBuildTargetInfo));
            var getSupportedGraphicsAPIsLambda = Expression.Lambda<Func<BuildTarget, GraphicsDeviceType[]>>(Expression.Call(null, getSupportedGraphicsAPIsInfo, buildTargetParameter), buildTargetParameter);
            var willEditorUseFirstGraphicsAPILambda = Expression.Lambda<Func<BuildTarget, bool>>(Expression.Call(null, willEditorUseFirstGraphicsAPIInfo, buildTargetParameter), buildTargetParameter);
            var requestCloseAndRelaunchWithCurrentArgumentsLambda = Expression.Lambda<Action>(Expression.Call(null, requestCloseAndRelaunchWithCurrentArgumentsInfo));
            GetLightmapEncodingQualityForPlatformGroup = getLightmapEncodingQualityForPlatformGroupLambda.Compile();
            SetLightmapEncodingQualityForPlatformGroup = setLightmapEncodingQualityForPlatformGroupLambda.Compile();
            CalculateSelectedBuildTarget = calculateSelectedBuildTargetLambda.Compile();
            GetSupportedGraphicsAPIs = getSupportedGraphicsAPIsLambda.Compile();
            WillEditorUseFirstGraphicsAPI = willEditorUseFirstGraphicsAPILambda.Compile();
            RequestCloseAndRelaunchWithCurrentArguments = requestCloseAndRelaunchWithCurrentArgumentsLambda.Compile();
        }

        [MenuItem("Window/Render Pipeline/HD Render Pipeline Wizard", priority = 10000)]
        static void OpenWindow()
        {
            GetWindow<HDWizard>("Render Pipeline Wizard");
        }

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

            // check assignation resolution from Selector
            ObjectSelector.CheckAssignationEvent<GameObject>(x => HDProjectSettings.defaultScenePrefab = x);
            ObjectSelector.CheckAssignationEvent<HDRenderPipelineAsset>(x => GraphicsSettings.renderPipelineAsset = x);
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

        void CreateOrLoad<T>()
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
                    break;
                case 2: //Load
                    ObjectSelector.Show(target, typeof(T));
                    break;
                default:
                    throw new ArgumentException("Unrecognized option");
            }
        }

        void CreateOrLoadDefaultScene()
        {
            switch (EditorUtility.DisplayDialogComplex(Style.scenePrefabTitle, Style.scenePrefabContent, Style.displayDialogCreate, "Cancel", Style.displayDialogLoad))
            {
                case 0: //create
                    CreateDefaultSceneFromPackageAnsAssignIt();
                    break;
                case 1: //cancel
                    break;
                case 2: //Load
                    ObjectSelector.Show(HDProjectSettings.defaultScenePrefab, typeof(GameObject));
                    break;
                default:
                    throw new ArgumentException("Unrecognized option");
            }
        }

        #region DRAWERS

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
            DrawConfigInfoLine(Style.hdrpAssetUsedLabel, Style.hdrpAssetUsedError, Style.ok, Style.resolve, IsHdrpAssetUsedCorrect, FixHdrpAssetUsed);
            DrawConfigInfoLine(Style.hdrpAssetRuntimeResourcesLabel, Style.hdrpAssetRuntimeResourcesError, Style.ok, Style.resolve, IsHdrpAssetRuntimeResourcesCorrect, FixHdrpAssetRuntimeResources);
            DrawConfigInfoLine(Style.hdrpAssetEditorResourcesLabel, Style.hdrpAssetEditorResourcesError, Style.ok, Style.resolve, IsHdrpAssetEditorResourcesCorrect, FixHdrpAssetEditorResources);
            DrawConfigInfoLine(Style.hdrpAssetDiffusionProfileLabel, Style.hdrpAssetDiffusionProfileError, Style.ok, Style.resolve, IsHdrpAssetDiffusionProfileCorrect, FixHdrpAssetDiffusionProfile);
            --EditorGUI.indentLevel;
            DrawConfigInfoLine(Style.defaultVolumeProfileLabel, Style.defaultVolumeProfileError, Style.ok, Style.resolve, IsDefaultSceneCorrect, FixDefaultScene);
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

        #region HDRP_FIXES

        bool IsHDRPAllCorrect() =>
            IsLightmapCorrect()
            && IsShadowmaskCorrect()
            && IsColorSpaceCorrect()
            && IsHdrpAssetCorrect()
            && IsDefaultSceneCorrect();
        void FixHDRPAll() => EditorApplication.update += FixHDRPAllAsync;
        void FixHDRPAllAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being addressed
            if (!IsColorSpaceCorrect())
            {
                FixColorSpace();
                return;
            }
            if (!IsLightmapCorrect())
            {
                FixLightmap();
                return;
            }
            if (!IsShadowmaskCorrect())
            {
                FixShadowmask();
                return;
            }
            if (!IsHdrpAssetCorrect())
            {
                FixHdrpAssetAsync();
                return;
            }
            if (!IsDefaultSceneCorrect())
            {
                FixDefaultScene();
                return;
            }
            EditorApplication.update -= FixHDRPAllAsync;
        }

        bool IsHdrpAssetCorrect() =>
            IsHdrpAssetUsedCorrect()
            && IsHdrpAssetRuntimeResourcesCorrect()
            && IsHdrpAssetEditorResourcesCorrect()
            && IsHdrpAssetDiffusionProfileCorrect();
        void FixHdrpAsset() => EditorApplication.update += FixHdrpAssetAsync;
        void FixHdrpAssetAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being addressed
            if (!IsHdrpAssetUsedCorrect())
            {
                FixHdrpAssetUsed();
                return;
            }
            if (!IsHdrpAssetRuntimeResourcesCorrect())
            {
                FixHdrpAssetRuntimeResources();
                return;
            }
            if (!IsHdrpAssetEditorResourcesCorrect())
            {
                FixHdrpAssetEditorResources();
                return;
            }
            if (!IsHdrpAssetDiffusionProfileCorrect())
            {
                FixHdrpAssetDiffusionProfile();
            }
            EditorApplication.update -= FixHdrpAssetAsync;
        }

        bool IsColorSpaceCorrect() => PlayerSettings.colorSpace == ColorSpace.Linear;
        void FixColorSpace() => PlayerSettings.colorSpace = ColorSpace.Linear;

        bool IsLightmapCorrect()
        {
            // Shame alert: plateform supporting Encodement are partly hardcoded
            // in editor (Standalone) and for the other part, it is all in internal code.
            return GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Standalone) == LightmapEncodingQualityCopy.High
                && GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android) == LightmapEncodingQualityCopy.High
                && GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Lumin) == LightmapEncodingQualityCopy.High
                && GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.WSA) == LightmapEncodingQualityCopy.High;
        }
        void FixLightmap()
        {
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Standalone, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Lumin, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.WSA, LightmapEncodingQualityCopy.High);
        }

        bool IsShadowmaskCorrect()
        {
            //QualitySettings.SetQualityLevel.set quality is too costy to be use at frame
            return QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask;
        }
        void FixShadowmask()
        {
            int currentQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
            }
            QualitySettings.SetQualityLevel(currentQuality, applyExpensiveChanges: false);
        }

        bool IsHdrpAssetUsedCorrect() => GraphicsSettings.renderPipelineAsset != null && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset;
        void FixHdrpAssetUsed() => CreateOrLoad<HDRenderPipelineAsset>();

        bool IsHdrpAssetRuntimeResourcesCorrect() =>
            IsHdrpAssetUsedCorrect()
            && HDRenderPipeline.defaultAsset.renderPipelineResources != null;
        void FixHdrpAssetRuntimeResources()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed();
            HDRenderPipeline.defaultAsset.renderPipelineResources
                = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset");
            ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsHdrpAssetEditorResourcesCorrect() =>
            IsHdrpAssetUsedCorrect()
            && HDRenderPipeline.defaultAsset.renderPipelineEditorResources != null;
        void FixHdrpAssetEditorResources()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed();
            HDRenderPipeline.defaultAsset.renderPipelineEditorResources
                = AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset");
            ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineEditorResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsHdrpAssetDiffusionProfileCorrect()
        {
            var profileList = HDRenderPipeline.defaultAsset?.diffusionProfileSettingsList;
            return IsHdrpAssetUsedCorrect() && profileList.Length != 0 && profileList.Any(p => p != null);
        }

        void FixHdrpAssetDiffusionProfile()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed();

            var hdAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            hdAsset.diffusionProfileSettingsList = hdAsset.renderPipelineEditorResources.defaultDiffusionProfileSettingsList;
        }

        bool IsDefaultSceneCorrect() => HDProjectSettings.defaultScenePrefab != null;
        void FixDefaultScene() => CreateOrLoadDefaultScene();

        #endregion

        #region HDRP_VR_FIXES

        bool IsVRAllCorrect()
            => IsVRSupportedForCurrentBuildTargetGroupCorrect();
        void FixVRAll() => EditorApplication.update += FixVRAllAsync;
        void FixVRAllAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being addressed
            if (!IsVRSupportedForCurrentBuildTargetGroupCorrect())
            {
                FixVRSupportedForCurrentBuildTargetGroup();
                return;
            }
            EditorApplication.update -= FixVRAllAsync;
        }

        bool IsVRSupportedForCurrentBuildTargetGroupCorrect()
            => VREditor.GetVREnabledOnTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        void FixVRSupportedForCurrentBuildTargetGroup()
            => VREditor.SetVREnabledOnTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup, true);

        #endregion

        #region HDRP_DXR_FIXES

        bool IsDXRAllCorrect()
            => IsDXRAutoGraphicsAPICorrect()
            && IsDXRDirect3D12Correct()
            && IsDXRCSharpKeyWordCorrect()
            && IsDXRActivationCorrect()
            && IsDXRAssetCorrect();

        void FixDXRAll() => EditorApplication.update += FixDXRAllAsync;
        void FixDXRAllAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being addressed
            if (!IsDXRAutoGraphicsAPICorrect())
            {
                FixDXRAutoGraphicsAPI();
                return;
            }
            if (!IsDXRDirect3D12Correct())
            {
                FixDXRDirect3D12(fromAsync: true);
                return;
            }
            if (!IsDXRCSharpKeyWordCorrect())
            {
                FixDXRCSharpKeyWord();
                return;
            }
            if (!IsDXRActivationCorrect())
            {
                FixDXRActivation();
                return;
            }
            if (!IsDXRAssetCorrect())
            {
                FixDXRAsset();
                return;
            }
            EditorApplication.update -= FixDXRAllAsync;
        }

        bool IsDXRAutoGraphicsAPICorrect()
            => !PlayerSettings.GetUseDefaultGraphicsAPIs(CalculateSelectedBuildTarget());
        void FixDXRAutoGraphicsAPI()
            => PlayerSettings.SetUseDefaultGraphicsAPIs(CalculateSelectedBuildTarget(), false);

        bool IsDXRDirect3D12Correct()
            => PlayerSettings.GetGraphicsAPIs(CalculateSelectedBuildTarget()).FirstOrDefault() == GraphicsDeviceType.Direct3D12;
        void FixDXRDirect3D12(bool fromAsync)
        {
            if (GetSupportedGraphicsAPIs(CalculateSelectedBuildTarget()).Contains(GraphicsDeviceType.Direct3D12))
            {
                var buidTarget = CalculateSelectedBuildTarget();
                if (PlayerSettings.GetGraphicsAPIs(CalculateSelectedBuildTarget()).Contains(GraphicsDeviceType.Direct3D12))
                {
                    PlayerSettings.SetGraphicsAPIs(
                        CalculateSelectedBuildTarget(),
                        new[] { GraphicsDeviceType.Direct3D12 }
                            .Concat(
                                PlayerSettings.GetGraphicsAPIs(buidTarget)
                                    .Where(x => x != GraphicsDeviceType.Direct3D12))
                            .ToArray());
                }
                else
                {
                    PlayerSettings.SetGraphicsAPIs(
                        CalculateSelectedBuildTarget(),
                        new[] { GraphicsDeviceType.Direct3D12 }
                            .Concat(PlayerSettings.GetGraphicsAPIs(buidTarget))
                            .ToArray());
                }
                if (fromAsync)
                    EditorApplication.update -= FixDXRAllAsync;
                ChangedFirstGraphicAPI(buidTarget);
            }
        }

        void ChangedFirstGraphicAPI(BuildTarget target)
        {
            // If we're changing the first API for relevant editor, this will cause editor to switch: ask for scene save & confirmation
            if (WillEditorUseFirstGraphicsAPI(target))
            {
                if (EditorUtility.DisplayDialog("Changing editor graphics device",
                    "You've changed the active graphics API. This requires a restart of the Editor. After restarting finish fixing DXR configuration by launching the wizard again.",
                    "Restart Editor", "Not now"))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        RequestCloseAndRelaunchWithCurrentArguments();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        bool IsDXRCSharpKeyWordCorrect()
            => PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Contains(k_DXRSupport_Token);
        void FixDXRCSharpKeyWord()
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                targetGroup,
                $"{PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup)};{k_DXRSupport_Token}");
        }

        bool IsDXRAssetCorrect()
            => GraphicsSettings.renderPipelineAsset != null
            && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset
            && HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources != null;
        void FixDXRAsset()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed();
            HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources
                = AssetDatabase.LoadAssetAtPath<HDRenderPipelineRayTracingResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRayTracingResources.asset");
            ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsDXRActivationCorrect()
            => GraphicsSettings.renderPipelineAsset != null
            && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset
            && (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings.supportRayTracing;
        void FixDXRActivation()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed();
            //as property returning struct make copy, use serializedproperty to modify it
            var serializedObject = new SerializedObject(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            var propertySupportRayTracing = serializedObject.FindProperty("m_RenderPipelineSettings.supportRayTracing");
            propertySupportRayTracing.boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}

