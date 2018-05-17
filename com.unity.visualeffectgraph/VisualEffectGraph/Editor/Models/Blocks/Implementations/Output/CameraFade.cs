using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public enum CameraMode
        {
            AllCameras,
            MainCamera,
            CustomCamera
        }

        public class InputProperties
        {
            [Tooltip("Distance to camera at which the particle will be fully faded")]
            public float FadedDistance = 0.5f;
            [Tooltip("Distance to camera at which the particle will be fully visible")]
            public float VisibleDistance = 2.0f;
        }

        public class InputPropertiesCustomCamera
        {
            [Tooltip("Custom camera used for fading")]
            public CameraType Camera;
        }

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Hide the particle when fully faded")]
        private bool hideWhenFaded = true;

        [SerializeField, VFXSetting, Tooltip("Whether fading should be applied to Color, Alpha or both")]
        private ColorApplicationMode fadeMode = ColorApplicationMode.Alpha;

        [SerializeField, VFXSetting, Tooltip("Which camera to use for fading? (All Cameras is for Output Contexts Only)")]
        private CameraMode cameraMode = CameraMode.MainCamera;

        public override string libraryName { get { return "Camera Fade"; } }
        public override string name { get { return string.Format("Camera Fade ({0})", ObjectNames.NicifyVariableName(fadeMode.ToString())); } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
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

                if (hideWhenFaded)
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.ReadWrite);
            }
        }


        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType("InputProperties");

                if (cameraMode == CameraMode.CustomCamera)
                    properties = properties.Concat(PropertiesFromType("InputPropertiesCustomCamera"));

                return properties;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression fadedDistExp = VFXValue.Constant(1.0f);
                VFXExpression visibleDistExp = VFXValue.Constant(1.0f);

                foreach (var param in base.parameters)
                {
                    // Do not transfer camera, we don't need it
                    if (param.name.StartsWith("Camera_"))
                        continue;

                    if (param.name == "VisibleDistance")
                    {
                        visibleDistExp = param.exp;
                        continue;
                    }

                    if (param.name == "FadedDistance")
                        fadedDistExp = param.exp;

                    yield return param;
                }

                yield return new VFXNamedExpression(new VFXExpressionDivide(VFXValue.Constant(1.0f), new VFXExpressionSubtract(visibleDistExp, fadedDistExp)), "InvFadeDistance");

                if(cameraMode == CameraMode.CustomCamera || cameraMode == CameraMode.MainCamera)
                {
                    VFXExpression cameraTransform;

                    if(cameraMode == CameraMode.CustomCamera)
                        cameraTransform = base.parameters.Where(o => o.name == "Camera_transform").First().exp;
                    else
                        cameraTransform = new VFXExpressionExtractMatrixFromMainCamera();

                    yield return new VFXNamedExpression(new VFXExpressionTransformPosition(cameraTransform, VFXValue.Constant(new Vector3(0, 0, 0))), "CameraPos");
                    yield return new VFXNamedExpression(new VFXExpressionTransformDirection(cameraTransform, VFXValue.Constant(new Vector3(0, 0, -1))), "CameraFront");
                }

            }
        }

        public override string source
        {
            get
            {
                string clipPosW = string.Empty;
                switch (cameraMode)
                {
                    case CameraMode.AllCameras: clipPosW = "TransformPositionVFXToClip(position).w"; break;

                    case CameraMode.MainCamera:
                    case CameraMode.CustomCamera: clipPosW = "dot(position-CameraPos, CameraFront);"; break;
                }


                return string.Format(@"
float clipPosW = {0};
float fade = saturate((clipPosW - FadedDistance) * InvFadeDistance);
{1}
{2}
{3}"
                    , clipPosW
                    , ((fadeMode & ColorApplicationMode.Color) != 0) ? "color *= fade;" : ""
                    , ((fadeMode & ColorApplicationMode.Alpha) != 0) ? "alpha *= fade;" : ""
                    , hideWhenFaded ? "if(fade == 0.0) alive=false;" : "");
            }
        }
    }
}
