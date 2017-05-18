using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Serializable]
    class VFXGraphAsset : ScriptableObject
    {
        public VFXGraph root { get { return m_Root; } }

        [SerializeField]
        private VFXGraph m_Root;

        public bool UpdateSubAssets()
        {
            bool modified = false;
            if (EditorUtility.IsPersistent(this))
            {
                Profiler.BeginSample("UpdateSubAssets");

                try
                {
                    HashSet<Object> persistentObjects = new HashSet<Object>(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)));
                    persistentObjects.Remove(this);

                    HashSet<Object> currentObjects = new HashSet<Object>();
                    m_Root.CollectDependencies(currentObjects);
                    currentObjects.Add(m_Root);

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

                Profiler.EndSample();

                if (modified)
                    EditorUtility.SetDirty(this);
            }

            return modified;
        }

        private void OnModelInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
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
        }

        void OnEnable()
        {
            if (m_Root == null)
                m_Root = ScriptableObject.CreateInstance<VFXGraph>();
            m_Root.onInvalidateDelegate += OnModelInvalidate;
        }

        public void RecompileIfNeeded()
        {
            if (m_ExpressionGraphDirty)
            {
                try
                {
                    if (m_Root != null)
                    {
                        var expressionGraph = new VFXExpressionGraph();
                        expressionGraph.CompileExpressions(m_Root, false);
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError(string.Format("Exception while compiling expression graph: {0}: {1}", e, e.StackTrace));
                }

                m_ExpressionGraphDirty = false;
            }
        }

        [NonSerialized]
        private bool m_ExpressionGraphDirty = true;
    }
}
