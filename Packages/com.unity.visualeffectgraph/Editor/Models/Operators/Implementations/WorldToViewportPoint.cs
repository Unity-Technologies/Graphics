using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-WorldToViewportPoint")]
    [VFXInfo(category = "Camera")]
    class WorldToViewportPoint : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the position to be transformed.")]
            public Position position;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var inputs = PropertiesFromType("InputProperties");

                if (camera == Block.CameraMode.Custom)
                    inputs = inputs.Concat(PropertiesFromType(typeof(Block.CameraHelper.CameraProperties)));

                return inputs;
            }
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the position in viewport space, normalized and relative to the camera. The bottom-left of the camera is (0,0); the top-right is (1,1). The z position is in world units from the camera.")]
            public Vector3 viewportPos;
        }

        [VFXSetting, Tooltip("Specifies which Camera to use to project the position. Can use the camera tagged 'Main', or a custom camera.")]
        public Block.CameraMode camera = Block.CameraMode.Main;

        override public string name { get { return "World To Viewport Point"; } }

        public override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot outputSlot)
        {
            return VFXCoordinateSpace.World;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expressions = Block.CameraHelper.AddCameraExpressions(GetExpressionsFromSlots(this), camera);
            // camera matrix is already in world even in custom mode due to GetOutputSpaceFromSlot returning world space
            Block.CameraMatricesExpressions matricesExpressions = Block.CameraHelper.GetMatricesExpressions(expressions, VFXCoordinateSpace.World, VFXCoordinateSpace.World);

            // result = position * VFXToView * ViewToClip
            VFXExpression positionExpression = inputExpression[0];
            VFXExpression viewPosExpression = new VFXExpressionTransformPosition(matricesExpressions.VFXToView.exp, positionExpression);
            VFXExpression clipPosExpression = new VFXExpressionTransformVector4(matricesExpressions.ViewToClip.exp, VFXOperatorUtility.CastFloat(viewPosExpression, VFXValueType.Float4, 1.0f));

            // normalize using w component and renormalize to range [0, 1]
            VFXExpression halfExpression = VFXValue.Constant(0.5f);
            VFXExpression normalizedExpression = new VFXExpressionCombine(new VFXExpression[]
            {
                (clipPosExpression.x / clipPosExpression.w) * halfExpression + halfExpression,
                (clipPosExpression.y / clipPosExpression.w) * halfExpression + halfExpression,
                viewPosExpression.z     // The z position is in world units from the camera
            });

            return new VFXExpression[]
            {
                normalizedExpression
            };
        }
    }
}
