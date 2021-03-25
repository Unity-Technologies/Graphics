using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Camera")]
    class ViewportToWorldPoint : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The position in viewport space, normalized and relative to the camera. The bottom-left of the camera is (0,0); the top-right is (1,1). The z position is in world units from the camera.")]
            public Vector3 viewportPosition;
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
            [Tooltip("Outputs the transformed position in world space.")]
            public Position position;
        }

        [VFXSetting, Tooltip("Specifies which Camera to use to project the position. Can use the camera tagged 'Main', or a custom camera.")]
        public Block.CameraMode camera = Block.CameraMode.Main;

        override public string name { get { return "Viewport To World Point"; } }

        public override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot outputSlot)
        {
            return VFXCoordinateSpace.World;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expressions = Block.CameraHelper.AddCameraExpressions(GetExpressionsFromSlots(this), camera);
            // camera matrix is already in world even in custom mode due to GetOutputSpaceFromSlot returning world space
            Block.CameraMatricesExpressions matricesExpressions = Block.CameraHelper.GetMatricesExpressions(expressions, VFXCoordinateSpace.World, VFXCoordinateSpace.World);

            // renormalize XY from viewport position [0, 1] to clip position [-1, 1]
            VFXExpression viewportPosExpression = inputExpression[0];
            VFXExpression oneExpression = VFXValue.Constant(1.0f);
            VFXExpression twoExpression = VFXValue.Constant(2.0f);
            VFXExpression clipPosExpression = new VFXExpressionCombine(viewportPosExpression.x * twoExpression - oneExpression, viewportPosExpression.y * twoExpression - oneExpression, oneExpression, oneExpression);

            // result = clipPos * ClipToView * ViewToVFX
            VFXExpression viewPosExpression = new VFXExpressionTransformVector4(matricesExpressions.ClipToView.exp, clipPosExpression);
            viewPosExpression = new VFXExpressionCombine(viewPosExpression.x, viewPosExpression.y, viewPosExpression.z) * viewportPosExpression.zzz;
            VFXExpression positionExpression = new VFXExpressionTransformPosition(matricesExpressions.ViewToVFX.exp, viewPosExpression);

            return new VFXExpression[]
            {
                positionExpression
            };
        }
    }
}
