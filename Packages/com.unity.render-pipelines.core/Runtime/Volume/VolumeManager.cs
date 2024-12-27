using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// The <see cref="VolumeManager"/> is a global manager responsible for tracking and managing all Volume settings across the currently loaded Scenes.
    /// It handles interpolation and blending of Volume settings at runtime, ensuring smooth transitions between different states of the volumes.
    /// This includes managing post-processing effects, lighting settings, and other environmental effects based on volume profiles.
    /// </summary>
    /// <remarks>
    ///
    /// The <see cref="VolumeManager"/> is initialized with two VolumeProfiles:
    /// 1. The default VolumeProfile defined in the Graphics Settings.
    /// 2. The default VolumeProfile defined in the Quality Settings.
    ///
    /// These profiles represent the baseline state of all volume parameters, and the VolumeManager interpolates values between them to create a final blended VolumeStack for each camera.
    /// Every frame, the VolumeManager updates the parameters of local and global VolumeComponents attached to game objects and ensures that the correct interpolation is applied across different volumes.
    /// The VolumeManager acts as the central "point of truth" for all VolumeComponent parameters in a scene.
    /// It provides default states for volume settings and dynamically interpolates between different VolumeProfiles to manage changes during runtime. This system is used to handle dynamic environmental and post-processing effects in Unity, ensuring smooth transitions across scene changes and camera views.
    ///
    /// The VolumeManager integrates seamlessly with Unity's Scriptable Render Pipelines, applying the appropriate settings for rendering effects such as lighting, bloom, exposure, etc., in real time.
    /// </remarks>
    /// <seealso cref="Volume"/>
    /// <seealso cref="VolumeProfile"/>
    /// <seealso cref="VolumeStack"/>
    /// <seealso cref="VolumeComponent"/>
    /// <seealso cref="VolumeParameter"/>
    /// <seealso cref="QualitySettings"/>
    /// <seealso cref="GraphicsSettings"/>
    public sealed partial class VolumeManager
    {
        static readonly ProfilerMarker k_ProfilerMarkerUpdate = new ("VolumeManager.Update");
        static readonly ProfilerMarker k_ProfilerMarkerReplaceData = new ("VolumeManager.ReplaceData");
        static readonly ProfilerMarker k_ProfilerMarkerEvaluateVolumeDefaultState = new ("VolumeManager.EvaluateVolumeDefaultState");

        static readonly Lazy<VolumeManager> s_Instance = new Lazy<VolumeManager>(() => new VolumeManager());

        /// <summary>
        /// The current singleton instance of <see cref="VolumeManager"/>.
        /// </summary>
        public static VolumeManager instance => s_Instance.Value;

        /// <summary>
        /// A reference to the main <see cref="VolumeStack"/>.
        /// </summary>
        /// <seealso cref="VolumeStack"/>
        public VolumeStack stack { get; set; }

        /// <summary>
        /// The current list of all available types that derive from <see cref="VolumeComponent"/>.
        /// </summary>
        [Obsolete("Please use baseComponentTypeArray instead.")]
        public IEnumerable<Type> baseComponentTypes => baseComponentTypeArray;

        static readonly Dictionary<Type, List<(string, Type)>> s_SupportedVolumeComponentsForRenderPipeline = new();

        internal List<(string, Type)> GetVolumeComponentsForDisplay(Type currentPipelineAssetType)
        {
            if (currentPipelineAssetType == null)
                return new List<(string, Type)>();

            if (!currentPipelineAssetType.IsSubclassOf(typeof(RenderPipelineAsset)))
                throw new ArgumentException(nameof(currentPipelineAssetType));

            if (s_SupportedVolumeComponentsForRenderPipeline.TryGetValue(currentPipelineAssetType, out var supportedVolumeComponents))
                return supportedVolumeComponents;

            if (baseComponentTypeArray == null)
                LoadBaseTypes(currentPipelineAssetType);

            supportedVolumeComponents = BuildVolumeComponentDisplayList(baseComponentTypeArray);
            s_SupportedVolumeComponentsForRenderPipeline[currentPipelineAssetType] = supportedVolumeComponents;

            return supportedVolumeComponents;
        }

        List<(string, Type)> BuildVolumeComponentDisplayList(Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));

            var volumes = new List<(string, Type)>();
            foreach (var t in types)
            {
                string path = string.Empty;
                bool skipComponent = false;

                // Look for the attributes of this volume component and decide how is added and if it needs to be skipped
                var attrs = t.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    switch (attr)
                    {
                        case VolumeComponentMenu attrMenu:
                        {
                            path = attrMenu.menu;
                            break;
                        }
                        case HideInInspector:
                        case ObsoleteAttribute:
                            skipComponent = true;
                            break;
                    }
                }

                if (skipComponent)
                    continue;

                // If no attribute or in case something went wrong when grabbing it, fallback to a
                // beautified class name
                if (string.IsNullOrEmpty(path))
                {
#if UNITY_EDITOR
                    path = ObjectNames.NicifyVariableName(t.Name);
#else
                    path = t.Name;
#endif
                }


                volumes.Add((path, t));
            }

            return volumes
                .OrderBy(i => i.Item1)
                .ToList();
        }

        /// <summary>
        /// The current list of all available types that derive from <see cref="VolumeComponent"/>.
        /// </summary>
        public Type[] baseComponentTypeArray { get; internal set; } // internal only for tests

        /// <summary>
        /// Global default profile that provides default values for volume components. VolumeManager applies
        /// this profile to its internal component default state first, before <see cref="qualityDefaultProfile"/>
        /// and <see cref="customDefaultProfiles"/>.
        /// </summary>
        public VolumeProfile globalDefaultProfile { get; private set; }

        /// <summary>
        /// Quality level specific volume profile that is applied to the default state after
        /// <see cref="globalDefaultProfile"/> and before <see cref="customDefaultProfiles"/>.
        /// </summary>
        public VolumeProfile qualityDefaultProfile { get; private set; }

        /// <summary>
        /// Collection of additional default profiles that can be used to override default values for volume components
        /// in a way that doesn't cause any overhead at runtime. Unity applies these Volume Profiles to its internal
        /// component default state after <see cref="globalDefaultProfile"/> and <see cref="qualityDefaultProfile"/>.
        /// The custom profiles are applied in the order that they appear in the collection.
        /// </summary>
        public ReadOnlyCollection<VolumeProfile> customDefaultProfiles { get; private set; }

        private readonly VolumeCollection m_VolumeCollection = new VolumeCollection();

        // Internal list of default state for each component type - this is used to reset component
        // states on update instead of having to implement a Reset method on all components (which
        // would be error-prone)
        // The "Default State" is evaluated as follows:
        //   Default-constructed VolumeComponents (VolumeParameter values coming from code)
        // + Values from globalDefaultProfile
        // + Values from qualityDefaultProfile
        // + Values from customDefaultProfiles
        // = Default State.
        VolumeComponent[] m_ComponentsDefaultState;

        // Flat list of every volume parameter in default state for faster per-frame stack reset.
        internal VolumeParameter[] m_ParametersDefaultState;

        /// <summary>
        /// Retrieve the default state for a given VolumeComponent type. Default state is defined as
        /// "default-constructed VolumeComponent + Default Profiles evaluated in order".
        /// </summary>
        /// <remarks>
        /// If you want just the VolumeComponent with default-constructed values without overrides from
        /// Default Profiles, use <see cref="ScriptableObject.CreateInstance(Type)"/>.
        /// </remarks>
        /// <param name="volumeComponentType">Type of VolumeComponent</param>
        /// <returns>VolumeComponent in default state, or null if the type is not found</returns>
        public VolumeComponent GetVolumeComponentDefaultState(Type volumeComponentType)
        {
            if (!typeof(VolumeComponent).IsAssignableFrom(volumeComponentType))
                return null;

            foreach (VolumeComponent component in m_ComponentsDefaultState)
            {
                if (component.GetType() == volumeComponentType)
                    return component;
            }

            return null;
        }

        // Recycled list used for volume traversal
        readonly List<Collider> m_TempColliders = new(8);

        // The default stack the volume manager uses.
        // We cache this as users able to change the stack through code and
        // we want to be able to switch to the default one through the ResetMainStack() function.
        VolumeStack m_DefaultStack;

        // List of stacks created through VolumeManager.
        readonly List<VolumeStack> m_CreatedVolumeStacks = new();

 		// Internal for tests
        internal VolumeManager()
        {
        }

        // Note: The "isInitialized" state and explicit Initialize/Deinitialize are only required because VolumeManger
        // is a singleton whose lifetime exceeds that of RenderPipelines. Thus it must be initialized & deinitialized
        // explicitly by the RP to handle pipeline switch gracefully. It would be better to get rid of singletons and
        // have the RP own the class instance instead.
        /// <summary>
        /// Returns whether <see cref="VolumeManager.Initialize(VolumeProfile,VolumeProfile)"/> has been called, and the
        /// class is in valid state. It is not valid to use VolumeManager before this returns true.
        /// </summary>
        public bool isInitialized { get; private set; }

        /// <summary>
        /// Initialize VolumeManager with specified global and quality default volume profiles that are used to evaluate
        /// the default state of all VolumeComponents. Should be called from <see cref="RenderPipeline"/> constructor.
        /// </summary>
        /// <param name="globalDefaultVolumeProfile">Global default volume profile.</param>
        /// <param name="qualityDefaultVolumeProfile">Quality default volume profile.</param>
        public void Initialize(VolumeProfile globalDefaultVolumeProfile = null, VolumeProfile qualityDefaultVolumeProfile = null)
        {
            Debug.Assert(!isInitialized);
            Debug.Assert(m_CreatedVolumeStacks.Count == 0);

            LoadBaseTypes(GraphicsSettings.currentRenderPipelineAssetType);
            InitializeVolumeComponents();

            globalDefaultProfile = globalDefaultVolumeProfile;
            qualityDefaultProfile = qualityDefaultVolumeProfile;
            EvaluateVolumeDefaultState();

            m_DefaultStack = CreateStack();
            stack = m_DefaultStack;

            isInitialized = true;
        }

        /// <summary>
        /// Deinitialize VolumeManager. Should be called from <see cref="RenderPipeline.Dispose()"/>.
        /// </summary>
        public void Deinitialize()
        {
            Debug.Assert(isInitialized);
            DestroyStack(m_DefaultStack);
            m_DefaultStack = null;
            foreach (var s in m_CreatedVolumeStacks)
                s.Dispose();
            m_CreatedVolumeStacks.Clear();
            baseComponentTypeArray = null;
            globalDefaultProfile = null;
            qualityDefaultProfile = null;
            customDefaultProfiles = null;
            isInitialized = false;
        }

        /// <summary>
        /// Assign the given VolumeProfile as the global default profile and update the default component state.
        /// </summary>
        /// <param name="profile">The VolumeProfile to use as the global default profile.</param>
        public void SetGlobalDefaultProfile(VolumeProfile profile)
        {
            globalDefaultProfile = profile;
            EvaluateVolumeDefaultState();
        }

        /// <summary>
        /// Assign the given VolumeProfile as the quality default profile and update the default component state.
        /// </summary>
        /// <param name="profile">The VolumeProfile to use as the quality level default profile.</param>
        public void SetQualityDefaultProfile(VolumeProfile profile)
        {
            qualityDefaultProfile = profile;
            EvaluateVolumeDefaultState();
        }

        /// <summary>
        /// Assign the given VolumeProfiles as custom default profiles and update the default component state.
        /// </summary>
        /// <param name="profiles">List of VolumeProfiles to set as default profiles, or null to clear them.</param>
        public void SetCustomDefaultProfiles(List<VolumeProfile> profiles)
        {
            var validProfiles = profiles ?? new List<VolumeProfile>();
            validProfiles.RemoveAll(x => x == null);
            customDefaultProfiles = new ReadOnlyCollection<VolumeProfile>(validProfiles);
            EvaluateVolumeDefaultState();
        }

        /// <summary>
        /// Call when a VolumeProfile is modified to trigger default state update if necessary.
        /// </summary>
        /// <param name="profile">VolumeProfile that has changed.</param>
        public void OnVolumeProfileChanged(VolumeProfile profile)
        {
            if (!isInitialized)
                return;

            if (globalDefaultProfile == profile ||
                qualityDefaultProfile == profile ||
                (customDefaultProfiles != null && customDefaultProfiles.Contains(profile)))
                EvaluateVolumeDefaultState();
        }

        /// <summary>
        /// Call when a VolumeComponent is modified to trigger default state update if necessary.
        /// </summary>
        /// <param name="component">VolumeComponent that has changed.</param>
        public void OnVolumeComponentChanged(VolumeComponent component)
        {
            var defaultProfiles = new List<VolumeProfile> { globalDefaultProfile, globalDefaultProfile };
            if (customDefaultProfiles != null)
                defaultProfiles.AddRange(customDefaultProfiles);

            foreach (var defaultProfile in defaultProfiles)
            {
                if (defaultProfile.components.Contains(component))
                {
                    EvaluateVolumeDefaultState();
                    return;
                }
            }
        }

        /// <summary>
        /// Creates and returns a new <see cref="VolumeStack"/> to use when you need to store
        /// the result of the Volume blending pass in a separate stack.
        /// </summary>
        /// <returns>A new <see cref="VolumeStack"/> instance with freshly loaded components.</returns>
        /// <seealso cref="VolumeStack"/>
        /// <seealso cref="Update(VolumeStack,Transform,LayerMask)"/>
        public VolumeStack CreateStack()
        {
            var stack = new VolumeStack();
            stack.Reload(baseComponentTypeArray);
            m_CreatedVolumeStacks.Add(stack);
            return stack;
        }

        /// <summary>
        /// Resets the main stack to be the default one.
        /// Call this function if you've assigned the main stack to something other than the default one.
        /// </summary>
        public void ResetMainStack()
        {
            stack = m_DefaultStack;
        }

        /// <summary>
        /// Destroy a Volume Stack
        /// </summary>
        /// <param name="stack">Volume Stack that needs to be destroyed.</param>
        public void DestroyStack(VolumeStack stack)
        {
            m_CreatedVolumeStacks.Remove(stack);
            stack.Dispose();
        }

        // For now, if a user is having a VolumeComponent with the old attribute for filtering support.
        // We are adding it to the supported volume components, but we are showing a warning.
        bool IsSupportedByObsoleteVolumeComponentMenuForRenderPipeline(Type t, Type pipelineAssetType)
        {
            var legacySupported = false;

#pragma warning disable CS0618
            var legacyPipelineAttribute = t.GetCustomAttribute<VolumeComponentMenuForRenderPipeline>();
            if (legacyPipelineAttribute != null)
            {
                Debug.LogWarning($"{nameof(VolumeComponentMenuForRenderPipeline)} is deprecated, use {nameof(SupportedOnRenderPipelineAttribute)} and {nameof(VolumeComponentMenu)} with {t} instead. #from(2023.1)");
#if UNITY_EDITOR
                var renderPipelineTypeFromAsset = RenderPipelineEditorUtility.GetPipelineTypeFromPipelineAssetType(pipelineAssetType);

                for (int i = 0; i < legacyPipelineAttribute.pipelineTypes.Length; ++i)
                {
                    if (legacyPipelineAttribute.pipelineTypes[i] == renderPipelineTypeFromAsset)
                    {
                        legacySupported = true;
                        break;
                    }
                }
#endif
            }
#pragma warning restore CS0618

            return legacySupported;
        }

        // This will be called only once at runtime and on domain reload / pipeline switch in the editor
        // as we need to keep track of any compatible component in the project
        internal void LoadBaseTypes(Type pipelineAssetType)
        {
            // Grab all the component types we can find that are compatible with current pipeline
            using (ListPool<Type>.Get(out var list))
            {
                foreach (var t in CoreUtils.GetAllTypesDerivedFrom<VolumeComponent>())
                {
                    if (t.IsAbstract)
                        continue;

                    var isSupported = SupportedOnRenderPipelineAttribute.IsTypeSupportedOnRenderPipeline(t, pipelineAssetType) ||
                                      IsSupportedByObsoleteVolumeComponentMenuForRenderPipeline(t, pipelineAssetType);

                    if (isSupported)
                        list.Add(t);
                }

                baseComponentTypeArray = list.ToArray();
            }
        }

        internal void InitializeVolumeComponents()
        {
            // Call custom static Init method if present
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var type in baseComponentTypeArray)
            {
                var initMethod = type.GetMethod("Init", flags);
                if (initMethod != null)
                {
                    initMethod.Invoke(null, null);
                }
            }
        }

        // Evaluate static default values for VolumeComponents, which is the baseline to reset the values to at the start of Update.
        internal void EvaluateVolumeDefaultState()
        {
            if (baseComponentTypeArray == null || baseComponentTypeArray.Length == 0)
                return;

            using var profilerScope = k_ProfilerMarkerEvaluateVolumeDefaultState.Auto();

            // TODO consider if the "component default values" array should be kept in memory separately. Creating the
            // instances is likely the slowest operation here, so doing that would mean it can only be done once in
            // Initialize() and the default state can be updated a lot quicker.

            // First, default-construct all VolumeComponents
            List<VolumeComponent> componentsDefaultStateList = new();
            foreach (var type in baseComponentTypeArray)
            {
                componentsDefaultStateList.Add((VolumeComponent) ScriptableObject.CreateInstance(type));
            }

            void ApplyDefaultProfile(VolumeProfile profile)
            {
                if (profile == null)
                    return;

                for (int i = 0; i < profile.components.Count; i++)
                {
                    var profileComponent = profile.components[i];
                    var defaultStateComponent = componentsDefaultStateList.FirstOrDefault(
                        x => x.GetType() == profileComponent.GetType());

                    if (defaultStateComponent != null && profileComponent.active)
                    {
                        // Ideally we would just call SetValue here. However, there are custom non-trivial
                        // implementations of VolumeParameter.Interp() (such as DiffusionProfileList) that make it
                        // necessary for us to call the it. This ensures the new DefaultProfile behavior works
                        // consistently with the old HDRP implementation where the Default Profile was implemented as
                        // a regular global volume inside the scene.
                        profileComponent.Override(defaultStateComponent, 1.0f);
                    }
                }
            }

            ApplyDefaultProfile(globalDefaultProfile);          // Apply global default profile first
            ApplyDefaultProfile(qualityDefaultProfile);         // Apply quality default profile second
            if (customDefaultProfiles != null)                  // Finally, apply custom default profiles in order
                foreach (var profile in customDefaultProfiles)
                    ApplyDefaultProfile(profile);

            // Build the flat parametersDefaultState list for fast per-frame resets
            var parametersDefaultStateList = new List<VolumeParameter>();
            foreach (var component in componentsDefaultStateList)
            {
                parametersDefaultStateList.AddRange(component.parameters);
            }

            m_ComponentsDefaultState = componentsDefaultStateList.ToArray();
            m_ParametersDefaultState = parametersDefaultStateList.ToArray();

            // All properties in stacks must be reset because the default state has changed
            foreach (var s in m_CreatedVolumeStacks)
            {
                s.requiresReset = true;
                s.requiresResetForAllProperties = true;
            }
        }

        /// <summary>
        /// Registers a new Volume in the manager. Unity does this automatically when a new Volume is
        /// enabled, or its layer changes, but you can use this function to force-register a Volume
        /// that is currently disabled.
        /// </summary>
        /// <param name="volume">The volume to register.</param>
        /// <seealso cref="Unregister"/>
        public void Register(Volume volume)
        {
            m_VolumeCollection.Register(volume, volume.gameObject.layer);
        }

        /// <summary>
        /// Unregisters a Volume from the manager. Unity does this automatically when a Volume is
        /// disabled or goes out of scope, but you can use this function to force-unregister a Volume
        /// that you added manually while it was disabled.
        /// </summary>
        /// <param name="volume">The Volume to unregister.</param>
        /// <seealso cref="Register"/>
        public void Unregister(Volume volume)
        {
            m_VolumeCollection.Unregister(volume, volume.gameObject.layer);
        }

        /// <summary>
        /// Checks if a <see cref="VolumeComponent"/> is active in a given LayerMask.
        /// </summary>
        /// <typeparam name="T">A type derived from <see cref="VolumeComponent"/></typeparam>
        /// <param name="layerMask">The LayerMask to check against</param>
        /// <returns><c>true</c> if the component is active in the LayerMask, <c>false</c>
        /// otherwise.</returns>
        public bool IsComponentActiveInMask<T>(LayerMask layerMask)
            where T : VolumeComponent
        {
            return m_VolumeCollection.IsComponentActiveInMask<T>(layerMask);
        }

        internal void SetLayerDirty(int layer)
        {
            m_VolumeCollection.SetLayerIndexDirty(layer);
        }

        internal void UpdateVolumeLayer(Volume volume, int prevLayer, int newLayer)
        {
            m_VolumeCollection.ChangeLayer(volume, prevLayer, newLayer);
        }

        // Go through all listed components and lerp overridden values in the global state
        void OverrideData(VolumeStack stack, List<VolumeComponent> components, float interpFactor)
        {
            var numComponents = components.Count;
            for (int i = 0; i < numComponents; i++)
            {
                var component = components[i];
                if (!component.active)
                    continue;

                var state = stack.GetComponent(component.GetType());
                if (state != null)
                {
                    component.Override(state, interpFactor);
                }
            }
        }

        // Faster version of OverrideData to force replace values in the global state.
        // NOTE: As an optimization, only the VolumeParameters with overrideState=true are reset. All other parameters
        // are assumed to be in their correct default state so no reset is necessary.
        internal void ReplaceData(VolumeStack stack)
        {
            using var profilerScope = k_ProfilerMarkerReplaceData.Auto();

            var stackParams = stack.parameters;
            bool resetAllParameters = stack.requiresResetForAllProperties;
            int count = stackParams.Length;
            Debug.Assert(count == m_ParametersDefaultState.Length);

            for (int i = 0; i < count; i++)
            {
                var stackParam = stackParams[i];
                if (stackParam.overrideState || resetAllParameters) // Only reset the parameters that have been overriden by a scene volume
                {
                    stackParam.overrideState = false;
                    stackParam.SetValue(m_ParametersDefaultState[i]);
                }
            }

            stack.requiresResetForAllProperties = false;
        }

        /// <summary>
        /// Checks component default state. This is only used in the editor to handle entering and exiting play mode
        /// because the instances created during playmode are automatically destroyed.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public void CheckDefaultVolumeState()
        {
            if (m_ComponentsDefaultState == null || (m_ComponentsDefaultState.Length > 0 && m_ComponentsDefaultState[0] == null))
            {
                EvaluateVolumeDefaultState();
            }
        }

        /// <summary>
        /// Checks the state of a given stack. This is only used in the editor to handle entering and exiting play mode
        /// because the instances created during playmode are automatically destroyed.
        /// </summary>
        /// <param name="stack">The stack to check.</param>
        [Conditional("UNITY_EDITOR")]
        public void CheckStack(VolumeStack stack)
        {
            if (stack.components == null)
            {
                stack.Reload(baseComponentTypeArray);
                return;
            }

            foreach (var kvp in stack.components)
            {
                if (kvp.Key == null || kvp.Value == null)
                {
                    stack.Reload(baseComponentTypeArray);
                    return;
                }
            }
        }

        // Returns true if must execute Update() in full, and false if we can early exit.
        bool CheckUpdateRequired(VolumeStack stack)
        {
            if (m_VolumeCollection.count == 0)
            {
                if (stack.requiresReset)
                {
                    // Update the stack one more time in case there was a volume that just ceased to exist. This ensures
                    // the stack will return to default values correctly.
                    stack.requiresReset = false;
                    return true;
                }

                // There were no volumes last frame either, and stack has been returned to defaults, so no update is
                // needed and we can early exit from Update().
                return false;
            }
            stack.requiresReset = true; // Stack must be reset every frame whenever there are volumes present
            return true;
        }

        /// <summary>
        /// Updates the global state of the Volume manager. Unity usually calls this once per Camera
        /// in the Update loop before rendering happens.
        /// </summary>
        /// <param name="trigger">A reference Transform to consider for positional Volume blending
        /// </param>
        /// <param name="layerMask">The LayerMask that the Volume manager uses to filter Volumes that it should consider
        /// for blending.</param>
        public void Update(Transform trigger, LayerMask layerMask)
        {
            Update(stack, trigger, layerMask);
        }

        /// <summary>
        /// Updates the Volume manager and stores the result in a custom <see cref="VolumeStack"/>.
        /// </summary>
        /// <param name="stack">The stack to store the blending result into.</param>
        /// <param name="trigger">A reference Transform to consider for positional Volume blending.
        /// </param>
        /// <param name="layerMask">The LayerMask that Unity uses to filter Volumes that it should consider
        /// for blending.</param>
        /// <seealso cref="VolumeStack"/>
        public void Update(VolumeStack stack, Transform trigger, LayerMask layerMask)
        {
            using var profilerScope = k_ProfilerMarkerUpdate.Auto();

            if (!isInitialized)
                return;

            Assert.IsNotNull(stack);

            CheckDefaultVolumeState();
            CheckStack(stack);

            if (!CheckUpdateRequired(stack))
                return;

            // Start by resetting the global state to default values.
            ReplaceData(stack);

            bool onlyGlobal = trigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : trigger.position;

            // Sort the cached volume list(s) for the given layer mask if needed and return it
            var volumes = GrabVolumes(layerMask);

            Camera camera = null;
            // Behavior should be fine even if camera is null
            if (!onlyGlobal)
                trigger.TryGetComponent<Camera>(out camera);

            // Traverse all volumes
            int numVolumes = volumes.Count;
            for (int i = 0; i < numVolumes; i++)
            {
                Volume volume = volumes[i];
                if (volume == null)
                    continue;

#if UNITY_EDITOR
                // Skip volumes that aren't in the scene currently displayed in the scene view
                if (!IsVolumeRenderedByCamera(volume, camera))
                    continue;
#endif

                // Skip disabled volumes and volumes without any data or weight
                if (!volume.enabled || volume.profileRef == null || volume.weight <= 0f)
                    continue;

                // Global volumes always have influence
                if (volume.isGlobal)
                {
                    OverrideData(stack, volume.profileRef.components, Mathf.Clamp01(volume.weight));
                    continue;
                }

                if (onlyGlobal)
                    continue;

                // If volume isn't global and has no collider, skip it as it's useless
                var colliders = m_TempColliders;
                volume.GetComponents(colliders);
                if (colliders.Count == 0)
                    continue;

                // Find closest distance to volume, 0 means it's inside it
                float closestDistanceSqr = float.PositiveInfinity;

                int numColliders = colliders.Count;
                for (int c = 0; c < numColliders; c++)
                {
                    var collider = colliders[c];
                    if (!collider.enabled)
                        continue;

                    var closestPoint = collider.ClosestPoint(triggerPos);
                    var d = (closestPoint - triggerPos).sqrMagnitude;

                    if (d < closestDistanceSqr)
                        closestDistanceSqr = d;
                }

                colliders.Clear();
                float blendDistSqr = volume.blendDistance * volume.blendDistance;

                // Volume has no influence, ignore it
                // Note: Volume doesn't do anything when `closestDistanceSqr = blendDistSqr` but we
                //       can't use a >= comparison as blendDistSqr could be set to 0 in which case
                //       volume would have total influence
                if (closestDistanceSqr > blendDistSqr)
                    continue;

                // Volume has influence
                float interpFactor = 1f;

                if (blendDistSqr > 0f)
                    interpFactor = 1f - (closestDistanceSqr / blendDistSqr);

                // No need to clamp01 the interpolation factor as it'll always be in [0;1[ range
                OverrideData(stack, volume.profileRef.components, interpFactor * Mathf.Clamp01(volume.weight));
            }
        }

        /// <summary>
        /// Get all volumes on a given layer mask sorted by influence.
        /// </summary>
        /// <param name="layerMask">The LayerMask that Unity uses to filter Volumes that it should consider.</param>
        /// <returns>An array of volume.</returns>
        public Volume[] GetVolumes(LayerMask layerMask)
        {
            var volumes = GrabVolumes(layerMask);
            volumes.RemoveAll(v => v == null);
            return volumes.ToArray();
        }

        List<Volume> GrabVolumes(LayerMask mask)
        {
            return m_VolumeCollection.GrabVolumes(mask);
        }

        static bool IsVolumeRenderedByCamera(Volume volume, Camera camera)
        {
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            // GameObject for default global volume may not belong to any scene, following check prevents it from being culled
            if (!volume.gameObject.scene.IsValid())
                return true;
            // IsGameObjectRenderedByCamera does not behave correctly when camera is null so we have to catch it here.
            return camera == null ? true : UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(volume.gameObject, camera);
#else
            return true;
#endif
        }
    }

    /// <summary>
    /// A scope in which a Camera filters a Volume.
    /// </summary>
    [Obsolete("VolumeIsolationScope is deprecated, it does not have any effect anymore.")]
    public struct VolumeIsolationScope : IDisposable
    {
        /// <summary>
        /// Constructs a scope in which a Camera filters a Volume.
        /// </summary>
        /// <param name="unused">Unused parameter.</param>
        public VolumeIsolationScope(bool unused) { }

        /// <summary>
        /// Stops the Camera from filtering a Volume.
        /// </summary>
        void IDisposable.Dispose() { }
    }
}
