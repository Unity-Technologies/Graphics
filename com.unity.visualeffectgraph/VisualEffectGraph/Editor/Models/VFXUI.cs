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


        public VFXUI Clone(Dictionary<VFXModel, VFXModel> oldNewMap)
        {
            VFXUI clone = Instantiate(this);

            foreach (var groupInfo in clone.groupInfos)
            {
                groupInfo.content = groupInfo.content.Where(t => oldNewMap.ContainsKey(t)).Select(t => oldNewMap[t]).ToArray();
            }

            return clone;
        }
    }
}
