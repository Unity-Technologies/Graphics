using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.VFX.Test.Unit.Operators
{
    [TestFixture]
    class VFXOperatorAddressingTests
    {
        public struct ApplyAddressingModeTestCase
        {
            public ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode _mode, uint _count)
            {
                mode = _mode;
                count = _count;
                expectedSequence = new uint[Mathf.Max(50, 7 * (int)_count)];

                //Naive implementation for reference
                if (mode == VFXOperatorUtility.SequentialAddressingMode.Clamp)
                {
                    for (uint i = 0; i < expectedSequence.Length; ++i)
                    {
                        expectedSequence[i] = i < count ? i : count - 1;
                    }
                }
                else if (mode == VFXOperatorUtility.SequentialAddressingMode.Wrap)
                {
                    uint current = 0u;
                    for (uint i = 0; i < expectedSequence.Length; ++i)
                    {
                        expectedSequence[i] = current;
                        current++;
                        if (current >= count)
                            current = 0u;
                    }
                }
                else if (mode == VFXOperatorUtility.SequentialAddressingMode.Mirror)
                {
                    uint current = 0u;
                    bool increment = true;
                    for (uint i = 0; i < expectedSequence.Length; ++i)
                    {
                        expectedSequence[i] = current;
                        if (increment)
                        {
                            current++;
                            if (current >= count)
                            {
                                increment = false;
                                current = count > 2u ? count - 2u : 0u;
                            }
                        }
                        else
                        {
                            if (current == 0u)
                            {
                                increment = true;
                                current = count == 1u ? 0u : 1u;
                            }
                            else
                            {
                                current--;
                            }
                        }
                    }
                }
            }

            public VFXOperatorUtility.SequentialAddressingMode mode;
            public uint count;
            public uint[] expectedSequence;

            public override string ToString()
            {
                return string.Format("{0}_{1}_{2}", mode.ToString(), count, expectedSequence.Length);
            }
        }


        static readonly ApplyAddressingModeTestCase[] ApplyAddressingModeTestCase_ValueSource =
        {
            //The 0 case is always undefined
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Wrap, 1u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Wrap, 4u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Clamp, 1u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Clamp, 4u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 1u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 2u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 3u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 4u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 7u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 8u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 9u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 13u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 15u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 27u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 32u),
            new ApplyAddressingModeTestCase(VFXOperatorUtility.SequentialAddressingMode.Mirror, 33u),
        };

        [Test]
        public void CheckExpectedSequence_ApplyAddressingMode([ValueSource("ApplyAddressingModeTestCase_ValueSource")] ApplyAddressingModeTestCase addressingMode)
        {
            var computedSequence = new uint[addressingMode.expectedSequence.Length];
            for (uint index = 0u; index < computedSequence.Length; ++index)
            {
                var indexExpr = VFXValue.Constant(index);
                var countExpr = VFXValue.Constant(addressingMode.count);
                var computed = VFXOperatorUtility.ApplyAddressingMode(indexExpr, countExpr, addressingMode.mode);

                var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation);
                var result = context.Compile(computed);

                computedSequence[index] = result.Get<uint>();
            }

            for (uint index = 0u; index < computedSequence.Length; ++index)
            {
                Assert.AreEqual(addressingMode.expectedSequence[index], computedSequence[index]);
            }
        }
    }
}
