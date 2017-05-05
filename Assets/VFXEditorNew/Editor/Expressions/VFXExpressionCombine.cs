using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace UnityEditor.VFX
{
    class VFXExpressionCombine : VFXExpressionFloatOperation
    {
        public VFXExpressionCombine() : this(new VFXExpression[] { VFXValueFloat.Default, VFXValueFloat.Default }
                                             )
        {
        }

        public VFXExpressionCombine(params VFXExpression[] parents)
        {
            if (parents.Length <= 1 || parents.Length > 4 || parents.Any(o => !IsFloatValueType(o.ValueType)))
            {
                throw new ArgumentException("Incorrect VFXExpressionCombine");
            }

            m_Parents = parents;
            switch (m_Parents.Length)
            {
                case 2:
                    m_Operation = VFXExpressionOp.kVFXCombine2fOp;
                    m_ValueType = VFXValueType.kFloat2;
                    break;
                case 3:
                    m_Operation = VFXExpressionOp.kVFXCombine3fOp;
                    m_ValueType = VFXValueType.kFloat3;
                    break;
                case 4:
                    m_Operation = VFXExpressionOp.kVFXCombine4fOp;
                    m_ValueType = VFXValueType.kFloat4;
                    break;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            var constParentFloat = reducedParents.Cast<VFXValueFloat>().Select(o => o.Get()).ToArray();
            if (constParentFloat.Length != m_Parents.Length)
            {
                throw new ArgumentException("Incorrect VFXExpressionCombine.ExecuteConstantOperation");
            }

            switch (m_Parents.Length)
            {
                case 2: return new VFXValueFloat2(new Vector2(constParentFloat[0], constParentFloat[1]), true);
                case 3: return new VFXValueFloat3(new Vector3(constParentFloat[0], constParentFloat[1], constParentFloat[2]), true);
                case 4: return new VFXValueFloat4(new Vector4(constParentFloat[0], constParentFloat[1], constParentFloat[2], constParentFloat[3]), true);
            }
            return null;
        }

        sealed public override string GetOperationCodeContent()
        {
            return string.Format("return {0}({1});", TypeToCode(ValueType), Parents.Select((o, i) => ParentsCodeName[i]).Aggregate((a, b) => string.Format("{0}, {1}", a, b)));
        }
    }
}
