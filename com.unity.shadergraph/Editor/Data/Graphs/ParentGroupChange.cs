using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    struct ParentGroupChange
    {
        public IGroupItem groupItem;
        public Guid oldGroupGuid;
        public Guid newGroupGuid;
    }
}

