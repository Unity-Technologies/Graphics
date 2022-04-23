using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// The Add operator supports different types of input and sets its output type based on input type.
    /// </summary>
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Add", "operator-add")]
    public class MathAdditionOperator : MathOperator, IRebuildNodeOnConnection, IRebuildNodeOnDisconnection
    {
        public override string Title
        {
            get => "Add";
            set {}
        }

        public override TypeHandle[] ValueInputTypes => new[]
        {TypeHandle.Float, TypeHandle.Int, TypeHandle.Vector2, TypeHandle.Vector3};

        public TypeHandle OperatorType => m_OperatorType;

        [SerializeField]
        [HideInInspector]
        TypeHandle m_OperatorType = TypeHandle.Float;

        public IEnumerable<IEdgeModel> SetOperatorType(TypeHandle type)
        {
            var edgeDiff = new NodeEdgeDiff(this, PortDirection.Input);

            m_OperatorType = type;
            DefineNode();

            return edgeDiff.GetDeletedEdges();
        }

        public override bool CheckInputs(out string errorMessage)
        {
            if (!base.CheckInputs(out errorMessage))
                return false;

            var firstInputType = FirstInputType();
            var badType = AllInputTypes().FirstOrDefault(t => !t.IsCompatibleWith(firstInputType));

            if (badType == default)
                return true;
            errorMessage = $"Node {DisplayTitle} tries to add {firstInputType} and {badType} together.";
            return false;
        }

        public override Value Evaluate()
        {
            return Values.Skip(1).Aggregate(Values.FirstOrDefault(), Sum);
        }

        Value Sum(Value a, Value b)
        {
            if (a.Type == TypeHandle.Float)
                return a.Float + b.Float;
            if (a.Type == TypeHandle.Int && b.Type == TypeHandle.Int)
                return a.Int + b.Int;
            if (a.Type == TypeHandle.Int)
                return a.Float + b.Float;
            if (a.Type == TypeHandle.Vector2)
                return a.Vector2 + b.Vector2;
            if (a.Type == TypeHandle.Vector3)
                return a.Vector3 + b.Vector3;
            throw new ArgumentOutOfRangeException(nameof(a), a, null);
        }

        static readonly List<TypeHandle> k_TypePriorities = new List<TypeHandle>
        { TypeHandle.Int, TypeHandle.Float, TypeHandle.Vector2, TypeHandle.Vector3 };

        protected override void AddOutputPorts()
        {
            this.AddDataOutputPort("Out", m_OperatorType);
        }

        /// <inheritdoc />
        protected override (string op, bool isInfix) GetCSharpOperator()
        {
            return ("+", true);
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort("Term " + (i + 1), m_OperatorType);
        }

        public IEnumerable<IEdgeModel> RebuildOnEdgeConnected(IEdgeModel connectedEdge)
        {
            if (connectedEdge.ToPort.NodeModel == this)
            {
                var isOtherPortConnected =
                    InputsByDisplayOrder.Any(p => p != connectedEdge.ToPort && p.IsConnected());
                var currentPriority = k_TypePriorities.IndexOf(m_OperatorType);
                var newPortType = connectedEdge.FromPort.DataTypeHandle;
                var newPortPriority = k_TypePriorities.IndexOf(newPortType);

                if (!isOtherPortConnected || newPortPriority > currentPriority)
                {
                    return SetOperatorType(newPortType);
                }
            }

            return Enumerable.Empty<IEdgeModel>();
        }

        public IEnumerable<IEdgeModel> RebuildOnEdgeDisconnected(IEdgeModel disconnectedEdge)
        {
            if (disconnectedEdge.ToPort.NodeModel == this)
            {
                var connectedTypes = this.GetInputPorts()
                    .Where(p => p != disconnectedEdge.ToPort && p.IsConnected())
                    .SelectMany(p => p.GetConnectedEdges().Select(e => e.FromPort.DataTypeHandle))
                    .ToList();
                var highestPrio = connectedTypes.Count == 0 ? 0 : connectedTypes.Max(t => k_TypePriorities.IndexOf(t));
                var newType = k_TypePriorities[highestPrio < 0 ? 0 : highestPrio];

                return SetOperatorType(newType);
            }

            return Enumerable.Empty<IEdgeModel>();
        }
    }
}
