using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    [VFXHelpURL("Block-UpdatePosition")]
    [VFXInfo(name = "Integration Update|Position", category = "Implicit")]
    class EulerIntegration : VFXBlock
    {
        public override string name => "Integration Update".AppendLabel("Position");
        public override VFXContextType compatibleContexts => VFXContextType.Update;
        public override VFXDataType compatibleData => VFXDataType.Particle;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override string source
        {
            get
            {
                return "position += velocity * deltaTime;";
            }
        }
    }
}
