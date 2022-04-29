using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphModelInspector : ModelInspector
    {
        public ShaderGraphModelInspector()
        {
            name = "sg-model-inspector";
            this.AddStylesheet("ShaderGraphModelInspector.uss");
        }
        /// <summary>
        /// Returns true if the inspector does not contain any field.
        /// </summary>
        /// <returns>True if the inspector does not contain any field.</returns>
        public override bool IsEmpty()
        {
            return PartList.Parts.Count == 0;
        }
    }
}
