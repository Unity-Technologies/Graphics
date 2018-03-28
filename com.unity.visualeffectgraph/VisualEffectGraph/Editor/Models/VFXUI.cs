using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Serializable]
    struct VFXNodeID
    {
        public VFXNodeID(VFXModel model, int id)
        {
            this.model = model;
            this.id = id;
        }

        public VFXModel model;
        public int id;
    }
    class VFXUI : ScriptableObject
    {
        [System.Serializable]
        public class GroupInfo
        {
            public string title;
            public Rect position;
            public VFXNodeID[] content;
        }

        public GroupInfo[] groupInfos;

        public Rect uiBounds;

        internal void Sanitize(VFXGraph graph)
        {
            if (groupInfos != null)
                foreach (var groupInfo in groupInfos)
                {
                    //Check first, rebuild after because in most case the content will be valid, saving an allocation.
                    if (groupInfo.content != null && groupInfo.content.Any(t => !graph.children.Contains(t.model)))
                    {
                        groupInfo.content = groupInfo.content.Where(t => graph.children.Contains(t.model)).ToArray();
                    }
                }
        }
    }
}
