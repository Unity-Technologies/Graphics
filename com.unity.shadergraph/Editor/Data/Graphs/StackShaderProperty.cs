using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class StackShaderProperty : AbstractShaderProperty<SerializableStack>
    {
        [SerializeField]
        private bool m_Modifiable = true;

        public StackShaderProperty()
        {
            value = new SerializableStack();
            displayName = "Stack";
            slotNames = new List<string>();
            slotNames.Add("Dummy");
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Stack; }
        }

        public bool modifiable
        {
            get { return m_Modifiable; }
            set { m_Modifiable = value; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override bool isBatchable
        {
            // Note we are semi batchable, constants are but texture slots not. Need to clarify this.
            get { return true; }
        }

        public override bool isExposable
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

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();

            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Stack) = {");
            result.Append(GetSlotNamesString(" "));
            result.Append("}");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            // This node needs to generate some properties both in batched as in unbatched mode
            throw new Exception("Don't use this, use GetPropertyDeclarationStringForBatchMode instead");
        }

        public override string GetPropertyDeclarationStringForBatchMode(GenerationMode mode, string delimiter = ";")
        {
            int numSlots = slotNames.Count;

            if (mode == GenerationMode.InConstantBuffer)
            {
                return string.Format("DECLARE_STACK_CB({0}){1}", referenceName, delimiter);
            }
            else
            {
                return string.Format("DECLARE_STACK{0}({1}, {2}){3}", (numSlots <= 1) ? "" : "" + numSlots, referenceName, GetSlotNamesString(), delimiter);
            }
        }

        public override string GetPropertyAsArgumentString()
        {
            throw new NotImplementedException();
            //return string.Format("TEXTURECUBE_PARAM({0}, sampler{0})", referenceName);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Stack )
            {
                name = referenceName,
                stackValue = value.stack
            };
        }

        public override AbstractMaterialNode ToConcreteNode()
        {
            return null;// new StackAssetNode { cubemap = value.stack };
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new StackShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
