using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// The Add operator supports different types of input and sets its output type based on input type.
    /// </summary>
    [Serializable]
    public class MathAdditionOperator : MathOperator, IRebuildNodeOnConnection, IRebuildNodeOnDisconnection
    {
        public override string Title
        {
            get => "Add";
            set {}
        }

        public override ValueType[] ValueInputTypes => new[]
        {ValueType.Float, ValueType.Int, ValueType.Vector2, ValueType.Vector3};

        TypeHandle m_OperatorType = TypeHandle.Int;

        public TypeHandle OperatorType
        {
            get => m_OperatorType;
            set
            {
                if (value != m_OperatorType)
                {
                    m_OperatorType = value;
                    DefineNode();
                }
            }
        }

        public override bool CheckInputs(out string errorMessage)
        {
            if (!base.CheckInputs(out errorMessage))
                return false;

            var firstInputType = this.FirstInputType();
            var badType = this.AllInputTypes().FirstOrDefault(t => !t.IsNumsOfSameLengthAs(firstInputType));

            if (badType == default)
                return true;
            errorMessage = $"Node {DisplayTitle} tries to add {this.FirstInputType()} and {badType} together.";
            return false;
        }

        public override Value Evaluate()
        {
            return Values.Skip(1).Aggregate(Values.FirstOrDefault(), Sum);
        }

        Value Sum(Value a, Value b)
        {
            switch (a.Type)
            {
                case ValueType.Float:
                    return a.Float + b.Float;
                case ValueType.Int when b.Type == ValueType.Int:
                    return a.Int + b.Int;
                case ValueType.Int:
                    return a.Float + b.Float;
                case ValueType.Vector2:
                    return a.Vector2 + b.Vector2;
                case ValueType.Vector3:
                    return a.Vector3 + b.Vector3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(a), a, null);
            }
        }

        static readonly List<TypeHandle> TypePriorities = new List<TypeHandle>
        { TypeHandle.Int, TypeHandle.Float, TypeHandle.Vector2, TypeHandle.Vector3 };

        protected override void AddOutputPorts()
        {
            this.AddDataOutputPort("Out", m_OperatorType);
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort("Term " + (i + 1), m_OperatorType);
        }

        public bool RebuildOnEdgeConnected(IEdgeModel connectedEdge)
        {
            if (connectedEdge.ToPort.NodeModel == this)
            {
                var isOtherPortConnected =
                    InputsByDisplayOrder.Any(p => p != connectedEdge.ToPort && p.IsConnected());
                var currentPriority = TypePriorities.IndexOf(m_OperatorType);
                var newPortType = connectedEdge.FromPort.DataTypeHandle;
                var newPortPriority = TypePriorities.IndexOf(newPortType);

                if (!isOtherPortConnected || newPortPriority > currentPriority)
                {
                    OperatorType = newPortType;
                    return true;
                }
            }

            return false;
        }

        public bool RebuildOnEdgeDisconnected(IEdgeModel disconnectedEdge)
        {
            if (disconnectedEdge.ToPort.NodeModel == this)
            {
                var connectedTypes = this.GetInputPorts()
                    .Where(p => p != disconnectedEdge.ToPort && p.IsConnected())
                    .Select(p => p.DataTypeHandle)
                    .ToList();
                var highestPrio = connectedTypes.Count == 0 ? 0 : connectedTypes.Max(t => TypePriorities.IndexOf(t));
                var newType = TypePriorities[highestPrio < 0 ? 0 : highestPrio];

                OperatorType = newType;
                return true;
            }

            return false;
        }
    }
}
