using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    [VFXHelpURL("Block-UpdateRotation")]
    [VFXInfo(name = "Integration Update|Rotation", category = "Implicit")]
    class AngularEulerIntegration : VFXBlock
    {
        public override string name => "Integration Update".AppendLabel("Rotation");
        public override VFXContextType compatibleContexts => VFXContextType.Update;
        public override VFXDataType compatibleData => VFXDataType.Particle;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                var data = GetData();

                if (data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityX))
                {
                    yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.AngularVelocityX, VFXAttributeMode.Read);
                }

                if (data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityY))
                {
                    yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.AngularVelocityY, VFXAttributeMode.Read);
                }

                if (data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityZ))
                {
                    yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.AngularVelocityZ, VFXAttributeMode.Read);
                }
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
                string src = string.Empty;
                var data = GetData();

                if (data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityX))
                    src += @"
angleX += angularVelocityX * deltaTime;
";

                if (data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityY))
                    src += @"
angleY += angularVelocityY * deltaTime;
";

                if (data.IsCurrentAttributeWritten(VFXAttribute.AngularVelocityZ))
                    src += @"
angleZ += angularVelocityZ * deltaTime;
";

                return src;
            }
        }
    }
}
