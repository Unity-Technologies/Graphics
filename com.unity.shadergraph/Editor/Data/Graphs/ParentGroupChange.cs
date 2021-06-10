using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    struct ParentGroupChange
    {
        public IGroupItem groupItem;
        public GroupData oldGroup;
        public GroupData newGroup;
    }
}
