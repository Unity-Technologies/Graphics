using System;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class PixelGraph : AbstractMaterialGraph
    {
        [NonSerialized]
        private Guid m_ActiveMasterNodeGUID;

        [SerializeField]
        private string m_ActiveMasterNodeGUIDSerialized;

        public PixelGraph()
        {
            m_ActiveMasterNodeGUID = Guid.NewGuid();
        }

        public IMasterNode masterNode
        {
            get
            {
                var found = GetNodeFromGuid(m_ActiveMasterNodeGUID) as IMasterNode;
                if (found != null)
                    return found;

                return GetNodes<IMasterNode>().FirstOrDefault();
            }
        }

        public string name
        {
            get { return "Graph_ " + masterNode.GetVariableNameForNode(); }
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            m_ActiveMasterNodeGUIDSerialized = m_ActiveMasterNodeGUID.ToString();
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (!string.IsNullOrEmpty(m_ActiveMasterNodeGUIDSerialized))
                m_ActiveMasterNodeGUID = new Guid(m_ActiveMasterNodeGUIDSerialized);
            else
                m_ActiveMasterNodeGUID = Guid.NewGuid();
        }
    }
}
