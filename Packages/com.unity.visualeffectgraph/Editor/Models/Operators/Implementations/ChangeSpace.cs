using System.Linq;

using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-ChangeSpace")]
    [VFXInfo(category = "Math/Geometry")]
    class ChangeSpace : VFXOperatorNumericUniform
    {
        [VFXSetting, SerializeField]
        VFXCoordinateSpace m_targetSpace = VFXCoordinateSpace.Local;

        public class InputProperties
        {
            [Tooltip("Sets the spaceable attribute whose space should be changed. This is useful for converting a world space position or direction to local, or vice-versa. ")]
            public Position x = Position.defaultValue;
        }

        protected override double defaultValueDouble
        {
            get
            {
                return 0.0;
            }
        }

        public override string libraryName { get { return "Change Space"; } }
        public override string name
        {
            get
            {
                return $"Change Space ({ ((GetNbOutputSlots() > 0) ? outputSlots[0].property.type.UserFriendlyName() : "null") })";
            }
        }

        protected override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowSpaceable;
            }
        }

        public sealed override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            return m_targetSpace;
        }

        internal override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            if (m_targetSpace == inputSlots[0].space)
            {
                manager.RegisterError("ChangeSpace_Input_Target_Are_Equals", VFXErrorType.Warning, "The input space and target space are identical. This operator won't do anything.");
            }

            base.GenerateErrors(manager);
        }

        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            //Called from VFXSlot.InvalidateExpressionTree, can be triggered from a space change, need to refresh block warning
            if (cause == InvalidationCause.kExpressionInvalidated)
            {
                model.RefreshErrors();
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            /* Actually, it's automatic because actualOutputSpace return target space
             * See SetOutExpression which use masterSlot.owner.GetOutputSpaceFromSlot
            var currentSpace = inputSlots[0].space;
            if (currentSpace == m_targetSpace)
            {
                return new[] { inputExpression[0] };
            }
            return new[] { ConvertSpace(inputExpression[0], inputSlots[0].GetSpaceTransformationType(), m_targetSpace) };
            */
            return inputExpression.ToArray();
        }
    }
}
