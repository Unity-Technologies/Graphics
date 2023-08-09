//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Profiling;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [InitializeOnLoad]
    class VFXGraphPreprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<string> assetToReimport = null;

#if VFX_HAS_TIMELINE
            UnityEditor.VFX.Migration.ActivationToControlTrack.SanitizePlayable(importedAssets);
#endif

            if (deletedAssets.Any())
            {
                VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.UpdateHistory());
            }

            foreach (var assetPath in importedAssets)
            {
                bool isVFX = VisualEffectAssetModificationProcessor.HasVFXExtension(assetPath);
                if (isVFX)
                {
                    VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);
                    if (resource == null)
                        continue;
                    VFXGraph graph = resource.GetOrCreateGraph(); //resource.graph should be already != null at this stage but GetOrCreateGraph is also assigning the visualEffectResource. It's required for UpdateSubAssets
                    if (graph != null)
                    {
                        bool wasGraphSanitized = graph.sanitized;

                        try
                        {
                            graph.SanitizeForImport();
                            if (!wasGraphSanitized && graph.sanitized)
                            {
                                assetToReimport ??= new List<string>();
                                assetToReimport.Add(assetPath);
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.LogErrorFormat("Exception during sanitization of {0} : {1}", assetPath, exception);
                        }

                        var window = VFXViewWindow.GetWindow(graph, false, false);
                        if (window != null)
                        {
                            window.UpdateTitle(assetPath);
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("VisualEffectGraphResource without graph : {0}", assetPath);
                    }
                }
            }

            //Relaunch previously skipped OnCompileResource
            if (assetToReimport != null)
            {
                AssetDatabase.StartAssetEditing();
                foreach (var assetPath in assetToReimport)
                {
                    try
                    {
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogErrorFormat("Exception during reimport of {0} : {1}", assetPath, exception);
                    }
                }
                AssetDatabase.StopAssetEditing();
            }
        }

        static string[] OnAddResourceDependencies(string assetPath)
        {
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);
            if (resource != null)
            {
                if (resource.graph is VFXGraph)
                    return resource.GetOrCreateGraph().UpdateImportDependencies();
                Debug.LogError("VisualEffectGraphResource without graph");
            }
            return null;
        }

        static void OnCompileResource(VisualEffectResource resource)
        {
            if (resource != null)
            {
                VFXGraph graph = resource.graph as VFXGraph;
                if (graph != null)
                {
                    if (!graph.sanitized)
                    {
                        //Early return, the reimport will be forced with the next OnPostprocessAllAssets after Sanitize
                        resource.ClearRuntimeData();
                    }
                    else
                    {
                        //Workaround, use backup system to prevent any modification of the graph during compilation
                        //The responsible of this unexpected change is PrepareSubgraphs => RecurseSubgraphRecreateCopy => ResyncSlots
                        //It will let the VFXGraph in a really bad state after compilation.
                        graph = resource.GetOrCreateGraph();
                        var dependencies = new HashSet<ScriptableObject>();
                        dependencies.Add(graph);
                        graph.CollectDependencies(dependencies);
                        var backup = VFXMemorySerializer.StoreObjectsToByteArray(dependencies.ToArray(), CompressionLevel.None);

                        graph.CompileForImport();

                        VFXMemorySerializer.ExtractObjects(backup, false);
                        //The backup during undo/redo is actually calling UnknownChange after ExtractObjects
                        //You have to avoid because it will call ResyncSlot
                    }
                }
                else
                    Debug.LogError("OnCompileResource error - VisualEffectResource without graph");
            }
        }

        static void OnSetupMaterial(VisualEffectResource resource, Material material, UnityObject model)
        {
            if (resource != null)
            {
                // sanity checks
                if (resource.graph == null)
                {
                    Debug.LogError("OnSetupMaterial error - VisualEffectResource without graph");
                    return;
                }
                if (!(model is VFXModel))
                {
                    Debug.LogError("OnSetupMaterial error - Passed object is not a VFXModel");
                    return;
                }
                //if (resource.graph != ((VFXModel)model).GetGraph())
                //{
                //    Debug.LogError("OnSetupMaterial error - VisualEffectResource and VFXModel graph do not match");
                //    return;
                //}

                if (!resource.GetOrCreateGraph().sanitized)
                {
                    Debug.LogError("OnSetupMaterial error - Graph hasn't been sanitized");
                    return;
                }

                // Actual call
                if (model is IVFXSubRenderer)
                {
                    ((IVFXSubRenderer)model).SetupMaterial(material);
                }
            }
        }

        static VFXGraphPreprocessor()
        {
            EditorApplication.update += CheckCompilationVersion;

            VisualEffectResource.onAddResourceDependencies = OnAddResourceDependencies;
            VisualEffectResource.onCompileResource = OnCompileResource;
            VisualEffectResource.onSetupMaterial = OnSetupMaterial;
        }

        static void CheckCompilationVersion()
        {
            EditorApplication.update -= CheckCompilationVersion;

            UnityObject vfxmanager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/VFXManager.asset").FirstOrDefault();
            SerializedObject serializedVFXManager = new SerializedObject(vfxmanager);
            var compiledVersionProperty = serializedVFXManager.FindProperty("m_CompiledVersion");
            var runtimeVersionProperty = serializedVFXManager.FindProperty("m_RuntimeVersion");

            if (compiledVersionProperty.intValue != VFXGraphCompiledData.compiledVersion || runtimeVersionProperty.intValue != VisualEffectAsset.currentRuntimeDataVersion)
            {
                string[] allVisualEffectAssets = AssetDatabase.FindAssets("t:VisualEffectAsset");
                compiledVersionProperty.intValue = (int)VFXGraphCompiledData.compiledVersion;
                runtimeVersionProperty.intValue = (int)VisualEffectAsset.currentRuntimeDataVersion;
                serializedVFXManager.ApplyModifiedProperties();

                AssetDatabase.StartAssetEditing();
                foreach (var guid in allVisualEffectAssets)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    AssetDatabase.ImportAsset(path);
                }
                AssetDatabase.StopAssetEditing();
            }
        }
    }
    class VFXAssetManager : EditorWindow
    {
        public static List<VisualEffectObject> GetAllVisualEffectObjects()
        {
            var vfxObjects = new List<VisualEffectObject>();
            var vfxObjectsGuid = AssetDatabase.FindAssets("t:VisualEffectObject");
            foreach (var guid in vfxObjectsGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var vfxObj = AssetDatabase.LoadAssetAtPath<VisualEffectObject>(assetPath);
                if (vfxObj != null)
                {
                    vfxObjects.Add(vfxObj);
                }
            }
            return vfxObjects;
        }

        public static void Build(bool forceDirty = false)
        {
            var vfxObjects = GetAllVisualEffectObjects();

            foreach (var vfxObj in vfxObjects)
            {
                if (VFXViewPreference.advancedLogs)
                    Debug.Log(string.Format("Recompile VFX asset: {0} ({1})", vfxObj, AssetDatabase.GetAssetPath(vfxObj)));

                var resource = vfxObj.GetResource();
                if (resource != null)
                {
                    VFXGraph graph = resource.GetOrCreateGraph();
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
                    if (forceDirty)
                        EditorUtility.SetDirty(resource);
                }
            }

            VFXExpression.ClearCache();
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();
        }

        [MenuItem("Edit/VFX/Rebuild And Save All VFX Graphs", priority = 320)]
        public static void BuildAndSave()
        {
            Build(true);
            AssetDatabase.SaveAssets();
        }
    }

    class VisualEffectAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static bool HasVFXExtension(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) &&
                (filePath.EndsWith(VisualEffectResource.Extension, StringComparison.OrdinalIgnoreCase)
              || filePath.EndsWith(VisualEffectSubgraphBlock.Extension, StringComparison.OrdinalIgnoreCase)
              || filePath.EndsWith(VisualEffectSubgraphOperator.Extension, StringComparison.OrdinalIgnoreCase)))
            {

// See this PR https://github.com/Unity-Technologies/Graphics/pull/6890
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return !AssetDatabase.IsValidFolder(filePath);
#else
                return true;
#endif
            }

            return false;
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            Profiler.BeginSample("VisualEffectAssetModicationProcessor.OnWillSaveAssets");
            bool started = false;
            try {
                foreach (string path in paths.Where(HasVFXExtension))
                {
                    if (!started)
                    {
                        started = true;
                        AssetDatabase.StartAssetEditing();
                    }
                    var vfxResource = VisualEffectResource.GetResourceAtPath(path);
                    if (vfxResource != null)
                    {
                        vfxResource.GetOrCreateGraph().UpdateSubAssets();
                        try
                        {
                            VFXGraph.compilingInEditMode = vfxResource.GetOrCreateGraph().GetCompilationMode() == VFXCompilationMode.Edition;
                            vfxResource.WriteAsset(); // write asset as the AssetDatabase won't do it.
                        }
                        finally
                        {
                            VFXGraph.compilingInEditMode = false;
                        }
                    }
                }
            }
            finally
            {
                if (started)
                    AssetDatabase.StopAssetEditing();
            }
            Profiler.EndSample();
            return paths;
        }
    }

    static class VisualEffectResourceExtensions
    {
        public static VFXGraph GetOrCreateGraph(this VisualEffectResource resource)
        {
            VFXGraph graph = resource.graph as VFXGraph;

            if (graph == null)
            {
                string assetPath = AssetDatabase.GetAssetPath(resource);
                AssetDatabase.ImportAsset(assetPath);

                graph = resource.GetContents().OfType<VFXGraph>().FirstOrDefault();
            }

            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<VFXGraph>();
                resource.graph = graph;
                graph.hideFlags |= HideFlags.HideInHierarchy;
                graph.visualEffectResource = resource;
                // in this case we must update the subassets so that the graph is added to the resource dependencies
                graph.UpdateSubAssets();
            }

            graph.visualEffectResource = resource;
            return graph;
        }

        public static void UpdateSubAssets(this VisualEffectResource resource)
        {
            resource.GetOrCreateGraph().UpdateSubAssets();
        }

        public static bool IsAssetEditable(this VisualEffectResource resource)
        {
            return AssetDatabase.IsOpenForEdit((UnityEngine.Object)resource.asset ?? resource.subgraph, StatusQueryOptions.UseCachedIfPossible);
        }
    }

    static class VisualEffectObjectExtensions
    {
        public static VisualEffectResource GetOrCreateResource(this VisualEffectObject asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);

            if (resource == null && !string.IsNullOrEmpty(assetPath))
            {
                resource = new VisualEffectResource();
                resource.SetAssetPath(assetPath);
            }
            return resource;
        }

        public static VisualEffectResource GetResource(this VisualEffectObject asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);

            if (resource == null && !string.IsNullOrEmpty(assetPath))
                throw new NullReferenceException($"VFX resource does not exist for this asset at path: {assetPath}");

            return resource;
        }
    }

    class VFXGraph : VFXModel
    {
        // Please add increment reason for each version below
        // 1: Size refactor
        // 2: Change some SetAttribute to spaceable slot
        // 3: Remove Masked from blendMode in Outputs and split feature to UseAlphaClipping
        // 4: TransformVector|Position|Direction & DistanceToSphere|Plane|Line have now spaceable outputs
        // 5: Harmonized position blocks composition: PositionAABox was the only one with Overwrite position
        // 6: Remove automatic strip orientation from quad strip context
        // 7: Add CameraBuffer type
        // 8: Bounds computation introduces a BoundsSettingMode for VFXDataParticles
        // 9: Update HDRP decal angle fade encoding
        // 10: Position Mesh and Skinned Mesh out of experimental (changing the list of flag and output types)
        // 11: Instancing
        // 12: Unexpected incorrect synchronization of output with ShaderGraph
        public static readonly int CurrentVersion = 12;

        [NonSerialized]
        internal static bool compilingInEditMode = false;

        public override void OnEnable()
        {
            base.OnEnable();
            m_ExpressionGraphDirty = true;
        }

        public override void OnSRPChanged()
        {
            m_GraphSanitized = false;
            m_ExpressionGraphDirty = true;
        }

        public VisualEffectResource visualEffectResource
        {
            get
            {
                return m_Owner;
            }
            set
            {
                if (m_Owner != value)
                {
                    m_Owner = value;
                    m_Owner.graph = this;
                    m_ExpressionGraphDirty = true;
                }
            }
        }
        [SerializeField]
        VFXUI m_UIInfos;

        public VFXUI UIInfos
        {
            get
            {
                if (m_UIInfos == null)
                {
                    m_UIInfos = ScriptableObject.CreateInstance<VFXUI>();
                }
                return m_UIInfos;
            }
        }

        public VFXParameterInfo[] m_ParameterInfo;

        private VFXSystemNames m_SystemNames = new VFXSystemNames();

        public VFXSystemNames systemNames { get { return m_SystemNames; } }

        public void BuildParameterInfo()
        {
            m_ParameterInfo = VFXParameterInfo.BuildParameterInfo(this);
            VisualEffectEditor.RepaintAllEditors();
        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return !(model is VFXGraph); // Can hold any model except other VFXGraph
        }

        public object Backup()
        {
            Profiler.BeginSample("VFXGraph.Backup");
            var dependencies = new HashSet<ScriptableObject>();

            dependencies.Add(this);
            CollectDependencies(dependencies);

            var result = VFXMemorySerializer.StoreObjectsToByteArray(dependencies.ToArray(), CompressionLevel.Fastest);

            Profiler.EndSample();

            return result;
        }

        public void Restore(object str)
        {
            Profiler.BeginSample("VFXGraph.Restore");
            var scriptableObject = VFXMemorySerializer.ExtractObjects(str as byte[], false);

            Profiler.BeginSample("VFXGraph.Restore SendUnknownChange");
            foreach (var model in scriptableObject.OfType<VFXModel>())
            {
                model.OnUnknownChange();
            }
            Profiler.EndSample();
            Profiler.EndSample();
            m_SystemNames.Sync(this);
            m_ExpressionGraphDirty = true;
            m_ExpressionValuesDirty = true;
            m_DependentDirty = true;
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            Profiler.BeginSample("VFXEditor.CollectDependencies");
            try
            {
                if (m_UIInfos != null)
                    objs.Add(m_UIInfos);
                base.CollectDependencies(objs, ownedOnly);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        public void SanitizeGraph()
        {
            if (m_GraphSanitized)
                return;

            var objs = new HashSet<ScriptableObject>();
            CollectDependencies(objs);

            if (version < 7)
            {
                SanitizeCameraBuffers(objs);
            }

            foreach (var model in objs.OfType<VFXModel>())
                try
                {
                    model.Sanitize(m_GraphVersion); // This can modify dependencies but newly created model are supposed safe so we dont care about retrieving new dependencies
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while sanitizing model: {0} of type {1}: {2} {3}", model.name, model.GetType(), e, e.StackTrace));
                }

            if (m_UIInfos != null)
                try
                {
                    m_UIInfos.Sanitize(this);
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while sanitizing VFXUI: : {0} {1}", e, e.StackTrace));
                }

            systemNames.Sync(this);

            if (version < 11)
            {
                visualEffectResource.instancingMode = VFXInstancingMode.Disabled;
            }

            int resourceCurrentVersion = 0;
            // Stop using reflection after 2020.2;
            FieldInfo info = typeof(VisualEffectResource).GetField("CurrentVersion", BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (info != null)
                resourceCurrentVersion = (int)info.GetValue(null);

            if (m_ResourceVersion < resourceCurrentVersion) // Graph not up to date
            {
                if (m_ResourceVersion < 1) // Version before gradient interpreted as linear
                {
                    foreach (var model in objs.OfType<VFXSlotGradient>())
                    {
                        Gradient value = (Gradient)model.value;
                        GradientColorKey[] keys = value.colorKeys;

                        for (int i = 0; i < keys.Length; ++i)
                        {
                            var colorKey = keys[i];
                            colorKey.color = colorKey.color.linear;
                            keys[i] = colorKey;
                        }
                        value.colorKeys = keys;
                        model.value = new Gradient();
                        model.value = value;
                    }
                }
            }
            m_ResourceVersion = resourceCurrentVersion;
            m_GraphSanitized = true;
            m_GraphVersion = CurrentVersion;

            UpdateSubAssets(); //Force remove no more referenced object from the asset & *important* register as persistent new dependencies
        }

        private void SanitizeCameraBuffers(HashSet<ScriptableObject> objs)
        {
            List<Tuple<int, string, int, string>> links = new List<Tuple<int, string, int, string>>();
            var cameraSlots = objs.Where(obj => obj is VFXSlot && (obj as VFXSlot).value is CameraType).ToArray();
            for (int i = 0; i < cameraSlots.Length; ++i)
            {
                var cameraSlot = cameraSlots[i] as VFXSlot;

                var depthBufferSlot = cameraSlot.children.First(slot => slot.name == "depthBuffer");
                SanitizeCameraBufferLinks(depthBufferSlot, i, cameraSlots, links);

                var colorBufferSlot = cameraSlot.children.First(slot => slot.name == "colorBuffer");
                SanitizeCameraBufferLinks(colorBufferSlot, i, cameraSlots, links);

                objs.Remove(cameraSlots[i]);
                cameraSlots[i] = cameraSlot.Recreate();
                objs.Add(cameraSlots[i]);
            }
            foreach (var link in links)
            {
                var cameraSlotFrom = cameraSlots[link.Item1] as VFXSlot;
                var slotFrom = cameraSlotFrom.children.First(slot => slot.name == link.Item2);

                var cameraSlotTo = cameraSlots[link.Item3] as VFXSlot;
                var slotTo = cameraSlotTo.children.First(slot => slot.name == link.Item4);

                slotFrom.Link(slotTo);
            }
        }

        private void SanitizeCameraBufferLinks(VFXSlot slotFrom, int indexFrom, ScriptableObject[] cameraSlots, List<Tuple<int, string, int, string>> links)
        {
            if (slotFrom != null && !(slotFrom is VFXSlotCameraBuffer))
            {
                foreach (var slotTo in slotFrom.LinkedSlots)
                {
                    int indexTo = Array.IndexOf(cameraSlots, slotTo.GetMasterSlot());
                    if (indexTo >= 0)
                    {
                        links.Add(new Tuple<int, string, int, string>(indexFrom, slotFrom.name, indexTo, slotTo.name));
                    }
                }
            }
        }

        public void ClearCompileData()
        {
            m_CompiledData = null;


            m_ExpressionValuesDirty = true;
        }

        [SerializeField]
        List<string> m_ImportDependencies;

        public void UpdateSubAssets()
        {
            if (visualEffectResource == null)
                return;
            Profiler.BeginSample("VFXEditor.UpdateSubAssets");
            try
            {
                var currentObjects = new HashSet<ScriptableObject>();
                currentObjects.Add(this);
                CollectDependencies(currentObjects);

                visualEffectResource.SetContents(currentObjects.Cast<UnityObject>().ToArray());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (cause == VFXModel.InvalidationCause.kStructureChanged
                || cause == VFXModel.InvalidationCause.kSettingChanged
                || cause == VFXModel.InvalidationCause.kConnectionChanged)
                m_SystemNames.Sync(this);

            base.OnInvalidate(model, cause);

            if (model is VFXParameter    //Something changed directly on VFXParameter (e.g. exposed state boolean)
                || model is VFXSlot && (model as VFXSlot).owner is VFXParameter //Something changed on a slot owned by a VFXParameter (e.g. the default value)
                || cause == VFXModel.InvalidationCause.kStructureChanged //A VFXParameter could have been removed
            )
            {
                BuildParameterInfo();
            }


            if (cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                UpdateSubAssets();
                if (model == this)
                    VFXSubgraphContext.CallOnGraphChanged(this);

                m_DependentDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kSettingChanged && model is VFXParameter)
            {
                VFXSubgraphContext.CallOnGraphChanged(this);
                m_DependentDirty = true;
            }

            if (cause != VFXModel.InvalidationCause.kExpressionInvalidated &&
                cause != VFXModel.InvalidationCause.kExpressionGraphChanged &&
                cause != VFXModel.InvalidationCause.kExpressionValueInvalidated &&
                cause != VFXModel.InvalidationCause.kUIChangedTransient &&
                (model.hideFlags & HideFlags.DontSave) == 0)
            {
                EditorUtility.SetDirty(this);
            }

            if (cause == VFXModel.InvalidationCause.kExpressionGraphChanged)
            {
                m_ExpressionGraphDirty = true;
                m_DependentDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kParamChanged)
            {
                m_ExpressionValuesDirty = true;
                m_DependentDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kMaterialChanged)
            {
                m_MaterialsDirty = true;
            }
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            RecompileIfNeeded(false, true);
            return compiledData.FindReducedExpressionIndexFromSlotCPU(slot);
        }

        public void SetCompilationMode(VFXCompilationMode mode, bool reimport = true)
        {
            if (m_CompilationMode != mode)
            {
                m_CompilationMode = mode;
                SetExpressionGraphDirty();
                if (reimport)
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        public VFXCompilationMode GetCompilationMode()
        {
            return m_CompilationMode;
        }

        public void ForceShaderDebugSymbols(bool enable, bool reimport = true)
        {
            if (m_ForceShaderDebugSymbols != enable)
            {
                m_ForceShaderDebugSymbols = enable;
                if (reimport)
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        public bool GetForceShaderDebugSymbols()
        {
            return m_ForceShaderDebugSymbols;
        }

        public void SetForceShaderValidation(bool forceShaderValidation, bool reimport = true)
        {
            if (m_ForceShaderValidation != forceShaderValidation)
            {
                m_ForceShaderValidation = forceShaderValidation;
                if (m_ForceShaderValidation)
                {
                    SetExpressionGraphDirty();
                    if (reimport)
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
                }
            }
        }

        public bool IsExpressionGraphDirty()
        {
            return m_ExpressionGraphDirty;
        }

        public void SetExpressionGraphDirty(bool dirty = true)
        {
            m_ExpressionGraphDirty = dirty;
            m_DependentDirty = dirty;
        }

        public void SetExpressionValueDirty()
        {
            m_ExpressionValuesDirty = true;
            m_DependentDirty = true;
        }

        public void BuildSubgraphDependencies()
        {
            if (m_SubgraphDependencies == null)
                m_SubgraphDependencies = new List<VisualEffectObject>();
            m_SubgraphDependencies.Clear();

            HashSet<VisualEffectObject> explored = new HashSet<VisualEffectObject>();
            RecurseBuildDependencies(explored, children);
        }

        void RecurseBuildDependencies(HashSet<VisualEffectObject> explored, IEnumerable<VFXModel> models)
        {
            foreach (var model in models)
            {
                if (model is VFXSubgraphContext)
                {
                    var subgraphContext = model as VFXSubgraphContext;

                    if (subgraphContext.subgraph != null && !explored.Contains(subgraphContext.subgraph))
                    {
                        explored.Add(subgraphContext.subgraph);
                        m_SubgraphDependencies.Add(subgraphContext.subgraph);
                        RecurseBuildDependencies(explored, subgraphContext.subgraph.GetResource().GetOrCreateGraph().children);
                    }
                }
                else if (model is VFXSubgraphOperator)
                {
                    var subgraphOperator = model as VFXSubgraphOperator;

                    if (subgraphOperator.subgraph != null && !explored.Contains(subgraphOperator.subgraph))
                    {
                        explored.Add(subgraphOperator.subgraph);
                        m_SubgraphDependencies.Add(subgraphOperator.subgraph);
                        RecurseBuildDependencies(explored, subgraphOperator.subgraph.GetResource().GetOrCreateGraph().children);
                    }
                }
                else if (model is VFXContext)
                {
                    foreach (var block in (model as VFXContext).children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;

                            if (subgraphBlock.subgraph != null && !explored.Contains(subgraphBlock.subgraph))
                            {
                                explored.Add(subgraphBlock.subgraph);
                                m_SubgraphDependencies.Add(subgraphBlock.subgraph);
                                RecurseBuildDependencies(explored, subgraphBlock.subgraph.GetResource().GetOrCreateGraph().children);
                            }
                        }
                    }
                }
            }
        }

        void RecurseSubgraphRecreateCopy(IEnumerable<VFXModel> children)
        {
            foreach (var child in children)
            {
                if (child is VFXSubgraphContext)
                {
                    var subgraphContext = child as VFXSubgraphContext;
                    subgraphContext.RecreateCopy();
                    if (subgraphContext.subgraph != null)
                    {
                        RecurseSubgraphRecreateCopy(subgraphContext.subChildren);
                    }
                }
                else if (child is VFXContext)
                {
                    foreach (var block in child.children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;
                            subgraphBlock.RecreateCopy();
                            if (subgraphBlock.subgraph != null)
                                RecurseSubgraphRecreateCopy(subgraphBlock.subChildren);
                        }
                    }
                }
                else if (child is VFXSubgraphOperator operatorChild)
                {
                    operatorChild.RecreateCopy();
                    if (operatorChild.ResyncSlots(true))
                        operatorChild.UpdateOutputExpressions();
                }
            }
        }

        private void SetFlattenedParentToSubblocks()
        {
            foreach (var child in children.OfType<VFXContext>())
                foreach (var block in child.children.OfType<VFXSubgraphBlock>())
                    block.SetSubblocksFlattenedParent();
        }

        void RecurseSubgraphPatchInputExpression(IEnumerable<VFXModel> children)
        {
            foreach (var child in children)
            {
                if (child is VFXSubgraphContext)
                {
                    var subgraphContext = child as VFXSubgraphContext;
                    subgraphContext.PatchInputExpressions();
                }
                else if (child is VFXContext)
                {
                    foreach (var block in child.children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;
                            subgraphBlock.PatchInputExpressions();
                        }
                    }
                }
                else if (child is VFXSubgraphOperator operatorChild)
                {
                    operatorChild.ResyncSlots(false);
                    operatorChild.UpdateOutputExpressions();
                }
            }
            foreach (var child in children)
            {
                if (child is VFXSubgraphContext)
                {
                    var subgraphContext = child as VFXSubgraphContext;
                    if (subgraphContext.subgraph != null && subgraphContext.subChildren != null)
                        RecurseSubgraphPatchInputExpression(subgraphContext.subChildren);
                }
                else if (child is VFXContext)
                {
                    foreach (var block in child.children)
                    {
                        if (block is VFXSubgraphBlock)
                        {
                            var subgraphBlock = block as VFXSubgraphBlock;
                            if (subgraphBlock.subgraph != null && subgraphBlock.subChildren != null)
                                RecurseSubgraphPatchInputExpression(subgraphBlock.subChildren);
                        }
                    }
                }
            }
        }

        void SubgraphDirty(VisualEffectObject subgraph)
        {
            if (m_SubgraphDependencies != null && m_SubgraphDependencies.Contains(subgraph))
            {
                PrepareSubgraphs();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        private void PrepareSubgraphs()
        {
            Profiler.BeginSample("PrepareSubgraphs");
            RecurseSubgraphRecreateCopy(children);
            SetFlattenedParentToSubblocks();
            RecurseSubgraphPatchInputExpression(children);
            Profiler.EndSample();
        }

        IEnumerable<VFXGraph> GetAllGraphs<T>() where T : VisualEffectObject
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);

            foreach (var assetPath in guids.Select(t => AssetDatabase.GUIDToAssetPath(t)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    var graph = asset.GetResource().GetOrCreateGraph();
                    yield return graph;
                }
            }
        }

        //Explicit compile must be used if we want to force compilation even if a dependency is needed, which me must not do on a deleted library import.
        public static bool explicitCompile { get; set; } = false;


        public void SanitizeForImport()
        {
            // We arrive from AssetPostProcess so dependencies are already loaded no need to worry about them (FB #1364156)

            foreach (var child in children)
                child.CheckGraphBeforeImport();

            SanitizeGraph();
        }

        public void CompileForImport()
        {
            if (VFXGraph.compilingInEditMode)
                m_CompilationMode = VFXCompilationMode.Edition;

            if (!GetResource().isSubgraph)
            {
                // Check Graph Before Import can be needed to synchronize modified shaderGraph
                foreach (var child in children)
                    child.CheckGraphBeforeImport();

                // Graph must have been sanitized at this point by the VFXGraphPreprocessor.OnPreprocess
                BuildSubgraphDependencies();
                PrepareSubgraphs();

                compiledData.Compile(m_CompilationMode, m_ForceShaderValidation, VFXViewPreference.generateShadersWithDebugSymbols || m_ForceShaderDebugSymbols);
            }
            m_ExpressionGraphDirty = false;
            m_ExpressionValuesDirty = false;
        }

        public static VFXCompileErrorReporter compileReporter = null;

        public void RecompileIfNeeded(bool preventRecompilation = false, bool preventDependencyRecompilation = false)
        {
            SanitizeGraph();

            if (!GetResource().isSubgraph)
            {
                bool considerGraphDirty = m_ExpressionGraphDirty && !preventRecompilation;
                if (considerGraphDirty)
                {
                    BuildSubgraphDependencies();
                    PrepareSubgraphs();

                    compiledData.Compile(m_CompilationMode, m_ForceShaderValidation, VFXViewPreference.generateShadersWithDebugSymbols || m_ForceShaderDebugSymbols);
                }
                else
                {
                    if (m_ExpressionValuesDirty && !m_ExpressionGraphDirty)
                        compiledData.UpdateValues();
                    if (m_MaterialsDirty && GetResource().asset != null)
                        UnityEngine.VFX.VFXManager.ResyncMaterials(GetResource().asset);
                }

                if (considerGraphDirty)
                    m_ExpressionGraphDirty = false;

                m_ExpressionValuesDirty = false;
                m_MaterialsDirty = false;
            }
            else if (m_ExpressionGraphDirty && !preventRecompilation)
            {
                BuildSubgraphDependencies();
                PrepareSubgraphs();
                m_ExpressionGraphDirty = false;
            }
            if (!preventDependencyRecompilation && m_DependentDirty)
            {
                var obj = GetResource().visualEffectObject;
                foreach (var graph in GetAllGraphs<VisualEffectAsset>())
                {
                    graph.SubgraphDirty(obj);
                }
                m_DependentDirty = false;
            }
        }

        private VFXGraphCompiledData compiledData
        {
            get
            {
                if (m_CompiledData == null)
                    m_CompiledData = new VFXGraphCompiledData(this);
                return m_CompiledData;
            }
        }

        public bool sanitized { get { return m_GraphSanitized; } }

        public int version { get { return m_GraphVersion; } }

        [SerializeField]
        private int m_GraphVersion = CurrentVersion;

        [SerializeField]
        private int m_ResourceVersion;

        [NonSerialized]
        private bool m_GraphSanitized = false;
        [NonSerialized]
        private bool m_ExpressionGraphDirty = true;
        [NonSerialized]
        private bool m_ExpressionValuesDirty = true;
        [NonSerialized]
        private bool m_DependentDirty = true;
        [NonSerialized]
        private bool m_MaterialsDirty = false;

        [NonSerialized]
        private VFXGraphCompiledData m_CompiledData;
        private VFXCompilationMode m_CompilationMode = VFXCompilationMode.Runtime;
        private bool m_ForceShaderDebugSymbols = false;
        private bool m_ForceShaderValidation = false;

        [NonSerialized]
        public Action<VFXGraph> onRuntimeDataChanged;

        [SerializeField]
        private List<VisualEffectObject> m_SubgraphDependencies = new List<VisualEffectObject>();

        [SerializeField]
        private string m_CategoryPath;

        public string categoryPath
        {
            get { return m_CategoryPath; }
            set { m_CategoryPath = value; }//TODO invalidate cache here
        }

        public ReadOnlyCollection<VisualEffectObject> subgraphDependencies
        {
            get { return m_SubgraphDependencies.AsReadOnly(); }
        }

        public string[] UpdateImportDependencies()
        {
            visualEffectResource.ClearImportDependencies();

            var dependencies = new HashSet<int>();
            GetImportDependentAssets(dependencies);
            var dependentAssetGUIDs = dependencies
                .Where(x => x != 0)
                .Select(x => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x)))
                .Distinct()
                .ToArray();

            foreach (var guid in dependentAssetGUIDs)
            {
                visualEffectResource.AddImportDependency(guid);
            }

            return dependentAssetGUIDs;
        }

        private VisualEffectResource m_Owner;
    }
}
