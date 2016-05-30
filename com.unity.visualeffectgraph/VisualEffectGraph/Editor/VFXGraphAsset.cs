using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace UnityEditor.Experimental.VFX
{
    public class VFXGraphAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        private VFXGraph m_Graph;

        [SerializeField]
        private string m_SerializedGraph;

        [SerializeField]
        private int m_SerializedHash;

        private int m_SystemsInvalidateID;
        private int m_ModelsInvalidateID;

        public VFXGraph Graph { get { return m_Graph; } }
        public int GraphHash { get { return m_SerializedHash; } }

        public void OnBeforeSerialize()
        {
            if (m_SystemsInvalidateID != m_Graph.systems.InvalidateID || m_ModelsInvalidateID != m_Graph.models.InvalidateID)
            {
                m_SystemsInvalidateID = m_Graph.systems.InvalidateID;
                m_ModelsInvalidateID = m_Graph.models.InvalidateID;
                m_SerializedGraph = ModelSerializer.Serialize(m_Graph); 
            }  
        }

        public void OnAfterDeserialize()
        {
        }

        void OnEnable()
        {
            if (m_SerializedGraph != null)
            {
                m_Graph = ModelSerializer.Deserialize(m_SerializedGraph);
                m_SerializedHash = m_SerializedGraph.GetHashCode();
            }

            if (m_Graph == null) // In case deserialization fails, create an empty graph
            {
                m_Graph = new VFXGraph();
                m_SerializedHash = -1;
            }
        }

        void OnDisable()
        {
        }
    }

    public class VFXAssetFactory
    {
        [MenuItem("Assets/Create/VFXAsset2", priority = 301)]
        private static void MenuCreatePostProcessingProfile()
        {
            var icon = (Texture2D)null; // TODO: Post-processing profile texture
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreatePostProcessingProfile>(), "New VFXGraph.asset", icon, null);
        }

        internal static VFXGraphAsset CreatePostProcessingProfileAtPath(string path)
        {
            VFXGraphAsset asset = ScriptableObject.CreateInstance<VFXGraphAsset>();
            asset.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }

    internal class DoCreatePostProcessingProfile : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            VFXGraphAsset asset = VFXAssetFactory.CreatePostProcessingProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }
    }
}
