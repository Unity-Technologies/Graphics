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

		public static readonly RectOffset NodeClientAreaSelectionPadding = new RectOffset(6, 7, 4, 10);

	}
}
