using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.BooleanShaderProperty")]
    public sealed class BooleanShaderProperty : AbstractShaderProperty<bool>
    {
        internal BooleanShaderProperty()
        {
            displayName = "Boolean";
        }

        public override PropertyType propertyType => PropertyType.Boolean;

        internal override bool isBatchable => true;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}[ToggleUI]{referenceName}(\"{displayName}\", Float) = {(value == true ? 1 : 0)}";
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new BooleanNode { value = new ToggleData(value) };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                booleanValue = value
            };
        }

        internal override ShaderInput Copy()
        {
            return new BooleanShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
