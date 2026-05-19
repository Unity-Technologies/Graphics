using System;
using UnityEngine;

namespace UnityEditor.U2D.Graphics.Profiler.UI
{
    record U2DGraphicsHierarchyNodeData
    {
        public readonly string name;
        public readonly EntityId entityId;
        public int triangleCount;
        public int vertexCount;
        public readonly int id;
        public string icon;
        [SerializeField]
        string m_DrawCountLabel = "";
        int m_DrawCount = 0;

        public U2DGraphicsHierarchyNodeData(string name, EntityId entityId, int triangleCount, int vertexCount, int id, string icon)
        {
            this.name = name;
            this.entityId = entityId;
            this.triangleCount = triangleCount;
            this.vertexCount = vertexCount;
            this.id = id;
            this.icon = icon;
        }

        public int drawCount
        {
            get => m_DrawCount;
            set
            {
                m_DrawCount = value;
                m_DrawCountLabel = value.ToString();
            }
        }

        public virtual bool Equals(U2DGraphicsHierarchyNodeData other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return entityId == other.entityId;
        }

        public override int GetHashCode()
        {
            return entityId.GetHashCode();
        }

        public static int Compare(U2DGraphicsHierarchyNodeData a, U2DGraphicsHierarchyNodeData b, string propertyToCompare)
        {
            switch (propertyToCompare)
            {
                case "name":
                case null:
                    return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
                case "triangleCount":
                    return a.triangleCount.CompareTo(b.triangleCount);
                case "vertexCount":
                    return a.vertexCount.CompareTo(b.vertexCount);
                default:
                    return 0;
            }
        }
    }
}
