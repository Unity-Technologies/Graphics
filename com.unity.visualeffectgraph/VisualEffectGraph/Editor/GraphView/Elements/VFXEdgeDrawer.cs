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
    class VFXEdgeDrawer : VisualElement
    {
        public GraphElement element
        {
            get; set;
        }
        public VFXEdgeDrawer()
        {
            pickingMode = PickingMode.Ignore;
        }

        public virtual bool EdgeIsInThisDrawer(VFXDataEdge edge)
        {
            return (edge.input != null && edge.input.node == element) ||
                (edge.output != null && edge.output.node == element);
        }

        public override void DoRepaint()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            GL.PushMatrix();
            Matrix4x4 invTrans = worldTransform.inverse;
            GL.modelview = GL.modelview * invTrans;

            foreach (var dataEdge in view.GetAllDataEdges())
            {
                if (EdgeIsInThisDrawer(dataEdge))
                {
                    GL.PushMatrix();
                    Matrix4x4 trans = dataEdge.edgeControl.worldTransform;
                    GL.modelview = GL.modelview * trans;
                    dataEdge.edgeControl.DoRepaint();
                    GL.PopMatrix();
                }
            }

            GL.PopMatrix();
        }
    }


    class VFXContextEdgeDrawer : VFXEdgeDrawer
    {
        public override bool EdgeIsInThisDrawer(VFXDataEdge edge)
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
