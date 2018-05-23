using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Output")]
    class CameraFade : VFXBlock
    {
        public enum ColorApplicationMode
        {
            Color = 1 << 0,
            Alpha = 1 << 1,
            ColorAndAlpha = Color | Alpha,
        }

        public class InputProperties
        {
            [Tooltip("Distance to camera at which the particle will be fully faded")]
            public float FadedDistance = 0.5f;
            [Tooltip("Distance to camera at which the particle will be fully visible")]
            public float VisibleDistance = 2.0f;
        }

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Cull the particle when fully faded, to reduce overdraw")]
        private bool cullWhenFaded = true;

        [SerializeField, VFXSetting, Tooltip("Whether fading should be applied to Color, Alpha or both")]
        private ColorApplicationMode fadeMode = ColorApplicationMode.Alpha;

        public override string libraryName { get { return "Camera Fade"; } }
        public override string name { get { return string.Format("Camera Fade ({0})", ObjectNames.NicifyVariableName(fadeMode.ToString())); } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);

                if ((fadeMode & ColorApplicationMode.Alpha) != 0)
                    yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.ReadWrite);
                if ((fadeMode & ColorApplicationMode.Color) != 0)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.ReadWrite);

                if (cullWhenFaded)
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.ReadWrite);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression fadedDistExp = null;
                VFXExpression visibleDistExp = null;

                foreach (var param in base.parameters)
                {
                    if (param.name == "VisibleDistance")
                    {
                        visibleDistExp = param.exp;
                        continue;
                    }

                    if (param.name == "FadedDistance")
                        fadedDistExp = param.exp;

                    yield return param;
                }

                if (visibleDistExp == null)
                    throw new Exception("Could not find VisibleDistance inputProperty");

                if (fadedDistExp == null)
                    throw new Exception("Could not find FadedDistance inputProperty");

                yield return new VFXNamedExpression(new VFXExpressionDivide(VFXOperatorUtility.OneExpression[VFXValueType.Float], new VFXExpressionSubtract(visibleDistExp, fadedDistExp)), "InvFadeDistance");
            }
        }

        public override string source
        {
            get
            {
                return string.Format(@"
float clipPosW = TransformPositionVFXToClip(position).w;
float fade = saturate((clipPosW - FadedDistance) * InvFadeDistance);
{0}
{1}
{2}"
                    , ((fadeMode & ColorApplicationMode.Color) != 0) ? "color *= fade;" : ""
                    , ((fadeMode & ColorApplicationMode.Alpha) != 0) ? "alpha *= fade;" : ""
                    , cullWhenFaded && GetParent().contextType == VFXContextType.kOutput ? "if(fade == 0.0) alive=false;" : "");
            }
        }
    }
}
