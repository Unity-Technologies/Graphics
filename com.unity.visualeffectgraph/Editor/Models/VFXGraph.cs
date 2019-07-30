//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Profiling;
using System.Reflection;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    public class VFXCacheManager : EditorWindow
    {
        private static List<VisualEffectAsset> GetAllVisualEffectAssets()
        {
            var vfxAssets = new List<VisualEffectAsset>();
            var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset");
            foreach (var guid in vfxAssetsGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                if (vfxAsset != null)
                {
                    vfxAssets.Add(vfxAsset);
                }
            }
            return vfxAssets;
        }

        [MenuItem("Edit/Visual Effects//Rebuild All Visual Effect Graphs", priority = 320)]
        public static void Build()
        {
            var vfxAssets = GetAllVisualEffectAssets();
            foreach (var vfxAsset in vfxAssets)
            {
                if (VFXViewPreference.advancedLogs)
                    Debug.Log(string.Format("Recompile VFX asset: {0} ({1})", vfxAsset, AssetDatabase.GetAssetPath(vfxAsset)));

                VFXExpression.ClearCache();
                vfxAsset.GetResource().GetOrCreateGraph().SetExpressionGraphDirty();
                vfxAsset.GetResource().GetOrCreateGraph().OnSaved();
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Edit/Visual Effects//Clear All Visual Effect Runtime Data", priority = 321)]
        public static void ClearRuntime()
        {
            var vfxAssets = GetAllVisualEffectAssets();
            foreach (var vfxAsset in vfxAssets)
            {
                if (VFXViewPreference.advancedLogs)
                    Debug.Log(string.Format("Clear VFX asset Runtime Data: {0} ({1})", vfxAsset, AssetDatabase.GetAssetPath(vfxAsset)));

                //Prevent possible automatic compilation afterwards ClearRuntimeData
                VFXExpression.ClearCache();
                vfxAsset.GetResource().GetOrCreateGraph().SetExpressionGraphDirty();
                vfxAsset.GetResource().GetOrCreateGraph().OnSaved();

                //Now effective clear runtime data
                vfxAsset.GetResource().shaderSources = new VFXShaderSourceDesc[] { }; //TODO: Should be done by ClearRuntimeData, remove this when it's fixed
                vfxAsset.GetResource().ClearRuntimeData();
            }
            AssetDatabase.SaveAssets();
        }
    }

    public class VisualEffectAssetModicationProcessor : UnityEditor.AssetModificationProcessor
    {

        public static bool HasVFXExtension(string filePath)
        {
            return filePath.EndsWith(VisualEffectResource.Extension)
                || filePath.EndsWith(VisualEffectSubgraphBlock.Extension)
                || filePath.EndsWith(VisualEffectSubgraphOperator.Extension);
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            Profiler.BeginSample("VisualEffectAssetModicationProcessor.OnWillSaveAssets");
            foreach (string path in paths.Where(t =>HasVFXExtension(t)))
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

    static class VisualEffectAssetExtensions
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

        public static VisualEffectResource GetResource<T>(this T asset) where T : VisualEffectObject
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
    }

    class VFXGraph : VFXModel
    {
        // Please add increment reason for each version below
        // 1: Size refactor
        // 2: Change some SetAttribute to spaceable slot
        public static readonly int CurrentVersion = 2;

        string shaderNamePrefix = "Hidden/VFX";
        public string GetContextShaderName(VFXContext context)
        {
            string prefix = shaderNamePrefix;
            if (context.GetData() != null)
            {
                string dataName = context.GetData().fileName;
                if (!string.IsNullOrEmpty(dataName))
                    prefix += "/" + dataName;
            }

            if (context.letter != '\0')
            {
                if (string.IsNullOrEmpty(context.label))
                    return string.Format("{2}/({0}) {1}", context.letter, libraryName, prefix);
                else
                    return string.Format("{2}/({0}) {1}", context.letter, context.label, prefix);
            }
            else
            {
                if (string.IsNullOrEmpty(context.label))
                    return string.Format("{1}/{0}", libraryName, prefix);
                else
                    return string.Format("{1}/{0}", context.label, prefix);
            }
        }
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

        public void BuildParameterInfo()
        {
            m_ParameterInfo = VFXParameterInfo.BuildParameterInfo(this);
            VisualEffectEditor.RepaintAllEditors();
        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return !(model is VFXGraph); // Can hold any model except other VFXGraph
        }

        //Temporary : Use reflection to access to StoreObjectsToByteArray (doesn't break previous behavior if editor isn't up to date)
        //TODO : Clean this when major version is released
        private static Func<ScriptableObject[], CompressionLevel, object> GetStoreObjectsFunction()
        {
            var advancedMethod = typeof(VFXMemorySerializer).GetMethod("StoreObjectsToByteArray", BindingFlags.Public | BindingFlags.Static);
            if (advancedMethod != null)
            {
                return delegate(ScriptableObject[] objects, CompressionLevel level)
                {
                    return advancedMethod.Invoke(null, new object[] { objects, level }) as byte[];
                };
            }

            return delegate(ScriptableObject[] objects, CompressionLevel level)
            {
                return VFXMemorySerializer.StoreObjects(objects) as object;
            };
        }

        private static Func<object, bool, ScriptableObject[]> GetExtractObjectsFunction()
        {
            var advancedMethod = typeof(VFXMemorySerializer).GetMethod("ExtractObjects", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(byte[]), typeof(bool) }, null);
            if (advancedMethod != null)
            {
                return delegate(object objects, bool asCopy)
                {
                    return advancedMethod.Invoke(null, new object[] { objects as byte[], asCopy }) as ScriptableObject[];
                };
            }

            return delegate(object objects, bool asCopy)
            {
                return VFXMemorySerializer.ExtractObjects(objects as string, asCopy);
            };
        }

        private static readonly Func<ScriptableObject[], CompressionLevel, object> k_fnStoreObjects = GetStoreObjectsFunction();
        private static readonly Func<object, bool, ScriptableObject[]> k_fnExtractObjects = GetExtractObjectsFunction();

        public object Backup()
        {
            Profiler.BeginSample("VFXGraph.Backup");
            var dependencies = new HashSet<ScriptableObject>();

            dependencies.Add(this);
            CollectDependencies(dependencies);

            var result = k_fnStoreObjects(dependencies.Cast<ScriptableObject>().ToArray(), CompressionLevel.Fastest);

            Profiler.EndSample();

            return result;
        }

        public void Restore(object str)
        {
            Profiler.BeginSample("VFXGraph.Restore");
            var scriptableObject = k_fnExtractObjects(str, false);

            Profiler.BeginSample("VFXGraph.Restore SendUnknownChange");
            foreach (var model in scriptableObject.OfType<VFXModel>())
            {
                model.OnUnknownChange();
            }
            Profiler.EndSample();
            Profiler.EndSample();
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
                EditorUtility.DisplayProgressBar("Saving...", "Rebuild", 0);
                RecompileIfNeeded(false,true);
                m_saved = true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Save failed : {0}", e);
            }
            EditorUtility.ClearProgressBar();
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

            m_GraphSanitized = true;
            m_GraphVersion = CurrentVersion;
            UpdateSubAssets(); //Should not be necessary : force remove no more referenced object from asset
        }

        public void ClearCompileData()
        {
            m_CompiledData = null;


            m_ExpressionValuesDirty = true;
        }

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
            base.OnInvalidate(model, cause);

            if (model is VFXParameter || model is VFXSlot && (model as VFXSlot).owner is VFXParameter)
            {
                BuildParameterInfo();
            }

            if (cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                UpdateSubAssets();
                if( model == this)
                    VFXSubgraphContext.CallOnGraphChanged(this);
            }

            if( cause == VFXModel.InvalidationCause.kSettingChanged && model is VFXParameter)
            {
                VFXSubgraphContext.CallOnGraphChanged(this);
            }

            if (cause != VFXModel.InvalidationCause.kExpressionInvalidated &&
                cause != VFXModel.InvalidationCause.kExpressionGraphChanged)
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
            RecompileIfNeeded(false,true);
            return compiledData.FindReducedExpressionIndexFromSlotCPU(slot);
        }

        public void SetCompilationMode(VFXCompilationMode mode)
        {
            if (m_CompilationMode != mode)
            {
                m_CompilationMode = mode;
                SetExpressionGraphDirty();
                RecompileIfNeeded(false, true);
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
                    RecompileIfNeeded(false, true);
                }
            }
        }

        public void SetExpressionGraphDirty()
        {
            m_ExpressionGraphDirty = true;
            m_DependentDirty = true;
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
            RecurseBuildDependencies(explored,children);
        }

        void RecurseBuildDependencies(HashSet<VisualEffectObject> explored,IEnumerable<VFXModel> models)
        {
            foreach(var model in models)
            {
                if( model is VFXSubgraphContext)
                {
                    var subgraphContext = model as VFXSubgraphContext;

                    if (subgraphContext.subgraph != null && !explored.Contains(subgraphContext.subgraph))
                    {
                        explored.Add(subgraphContext.subgraph);
                        m_SubgraphDependencies.Add(subgraphContext.subgraph);
                        RecurseBuildDependencies(explored, subgraphContext.subgraph.GetResource().GetOrCreateGraph().children);
                    }
                }
                else if( model is VFXSubgraphOperator)
                {
                    var subgraphOperator = model as VFXSubgraphOperator;

                    if (subgraphOperator.subgraph != null && !explored.Contains(subgraphOperator.subgraph))
                    {
                        explored.Add(subgraphOperator.subgraph);
                        m_SubgraphDependencies.Add(subgraphOperator.subgraph);
                        RecurseBuildDependencies(explored, subgraphOperator.subgraph.GetResource().GetOrCreateGraph().children);
                    }
                }
                else if( model is VFXContext)
                {
                    foreach( var block in (model as VFXContext).children)
                    {
                        if( block is VFXSubgraphBlock)
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

        void RecurseSubgraphRecreateCopy(VFXGraph graph)
        {
            foreach (var child in graph.children)
            {
                if (child is VFXSubgraphContext)
                {
                    var subgraphContext = child as VFXSubgraphContext;
                    if( subgraphContext.subgraph != null)
                        RecurseSubgraphRecreateCopy(subgraphContext.subgraph.GetResource().GetOrCreateGraph());
                    subgraphContext.RecreateCopy();
                }
                else if(child is VFXContext)
                {
                    foreach( var block in child.children)
                    {
                        if( block is VFXSubgraphBlock)
                        {

                            var subgraphBlock = block as VFXSubgraphBlock;
                            if (subgraphBlock.subgraph != null)
                                RecurseSubgraphRecreateCopy(subgraphBlock.subgraph.GetResource().GetOrCreateGraph());
                            subgraphBlock.RecreateCopy();
                        }
                    }
                }
            }
        }

        void SubgraphDirty(VisualEffectObject subgraph)
        {
            if (m_SubgraphDependencies != null && m_SubgraphDependencies.Contains(subgraph))
            {
                RecurseSubgraphRecreateCopy(this);
                compiledData.Compile(m_CompilationMode, m_ForceShaderValidation);
                m_ExpressionGraphDirty = false;

                m_ExpressionValuesDirty = false;
            }
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

        public void ComputeDataIndices()
        {
            VFXContext[] directContexts = children.OfType<VFXContext>().ToArray();

            HashSet<ScriptableObject> dependencies = new HashSet<ScriptableObject>();
            CollectDependencies(dependencies, false);

            VFXContext[] allContexts = dependencies.OfType<VFXContext>().ToArray();

            IEnumerable<VFXData> datas = allContexts.Select(t => t.GetData()).Where(t => t != null).Distinct().OrderBy(t => directContexts.Contains(t.owners.First()) ? 0 : 1);

            int cpt = 1;
            foreach (var data in datas)
            {
                data.index = cpt++;
            }
        }

        public void RecompileIfNeeded(bool preventRecompilation = false, bool preventDependencyRecompilation = false)
        {
            SanitizeGraph();

            if (! GetResource().isSubgraph)
            {
                bool considerGraphDirty = m_ExpressionGraphDirty && !preventRecompilation;
                if (considerGraphDirty)
                {
                    BuildSubgraphDependencies();
                    RecurseSubgraphRecreateCopy(this);

                    ComputeDataIndices();

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
            else if(m_ExpressionGraphDirty && !preventRecompilation)
            {
                BuildSubgraphDependencies();
                RecurseSubgraphRecreateCopy(this);
                m_ExpressionGraphDirty = false;
            }
            if(!preventDependencyRecompilation && m_DependentDirty)
            {
                if (m_DependentDirty)
                {
                    var obj = GetResource().visualEffectObject;
                    foreach (var graph in GetAllGraphs<VisualEffectAsset>())
                    {
                        graph.SubgraphDirty(obj);
                    }
                    m_DependentDirty = false;
                }
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

        private VisualEffectResource m_Owner;
    }
}
