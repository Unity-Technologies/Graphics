using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for inspecting values of a model.
    /// </summary>
    /// <remarks>
    /// This class does nothing by itself. Its PartList needs to be populated with concrete derivatives of <see cref="FieldsInspector"/>.
    /// </remarks>
    public class ModelInspector : ModelUI
    {
        public static readonly string ussClassName = "ge-model-inspector";

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(ussClassName);
        }
    }
}
