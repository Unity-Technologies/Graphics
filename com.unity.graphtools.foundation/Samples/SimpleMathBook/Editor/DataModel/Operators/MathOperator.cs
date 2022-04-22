using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathOperator : MathNode
    {
        [SerializeField]
        [ModelSetting]
        [InspectorUseSetterMethod(nameof(SetInputPortCount))]
        [Tooltip("Number of inputs.")]
        int m_InputPortCount = 2;

        public List<Value> Values => this.GetInputPorts().Select(portModel => portModel == null ? 0 : GetValue(portModel)).ToList();

        public int InputPortCount => m_InputPortCount;

        public void SetInputPortCount(int count,
            out IEnumerable<IGraphElementModel> newModels,
            out IEnumerable<IGraphElementModel> changedModels,
            out IEnumerable<IGraphElementModel> deletedModels)
        {
            var edgeDiff = new NodeEdgeDiff(this, PortDirection.Input);

            m_InputPortCount = Math.Max(2, count);
            DefineNode();

            newModels = null;
            changedModels = null;
            deletedModels = edgeDiff.GetDeletedEdges();
        }
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddOutputPorts();
            AddInputPorts();
        }

        protected virtual void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<float>("Port " + i);
        }

        protected virtual void AddOutputPorts()
        {
            this.AddDataOutputPort<float>("Output");
        }

        protected abstract (string op, bool isInfix) GetCSharpOperator();

        /// <inheritdoc />
        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variables = new List<string>();
            foreach (var portModel in InputsByDisplayOrder)
            {
                var variable = context.GenerateCodeForPort(portModel);
                variables.Add(variable);
            }

            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");

            if (variables.Count == 0)
            {
                context.Statements.Add($"{result} = default");
                return result;
            }

            var (op, isInfix) = GetCSharpOperator();
            context.Statements.Add($"{result} = {variables.First()}");
            foreach (var variable in variables.Skip(1))
            {
                context.Statements.Add(isInfix ? $"{result} = {result} {op} {variable}" : $"{result} = {op}({result}, {variable})");
            }

            return result;
        }
    }
}
