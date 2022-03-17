using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    interface IVariableNodeModel
    {
        public int InputCount { get; }
        public int OutputCount { get; }
        public int VerticalInputCount { get; }
        public int VerticalOutputCount { get; }

        void AddPort(PortOrientation orientation, PortDirection direction, TypeHandle type);
        IEnumerable<IEdgeModel> RemovePort(PortOrientation orientation, PortDirection direction);
    }

    [Serializable]
    class VariableNodeHelper
    {
        [SerializeField, HideInInspector]
        List<TypeHandle> m_InputTypes = new List<TypeHandle>(new[] { TypeHandle.Float });

        [SerializeField, HideInInspector]
        List<TypeHandle> m_OutputTypes = new List<TypeHandle>(new[] { TypeHandle.Float });

        [SerializeField, HideInInspector]
        int m_VerticalInputCount;

        [SerializeField, HideInInspector]
        int m_VerticalOutputCount;

        public int InputCount => m_InputTypes.Count;

        public int OutputCount => m_OutputTypes.Count;

        public int VerticalInputCount => m_VerticalInputCount;

        public int VerticalOutputCount => m_VerticalOutputCount;

        public void OnDefineNode(NodeModel node)
        {
            for (var i = 0; i < m_InputTypes.Count; i++)
                node.AddDataInputPort("In " + (i + 1), m_InputTypes[i]);

            for (var i = 0; i < m_OutputTypes.Count; i++)
                node.AddDataOutputPort("Out " + (i + 1), m_OutputTypes[i]);

            for (var i = 0; i < m_VerticalInputCount; i++)
                node.AddExecutionInputPort("VIn " + (i + 1), orientation: PortOrientation.Vertical);

            for (var i = 0; i < m_VerticalOutputCount; i++)
                node.AddExecutionOutputPort("VOut " + (i + 1), orientation: PortOrientation.Vertical);
        }

        public void AddPort(PortOrientation orientation, PortDirection direction, TypeHandle type)
        {
            if (orientation == PortOrientation.Horizontal)
            {
                if (direction == PortDirection.Input)
                {
                    m_InputTypes.Add(string.IsNullOrEmpty(type.Identification) ? TypeHandle.Vector2 : type);
                }
                else
                {
                    m_OutputTypes.Add(string.IsNullOrEmpty(type.Identification) ? TypeHandle.Vector2 : type);
                }
            }
            else
            {
                if (direction == PortDirection.Input)
                    m_VerticalInputCount++;
                else
                    m_VerticalOutputCount++;
            }
        }

        public void RemovePort(PortOrientation orientation, PortDirection direction)
        {
            if (orientation == PortOrientation.Horizontal)
            {
                if (direction == PortDirection.Input)
                {
                    if (m_InputTypes.Count > 0)
                    {
                        m_InputTypes.RemoveAt(m_InputTypes.Count - 1);
                    }
                }
                else
                {
                    if (m_OutputTypes.Count > 0)
                    {
                        m_OutputTypes.RemoveAt(m_OutputTypes.Count - 1);
                    }
                }
            }
            else
            {
                if (direction == PortDirection.Input)
                {
                    if (m_VerticalInputCount > 0)
                    {
                        m_VerticalInputCount--;
                    }
                }
                else
                {
                    if (m_VerticalOutputCount > 0)
                    {
                        m_VerticalOutputCount--;
                    }
                }
            }
        }
    }
}
