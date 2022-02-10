using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    [Serializable]
    public class CompatibilityTestNodeModel : NodeModel
    {
        public const string inputExecutionPortPrefix = "execIn";
        public const string outputExecutionPortPrefix = "execOut";
        public const string inputDataPortPrefix = "dataIn";
        public const string outputDataPortPrefix = "dataOut";

        [SerializeField]
        int m_ExecIn;
        [SerializeField]
        int m_ExecOut;
        [SerializeField]
        int m_DataIn;
        [SerializeField]
        int m_DataOut;

        public (int ExecIn, int ExecOut, int DataIn, int DataOut) PortCounts
        {
            get => (m_ExecIn, m_ExecOut, m_DataIn, m_DataOut);
            set => (m_ExecIn, m_ExecOut, m_DataIn, m_DataOut) = value;
        }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            for (var i = 0; i < PortCounts.ExecIn; i++)
            {
                this.AddExecutionInputPort($"{inputExecutionPortPrefix}{i}");
            }
            for (var i = 0; i < PortCounts.ExecOut; i++)
            {
                this.AddExecutionOutputPort($"{outputExecutionPortPrefix}{i}");
            }
            for (var i = 0; i < PortCounts.DataIn; i++)
            {
                this.AddDataInputPort<int>($"{inputDataPortPrefix}{i}");
            }
            for (var i = 0; i < PortCounts.DataOut; i++)
            {
                this.AddDataOutputPort<int>($"{outputDataPortPrefix}{i}");
            }
        }
    }
}
