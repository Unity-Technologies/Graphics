using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXContextEdgeDrawer : EdgeDrawer
    {
        public override bool EdgeIsInThisDrawer(Edge edge)
        {
            VFXContextUI context = this.element as VFXContextUI;
            if ((edge.input != null && edge.input.node == context.ownData) ||
                (edge.output != null && edge.output.node == context.ownData)
                )
            {
                return true;
            }


            foreach (var block in context.GetAllBlocks())
            {
                if ((edge.input != null && edge.input.node == block) ||
                    (edge.output != null && edge.output.node == block)
                    )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
