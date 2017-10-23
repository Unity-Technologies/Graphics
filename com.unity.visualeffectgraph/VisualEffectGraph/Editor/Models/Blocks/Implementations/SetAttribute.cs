using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute", autoRegister = false)]
    class SetAttribute : VFXBlock
    {
        [VFXSetting]
        [StringProvider(typeof(AttributeProvider))]
        public string attribute = VFXAttribute.All.First();

        public override string name { get { return "Set Attribute " + attribute; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                return new List<VFXAttributeInfo>() { new VFXAttributeInfo(currentAttribute, VFXAttributeMode.Write) };
            }
        }
        static private string GenerateLocalAttributeName(string name)
        {
            return name[0].ToString().ToUpper() + name.Substring(1);
        }

        public override string source
        {
            get
            {
                var attribute = currentAttribute;
                return string.Format("{0} = {1};", attribute.name, GenerateLocalAttributeName(attribute.name));
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), GenerateLocalAttributeName(currentAttribute.name)), currentAttribute.value.GetContent());
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return VFXAttribute.Find(attribute);
            }
        }
    }
}
