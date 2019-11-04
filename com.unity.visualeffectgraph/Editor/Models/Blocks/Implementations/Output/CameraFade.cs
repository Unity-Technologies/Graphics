using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Output")]
    class CameraFade : VFXBlock
    {
        public enum FadeApplicationMode
        {
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha,
        }

        public class InputProperties
        {
            [Tooltip("Sets the distance from the camera at which the particle is fully faded out.")]
            public float FadedDistance = 0.5f;
            [Tooltip("Sets the distance from the camera at which the particle is fully visible and not affected by the camera fade.")]
            public float VisibleDistance = 2.0f;
        }

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, the particle is culled when it is fully faded to reduce overdraw.")]
        private bool cullWhenFaded = true;

        [SerializeField, VFXSetting, Tooltip("Specifies how the particle fades out when it gets near the camera. It can have its alpha fade out, its color fade to black, or both. ")]
        private FadeApplicationMode fadeMode = FadeApplicationMode.Alpha;

        public override string libraryName { get { return "Camera Fade"; } }
        public override string name { get { return string.Format("Camera Fade ({0})", ObjectNames.NicifyVariableName(fadeMode.ToString())); } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.Output; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);

                if ((fadeMode & FadeApplicationMode.Alpha) != 0)
                    yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.ReadWrite);
                if ((fadeMode & FadeApplicationMode.Color) != 0)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.ReadWrite);

                if (cullWhenFaded)
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Write);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var param in base.parameters)
                {
                    if (param.name == "VisibleDistance") continue;
                    yield return param;
                }

                VFXExpression visibleDistExp = base.parameters.Where(o => o.name == "VisibleDistance").First().exp;

                if (visibleDistExp == null)
                    throw new Exception("Could not find VisibleDistance inputProperty");

                VFXExpression fadedDistExp = base.parameters.Where(o => o.name == "FadedDistance").First().exp;

                if (fadedDistExp == null)
                    throw new Exception("Could not find FadedDistance inputProperty");

                yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(new VFXExpressionSubtract(visibleDistExp, fadedDistExp)), "InvFadeDistance");
            }
        }

        public override string source
        {
            get
            {
                string outCode = @"
float clipPosW = TransformPositionVFXToClip(position).w;
float fade = saturate((clipPosW - FadedDistance) * InvFadeDistance);
";
                if ((fadeMode & FadeApplicationMode.Color) != 0)
                    outCode += "color *= fade;\n";

                if ((fadeMode & FadeApplicationMode.Alpha) != 0)
                    outCode += "alpha *= fade;\n";

                if (cullWhenFaded)
                    outCode += "if(fade == 0.0) alive=false;";

                return outCode;
            }
        }
    }
}
