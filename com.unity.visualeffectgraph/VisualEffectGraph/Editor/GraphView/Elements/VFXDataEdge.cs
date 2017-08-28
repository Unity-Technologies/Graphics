using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    internal class VFXDataEdgeControl : EdgeControl
    {
        protected override void DrawEdge()
        {
            Vector3[] points = controlPoints;

            VFXDataEdge edge = this.GetFirstAncestorOfType<VFXDataEdge>();
            VFXDataEdgePresenter edgePresenter = edge.GetPresenter<VFXDataEdgePresenter>();
            VFXDataAnchorPresenter inputPresenter = edgePresenter.input as VFXDataAnchorPresenter;
            Color edgeColor = edge.style.borderColor;


            Vector3 trueEnd = points[3];
            if (inputPresenter != null && inputPresenter.sourceNode is VFXBlockPresenter)
            {
                trueEnd = trueEnd + new Vector3(-10, 0, 0);
            }

            GraphView view = this.GetFirstAncestorOfType<GraphView>();

            float realWidth = edgePresenter.selected ? edgeWidth * 2 : edgeWidth;
            if (realWidth * view.scale < 1.5f)
            {
                realWidth = 1.5f / view.scale;
            }

            VFXEdgeUtils.RenderBezier(points[0], trueEnd, points[1], points[2], edgeColor, realWidth);
        }

        protected override void DrawEndpoint(Vector2 pos, bool start)
        {
        }
    }
    internal class VFXDataEdge : Edge
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
            if (outputPresenter != null && panel != null)
                panel.dataWatch.ForceDirtyNextPoll(outputPresenter);

            if (inputPresenter != null && panel != null)
                panel.dataWatch.ForceDirtyNextPoll(inputPresenter);

            System.Type type = inputPresenter != null ? inputPresenter.anchorType : outputPresenter.anchorType;

            AddToClassList(VFXTypeDefinition.GetTypeCSSClass(type));
        }

        protected override EdgeControl CreateEdgeControl()
        {
            return new VFXDataEdgeControl
            {
                capRadius = 4,
                interceptWidth = 3
            };
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
