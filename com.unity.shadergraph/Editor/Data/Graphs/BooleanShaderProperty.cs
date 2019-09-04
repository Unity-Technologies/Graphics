using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class BooleanShaderProperty : AbstractShaderProperty<bool>
    {
        public BooleanShaderProperty()
        {
            displayName = "Boolean";
        }

        public override PropertyType propertyType => PropertyType.Boolean;
        
        public override bool isBatchable => true;
        public override bool isExposable => true;
        public override bool isRenamable => true;
        
        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}[ToggleUI]{referenceName}(\"{displayName}\", Float) = {(value == true ? 1 : 0)}";
        }
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new BooleanNode { value = new ToggleData(value) };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                booleanValue = value
            };
        }

        public override ShaderInput Copy()
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
