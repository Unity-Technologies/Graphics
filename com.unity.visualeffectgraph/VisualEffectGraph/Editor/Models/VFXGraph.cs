//#define USE_SHADER_AS_SUBASSET
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
#if !USE_SHADER_AS_SUBASSET
    public class VFXCacheManager : EditorWindow
    {
        //[MenuItem("VFX Editor/Build All VFXs")]
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
                Debug.Log(string.Format("Recompile VFX asset: {0} ({1})", vfxAsset, AssetDatabase.GetAssetPath(vfxAsset)));
                vfxAsset.GetResource().GetOrCreateGraph().SetExpressionGraphDirty();
                vfxAsset.GetResource().GetOrCreateGraph().OnSaved();
            }
            AssetDatabase.SaveAssets();
        }
    }
#endif

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
            ScriptableObject g = resource.graph;
            if (g == null)
            {
                string assetPath = AssetDatabase.GetAssetPath(resource);
                AssetDatabase.ImportAsset(assetPath);

                g = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<VFXGraph>().FirstOrDefault();
            }

            if (g == null)
            {
                g = ScriptableObject.CreateInstance<VFXGraph>();
                g.name = "VFXGraph";
                resource.graph = g;
                g.hideFlags |= HideFlags.HideInHierarchy;
                ((VFXGraph)g).UpdateSubAssets();
            }

            VFXGraph graph = (VFXGraph)g;
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
                float stepCount = 1;
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

            if (m_UIInfos != null)
                try
                {
                    m_UIInfos.Sanitize(this);
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while sanitizing VFXUI: : {0}", e.StackTrace));
                }

            m_GraphSanitized = true;
        }

        public void ClearCompileData()
        {
            m_CompiledData = null;


            m_ExpressionValuesDirty = true;
        }

        public bool UpdateSubAssets()
        {
            bool modified = false;

            Profiler.BeginSample("VFXEditor.UpdateSubAssets");

            try
            {
                var currentObjects = new HashSet<ScriptableObject>();
                currentObjects.Add(this);
                CollectDependencies(currentObjects);

                if (m_UIInfos != null)
                    currentObjects.Add(m_UIInfos);

                // Add sub assets that are not already present
                foreach (var obj in currentObjects)
                {
                    if (obj.hideFlags != hideFlags)
                    {
                        obj.hideFlags = hideFlags;
                        modified = true;
                    }
                }

                visualEffectResource.SetDependencies(currentObjects.Cast<Object>().ToArray());
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

            return modified;
        }

        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            m_saved = false;
            base.OnInvalidate(model, cause);

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
                var vfxAsset = compiledData.visualEffectResource.asset;
                foreach (var component in VFXManager.GetComponents())
                {
                    if (component.visualEffectAsset == vfxAsset)
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

        private VisualEffectResource m_Owner;
    }
}
