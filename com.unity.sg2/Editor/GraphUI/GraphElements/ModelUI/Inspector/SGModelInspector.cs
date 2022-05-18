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
            foreach (var fieldsPart in PartList.Parts)
            {
                if(fieldsPart.PartName != fieldsPartName)
                    continue;
                if (fieldsPart is SGFieldsInspector fieldsInspector && !fieldsInspector.IsEmpty())
                    return false;
            }

            return true;
        }
    }
}
