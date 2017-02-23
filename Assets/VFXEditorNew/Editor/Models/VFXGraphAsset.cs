using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    public class VFXGraphAssetFactory
    {
        [MenuItem("Assets/Create/VFXGraphAsset", priority = 301)]
        private static void MenuCreateVFXGraphAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateVFXGraphAsset>(), "New VFXGraph.asset", null, null);
        }

        internal static VFXGraphAsset CreateVFXGraphAssetAtPath(string path)
        {
            VFXGraphAsset asset = ScriptableObject.CreateInstance<VFXGraphAsset>();
            asset.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }

    internal class DoCreateVFXGraphAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            VFXGraphAsset asset = VFXGraphAssetFactory.CreateVFXGraphAssetAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }

    [Serializable]
    class VFXGraphAsset : ScriptableObject
    {
        public VFXGraph root { get { return m_Root; } }

        [SerializeField]
        private VFXGraph m_Root;

        private void OnModelInvalidate(VFXModel model,VFXModel.InvalidationCause cause)
        {
            EditorUtility.SetDirty(this);
        }

        void OnEnable()
        {
            if (m_Root == null)
                m_Root = new VFXGraph();
            m_Root.onInvalidateDelegate += OnModelInvalidate;
        }
    }

    [Serializable]
    class VFXGraph : VFXModel
    {
        public delegate void InvalidateEvent(VFXModel model,InvalidationCause cause);

        public event InvalidateEvent onInvalidateDelegate;

        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return true; // Can hold any model
        }

        protected override void OnInvalidate(VFXModel model,InvalidationCause cause)
        {
            if (onInvalidateDelegate != null)
                onInvalidateDelegate(model, cause); 
        }

        private ScriptableObject m_Owner;
    }
}
