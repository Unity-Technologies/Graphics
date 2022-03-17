using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for inspecting values of a model.
    /// </summary>
    /// <remarks>
    /// This class does nothing by itself. Its PartList needs to be populated with concrete derivatives of <see cref="FieldsInspector"/>.
    /// </remarks>
    public class ModelInspector : ModelView
    {
        public static readonly string ussClassName = "ge-model-inspector";
        public static readonly string fieldsPartName = "fields";

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Returns true if the inspector does not contain any field.
        /// </summary>
        /// <returns>True if the inspector does not contain any field.</returns>
        public virtual bool IsEmpty()
        {
            var fieldsPart = PartList.Parts.FirstOrDefault(p => p.PartName == fieldsPartName);
            return !(fieldsPart is FieldsInspector fieldsInspector) || fieldsInspector.FieldCount == 0;
        }
    }
}
