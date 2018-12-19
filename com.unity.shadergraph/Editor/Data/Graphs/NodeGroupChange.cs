using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public struct NodeGroupChange
    {
        public Guid nodeGuid;
        public Guid oldGroupGuid;
        public Guid newGroupGuid;
    }
}

