using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Size")]
    class SizeOverLife : VFXBlock
    {
        public enum Composition
        {
            Overwrite,
            Scale
        }

        [VFXSetting]
        public Composition composition;

        public override string name { get { return "Size over Life"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.ReadCurrent);
                yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.ReadCurrent);

                if (composition == Composition.Overwrite)
                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.WriteCurrent);
                if (composition == Composition.Scale)
                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.ReadWriteCurrent);
            }
        }

        public class InputProperties
        {
            public AnimationCurve curve;
        }

        public override string source
        {
            get
            {
                string outSource = @"
float sampledCurve = SampleCurve(curve, age/lifetime);
";
                switch (composition)
                {
                    case Composition.Overwrite: outSource += "size = sampledCurve;";    break;
                    case Composition.Scale:     outSource += "size *= sampledCurve;";   break;
                }
                return outSource;
            }
        }
    }
}
