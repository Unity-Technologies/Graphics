using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    // Node a StackShaderProperty has no user settable values it is only used to ensure correct data is emitted into the shader. So we use a dummy value of  "int" type.
    class StackShaderProperty : AbstractShaderProperty<int>
    {
        [SerializeField]
        private bool m_Modifiable = false;

        public StackShaderProperty()
        {
            displayName = "Stack";
            slotNames = new List<string>();
            slotNames.Add("Dummy");
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector1; }
        }

        public bool modifiable
        {
            get { return m_Modifiable; }
            set { m_Modifiable = value; }
        }

        internal override bool isBatchable
        {
            get { return referenceName.EndsWith("_cb"); }
        }

        internal override bool isRenamable
        {
            get { return true; }
        }

        internal override bool isExposable
        {
            get { return true; }
        }

        public List<string> slotNames;

        private string GetSlotNamesString(string delimiter=",")
        {
            var result = new StringBuilder();

            for (int i = 0; i < slotNames.Count; i++)
            {
                if (i != 0) result.Append(delimiter);
                result.Append(slotNames[i]);
            }

            return result.ToString();
        }

        internal override string GetPropertyBlockString()
        {
            return ""; //A stack only has variables declared in the actual shader not in the shaderlab wrapper code
        }

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            // This node needs to generate some properties both in batched as in unbatched mode
            int numSlots = slotNames.Count;

            if (referenceName.EndsWith("_cb"))
            {
                return string.Format("DECLARE_STACK_CB({0}){1}", referenceName.Substring(0, referenceName.Length - 3), delimiter);
            }
            else
            {
                return string.Format("DECLARE_STACK{0}({1}, {2}){3}", (numSlots <= 1) ? "" : "" + numSlots, referenceName, GetSlotNamesString(), delimiter);
            }
        }

        internal override string GetPropertyAsArgumentString()
        {
            throw new NotImplementedException();
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Vector1)
            {
                name = referenceName,
                floatValue = 1.0f
            };
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return null;
        }

        internal override ShaderInput Copy()
        {
            var copied = new StackShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
