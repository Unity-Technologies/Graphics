using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Serialization;

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
        public class UIInfo
        {
            public string title;
            public Rect position;
        }

        [System.Serializable]
        public class GroupInfo : UIInfo
        {
            [FormerlySerializedAs("content")]
            public VFXNodeID[] contents;
        }

        [System.Serializable]
        public class StickyNoteInfo : UIInfo
        {
            public string contents;

            public string theme;
        }

        public GroupInfo[] groupInfos;
        public StickyNoteInfo[] stickyNoteInfos;

        public Rect uiBounds;

        internal void Sanitize(VFXGraph graph)
        {
            foreach (var groupInfo in groupInfos)
            {
                //Check first, rebuild after because in most case the content will be valid, saving an allocation.
                if (groupInfo.contents.Any(t => !graph.children.Contains(t.model)))
                {
                    groupInfo.contents = groupInfo.contents.Where(t => graph.children.Contains(t.model)).ToArray();
                }
            }
        }
    }
}
