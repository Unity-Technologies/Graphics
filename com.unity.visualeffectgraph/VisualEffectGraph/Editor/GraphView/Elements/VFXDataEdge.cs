using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    internal class VFXEdge : Edge
    {
        public virtual void OnDisplayChanged()
        {
        }
    }

    internal class VFXDataEdge : VFXEdge
    {
        public VFXDataEdge()
        {
        }

        public override int layer
        {
            get
            {
                return -1;
            }
        }

        public override void OnDisplayChanged()
        {
            VFXDataEdgePresenter edgePresenter = GetPresenter<VFXDataEdgePresenter>();
            VFXDataAnchorPresenter outputPresenter = edgePresenter.output as VFXDataAnchorPresenter;
            VFXDataAnchorPresenter inputPresenter = edgePresenter.input as VFXDataAnchorPresenter;

            VFXView view = this.GetFirstAncestorOfType<VFXView>();

            var nodes = view.GetAllNodes().Where(t => (outputPresenter != null && t.presenter == outputPresenter.sourceNode) || (inputPresenter != null && t.presenter == inputPresenter.sourceNode));

            foreach (var node in nodes)
            {
                if (node is VFXStandaloneSlotContainerUI)
                    (node as VFXStandaloneSlotContainerUI).DirtyDrawer();
                else // node must be from a VFXContext
                    node.GetFirstAncestorOfType<VFXContextUI>().DirtyDrawer();
            }
            //TODO VFXContext dirtydrawer when existing
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            foreach (var cls in VFXTypeDefinition.GetTypeCSSClasses())
                RemoveFromClassList(cls);


            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;


            if (outputPresenter == null && inputPresenter == null)
                return;
            /*if (outputPresenter != null && panel != null)
                panel.dataWatch.ForceDirtyNextPoll(outputPresenter);

            if (inputPresenter != null && panel != null)
                panel.dataWatch.ForceDirtyNextPoll(inputPresenter);*/

            System.Type type = inputPresenter != null ? inputPresenter.anchorType : outputPresenter.anchorType;

            AddToClassList(VFXTypeDefinition.GetTypeCSSClass(type));
            OnAnchorChanged();
        }

        public override void OnSelected()
        {
            DirtyAnchors();
        }

        public override void OnUnselected()
        {
            DirtyAnchors();
        }

        void DirtyAnchors()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();

            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;

            VFXDataAnchor outputAnchor = view.GetDataAnchorByPresenter(outputPresenter as VFXDataAnchorPresenter);
            VFXDataAnchor inputAnchor = view.GetDataAnchorByPresenter(inputPresenter as VFXDataAnchorPresenter);

            if (outputAnchor != null && outputAnchor.node is IEdgeDrawerOwner)
            {
                (outputAnchor.node as IEdgeDrawerOwner).DirtyDrawer();
            }
            if (inputAnchor != null && inputAnchor.node is IEdgeDrawerOwner)
            {
                (inputAnchor.node as IEdgeDrawerOwner).DirtyDrawer();
            }
        }

        public void OnAnchorChanged()
        {
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;

            VFXView view = GetFirstAncestorOfType<VFXView>();

            if (view != null)
            {
                VFXDataAnchor outputAnchor = view.GetDataAnchorByPresenter(outputPresenter as VFXDataAnchorPresenter);
                VFXDataAnchor inputAnchor = view.GetDataAnchorByPresenter(inputPresenter as VFXDataAnchorPresenter);

                VFXEdgeControl edgeControl = this.edgeControl as VFXEdgeControl;

                if (GetPresenter<EdgePresenter>().selected)
                {
                    edgeControl.inputColor = edgeControl.outputColor = selectedColor;
                }
                else
                {
                    if (inputAnchor != null)
                    {
                        edgeControl.inputColor = inputAnchor.anchorColor;
                    }
                    else if (outputAnchor != null)
                    {
                        edgeControl.inputColor = outputAnchor.anchorColor;
                    }

                    if (outputAnchor != null)
                    {
                        edgeControl.outputColor = outputAnchor.anchorColor;
                    }
                    else if (inputAnchor != null)
                    {
                        edgeControl.outputColor = inputAnchor.anchorColor;
                    }
                }
            }
        }

        protected override EdgeControl CreateEdgeControl()
        {
            return new VFXEdgeControl
            {
                capRadius = 4,
                interceptWidth = 3
            };
        }

        protected override void DrawEdge()
        {
            UpdateEdgeControl();
        }

#if false
        protected override void DrawEdge()
        {
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            VFXDataAnchorPresenter inputPresenter = edgePresenter.input as VFXDataAnchorPresenter;

            if (outputPresenter == null && inputPresenter == null)
                return;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(ref from, ref to);
            Color edgeColor = style.borderColor;

            if (inputPresenter != null && inputPresenter.sourceNode is VFXBlockPresenter)
            {
                to = to + new Vector2(-10, 0);
            }


            Orientation orientation = Orientation.Horizontal;
            Vector3[] points, tangents;
            GetTangents(orientation, from, to, out points, out tangents);


            GraphView view = this.GetFirstAncestorOfType<GraphView>();

            float realWidth = edgePresenter.selected ? edgeWidth * 2 : edgeWidth;
            if (realWidth * view.scale < 1.5f)
            {
                realWidth = 1.5f / view.scale;
            }
            VFXFlowEdge.RenderBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, realWidth);
            /*if (edgePresenter.selected)
            {
                Handles.DrawBezier(points[0] + Vector3.down, points[1] + Vector3.down , tangents[0] + Vector3.down , tangents[1] + Vector3.down , edgeColor, null, 2f);
                Handles.DrawBezier(points[0] + Vector3.up , points[1] + Vector3.up , tangents[0] + Vector3.up , tangents[1] + Vector3.up , edgeColor, null, 2f);
            }
            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 2f);*/
        }

#endif
    }
}
