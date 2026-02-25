using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    readonly struct TaskDependency : IEquatable<TaskDependency>
    {
        public TaskDependency(TaskNodeId nodeId, TaskNodeId parentNodeId)
        {
            NodeId = nodeId;
            ParentNodeId = parentNodeId;
        }

        public TaskNodeId NodeId { get; }
        public TaskNodeId ParentNodeId { get; }

        public bool Equals(TaskDependency other)
        {
            return NodeId.Equals(other.NodeId) && ParentNodeId.Equals(other.ParentNodeId);
        }

        public override bool Equals(object obj)
        {
            return obj is TaskDependency other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeId, ParentNodeId);
        }
    }

    readonly struct DataDependency : IEquatable<DataDependency>
    {
        public DataDependency(DataNodeId nodeId, DataNodeId parentNodeId)
        {
            NodeId = nodeId;
            ParentNodeId = parentNodeId;
        }

        public DataNodeId NodeId { get; }
        public DataNodeId ParentNodeId { get; }

        public bool Equals(DataDependency other)
        {
            return NodeId.Equals(other.NodeId) && ParentNodeId.Equals(other.ParentNodeId);
        }

        public override bool Equals(object obj)
        {
            return obj is DataDependency other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NodeId, ParentNodeId);
        }
    }
}
