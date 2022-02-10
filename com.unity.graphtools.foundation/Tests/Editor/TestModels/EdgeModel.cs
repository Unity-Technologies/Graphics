using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class EdgeModel : BasicModel.EdgeModel
    {
        [SerializeField, HideInInspector]
        GraphModel m_GraphModel;

        public override IGraphModel GraphModel => m_GraphModel;

        IPortModel m_FromPort;
        IPortModel m_ToPort;

        public override string EdgeLabel => m_EdgeLabel;

        public override IPortModel FromPort
        {
            get => m_FromPort;
            set
            {
                OnPortChanged(m_FromPort, value);
                m_FromPort = value;
            }
        }

        public override IPortModel ToPort
        {
            get => m_ToPort;
            set
            {
                OnPortChanged(m_ToPort, value);
                m_ToPort = value;
            }
        }

        public void SetGraphModel(GraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }
    }
}
