#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Overlay(typeof(SimpleGraphViewWindow), k_Id, "Evaluation Result", true,
        defaultDockZone = DockZone.BottomToolbar, defaultDockPosition = DockPosition.Top,
        defaultLayout = Layout.HorizontalToolbar)]
    [Icon("Packages/com.unity.graphtools.foundation/Samples/SimpleMathBook/Editor/Stylesheets/Icons/EvaluationResults.png")]
    class EvaluationResultToolbar : OverlayToolbar
    {
        const string k_Id = "gtf-mathbook-result-toolbar";

        /// <inheritdoc />
        public override IEnumerable<string> toolbarElements => new[] { "GTF/MathBook Sample/Evaluation Result" };

        /// <inheritdoc />
        protected override Layout supportedLayouts => Layout.HorizontalToolbar;

        public EvaluationResultToolbar()
        {
            AddStylesheet("Packages/com.unity.graphtools.foundation/Samples/SimpleMathBook/Editor/Stylesheets/EvaluationResultToolbar.uss");
        }
    }
}
#endif
