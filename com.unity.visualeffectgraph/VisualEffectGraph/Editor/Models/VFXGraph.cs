//#define USE_SHADER_AS_SUBASSET
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.Profiling;
using System.Reflection;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
#if !USE_SHADER_AS_SUBASSET
    public class VFXCacheManager : EditorWindow
    {
        //[MenuItem("VFX Editor/Clear VFXCache")]
        public static void Clear()
        {
            FileUtil.DeleteFileOrDirectory(VFXGraphCompiledData.baseCacheFolder);
        }

        //[MenuItem("VFX Editor/Build VFXCache")]
        public static void Build()
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

            foreach (var vfxAsset in vfxAssets)
            {
                Debug.Log(string.Format("Recompile VFX asset: {0} ({1})", vfxAsset, AssetDatabase.GetAssetPath(vfxAsset.GetInstanceID())));
                vfxAsset.GetOrCreateGraph().SetExpressionGraphDirty();
                vfxAsset.GetOrCreateGraph().OnSaved();
            }
            AssetDatabase.SaveAssets();
        }
    }
#endif


    public class VisualEffectAssetPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (asset != null)
                {
                    asset.GetOrCreateGraph();
                }
            }
        }
    }

    public class VisualEffectAssetModicationProcessor : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            Profiler.BeginSample("VisualEffectAssetModicationProcessor.OnWillSaveAssets");
            foreach (string path in paths)
            {
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                if (vfxAsset != null)
                {
                    var graph = vfxAsset.GetOrCreateGraph();
                    graph.OnSaved();
                }
            }
            Profiler.EndSample();
            return paths;
        }
    }

    static class VisualEffectAssetExtensions
    {
        public static VFXGraph GetOrCreateGraph(this VisualEffectAsset asset)
        {
            ScriptableObject g = asset.graph;
            if (g == null)
            {
                g = ScriptableObject.CreateInstance<VFXGraph>();
                g.name = "VFXGraph";
                asset.graph = g;
                g.hideFlags |= HideFlags.HideInHierarchy;
                ((VFXGraph)g).UpdateSubAssets();
            }

            VFXGraph graph = (VFXGraph)g;
            graph.visualEffectAsset = asset;
            return graph;
        }

        public static void UpdateSubAssets(this VisualEffectAsset asset)
        {
            asset.GetOrCreateGraph().UpdateSubAssets();
        }
    }

    class VFXGraph : VFXModel
    {
        public VisualEffectAsset visualEffectAsset
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


        [Serializable]
        public struct ParameterInfo
        {
            public ParameterInfo(string exposedName)
            {
                name = exposedName;
                path = null;
                min = Mathf.NegativeInfinity;
                max = Mathf.Infinity;
                descendantCount = 0;
                sheetType = null;
            }
            public string name;
            public string path;

            public string sheetType;

            public float min;
            public float max;

            public int descendantCount;
        }

        public ParameterInfo[] m_ParameterInfo;

        public void BuildParameterInfo()
        {
            var parameters = children.OfType<VFXParameter>().Where(t => t.exposed).OrderBy(t => t.order).ToArray();

            List<ParameterInfo> infos = new List<ParameterInfo>();
            List<ParameterInfo> subList = new List<ParameterInfo>();
            foreach( var parameter in parameters)
            {
                string rootFieldName = VisualEffectUtility.GetTypeField(parameter.type);
                
                ParameterInfo paramInfo = new ParameterInfo(parameter.exposedName);
                if( rootFieldName != null)
                {
                    paramInfo.sheetType = rootFieldName;
                    paramInfo.path = paramInfo.name;
                    if (parameter.hasRange)
                    {
                        float min = (float)System.Convert.ChangeType(parameter.m_Min.Get(), typeof(float));
                        float max = (float)System.Convert.ChangeType(parameter.m_Max.Get(), typeof(float));
                        paramInfo.min = min;
                        paramInfo.max = max;
                    }
                    paramInfo.descendantCount = 0;
                }
                else
                {
                    paramInfo.descendantCount = RecurseBuildParameterInfo(subList,parameter.type,parameter.exposedName);
                }

                
                
                infos.Add(paramInfo);
                infos.AddRange(subList);
                subList.Clear();
            }
            m_ParameterInfo = infos.ToArray();
        }
        int RecurseBuildParameterInfo(List<ParameterInfo> infos,System.Type type, string path)
        {
            int count = 0;
            if (type.IsValueType)
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

                List<ParameterInfo> subList = new List<ParameterInfo>();
                foreach (var field in fields)
                {
                    ParameterInfo info = new ParameterInfo(field.Name);

                    info.path = path + "_" + field.Name;

                    string fieldName = VisualEffectUtility.GetTypeField(field.FieldType);

                    if (fieldName != null)
                    {
                        info.sheetType = fieldName;
                        RangeAttribute attr = field.GetCustomAttributes(true).OfType<RangeAttribute>().FirstOrDefault();
                        if( attr != null)
                        {
                            info.min = attr.min;
                            info.max = attr.max;
                        }
                        info.descendantCount = 0;
                        count++;
                    }
                    else
                    {
                        if( field.FieldType.IsEnum) // For space
                            continue;
                        info.descendantCount = RecurseBuildParameterInfo(subList, field.FieldType,info.path);
                    }
                    infos.Add(info);
                    infos.AddRange(subList);
                    subList.Clear();
                    count += info.descendantCount;
                }
            }
            return count;
        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return !(model is VFXGraph); // Can hold any model except other VFXGraph
        }

        public string Backup()
        {
            Profiler.BeginSample("VFXGraph.Backup");
            var dependencies = new HashSet<ScriptableObject>();

            dependencies.Add(this);
            CollectDependencies(dependencies);


            var result = VFXMemorySerializer.StoreObjects(dependencies.Cast<ScriptableObject>().ToArray());

            Profiler.EndSample();

            return result;
        }

        public void Restore(string str)
        {
            Profiler.BeginSample("VFXGraph.Restore");
            var scriptableObject = VFXMemorySerializer.ExtractObjects(str, false);

            Profiler.BeginSample("VFXGraph.Restore SendUnknownChange");
            foreach (var model in scriptableObject.OfType<VFXModel>())
            {
                model.OnUnknownChange();
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs)
        {
            Profiler.BeginSample("VFXEditor.CollectDependencies");
            try
            {
                if (m_UIInfos != null)
                    objs.Add(m_UIInfos);
                base.CollectDependencies(objs);
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
                RecompileIfNeeded();
                float currentStep = 0;

#if USE_SHADER_AS_SUBASSET
                float stepCount = m_GeneratedComputeShader.Count + m_GeneratedShader.Count + 1;

                var oldComputeShader = m_GeneratedComputeShader.ToArray();
                var oldShader = m_GeneratedShader.ToArray();
                var oldPath = oldComputeShader.Select(o => AssetDatabase.GetAssetPath(o)).Concat(oldShader.Select(o => AssetDatabase.GetAssetPath(o))).ToArray();

                m_GeneratedComputeShader.Clear();
                m_GeneratedShader.Clear();

                for (int i = 0; i < oldComputeShader.Length; ++i)
                {
                    var compute = oldComputeShader[i];
                    EditorUtility.DisplayProgressBar("Saving...", string.Format("ComputeShader embedding {0}/{1}", i, oldComputeShader.Length), (++currentStep) / stepCount);
                    var computeShaderCopy = Instantiate<ComputeShader>(compute);
                    DestroyImmediate(compute, true);
                    m_GeneratedComputeShader.Add(computeShaderCopy);
                }

                for (int i = 0; i < oldShader.Length; ++i)
                {
                    var shader = oldShader[i];
                    EditorUtility.DisplayProgressBar("Saving...", string.Format("Shader embedding {0}/{1}", i, oldShader.Length), (++currentStep) / stepCount);
                    var shaderCopy = Instantiate<Shader>(shader);
                    DestroyImmediate(shader, true);
                    m_GeneratedShader.Add(shaderCopy);
                }
#else
                float stepCount = 1;
#endif

                // hide all sub assets
                var assets = allAssets;
                foreach (var asset in assets)
                {
                    asset.hideFlags |= HideFlags.HideInHierarchy;
                }
                hideFlags |= HideFlags.HideInHierarchy;

                EditorUtility.DisplayProgressBar("Saving...", "UpdateSubAssets", (++currentStep) / stepCount);
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
                    model.Sanitize(); // This can modify dependencies but newly created model are supposed safe so we dont care about retrieving new dependencies
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
        }

        IEnumerable<Object> allAssets
        {
            get {return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)).Where(o => o is VFXModel || o is ComputeShader || o is Shader || o is VFXUI); }
        }

        public  bool displaySubAssets
        {
            get {return (hideFlags & HideFlags.HideInHierarchy) == 0; }
            set
            {
                var persistentAssets = allAssets;

                if (value)
                {
                    hideFlags &= ~HideFlags.HideInHierarchy;
                }
                else
                {
                    hideFlags |= HideFlags.HideInHierarchy;
                }

                foreach (var asset in persistentAssets)
                {
                    if (value)
                    {
                        asset.hideFlags &= ~HideFlags.HideInHierarchy;
                    }
                    else
                    {
                        asset.hideFlags |= HideFlags.HideInHierarchy;
                    }
                }

                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(visualEffectAsset));
            }
        }


        public void ClearCompileData()
        {
            m_CompiledData = null;


            m_ExpressionValuesDirty = true;
        }

        public bool UpdateSubAssets()
        {
            bool modified = false;

            if (!EditorUtility.IsPersistent(this) && (this.visualEffectAsset != null && EditorUtility.IsPersistent(this.visualEffectAsset)))
            {
                string assetPath = AssetDatabase.GetAssetPath(this.visualEffectAsset);
                AssetDatabase.AddObjectToAsset(this, assetPath);
            }

            if (EditorUtility.IsPersistent(this))
            {
                Profiler.BeginSample("VFXEditor.UpdateSubAssets");

                try
                {
                    var persistentObjects = new HashSet<Object>(allAssets);
                    persistentObjects.Remove(this);

                    var currentObjects = new HashSet<ScriptableObject>();
                    CollectDependencies(currentObjects);

#if USE_SHADER_AS_SUBASSET
                    if (m_GeneratedComputeShader != null)
                    {
                        foreach (var compute in m_GeneratedComputeShader)
                        {
                            currentObjects.Add(compute);
                        }
                    }

                    if (m_GeneratedShader != null)
                    {
                        foreach (var shader in m_GeneratedShader)
                        {
                            currentObjects.Add(shader);
                        }
                    }
#endif

                    if (m_UIInfos != null)
                        currentObjects.Add(m_UIInfos);
                    // Add sub assets that are not already present
                    foreach (var obj in currentObjects)
                        if (!persistentObjects.Contains(obj))
                        {
                            obj.name = obj.GetType().Name;
                            AssetDatabase.AddObjectToAsset(obj, this);
                            obj.hideFlags = hideFlags;
                            modified = true;
                        }

                    // Remove sub assets that are not referenced anymore
                    foreach (var obj in persistentObjects)
                        if (obj is ScriptableObject && !currentObjects.Contains(obj as ScriptableObject))
                        {
                            AssetDatabase.RemoveObjectFromAsset(obj);
                            modified = true;
                        }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
                finally
                {
                    Profiler.EndSample();
                }

                if (modified)
                    EditorUtility.SetDirty(this);
            }

            return modified;
        }

        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            m_saved = false;
            base.OnInvalidate(model, cause);

            if (model is VFXParameter)
            {
                BuildParameterInfo();
            }

            if (cause == VFXModel.InvalidationCause.kStructureChanged)
            {
                //Debug.Log("UPDATE SUB ASSETS");
                if (UpdateSubAssets())
                {
                    //AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
                }
            }

            if (cause != VFXModel.InvalidationCause.kExpressionInvalidated &&
                cause != VFXModel.InvalidationCause.kExpressionGraphChanged)
            {
                //Debug.Log("ASSET DIRTY " + cause);
                EditorUtility.SetDirty(this);
            }

            if (cause == VFXModel.InvalidationCause.kExpressionGraphChanged)
            {
                m_ExpressionGraphDirty = true;
            }

            if (cause == VFXModel.InvalidationCause.kParamChanged)
            {
                m_ExpressionValuesDirty = true;
            }
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            RecompileIfNeeded();
            return compiledData.FindReducedExpressionIndexFromSlotCPU(slot);
        }

        public void SetExpressionGraphDirty()
        {
            m_ExpressionGraphDirty = true;
        }

        public void RecompileIfNeeded(bool preventRecompilation = false)
        {
            SanitizeGraph();

            bool considerGraphDirty = m_ExpressionGraphDirty && !preventRecompilation;
            if (considerGraphDirty)
            {
                compiledData.Compile();
            }
            else if (m_ExpressionValuesDirty && !m_ExpressionGraphDirty)
            {
                compiledData.UpdateValues();
            }

            if (considerGraphDirty || m_ExpressionValuesDirty)
            {
                foreach (var component in VFXManager.GetComponents())
                {
                    if (component.visualEffectAsset == compiledData.visualEffectAsset)
                    {
                        component.SetVisualEffectAssetDirty(considerGraphDirty);
                    }
                }
            }

            if (considerGraphDirty)
                m_ExpressionGraphDirty = false;
            m_ExpressionValuesDirty = false;
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

        [NonSerialized]
        private bool m_GraphSanitized = false;
        [NonSerialized]
        private bool m_ExpressionGraphDirty = true;
        [NonSerialized]
        private bool m_ExpressionValuesDirty = true;

        [NonSerialized]
        private VFXGraphCompiledData m_CompiledData;

        [SerializeField]
        protected bool m_saved = false;

        public bool saved { get { return m_saved; } }

        private VisualEffectAsset m_Owner;
    }
}
