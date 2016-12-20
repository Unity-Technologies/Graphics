using System;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class MaterialGraph : AbstractMaterialGraph
    {
        [NonSerialized]
        private Guid m_ActiveMasterNodeGUID;

        [SerializeField]
        private string m_ActiveMasterNodeGUIDSerialized;

        public MaterialGraph()
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
            set
            {
                var previousMaster = masterNode;

                if (value == null)
                    m_ActiveMasterNodeGUID = Guid.NewGuid();
                else
                    m_ActiveMasterNodeGUID = value.guid;
                
                masterNode.onModified(masterNode, ModificationScope.Node);
                previousMaster.onModified(previousMaster, ModificationScope.Node);
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
