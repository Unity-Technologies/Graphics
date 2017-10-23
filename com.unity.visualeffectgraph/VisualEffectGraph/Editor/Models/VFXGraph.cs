//#define USE_SHADER_AS_SUBASSET
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
#if !USE_SHADER_AS_SUBASSET
    public class VFXCacheManager : EditorWindow
    {
        [MenuItem("VFX Editor/Rebuild VFXCache")]
        public static void Rebuild()
        {
            FileUtil.DeleteFileOrDirectory(VFXGraphCompiledData.baseCacheFolder);
            var vfxAssets = new List<VFXAsset>();
            var vfxAssetsGuid = AssetDatabase.FindAssets("t:VFXAsset");
            foreach (var guid in vfxAssetsGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VFXAsset>(assetPath);
                if (vfxAsset != null)
                {
                    vfxAssets.Add(vfxAsset);
                }
            }

            foreach (var vfxAsset in vfxAssets)
            {
                vfxAsset.GetOrCreateGraph().OnSaved();
            }
            AssetDatabase.SaveAssets();
        }
    }
#endif

    public class VFXAssetModicationProcessor : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VFXAsset>(path);
                if (vfxAsset != null)
                {
                    var graph = vfxAsset.GetOrCreateGraph();
                    graph.OnSaved();
                }
            }
            return paths;
        }
    }

    static class VFXAssetExtensions
    {
        public static VFXGraph GetOrCreateGraph(this VFXAsset asset)
        {
            ScriptableObject g = asset.graph;
            if (g == null)
            {
                g = ScriptableObject.CreateInstance<VFXGraph>();
                g.name = "VFXGraph";
                asset.graph = g;
            }

            VFXGraph graph = (VFXGraph)g;
            graph.vfxAsset = asset;
            return graph;
        }

        public static void UpdateSubAssets(this VFXAsset asset)
        {
            asset.GetOrCreateGraph().UpdateSubAssets();
        }
    }

    class VFXGraph : VFXModel
    {
        public VFXAsset vfxAsset
        {
            get
            {
                return m_Owner;
            }
            set
            {
                m_Owner = value;
                m_ExpressionGraphDirty = true;
            }
        }

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return !(model is VFXGraph); // Can hold any model except other VFXGraph
        }

        public override T Clone<T>()
        {
            Profiler.BeginSample("VFXEditor.CloneGraph");
            try
            {
                var from = children.ToArray();
                var copy = from.Select(o => o.Clone<VFXModel>()).ToArray();
                VFXSlot.ReproduceLinkedSlotFromHierachy(from, copy);
                VFXContext.ReproduceLinkedFlowFromHiearchy(from, copy);

                var clone = CreateInstance(GetType()) as VFXGraph;
                clone.m_Children = new List<VFXModel>();
                foreach (var model in copy)
                {
                    clone.AddChild(model, -1, false);
                }
                return clone as T;
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        public override void CollectDependencies(HashSet<Object> objs)
        {
            Profiler.BeginSample("VFXEditor.CollectDependencies");
            try
            {
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
                m_ExpressionGraphDirty = true;
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
                EditorUtility.DisplayProgressBar("Saving...", "UpdateSubAssets", (++currentStep) / stepCount);
                UpdateSubAssets();
                m_saved = true;
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Save failed : {0}", e);
            }
            EditorUtility.ClearProgressBar();
        }

        public bool UpdateSubAssets()
        {
            bool modified = false;
            if (EditorUtility.IsPersistent(this))
            {
                Profiler.BeginSample("VFXEditor.UpdateSubAssets");

                try
                {
                    var persistentObjects = new HashSet<Object>(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)).Where(o => o is VFXModel || o is ComputeShader || o is Shader));
                    persistentObjects.Remove(this);

                    var currentObjects = new HashSet<Object>();
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

                    // Add sub assets that are not already present
                    foreach (var obj in currentObjects)
                        if (!persistentObjects.Contains(obj))
                        {
                            obj.name = obj.GetType().Name;
                            AssetDatabase.AddObjectToAsset(obj, this);
                            modified = true;
                        }

                    // Remove sub assets that are not referenced anymore
                    foreach (var obj in persistentObjects)
                        if (!currentObjects.Contains(obj))
                        {
                            AssetDatabase.RemoveObject(obj);
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
                foreach (var component in VFXComponent.GetAllActive())
                {
                    if (component.vfxAsset == compiledData.vfxAsset)
                    {
                        component.SetVfxAssetDirty(considerGraphDirty);
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
        private bool m_ExpressionGraphDirty = true;
        [NonSerialized]
        private bool m_ExpressionValuesDirty = true;

        [NonSerialized]
        private VFXGraphCompiledData m_CompiledData;

        [SerializeField]
        protected bool m_saved = false;

        public bool saved { get { return m_saved; } }

        private VFXAsset m_Owner;
    }
}
