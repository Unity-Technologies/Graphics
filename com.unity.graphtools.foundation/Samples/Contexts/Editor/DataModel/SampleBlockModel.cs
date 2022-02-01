using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class SampleBlockModelBase : BlockNodeModel, IVariableNodeModel
    {
        [SerializeField]
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

        public void RemovePort(PortOrientation orientation, PortDirection direction)
        {
            m_Helper.RemovePort(orientation, direction);

            DefineNode();
        }
    }

    [Serializable]
    public class SampleBlockModelA1 : SampleBlockModelBase
    {
        public override bool IsCompatibleWith(IContextNodeModel context)
        {
            return context is SampleContextModelA;
        }
    }

    [Serializable]
    public class SampleBlockModelA2 : SampleBlockModelBase
    {
        public override bool IsCompatibleWith(IContextNodeModel context)
        {
            return context is SampleContextModelA;
        }
    }

    [Serializable]
    public class SampleBlockModelB1 : SampleBlockModelBase
    {
        public override bool IsCompatibleWith(IContextNodeModel context)
        {
            return context is SampleContextModelB;
        }
    }

    [Serializable]
    public class SampleBlockModelB2 : SampleBlockModelBase
    {
        public override bool IsCompatibleWith(IContextNodeModel context)
        {
            return context is SampleContextModelB;
        }
    }
}
