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
            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            if (view != null)
            {
                var nodes = view.GetAllNodes().Where(t => (output != null && t == output.node) || (input != null && t == input.node));

                foreach (var node in nodes)
                {
                    if (node is VFXStandaloneSlotContainerUI)
                        (node as VFXStandaloneSlotContainerUI).DirtyDrawer();
                    else // node must be from a VFXBlockUI
                        node.GetFirstAncestorOfType<VFXContextUI>().DirtyDrawer();
                }
            }
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            VFXDataEdgePresenter presenter = GetPresenter<VFXDataEdgePresenter>();

            bool dirty = false;

            if (dirty)
            {
                Dirty(ChangeType.Repaint);
                UpdateEdgeControl();
            }
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

        public override void OnAnchorChanged(bool isInput)
        {
            base.OnAnchorChanged(isInput);

            UpdateColor();
        }

        public virtual void UpdateColor()
        {
            if (selected)
            {
                edgeControl.inputColor = edgeControl.outputColor = selectedColor;
            }
            else
            {
                if (input != null)
                {
                    edgeControl.inputColor = (input as VFXDataAnchor).anchorColor;
                }
                else if (output != null)
                {
                    edgeControl.inputColor = (output as VFXDataAnchor).anchorColor;
                }
                if (output != null)
                {
                    edgeControl.outputColor = (output as VFXDataAnchor).anchorColor;
                }
                else if (input != null)
                {
                    edgeControl.outputColor = (input as VFXDataAnchor).anchorColor;
                }
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            UpdateColor();
            DirtyAnchors();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            UpdateColor();
            DirtyAnchors();
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
            EdgePresenter edgePresenter = GetPresenter<EdgePresenter>();
            /*
            if (presenter != null && (output == null || output.presenter != edgePresenter.output))
            {

                GraphView view = GetFirstAncestorOfType<GraphView>();
                if (view != null)
                    output = view.Query().OfType<NodeAnchor>().Where(t => t.presenter == edgePresenter.output);
            }
            if (presenter != null && (input == null || input.presenter != edgePresenter.input))
            {
                GraphView view = GetFirstAncestorOfType<GraphView>();
                if (view != null)
                    input = view.Query().OfType<NodeAnchor>().Where(t => t.presenter == edgePresenter.input);
            }*/

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
