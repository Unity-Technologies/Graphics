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
        public enum AttributeMapSampleMode
        {
            IndexRelative,
            Index,
            Sequential,
            Sample2DLOD,
            Sample3DLOD,
            Random,
            RandomUniformPerParticle,
        }

        [VFXSetting]
        [StringProvider(typeof(AttributeProvider))]
        [Tooltip("Target Attribute")]
        public string attribute = VFXAttribute.All.First();

        [VFXSetting]
        [Tooltip("How to compose the attribute with its previous value")]
        public AttributeCompositionMode Composition = AttributeCompositionMode.Overwrite;

        [VFXSetting]
        [Tooltip("How to sample inside the AttributeMap")]
        public AttributeMapSampleMode SampleMode = AttributeMapSampleMode.RandomUniformPerParticle;

        public override string name { get { return "Attribute from Map"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(currentAttribute, Composition == AttributeCompositionMode.Overwrite ? VFXAttributeMode.Write : VFXAttributeMode.ReadWrite);
                if (SampleMode == AttributeMapSampleMode.Sequential) yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
                if (SampleMode == AttributeMapSampleMode.Random) yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                if (SampleMode == AttributeMapSampleMode.RandomUniformPerParticle) yield return new VFXAttributeInfo(VFXAttribute.Phase, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                string textureInputPropertiesType = "InputProperties2DTexture";

                if (SampleMode == AttributeMapSampleMode.Sample3DLOD)
                    textureInputPropertiesType = "InputProperties3DTexture";

                foreach (var prop in PropertiesFromType(textureInputPropertiesType))
                    yield return prop;


                if (SampleMode == AttributeMapSampleMode.IndexRelative)
                {
                    foreach (var prop in PropertiesFromType("InputPropertiesRelative"))
                        yield return prop;
                }

                if (SampleMode == AttributeMapSampleMode.Index)
                {
                    foreach (var prop in PropertiesFromType("InputPropertiesIndex"))
                        yield return prop;
                }

                if (SampleMode == AttributeMapSampleMode.Sample2DLOD)
                {
                    foreach (var prop in PropertiesFromType("InputPropertiesSample2DLOD"))
                        yield return prop;
                }

                if (SampleMode == AttributeMapSampleMode.Sample3DLOD)
                {
                    foreach (var prop in PropertiesFromType("InputPropertiesSample3DLOD"))
                        yield return prop;
                }
                if (Composition == AttributeCompositionMode.Blend)
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
                string biasScale = "value = (value  + valueBias) * valueScale;";
                string output = "";
                var attribute = currentAttribute;
                string attributeName = attribute.name;

                if (SampleMode == AttributeMapSampleMode.Sample2DLOD || SampleMode == AttributeMapSampleMode.Sample3DLOD)
                {
                    output += string.Format(@"
{0} value = ({0})attributeMap.t.SampleLevel(attributeMap.s, SamplePosition, LOD);
{1}
", GetCompatTypeString(attribute), biasScale);
                }
                else // All other SampleModes
                {
                    string samplePos = "0";
                    switch (SampleMode)
                    {
                        case AttributeMapSampleMode.IndexRelative: samplePos = "relativePos * count"; break;
                        case AttributeMapSampleMode.Index: samplePos = "index % count"; break;
                        case AttributeMapSampleMode.Sequential: samplePos = "particleId % count"; break;
                        case AttributeMapSampleMode.Random: samplePos = "RAND * count"; break;
                        case AttributeMapSampleMode.RandomUniformPerParticle: samplePos = "phase * count"; break;
                    }

                    output += string.Format(@"
uint width, height;
attributeMap.t.GetDimensions(width, height);
uint count = width * height;
uint id = clamp(uint({0}), 0, count - 1);
{1} value = ({1})attributeMap.t.Load(int3(id % width, id / width,0));
{2}
", samplePos, GetCompatTypeString(attribute), biasScale);
                }

                if (Composition != AttributeCompositionMode.Blend)
                    return output + string.Format(VFXBlockUtility.GetComposeFormatString(Composition), attributeName, "value");
                else
                    return output + string.Format(VFXBlockUtility.GetComposeFormatString(Composition), attributeName, "value", "blend");
            }
        }

        public class InputProperties2DTexture
        {
            [Tooltip("AttributeMap texture to read attributes from")]
            public Texture2D attributeMap;
        }
        public class InputProperties3DTexture
        {
            [Tooltip("3D AttributeMap texture to read attributes from")]
            public Texture3D attributeMap;
        }

        public class InputPropertiesRelative
        {
            [Tooltip("Position in range [0..1] to sample")]
            public float relativePos = 0.0f;
        }

        public class InputPropertiesIndex
        {
            [Tooltip("Absolute index to sample")]
            public uint index = 0;
        }

        public class InputPropertiesSample2DLOD
        {
            [Tooltip("Absolute index to sample")]
            public Vector2 SamplePosition = Vector2.zero;
            public float LOD = 0.0f;
        }
        public class InputPropertiesSample3DLOD
        {
            [Tooltip("Absolute index to sample")]
            public Vector3 SamplePosition = Vector2.zero;
            public float LOD = 0.0f;
        }

        public class InputPropertiesBlend
        {
            [Tooltip("Blend fraction with previous value")]
            public float blend = 0.5f;
        }

        public class InputPropertiesScaleFloat
        {
            [Tooltip("Bias Applied to the read float value")]
            public float valueBias = 0.0f;
            [Tooltip("Scale Applied to the read float value")]
            public float valueScale = 1.0f;
        }
        public class InputPropertiesScaleFloat2
        {
            [Tooltip("Bias Applied to the read Vector2 value")]
            public Vector2 valueBias = new Vector2(0.0f, 0.0f);
            [Tooltip("Scale Applied to the read Vector2 value")]
            public Vector2 valueScale = new Vector2(1.0f, 1.0f);
        }
        public class InputPropertiesScaleFloat3
        {
            [Tooltip("Bias Applied to the read Vector3 value")]
            public Vector3 valueBias = new Vector3(0.0f, 0.0f, 0.0f);
            [Tooltip("Scale Applied to the read Vector3 value")]
            public Vector3 valueScale = new Vector3(1.0f, 1.0f, 1.0f);
        }

        public class InputPropertiesScaleFloat4
        {
            [Tooltip("Bias Applied to the read Vector4 value")]
            public Vector4 valueBias = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            [Tooltip("Scale Applied to the read Vector4 value")]
            public Vector4 valueScale = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        }

        private VFXAttribute currentAttribute { get { return VFXAttribute.Find(attribute); } }

        private static string GetCompatTypeString(VFXAttribute attrib)
        {
            if (!VFXExpression.IsUniform(attrib.type))
                throw new InvalidOperationException("Trying to fetch an attribute of type: " + attrib.type);

            return VFXExpression.TypeToCode(attrib.type);
        }
    }
}
