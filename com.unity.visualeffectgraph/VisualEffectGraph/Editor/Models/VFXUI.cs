using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    class VFXUI : ScriptableObject
    {
        [System.Serializable]
        public class GroupInfo
        {
            public string title;
            public Rect position;
            public VFXModel[] content;
        }

        public GroupInfo[] groupInfos;
    }
}
