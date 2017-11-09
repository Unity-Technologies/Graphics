using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute")]
    class AttributeFromMap : VFXBlock
    {
        // TODO: Let's factorize this this into a utility class
        public enum RandomMode
        {
            Constant,
            Random,
            RandomUniformPerParticle,
        }

        public enum AttributeMapDataLayout
        {
            Unsigned8Bits,
            Signed8BitsGrayCentered,
            SignedFloat,
        }

        [VFXSetting]
        [StringProvider(typeof(AttributeProvider))]
        public string attribute = VFXAttribute.All.First();

        [VFXSetting]
        [Tooltip("How to compose the attribute with its previous value")]
        public AttributeCompositionMode composition = AttributeCompositionMode.Overwrite;

        [VFXSetting]
        [Tooltip("How to sample inside the AttributeMap")]
        public RandomMode randomMode = RandomMode.RandomUniformPerParticle;

        [VFXSetting]
        [Tooltip("How the data is stored in the Texture")]
        public AttributeMapDataLayout Layout = AttributeMapDataLayout.SignedFloat;

        public override string name { get { return "Attribute from Map"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(currentAttribute, composition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
                if (randomMode == RandomMode.Random) yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                if (randomMode == RandomMode.RandomUniformPerParticle) yield return new VFXAttributeInfo(VFXAttribute.Phase, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var prop in PropertiesFromType("InputProperties"))
                    yield return prop;

                if (randomMode == RandomMode.Constant)
                {
                    foreach (var prop in PropertiesFromType("InputPropertiesConstant"))
                        yield return prop;
                }

                if (composition == AttributeCompositionMode.Blend)
                {
                    foreach (var prop in PropertiesFromType("InputPropertiesBlend"))
                        yield return prop;
                }

                if (VFXExpression.IsUniform(currentAttribute.type))
                {
                    string scaleInputPropertiesType = "InputPropertiesScaleFloat";
                    int count = VFXExpression.TypeToSize(currentAttribute.type);
                    switch (count)
                    {
                        case 2: scaleInputPropertiesType = "InputPropertiesScaleFloat2"; break;
                        case 3: scaleInputPropertiesType = "InputPropertiesScaleFloat3"; break;
                        case 4: scaleInputPropertiesType = "InputPropertiesScaleFloat4"; break;
                    }

                    foreach (var prop in PropertiesFromType(scaleInputPropertiesType))
                        yield return prop;
                }
            }
        }

        public override string source
        {
            get
            {
                var attribute = currentAttribute;
                string attributeName = attribute.name;

                string samplePos = "0";
                switch (randomMode)
                {
                    case RandomMode.Constant:                   samplePos = "uint(relativePos * count)"; break;
                    case RandomMode.Random:                     samplePos = "uint(RAND * count)"; break;
                    case RandomMode.RandomUniformPerParticle:   samplePos = "uint(phase * count)"; break;
                }

                string scale = (Layout == AttributeMapDataLayout.Signed8BitsGrayCentered) ? "value = (value - 0.5f) * valueScale;" : "value *= valueScale;";
                string output = string.Format(@"
uint width, height;
attributeMap.t.GetDimensions(width, height);
uint count = width * height;
uint id = {0};
{1} value = attributeMap.t[uint2(id % width, id / width)]{2};
{3}
", samplePos, GetCompatTypeString(attribute), GetCompatTypeSubscript(attribute), scale);
                if (composition != AttributeCompositionMode.Blend)
                    return output + string.Format(VFXBlockUtility.GetComposeFormatString(composition), attributeName, "value");
                else
                    return output + string.Format(VFXBlockUtility.GetComposeFormatString(composition), attributeName, "value", "blend");
            }
        }

        public class InputProperties
        {
            public Texture2D attributeMap;
        }

        public class InputPropertiesConstant
        {
            public float relativePos = 0.0f;
        }
        public class InputPropertiesBlend
        {
            public float blend = 0.5f;
        }

        public class InputPropertiesScaleFloat
        {
            public float valueScale = 1.0f;
        }
        public class InputPropertiesScaleFloat2
        {
            public Vector2 valueScale = new Vector2(1.0f, 1.0f);
        }
        public class InputPropertiesScaleFloat3
        {
            public Vector3 valueScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
        public class InputPropertiesScaleFloat4
        {
            public Vector4 valueScale = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        }

        private string GetCompatTypeString(VFXAttribute attrib)
        {
            if (!VFXExpression.IsUniform(attrib.type))
                throw new InvalidOperationException("Trying to fetch an attribute of type: " + attrib.type);

            return VFXExpression.TypeToCode(attrib.type);
        }

        private string GetCompatTypeSubscript(VFXAttribute attrib)
        {
            if (!VFXExpression.IsUniform(attrib.type))
                throw new InvalidOperationException("Trying to fetch an attribute of type: " + attrib.type);

            int count = VFXExpression.TypeToSize(attrib.type);
            switch (count)
            {
                case 2: return ".xy";
                case 3: return ".xyz";
                case 4: return ".xyzw";
            }
            return "";
        }

        private VFXAttribute currentAttribute { get { return VFXAttribute.Find(attribute); } }

        static private string GenerateLocalAttributeName(string name)
        {
            return name[0].ToString().ToUpper() + name.Substring(1);
        }
    }
}
