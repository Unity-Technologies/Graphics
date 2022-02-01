using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Constant model for <see cref="Quaternion"/>.
    /// </summary>
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class QuaternionConstant : Constant<Quaternion>
    {
        public override object DefaultValue => Quaternion.identity;
    }
}
