using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    class SampleNodeModel : NodeModel, IVariableNodeModel
    {
        [SerializeField, HideInInspector]
        VariableNodeHelper m_Helper = new VariableNodeHelper();

        public int InputCount => m_Helper.InputCount;

        public int OutputCount => m_Helper.OutputCount;

        public int VerticalInputCount => m_Helper.VerticalInputCount;

        public int VerticalOutputCount => m_Helper.VerticalOutputCount;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            m_Helper.OnDefineNode(this);
        }

        public void AddPort(PortOrientation orientation, PortDirection direction, TypeHandle type)
        {
            m_Helper.AddPort(orientation, direction, type);

            DefineNode();
        }

        public IEnumerable<IEdgeModel> RemovePort(PortOrientation orientation, PortDirection direction)
        {
            var edgeDiff = new NodeEdgeDiff(this, direction);

            m_Helper.RemovePort(orientation, direction);
            DefineNode();

            return edgeDiff.GetDeletedEdges();
        }
    }
}
