using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    //[VFXInfo(category = "Size")] DEPRECATED
    class SizeOverLife : VFXBlock
    {
        [Tooltip("How the new computed size is composed with its previous value")]
        [VFXSetting]
        public AttributeCompositionMode composition;


        public override string name { get { return "Size over Life"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);

                if (composition == AttributeCompositionMode.Overwrite)
                    yield return new VFXAttributeInfo(VFXAttribute.SizeX, VFXAttributeMode.Write);
                else
                    yield return new VFXAttributeInfo(VFXAttribute.SizeX, VFXAttributeMode.ReadWrite);
            }
        }

        private IEnumerable<string> skipInputProperties
        {
            get
            {
                if (composition != AttributeCompositionMode.Blend)
                    yield return "Blend";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                return base.inputProperties.Where(o => !skipInputProperties.Any(a => a == o.property.name));
            }
        }

        public class InputProperties
        {
            [Tooltip("The value of the size over the relative (0..1) lifetime of the particle")]
            public AnimationCurve curve = VFXResources.defaultResources.animationCurve;
            [Tooltip("Size Blending factor")]
            [Range(0.0f, 1.0f)]
            public float Blend = 0.5f;
        }

        public override string source
        {
            get
            {
                string outSource = string.Format(@"
float sampledCurve = SampleCurve(curve, age/lifetime);
{0}", VFXBlockUtility.GetComposeString(composition, "sizeX", "sampledCurve", "Blend"));

                return outSource;
            }
        }

        public override void Sanitize()
        {
            Debug.Log("Sanitizing Graph: Automatically replace SizeOverLife with AttributeOverLife");

            var attributeOverLife = CreateInstance<AttributeOverLife>();

            attributeOverLife.SetSettingValue("attribute", "size");
            attributeOverLife.SetSettingValue("Composition", composition);
            attributeOverLife.SetSettingValue("channels", VariadicChannelOptions.X);

            // Transfer links
            VFXSlot.CopyLinksAndValue(attributeOverLife.GetInputSlot(0), GetInputSlot(0), true);

            ReplaceModel(attributeOverLife, this);
        }
    }
}
