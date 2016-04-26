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
        public static readonly float DebugWindowWidth = 480;

        // NODE
        public static readonly float NodeDefaultWidth = 360;
        public static readonly RectOffset NodeImplicitContextOffset = new RectOffset(0, 0, -12, -16);
        public static readonly float NodeHeaderHeight = 28.0f;

        // NODE CLIENT AREA
        public static readonly RectOffset NodeClientAreaOffset = new RectOffset(-16, -16, 0, 12);

        // NODEBLOCK CONTAINER
        public static readonly Vector2 NodeBlockContainerPosition = new Vector3(7f, 30f);
        public static readonly Vector2 NodeBlockContainerSizeOffset = new Vector2(- 15f, 0.0f);
        public static readonly float NodeBlockContainerSeparatorHeight = 16.0f;
        public static readonly float NodeBlockContainerSeparatorOffset = 8.0f;
        public static readonly float NodeBlockContainerEmptyHeight = 48.0f;

        // NODE BLOCKS
        public static readonly float NodeBlockHeaderHeight = 40f;
        public static readonly float NodeBlockFooterHeight = 4f;
        public static readonly float NodeBlockParameterHeight = 16f;
        public static readonly float NodeBlockParameterSpacingHeight = 6f;
        public static readonly float NodeBlockAdditionalHeight = 16f;
        public static readonly Rect NodeBlockHeaderFoldoutRect = new Rect(new Vector2(20f, 8f), new Vector2(24f, 24f));
        // public static readonly Rect NodeBlockHeaderIconRect = new Rect(new Vector2(56f, 12f), new Vector2(16f, 16f));
        public static readonly Rect NodeBlockHeaderIconRect =new Rect(new Vector2(52f, 8f), new Vector2(24f, 24f));
        public static readonly Vector2 NodeBlockHeaderLabelPosition = new Vector2(80.0f, 0.0f);

        // PARAMETER FIELDS
        public static readonly RectOffset ParameterFieldRectOffset = new RectOffset(24, 32, 0, 0);
        public static readonly float ParameterFieldLabelWidth = 64.0f;
        public static readonly float ParameterFieldIndentWidth = 8.0f;
        public static readonly float ParameterFieldFoldOutWidth = 24.0f;

        // EDGE AND ANCHORS
        public static readonly Vector3 FlowAnchorSize = new Vector3(64.0f, 32.0f, 1.0f);
        public static readonly Color FlowAnchorDefaultColor = new Color(0.8f, 0.8f, 0.8f);
        public static readonly Vector3 DataAnchorSize = new Vector3(16f, 16f, 1.0f);
        public static readonly Color DataAnchorDefaultColor = new Color(0.8f, 0.8f, 0.8f);
        public static readonly float FlowEdgeWidth = 40.0f;
        public static readonly float DataEdgeWidth = 8.0f;

        // EVENT NODES
        public static readonly Vector2 EventNodeDefaultScale = new Vector2(240, 120f);
        public static readonly RectOffset EventNodeTextRectOffset = new RectOffset(16, 16, 24, 48);

    }
}
