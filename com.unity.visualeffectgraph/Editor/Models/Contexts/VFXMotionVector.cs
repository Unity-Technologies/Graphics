using System;
using System.Collections.Generic;
using UnityEditor.Experimental.VFX;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXMotionVector : VFXContext
    {
        public VFXMotionVector() : base(VFXContextType.Update, VFXDataType.Particle, VFXDataType.Particle) {}
        public override string name => "MotionVector";

        private VFXContext m_encapsulatedOutput;
        public VFXContext encapsulatedOutput => m_encapsulatedOutput;
        public void SetEncapsulatedOutput(VFXContext context)
        {
            if (m_encapsulatedOutput != null)
                throw new InvalidOperationException("Unexpected SetEncapsulatedOutput called twice, supposed to be call only once after construction");
            m_encapsulatedOutput = context;
        }

        public override string codeGeneratorTemplate
        {
            get
            {
                return VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXMotionVector";
            }
        }
        public override bool codeGeneratorCompute { get { return true; } }
        public override bool doesIncludeCommonCompute { get { return false; } }

        public override VFXTaskType taskType { get { return VFXTaskType.Update; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (!m_encapsulatedOutput)
                throw new NullReferenceException("Unexpected call of GetExpressionMapper with a null m_encapsulatedOutput");

            var expressionMapper = m_encapsulatedOutput.GetExpressionMapper(target);
            if (target == VFXDeviceTarget.GPU)
            {
                var currentFrameIndex = expressionMapper.FromNameAndId("currentFrameIndex", -1);
                if (currentFrameIndex == null)
                    Debug.LogError("CurrentFrameIndex isn't reachable in encapsulatedOutput for motionVector");

                //Since it's a compute shader without renderer associated, these entries aren't automatically sent
                expressionMapper.AddExpression(VFXBuiltInExpression.LocalToWorld, "unity_ObjectToWorld", -1);
                expressionMapper.AddExpression(VFXBuiltInExpression.WorldToLocal, "unity_WorldToObject", -1);
            }
            return expressionMapper;
        }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                foreach (var inBase in base.implicitPostBlock)
                    yield return inBase;

                if (m_encapsulatedOutput != null)
                {
                    foreach (var block in m_encapsulatedOutput.activeChildrenWithImplicit)
                    {
                        yield return block;
                    }
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
            }
        }
    }
}
