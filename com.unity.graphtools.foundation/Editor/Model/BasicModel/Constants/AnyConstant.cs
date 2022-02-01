using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Constant model for objects of any type.
    /// </summary>
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class AnyConstant : Constant<object>
    {
        /// <inheritdoc />
        public override Type Type => Value != null ? Value.GetType() : typeof(object);
    }
}
