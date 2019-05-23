using UnityEngine;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [InitializeOnLoad]
    public class HDWizard : EditorWindow
    {
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
            public static readonly GUIContent allConfigurationLabel = EditorGUIUtility.TrTextContent("HDRP configuration");
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

        static HDWizard()
        {
            Type playerSettingsType = typeof(PlayerSettings);
            Type LightEncodingQualityType = playerSettingsType.Assembly.GetType("UnityEditor.LightmapEncodingQuality");
            var qualityVariable = Expression.Variable(LightEncodingQualityType, "quality_internal");
            var buildTargetGroupParameter = Expression.Parameter(typeof(BuildTargetGroup), "platformGroup");
            var qualityParameter = Expression.Parameter(typeof(LightmapEncodingQualityCopy), "quality");
            var getLightmapEncodingQualityForPlatformGroupInfo = playerSettingsType.GetMethod("GetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            var setLightmapEncodingQualityForPlatformGroupInfo = playerSettingsType.GetMethod("SetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            var getLightmapEncodingQualityForPlatformGroupBlock = Expression.Block(
                new[] { qualityVariable },
                Expression.Assign(qualityVariable, Expression.Call(getLightmapEncodingQualityForPlatformGroupInfo, buildTargetGroupParameter)),
                Expression.Convert(qualityVariable, typeof(LightmapEncodingQualityCopy))
                );
            var setLightmapEncodingQualityForPlatformGroupBlock = Expression.Block(
                new[] { qualityVariable },
                Expression.Assign(qualityVariable, Expression.Convert(qualityParameter, LightEncodingQualityType)),
                Expression.Call(setLightmapEncodingQualityForPlatformGroupInfo, buildTargetGroupParameter, qualityVariable)
                );
            var getLightmapEncodingQualityForPlatformGroupLambda = Expression.Lambda<Func<BuildTargetGroup, LightmapEncodingQualityCopy>>(getLightmapEncodingQualityForPlatformGroupBlock, buildTargetGroupParameter);
            var setLightmapEncodingQualityForPlatformGroupLambda = Expression.Lambda<Action<BuildTargetGroup, LightmapEncodingQualityCopy>>(setLightmapEncodingQualityForPlatformGroupBlock, buildTargetGroupParameter, qualityParameter);
            GetLightmapEncodingQualityForPlatformGroup = getLightmapEncodingQualityForPlatformGroupLambda.Compile();
            SetLightmapEncodingQualityForPlatformGroup = setLightmapEncodingQualityForPlatformGroupLambda.Compile();

            WizardBehaviour();
        }

        [MenuItem("Window/Analysis/Render Pipeline Wizard", priority = 113)]
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

        static void WizardBehaviour()
        {
            if (HDProjectSettings.hasStartPopup)
            {
                //We need to wait at least one frame or the popup will not show up
                frameToWait = 10;
                EditorApplication.update += OpenWindowDelayed;
            }
        }

        void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string changedProjectSettingsFolderPath = EditorGUILayout.DelayedTextField(Style.hdrpProjectSettingsPath, HDProjectSettings.projectSettingsFolderPath);
            if (EditorGUI.EndChangeCheck())
            {
                HDProjectSettings.projectSettingsFolderPath = changedProjectSettingsFolderPath;
            }
            if (GUILayout.Button(Style.firstTimeInit, EditorStyles.miniButton, GUILayout.Width(100), GUILayout.ExpandWidth(false)))
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

            EditorGUI.BeginChangeCheck();
            GameObject changedDefaultScene = EditorGUILayout.ObjectField(Style.defaultScene, HDProjectSettings.defaultScenePrefab, typeof(GameObject), allowSceneObjects: false) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                //only affect on change as it will write the guid on disk
                HDProjectSettings.defaultScenePrefab = changedDefaultScene;
            }

            EditorGUILayout.Space();
            DrawConfigInfo();

            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            bool changedHasStatPopup = EditorGUILayout.Toggle(Style.haveStartPopup, HDProjectSettings.hasStartPopup);
            if (EditorGUI.EndChangeCheck())
            {
                HDProjectSettings.hasStartPopup = changedHasStatPopup;
            }

            GUILayout.EndScrollView();

            // check assignation resolution from Selector
            ObjectSelector.CheckAssignationEvent<GameObject>(x => HDProjectSettings.defaultScenePrefab = x);
            ObjectSelector.CheckAssignationEvent<HDRenderPipelineAsset>(x => GraphicsSettings.renderPipelineAsset = x);
        }

        void CreateDefaultSceneFromPackageAnsAssignIt()
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

            var hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;

            string defaultScenePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + hdrpAsset.renderPipelineEditorResources.defaultScene.name + ".prefab";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(hdrpAsset.renderPipelineEditorResources.defaultScene), defaultScenePath);
            string defaultRenderSettingsProfilePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + hdrpAsset.renderPipelineEditorResources.defaultRenderSettingsProfile.name + ".asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(hdrpAsset.renderPipelineEditorResources.defaultRenderSettingsProfile), defaultRenderSettingsProfilePath);
            string defaultPostProcessingProfilePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + hdrpAsset.renderPipelineEditorResources.defaultPostProcessingProfile.name + ".asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(hdrpAsset.renderPipelineEditorResources.defaultPostProcessingProfile), defaultPostProcessingProfilePath);

            GameObject defaultScene = AssetDatabase.LoadAssetAtPath<GameObject>(defaultScenePath);
            VolumeProfile defaultRenderSettingsProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultRenderSettingsProfilePath);
            VolumeProfile defaultPostProcessingProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultPostProcessingProfilePath);

            foreach (var volume in defaultScene.GetComponentsInChildren<Volume>())
            {
                if (volume.sharedProfile.name.StartsWith(hdrpAsset.renderPipelineEditorResources.defaultRenderSettingsProfile.name))
                    volume.sharedProfile = defaultRenderSettingsProfile;
                else if (volume.sharedProfile.name.StartsWith(hdrpAsset.renderPipelineEditorResources.defaultPostProcessingProfile.name))
                    volume.sharedProfile = defaultPostProcessingProfile;
            }

            HDProjectSettings.defaultScenePrefab = defaultScene;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void DrawConfigInfo()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Style.allConfigurationLabel, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (!IsAllCorrect() && GUILayout.Button(Style.resolveAll, EditorStyles.miniButton, GUILayout.Width(100), GUILayout.ExpandWidth(false)))
                FixAll();
            EditorGUILayout.EndHorizontal();

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

        void DrawConfigInfoLine(GUIContent label, string error, GUIContent ok, GUIContent resolverButtonLabel, Func<bool> tester, Action resolver, GUIContent AdditionalCheckButtonLabel = null, Func<bool> additionalTester = null)
        {
            bool wellConfigured = tester();
            EditorGUILayout.LabelField(label, wellConfigured ? Style.ok : Style.fail);
            if (wellConfigured)
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(error, MessageType.Error);
            EditorGUILayout.BeginVertical(GUILayout.Width(108), GUILayout.ExpandWidth(false));
            EditorGUILayout.Space();
            if (GUILayout.Button(resolverButtonLabel, EditorStyles.miniButton))
                resolver();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
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

        bool IsAllCorrect() =>
            IsLightmapCorrect()
            && IsShadowmaskCorrect()
            && IsColorSpaceCorrect()
            && IsHdrpAssetCorrect()
            && IsDefaultSceneCorrect();
        void FixAll() => EditorApplication.update += FixAllAsync;
        void FixAllAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being adressed
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
            EditorApplication.update -= FixAllAsync;
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
            //one is currently being adressed
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
            && (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineResources != null;
        void FixHdrpAssetRuntimeResources()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed();
            (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineResources
                = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset");
            ResourceReloader.ReloadAllNullIn((GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsHdrpAssetEditorResourcesCorrect() =>
            IsHdrpAssetUsedCorrect()
            && (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineEditorResources != null;
        void FixHdrpAssetEditorResources()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed();
            (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineEditorResources
                = AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset");
            ResourceReloader.ReloadAllNullIn((GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineEditorResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsHdrpAssetDiffusionProfileCorrect()
        {
            var profileList = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset)?.diffusionProfileSettingsList;
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
    }
}

