//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Profiling;
using System.Reflection;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    public class VFXCacheManager : EditorWindow
    {
        [MenuItem("Edit/Visual Effects//Rebuild All Visual Effect Graphs", priority = 320)]
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
                if (VFXViewPreference.advancedLogs)
                    Debug.Log(string.Format("Recompile VFX asset: {0} ({1})", vfxAsset, AssetDatabase.GetAssetPath(vfxAsset)));

                VFXExpression.ClearCache();
                vfxAsset.GetResource().GetOrCreateGraph().SetExpressionGraphDirty();
                vfxAsset.GetResource().GetOrCreateGraph().OnSaved();
            }
            AssetDatabase.SaveAssets();
        }
    }

    public class VisualEffectAssetModicationProcessor : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            Profiler.BeginSample("VisualEffectAssetModicationProcessor.OnWillSaveAssets");
            foreach (string path in paths.Where(t => t.EndsWith(".vfx")))
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

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (assetPath.EndsWith(".vfx"))
            {
                VisualEffectResource.DeleteAtPath(assetPath);
            }

            return AssetDeleteResult.DidNotDelete;
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

        public static VisualEffectResource GetResource(this VisualEffectAsset asset)
        {
            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(AssetDatabase.GetAssetPath(asset));

            if (resource == null)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                resource = VisualEffectResource.GetResourceAtPath(assetPath);
                if (resource == null)
                {
                    resource = new VisualEffectResource();
                    resource.SetAssetPath(assetPath);
                }
            }
            return resource;
        }
    }

    class VFXGraph : VFXModel
    {
        // Please add increment reason for each version below
        // size refactor
        public static readonly int CurrentVersion = 1;

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

                visualEffectResource.SetContents(currentObjects.Cast<Object>().ToArray());
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
            }

            if (cause != VFXModel.InvalidationCause.kExpressionInvalidated &&
                cause != VFXModel.InvalidationCause.kExpressionGraphChanged)
            {
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

        public void SetCompilationMode(VFXCompilationMode mode)
        {
            if (m_CompilationMode != mode)
            {
                m_CompilationMode = mode;
                SetExpressionGraphDirty();
                RecompileIfNeeded();
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
                    RecompileIfNeeded();
                }
            }
        }

        public void SetExpressionGraphDirty()
        {
            m_ExpressionGraphDirty = true;
        }

        public void SetExpressionValueDirty()
        {
            m_ExpressionValuesDirty = true;
        }

        public void RecompileIfNeeded(bool preventRecompilation = false)
        {
            SanitizeGraph();

            bool considerGraphDirty = m_ExpressionGraphDirty && !preventRecompilation;
            if (considerGraphDirty)
            {
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
        private int m_GraphVersion = 0;

        [NonSerialized]
        private bool m_GraphSanitized = false;
        [NonSerialized]
        private bool m_ExpressionGraphDirty = true;
        [NonSerialized]
        private bool m_ExpressionValuesDirty = true;

        [NonSerialized]
        private VFXGraphCompiledData m_CompiledData;
        private VFXCompilationMode m_CompilationMode = VFXCompilationMode.Runtime;
        private bool m_ForceShaderValidation = false;

        [SerializeField]
        protected bool m_saved = false;

        [Serializable]
        public struct CustomAttribute
        {
            public string name;
            public VFXValueType type;
        }

        [SerializeField]
        List<CustomAttribute> m_CustomAttributes;


        public IEnumerable<string> customAttributes
        {
            get { return m_CustomAttributes.Select(t=>t.name); }
        }

        public int GetCustomAttributeCount()
        {
            return m_CustomAttributes != null ? m_CustomAttributes.Count : 0;
        }


        public bool HasCustomAttribute(string name)
        {
            return m_CustomAttributes.Any(t => t.name == name);
        }

        public string GetCustomAttributeName(int index)
        {
            return m_CustomAttributes[index].name;
        }

        public VFXValueType GetCustomAttributeType(string name)
        {
            return m_CustomAttributes.FirstOrDefault(t => t.name == name).type;
        }

        public VFXValueType GetCustomAttributeType(int index)
        {
            return m_CustomAttributes[index].type;
        }

        public void SetCustomAttributeName(int index,string newName)
        {
            if (index >= m_CustomAttributes.Count)
                throw new System.ArgumentException("Invalid Index");
            if (m_CustomAttributes.Any(t => t.name == newName) || VFXAttribute.AllIncludingVariadic.Any(t => t == newName))
            {
                newName = "Attribute";
                int cpt = 1;
                while (m_CustomAttributes.Select((t, i) => t.name == name && i != index).Where(t => t).Count() > 0)
                {
                    newName = string.Format("Attribute{0}", cpt++);
                }
            }


            string oldName = m_CustomAttributes[index].name;

            m_CustomAttributes[index] = new CustomAttribute { name = newName, type = m_CustomAttributes[index].type };

            Invalidate(InvalidationCause.kSettingChanged);

            RenameAttribute(oldName, newName);
        }

        public void SetCustomAttributeType(int index, VFXValueType newType)
        {
            if (index >= m_CustomAttributes.Count)
                throw new System.ArgumentException("Invalid Index");
            //TODO check that newType is an anthorized type for custom attributes.

            m_CustomAttributes[index] = new CustomAttribute { name = m_CustomAttributes[index].name, type = newType };

            string name = m_CustomAttributes[index].name;
            ForEachSettingUsingAttribute((model, setting) =>
            {
                if (name == (string)setting.GetValue(model))
                    model.Invalidate(InvalidationCause.kSettingChanged);
                return false;
            });

            Invalidate(InvalidationCause.kSettingChanged);
        }

        public void AddCustomAttribute()
        {
            if (m_CustomAttributes == null)
                m_CustomAttributes = new List<CustomAttribute>();
            string name = "Attribute";
            int cpt = 1;
            while (m_CustomAttributes.Any(t => t.name == name))
            {   
                name = string.Format("Attribute{0}", cpt++);
            }
            m_CustomAttributes.Add(new CustomAttribute { name = name, type = VFXValueType.Float });
            Invalidate(InvalidationCause.kSettingChanged);
        }

        //Execute action on each settings used to store an attribute, until one return true;
        bool ForEachSettingUsingAttributeInModel(VFXModel model, Func<FieldInfo,bool> action)
        {
            var settings = model.GetSettings(true);

            foreach (var setting in settings)
            {
                if (setting.FieldType == typeof(string))
                {
                    var attribute = setting.GetCustomAttributes().OfType<StringProviderAttribute>().FirstOrDefault();
                    if (attribute != null && (typeof(ReadWritableAttributeProvider).IsAssignableFrom(attribute.providerType) || typeof(AttributeProvider).IsAssignableFrom(attribute.providerType)))
                    {
                        if (action(setting))
                            return true;
                    }
                }
            }

            return false;
        }
        bool ForEachSettingUsingAttribute(Func<VFXModel,FieldInfo, bool> action)
        {
            foreach (var child in children)
            {
                if (child is VFXOperator)
                {
                    if (ForEachSettingUsingAttributeInModel(child, s => action(child, s)))
                        return true;
                }
                else if (child is VFXContext)
                {
                    if (ForEachSettingUsingAttributeInModel(child, s => action(child, s)))
                        return true;
                    foreach (var block in (child as VFXContext).children)
                    {
                        if (ForEachSettingUsingAttributeInModel(block, s => action(block, s)))
                            return true;
                    }
                }
            }

            return false;
        }

        public bool HasCustomAttributeUses(string name)
        {
            return ForEachSettingUsingAttribute((model,setting)=> name == (string)setting.GetValue(model));
        }

        public void RenameAttribute(string oldName,string newName)
        {
            ForEachSettingUsingAttribute((model, setting) =>
            {
                if (oldName == (string)setting.GetValue(model))
                {
                    setting.SetValue(model, newName);
                    model.Invalidate(InvalidationCause.kSettingChanged);
                }
                return false;
            });
        }

        public void RemoveCustomAttribute(int index)
        {
            if (index >= m_CustomAttributes.Count)
                throw new System.ArgumentException("Invalid Index");

            var modelUsingAttributes = new List<VFXModel>();

            string name = GetCustomAttributeName(index);

            ForEachSettingUsingAttribute((model, setting) =>
            {
                if (name == (string)setting.GetValue(model))
                    modelUsingAttributes.Add(model);
                return false;
            });

            foreach(var model in modelUsingAttributes)
            {
                model.GetParent().RemoveChild(model);
            }

            m_CustomAttributes.RemoveAt(index);
            Invalidate(InvalidationCause.kSettingChanged);
        }


        public void MoveCustomAttribute(int movedIndex,int destinationIndex)
        {
            if (movedIndex >= m_CustomAttributes.Count || destinationIndex >= m_CustomAttributes.Count)
                throw new System.ArgumentException("Invalid Index");
            
            var attr = m_CustomAttributes[movedIndex];
            m_CustomAttributes.RemoveAt(movedIndex);
            if (movedIndex < destinationIndex)
                movedIndex--;
            m_CustomAttributes.Insert(destinationIndex, attr);
            Invalidate(InvalidationCause.kUIChanged);
        }

        public bool saved { get { return m_saved; } }

        private VisualEffectResource m_Owner;
    }
}
