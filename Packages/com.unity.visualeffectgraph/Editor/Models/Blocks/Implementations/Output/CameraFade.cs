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

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, the fade will also affect shadow map generation. This could have unexpected results in the shadow when using multiple cameras.")]
        private bool affectShadows = false;

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

        protected override sealed void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            if (affectShadows && Camera.allCamerasCount > 1)
                manager.RegisterError("CameraFadeShadowsMultipleCamera", VFXErrorType.Warning, "Camera fade in shadow maps may be incorrect when rendered in more than one camera.");
        }

        public override string source
        {
            get
            {
                string outCode = @"float fade = 1;
#if VFX_PASSDEPTH == VFX_PASSDEPTH_SHADOW";
                if (affectShadows)
                {
                    outCode += @"
float3 posWS = TransformPositionVFXToWorld(position);
float3 posToCamera = VFXTransformPositionWorldToCameraRelative(posWS);
float distance = dot(posToCamera, VFXGetCameraWorldDirection());
fade = saturate((distance - FadedDistance) * InvFadeDistance);";
                }

                outCode += @"
#else
float distance = TransformPositionVFXToClip(position).w;
fade = saturate((distance - FadedDistance) * InvFadeDistance);
#endif
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
