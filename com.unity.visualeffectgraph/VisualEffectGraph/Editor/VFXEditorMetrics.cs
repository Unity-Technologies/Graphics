using System;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;

namespace UnityEditor.Experimental
{
    public class VFXEditorMetrics
    {
        // EDITOR STANDARDS

        public static readonly int WindowPadding = 16;
        public static readonly int LibraryWindowWidth = 320;

        public static readonly int PreviewWindowWidth = 480;
        public static readonly int PreviewWindowHeight = 320;

        // NODE
        public static readonly float NodeHeightOffset = 50.0f;
        public static readonly float NodeDefaultWidth = 360;
        public static readonly RectOffset NodeImplicitContextOffset = new RectOffset(0, 0, -8, -10);

        // NODE CLIENT AREA

        public static readonly RectOffset NodeClientAreaOffset= new RectOffset(-16, -16, -24, 0);
        public static readonly RectOffset NodeClientAreaSelectionPadding = new RectOffset(6, 7, 4, 10);
        public static readonly Vector3 NodeClientAreaPosition = new Vector3(0.0f, 24.0f, 0.0f);

        // NODEBLOCK CONTAINER

        public static readonly Vector3 NodeBlockContainerPosition = new Vector3(7f, 30f, 0.0f);
        public static readonly Vector2 NodeBlockContainerSizeOffset = new Vector2(- 15f, - 40f);

        public static readonly float NodeBlockContainerSeparatorHeight = 16.0f;
        public static readonly float NodeBlockContainerSeparatorOffset = 8.0f;

        public static readonly float NodeBlockContainerEmptyHeight = 80.0f;
        

        // NODE BLOCKS

        public static readonly float NodeBlockHeaderHeight = 32f;
        public static readonly float NodeBlockParameterHeight = 20f;
        public static readonly float NodeBlockAdditionalHeight = 18f;

        public static readonly Rect NodeBlockCollapserArrowRect = new Rect(new Vector2(4f, 4f), new Vector2(24f, 24f));
        public static readonly Vector2 NodeBlockCollapserLabelPosition = new Vector2(40.0f, 0.0f);

        public static readonly Vector2 NodeBlockParameterLabelPosition = new Vector2(24.0f, 0.0f);


        // EDGE AND ANCHORS

        public static readonly Vector3 FlowAnchorSize = new Vector3(64.0f, 32.0f, 1.0f);
        public static readonly Color FlowAnchorDefaultColor = new Color(0.8f, 0.8f, 0.8f);

        public static readonly Vector3 DataAnchorSize = new Vector3(16f, 16f, 1.0f);
        public static readonly Color DataAnchorDefaultColor = new Color(0.8f, 0.8f, 0.8f);

        public static readonly float FlowEdgeWidth = 40.0f;
        public static readonly float DataEdgeWidth = 5.0f;

    }
}
