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
        public GraphElementPresenter presenter
        {
            get; set;
        }
        public VFXEdgeDrawer()
        {
            pickingMode = PickingMode.Ignore;
        }

        public virtual bool EdgeIsInThisDrawer(VFXDataEdgePresenter edgePresenter)
        {
            return (edgePresenter.input != null && (edgePresenter.input as VFXDataAnchorPresenter).sourceNode == presenter) ||
                (edgePresenter.output != null && (edgePresenter.output as VFXDataAnchorPresenter).sourceNode == presenter);
        }

        public override void DoRepaint()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            GL.PushMatrix();
            Matrix4x4 invTrans = worldTransform.inverse;
            GL.modelview = GL.modelview * invTrans;

            foreach (var dataEdge in view.GetAllDataEdges())
            {
                VFXDataEdgePresenter edgePresenter = dataEdge.GetPresenter<VFXDataEdgePresenter>();
                if (EdgeIsInThisDrawer(edgePresenter))
                {
                    VFXDataEdge edge = view.GetDataEdgeByPresenter(edgePresenter);
                    GL.PushMatrix();
                    Matrix4x4 trans = edge.edgeControl.worldTransform;
                    GL.modelview = GL.modelview * trans;
                    edge.edgeControl.DoRepaint();
                    GL.PopMatrix();
                }
            }

            GL.PopMatrix();
        }
    }


    class VFXContextEdgeDrawer : VFXEdgeDrawer
    {
        public override bool EdgeIsInThisDrawer(VFXDataEdgePresenter edgePresenter)
        {
            VFXContextPresenter presenter = this.presenter as VFXContextPresenter;
            if (
                (edgePresenter.input != null && (edgePresenter.input as VFXDataAnchorPresenter).sourceNode == presenter.slotPresenter) ||
                (edgePresenter.output != null && (edgePresenter.output as VFXDataAnchorPresenter).sourceNode == presenter.slotPresenter)
                )
            {
                return true;
            }


            foreach (var blockPresenter in presenter.blockPresenters)
            {
                if (
                    (edgePresenter.input != null && (edgePresenter.input as VFXDataAnchorPresenter).sourceNode == blockPresenter) ||
                    (edgePresenter.output != null && (edgePresenter.output as VFXDataAnchorPresenter).sourceNode == blockPresenter)
                    )
                {
                    return true;
                }
            }

            return false;
        }
    }
}
