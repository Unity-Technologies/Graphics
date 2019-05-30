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

#region ShaderValueType
        public override PropertyType propertyType => PropertyType.Boolean;
#endregion

#region Capabilities
        public override bool isBatchable => true;
        public override bool isExposable => true;
        public override bool isRenamable => true;
#endregion

#region PropertyBlock
        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}[ToggleUI] {referenceName}(\"{displayName}\", Float) = {(value == true ? 1 : 0)}";
        }
#endregion

#region Utility
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

        public override AbstractShaderProperty Copy()
        {
            var copied = new BooleanShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
#endregion
    }
}
