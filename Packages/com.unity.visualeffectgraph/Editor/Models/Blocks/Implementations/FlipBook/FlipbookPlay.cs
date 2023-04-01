using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    [VFXHelpURL("Block-FlipbookPlayer")]
    [VFXInfo(category = "FlipBook")]
    class FlipbookPlay : VFXBlock
    {
        public enum Mode
        {
            Constant,
            CurveOverLife,
        }

        [VFXSetting, Tooltip("Specifies whether particles use a constant frame rate or a curve sampled over the particle’s lifetime when playing the flipbook.")]
        public Mode mode = Mode.Constant;

        public override string name { get { return "Flipbook Player"; } }

        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }

        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.ReadWrite);
                if (mode == Mode.CurveOverLife)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                }
            }
        }
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case Mode.Constant: return PropertiesFromType("InputPropertiesConstant");
                    case Mode.CurveOverLife: return PropertiesFromType("InputPropertiesCurveOverLife");
                    default: throw new NotImplementedException("Unimplemented enum in FlipbookPlay.inputProperties :" + mode);
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                // this function gets all the InputProperties members and sends them to the node block
                foreach (var p in GetExpressionsFromSlots(this)) yield return p;

                // this statement enables using deltaTime builtin expressin in the nodeblock Source.
                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public class InputPropertiesConstant
        {
            [Tooltip("Sets the frame rate of the flipbook in frames per second.")]
            public float FrameRate = 25.0f;
        }

        public class InputPropertiesCurveOverLife
        {
            [Tooltip("Sets the frame rate of the flipbook from a curve sampled over its lifetime.")]
            public AnimationCurve FrameRate = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 25.0f), new Keyframe(1.0f, 2.0f) });
        }

        // Source code is the actual code of your nodeblock where you can access properties, attributes and optionally parameters.
        public override string source
        {
            get
            {
                string advance = "";
                switch (mode)
                {
                    case Mode.Constant: advance = "FrameRate"; break;
                    case Mode.CurveOverLife: advance = "SampleCurve(FrameRate, age/lifetime)"; break;
                }

                string outSource = string.Format("texIndex += {0} * deltaTime;", advance);
                return outSource;
            }
        }
    }
}
