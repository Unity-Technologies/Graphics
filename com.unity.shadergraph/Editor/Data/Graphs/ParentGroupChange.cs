using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    struct ParentGroupChange
    {
        public IGroupItem groupItem;
        public string oldGroupId;
        public string newGroupId;
        public bool newGroupIsEmpty;
    }
}

