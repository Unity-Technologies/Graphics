//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Profiling;


using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [InitializeOnLoad]
    class VFXGraphPreprocessor : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            bool isVFX = VisualEffectAssetModicationProcessor.HasVFXExtension(assetPath);
            if (isVFX)
            {
                VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);
                if (resource == null)
                    return;
                VFXGraph graph = resource.graph as VFXGraph;
                if (graph != null)
                    graph.SanitizeForImport();
                else
                    Debug.LogError("VisualEffectGraphResource without graph");
            }
        }

        static string[] OnAddResourceDependencies(string assetPath)
        {
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);
            if (resource != null)
            {
                VFXGraph graph = resource.graph as VFXGraph;
                if (graph != null)
                    return resource.GetOrCreateGraph().GetImportDependencies();
                else
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
                    resource.GetOrCreateGraph().CompileForImport();
                else
                    Debug.LogError("VisualEffectGraphResource without graph");
            }
        }

        static VFXGraphPreprocessor()
        {
            EditorApplication.update += CheckCompilationVersion;

            VisualEffectResource.onAddResourceDependencies = OnAddResourceDependencies;
            VisualEffectResource.onCompileResource = OnCompileResource;
        }

        static void CheckCompilationVersion()
        {
            EditorApplication.update -= CheckCompilationVersion;
            string[] allVisualEffectAssets = AssetDatabase.FindAssets("t:VisualEffectAsset");

            UnityObject vfxmanager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/VFXManager.asset").FirstOrDefault();
            SerializedObject serializedVFXManager = new SerializedObject(vfxmanager);
            var compiledVersionProperty = serializedVFXManager.FindProperty("m_CompiledVersion");
            var runtimeVersionProperty = serializedVFXManager.FindProperty("m_RuntimeVersion");

            if (compiledVersionProperty.intValue != VFXGraphCompiledData.compiledVersion || runtimeVersionProperty.intValue != VisualEffectAsset.currentRuntimeDataVersion)
            {
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
    class VFXCacheManager : EditorWindow
    {
        private static List<VisualEffectObject> GetAllVisualEffectObjects()
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

        [MenuItem("Edit/Visual Effects/Rebuild And Save All Visual Effect Graphs", priority = 320)]
        public static void Build()
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
                    EditorUtility.SetDirty(resource);
                }
            }

            VFXExpression.ClearCache();
            AssetDatabase.SaveAssets();
        }
    }

    class VisualEffectAssetModicationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static bool HasVFXExtension(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
            return filePath.EndsWith(VisualEffectResource.Extension)
                || filePath.EndsWith(VisualEffectSubgraphBlock.Extension)
                || filePath.EndsWith(VisualEffectSubgraphOperator.Extension);
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            Profiler.BeginSample("VisualEffectAssetModicationProcessor.OnWillSaveAssets");
            foreach (string path in paths.Where(t => HasVFXExtension(t)))
            {
                var vfxResource = VisualEffectResource.GetResourceAtPath(path);
                if (vfxResource != null)
                {
                    var graph = vfxResource.GetOrCreateGraph();
                    graph.OnSaved();
                    vfxResource.WriteAsset(); // write asset as the AssetDatabase won't do it.
                }
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
            return AssetDatabase.IsOpenForEdit(resource.asset, StatusQueryOptions.UseCachedIfPossible);
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
        public static readonly int CurrentVersion = 6;

        public readonly VFXErrorManager errorManager = new VFXErrorManager();


        public override void OnEnable()
        {
            base.OnEnable();
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

        public void OnSaved()
        {
            try
            {
                m_saved = true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Save failed : {0}", e);
            }
        }

        public void SanitizeGraph()
        {
            if (m_GraphSanitized)
                return;

            var objs = new HashSet<ScriptableObject>();
            CollectDependencies(objs);

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
                    Debug.LogError(string.Format("Exception while sanitizing VFXUI: : {0} {1}", e , e.StackTrace));
                }

            systemNames.Sync(this);


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

#if !CASE_1289829_HAS_BEEN_FIXED
            if (visualEffectResource != null && (visualEffectResource.updateMode & VFXUpdateMode.ExactFixedTimeStep) == VFXUpdateMode.ExactFixedTimeStep)
            {
                visualEffectResource.updateMode = visualEffectResource.updateMode & ~VFXUpdateMode.ExactFixedTimeStep;
                Debug.Log("Sanitize : Exact Fixed Time has been automatically reset to false to avoid an unexpected behavior.");
            }
#endif

            UpdateSubAssets(); //Should not be necessary : force remove no more referenced object from asset
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
            m_saved = false;

            if (cause == VFXModel.InvalidationCause.kStructureChanged || cause == VFXModel.InvalidationCause.kSettingChanged)
                m_SystemNames.Sync(this);

            base.OnInvalidate(model, cause);

            if (model is VFXParameter || model is VFXSlot && (model as VFXSlot).owner is VFXParameter)
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
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            RecompileIfNeeded(false, true);
            return compiledData.FindReducedExpressionIndexFromSlotCPU(slot);
        }

        public void SetCompilationMode(VFXCompilationMode mode)
        {
            if (m_CompilationMode != mode)
            {
                m_CompilationMode = mode;
                SetExpressionGraphDirty();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            }
        }

        public void SetForceShaderValidation(bool forceShaderValidation)
        {
            if (m_ForceShaderValidation != forceShaderValidation)
            {
                m_ForceShaderValidation = forceShaderValidation;
                if (m_ForceShaderValidation)
                {
                    SetExpressionGraphDirty();
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
            if (!explicitCompile)
            {
                HashSet<int> dependentAsset = new HashSet<int>();
                GetImportDependentAssets(dependentAsset);

                foreach (var instanceID in dependentAsset)
                {
                    if (instanceID != 0 && EditorUtility.InstanceIDToObject(instanceID) == null)
                    {
                        return;
                    }
                }
            }

            foreach (var child in children)
                child.CheckGraphBeforeImport();

            SanitizeGraph();
        }

        public void CompileForImport()
        {
            if (!GetResource().isSubgraph)
            {
                // Don't pursue the compile if one of the dependency is not yet loaded
                // which happen at first import with .pcache
                if (!explicitCompile)
                {
                    HashSet<int> dependentAsset = new HashSet<int>();
                    GetImportDependentAssets(dependentAsset);

                    foreach (var instanceID in dependentAsset)
                    {
                        if (instanceID != 0 && EditorUtility.InstanceIDToObject(instanceID) == null)
                        {
                            return;
                        }
                    }
                }
                // Graph must have been sanitized at this point by the VFXGraphPreprocessor.OnPreprocess
                BuildSubgraphDependencies();
                PrepareSubgraphs();

                compiledData.Compile(m_CompilationMode, m_ForceShaderValidation);
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

                    compiledData.Compile(m_CompilationMode, m_ForceShaderValidation);
                }
                else if (m_ExpressionValuesDirty && !m_ExpressionGraphDirty)
                {
                    compiledData.UpdateValues();
                }

                if (considerGraphDirty)
                    m_ExpressionGraphDirty = false;
                m_ExpressionValuesDirty = false;
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
        private VFXGraphCompiledData m_CompiledData;
        private VFXCompilationMode m_CompilationMode = VFXCompilationMode.Runtime;
        private bool m_ForceShaderValidation = false;

        [NonSerialized]
        public Action<VFXGraph> onRuntimeDataChanged;

        [SerializeField]
        protected bool m_saved = false;

        public bool saved { get { return m_saved; } }

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

        public string[] GetImportDependencies()
        {
            visualEffectResource.ClearImportDependencies();

            HashSet<int> dependentAsset = new HashSet<int>();
            GetImportDependentAssets(dependentAsset);

            foreach (var dep in dependentAsset)
            {
                if (dep != 0)
                    visualEffectResource.AddImportDependency(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dep)));
            }

            return dependentAsset.Select(t => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(t))).Distinct().ToArray();
        }

        private VisualEffectResource m_Owner;
    }
}
