using System;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;

namespace UnityEditor.Experimental
{
    public class VFXEditorMetrics
    {
        public static readonly int WindowPadding = 16;
        public static readonly int LibraryWindowWidth = 320;
        public static readonly int DefaultNodeWidth = 320;
        public static readonly int PreviewWindowWidth = 480;
        public static readonly int PreviewWindowHeight = 320;

        public static readonly float FlowEdgeWidth = 40.0f;
        public static readonly float DataEdgeWidth = 5.0f;

        public static readonly float NodeBlockHeaderHeight = 32f;
        public static readonly float NodeBlockParameterHeight = 20f;
        public static readonly float NodeBlockAdditionalHeight = 14f;

        public static readonly Rect NodeBlockCollapserArrowRect = new Rect(new Vector2(4f, 4f), new Vector2(24f, 24f));
        public static readonly Vector2 NodeBlockCollapserLabelPosition = new Vector2(40.0f, 0.0f);

        public static readonly Vector3 FlowAnchorSize = new Vector3(64.0f, 32.0f, 1.0f);
        public static readonly Color FlowAnchorDefaultColor = new Color(0.8f, 0.8f, 0.8f);

        public static readonly RectOffset NodeClientArea_SelectionPadding = new RectOffset(6, 7, 4, 10);

    }
}
