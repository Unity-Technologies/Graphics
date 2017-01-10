using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    public class VFXModelContainerFactory
    {
        [MenuItem("Assets/Create/VFXModelContainer", priority = 301)]
        private static void MenuCreateVFXModelContainer()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateVFXModelContainer>(), "New VFXModelContainer.asset", null, null);
        }

        internal static VFXModelContainer CreateVFXModelContainerAtPath(string path)
        {
            VFXModelContainer asset = ScriptableObject.CreateInstance<VFXModelContainer>();
            asset.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }

    internal class DoCreateVFXModelContainer : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            VFXModelContainer asset = VFXModelContainerFactory.CreateVFXModelContainerAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }

    // Just a temp asset to hold a VFX graph
    [Serializable]
    class VFXModelContainer : ScriptableObject,  ISerializationCallbackReceiver
    {
        [NonSerialized]
        public List<VFXModel> m_Roots;

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializedRoots;

        public virtual void OnBeforeSerialize()
        {
            m_SerializedRoots = SerializationHelper.Serialize<VFXModel>(m_Roots);
        }

        public virtual void OnAfterDeserialize()
        {
            m_Roots = SerializationHelper.Deserialize<VFXModel>(m_SerializedRoots, null);
            m_SerializedRoots = null; // No need to keep it
        }

        void OnEnable()
        {
            if (m_Roots == null)
                m_Roots = new List<VFXModel>();
        }
    }
}
