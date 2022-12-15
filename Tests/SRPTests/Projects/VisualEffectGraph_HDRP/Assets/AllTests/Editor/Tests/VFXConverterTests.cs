#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System.Runtime.InteropServices;
using UnityEngine;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine.VFX;
using UnityEngine.TestTools;
using System;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXConverterTests
    {
        public struct Conversion
        {
            public object value;
            public System.Type targetType;
            public object expectedResult;


            public override string ToString()
            {
                return string.Format("Convert {0} to type {1} is {2}", value == null ? (object)"null" : value, targetType.UserFriendlyName(), expectedResult == null ? (object)"null" : expectedResult);
            }
        }

        static Conversion[] s_Conversions;

        static Conversion[] s_FailingConversions;

        static Conversion[] conversions
        {
            get { BuildValueSources(); return s_Conversions; }
        }
        static Conversion[] failingConversions
        {
            get { BuildValueSources(); return s_FailingConversions; }
        }


        [OneTimeTearDown]
        public void TearDown()
        {
        }

        static void BuildValueSources()
        {
            if (s_Conversions == null)
            {
                s_Conversions = new Conversion[]
                {
                    // Vector3
                    new Conversion {value = new Vector3(1, 2, 3), targetType = typeof(Vector2), expectedResult = new Vector2(1, 2)},
                    new Conversion {value = new Vector3(1, 2, 3), targetType = typeof(Vector3), expectedResult = new Vector3(1, 2, 3)},
                    new Conversion
                    {
                        value = new Vector3(1, 2, 3),
                        targetType = typeof(Vector4),
                        expectedResult = new Vector4(1, 2, 3, 0)
                    },
                    new Conversion {value = new Vector3(1, 2, 3), targetType = typeof(float), expectedResult = 1.0f},
                    new Conversion {value = new Vector3(1, 2, 3), targetType = typeof(int), expectedResult = 1},
                    new Conversion {value = new Vector3(1, 2, 3), targetType = typeof(uint), expectedResult = 1u},
                    new Conversion
                    {
                        value = new Vector3(0.1f, 0.2f, 0.3f),
                        targetType = typeof(Color),
                        expectedResult = new Color(0.1f, 0.2f, 0.3f)
                    },
                    // Vector2
                    new Conversion {value = new Vector2(1, 2), targetType = typeof(Vector2), expectedResult = new Vector2(1, 2)},
                    new Conversion {value = new Vector2(1, 2), targetType = typeof(Vector3), expectedResult = new Vector3(1, 2, 0)},
                    new Conversion {value = new Vector2(1, 2), targetType = typeof(Vector4), expectedResult = new Vector4(1, 2, 0, 0)},
                    new Conversion {value = new Vector2(1, 2), targetType = typeof(float), expectedResult = 1.0f},
                    new Conversion {value = new Vector2(1, 2), targetType = typeof(int), expectedResult = 1},
                    new Conversion {value = new Vector2(1, 2), targetType = typeof(uint), expectedResult = 1u},
                    new Conversion
                    {
                        value = new Vector2(0.1f, 0.2f),
                        targetType = typeof(Color),
                        expectedResult = new Color(0.1f, 0.2f, 0.0f)
                    },
                    // Vector4
                    new Conversion {value = new Vector4(1, 2, 3, 4), targetType = typeof(Vector2), expectedResult = new Vector2(1, 2)},
                    new Conversion
                    {
                        value = new Vector4(1, 2, 3, 4),
                        targetType = typeof(Vector3),
                        expectedResult = new Vector3(1, 2, 3)
                    },
                    new Conversion
                    {
                        value = new Vector4(1, 2, 3, 4),
                        targetType = typeof(Vector4),
                        expectedResult = new Vector4(1, 2, 3, 4)
                    },
                    new Conversion {value = new Vector4(1, 2, 3, 4), targetType = typeof(float), expectedResult = 1.0f},
                    new Conversion {value = new Vector4(1, 2, 3, 4), targetType = typeof(int), expectedResult = 1},
                    new Conversion {value = new Vector4(1, 2, 3, 4), targetType = typeof(uint), expectedResult = 1u},
                    new Conversion
                    {
                        value = new Vector4(0.1f, 0.2f, 0.3f, 0.4f),
                        targetType = typeof(Color),
                        expectedResult = new Color(0.1f, 0.2f, 0.3f, 0.4f)
                    },
                    // Color
                    new Conversion
                    {
                        value = new Color(0.1f, 0.2f, 0.3f, 0.4f),
                        targetType = typeof(Vector2),
                        expectedResult = new Vector2(0.1f, 0.2f)
                    },
                    new Conversion
                    {
                        value = new Color(0.1f, 0.2f, 0.3f, 0.4f),
                        targetType = typeof(Vector3),
                        expectedResult = new Vector3(0.1f, 0.2f, 0.3f)
                    },
                    new Conversion
                    {
                        value = new Color(0.1f, 0.2f, 0.3f, 0.4f),
                        targetType = typeof(Vector4),
                        expectedResult = new Vector4(0.1f, 0.2f, 0.3f, 0.4f)
                    },
                    new Conversion {value = new Color(0.1f, 0.2f, 0.3f, 0.4f), targetType = typeof(float), expectedResult = 0.4f},
                    new Conversion
                    {
                        value = new Color(0.1f, 0.2f, 0.3f, 0.4f),
                        targetType = typeof(Color),
                        expectedResult = new Color(0.1f, 0.2f, 0.3f, 0.4f)
                    },

                    new Conversion {value = 1.1f, targetType = typeof(int), expectedResult = 1},
                    new Conversion {value = 1.1f, targetType = typeof(uint), expectedResult = 1u},
                    new Conversion {value = -1.1f, targetType = typeof(uint), expectedResult = 0u},
                    new Conversion {value = (uint)int.MaxValue + 2u, targetType = typeof(int), expectedResult = 0},
                    new Conversion
                    {
                        value = (uint)int.MaxValue + 2u,
                        targetType = typeof(uint),
                        expectedResult = (uint)int.MaxValue + 2u
                    },
                    new Conversion {value = null, targetType = typeof(uint), expectedResult = null},
                    new Conversion {value = null, targetType = typeof(Texture2D), expectedResult = null},
                    new Conversion {value = null, targetType = typeof(Vector3), expectedResult = null},

                    new Conversion {value = new Vector3(1, 2, 3), targetType = typeof(DirectionType), expectedResult = new DirectionType() { direction = new Vector3(1, 2, 3) } },
                    new Conversion {value = new Vector3(4, 5, 6), targetType = typeof(Vector), expectedResult = new Vector() { vector = new Vector3(4, 5, 6) } },
                    new Conversion {value = new Vector3(7, 8, 9), targetType = typeof(Position), expectedResult = new Position() { position = new Vector3(7, 8, 9) } },

                    new Conversion {value = new DirectionType() { direction = new Vector3(1, 2, 3) }, targetType = typeof(Vector3), expectedResult = new Vector3(1, 2, 3) },
                    new Conversion {value = new Vector() { vector = new Vector3(4, 5, 6) }, targetType = typeof(Vector3), expectedResult = new Vector3(4, 5, 6) },
                    new Conversion {value = new Position() { position = new Vector3(7, 8, 9) }, targetType = typeof(Vector3), expectedResult = new Vector3(7, 8, 9) },

                    new Conversion {value = 3, targetType = typeof(VFXValueType), expectedResult = VFXValueType.Float3},
                    new Conversion {value = 3u, targetType = typeof(VFXValueType), expectedResult = VFXValueType.Float3},
                    new Conversion {value = VFXValueType.Float3, targetType = typeof(int), expectedResult = 3},
                    new Conversion {value = VFXValueType.Float3, targetType = typeof(uint), expectedResult = 3u}
                };

                s_FailingConversions = new Conversion[]
                {
                    new Conversion
                    {
                        value = Matrix4x4.TRS(new Vector3(1, 2, 3), Quaternion.Euler(10, 20, 30), new Vector3(4, 5, 6)),
                        targetType = typeof(float),
                        expectedResult = null
                    },
                };
            }
        }

        [Test]
        public void SimpleConvertTest([ValueSource("conversions")] Conversion conversion)
        {
            Assert.AreEqual(conversion.expectedResult, VFXConverter.ConvertTo(conversion.value, conversion.targetType));
        }

        //TEMP disable LogAssert.Expect, still failing running on katana
#if _ENABLE_LOG_EXCEPT_TEST
        [Test]
        public void FailingConvertTest([ValueSource("failingConversions")] Conversion conversion)
        {
            LogAssert.Expect(LogType.Error, string.Format("Cannot cast from {0} to {1}", conversion.value.GetType(), conversion.targetType));
            Assert.IsNull(VFXConverter.ConvertTo(conversion.value, conversion.targetType));
        }

#endif

        [Test]
        public void MatrixToTransformTest()
        {
            Matrix4x4 value = Matrix4x4.TRS(new Vector3(1, 2, 3), Quaternion.Euler(10, 20, 30), new Vector3(4, 5, 6));

            Transform transform = VFXConverter.ConvertTo<Transform>(value);

            float epsilon = 0.00001f;

            Assert.AreEqual(transform.position.x, 1, epsilon);
            Assert.AreEqual(transform.position.y, 2, epsilon);
            Assert.AreEqual(transform.position.z, 3, epsilon);

            Assert.AreEqual(transform.angles.x, 10, epsilon);
            Assert.AreEqual(transform.angles.y, 20, epsilon);
            Assert.AreEqual(transform.angles.z, 30, epsilon);

            Assert.AreEqual(transform.scale.x, 4, epsilon);
            Assert.AreEqual(transform.scale.y, 5, epsilon);
            Assert.AreEqual(transform.scale.z, 6, epsilon);
        }

        [Test]
        public void LogarithmicScaleTest_Invalid_Minimum()
        {
            Assert.Throws(typeof(ArgumentException), () => new LogarithmicSliderScale(new Vector2(0, 1000)));
            Assert.Throws(typeof(ArgumentException), () => new LogarithmicSliderScale(new Vector2(-1, 1000)));
        }

        public void LogarithmicScaleTest_Invalid_Range_With_Snapping()
        {
            Assert.Throws(typeof(ArgumentException), () => new LogarithmicSliderScale(new Vector2(10, 1005), 10, true));
            Assert.Throws(typeof(ArgumentException), () => new LogarithmicSliderScale(new Vector2(5, 1024), 2, true));
        }

        [Test]
        public void LogarithmicScaleTest_Base10_NoSnap()
        {
            var logScale = new LogarithmicSliderScale(new Vector2(1, 1000), 10, false);

            Assert.AreEqual(1f, logScale.ToScaled(1f));
            Assert.AreEqual(1.06f, logScale.ToScaled(10f), 1e-2);
            Assert.AreEqual(2f, logScale.ToScaled(100f), 1e-1);
            Assert.AreEqual(1000f, logScale.ToScaled(1000f));

            Assert.AreEqual(1, logScale.ToLinear(1));
            Assert.AreEqual(100, logScale.ToLinear(2), 2);
            Assert.AreEqual(1000, logScale.ToLinear(1000));
        }

        [Test]
        public void LogarithmicScaleTest_Base10_Min_Not_1_NoSnap()
        {
            var logScale = new LogarithmicSliderScale(new Vector2(150, 1000), 10, false);

            Assert.AreEqual(150f, logScale.ToScaled(1f), 1e-3);
            Assert.AreEqual(150f, logScale.ToScaled(10f), 1e-3);
            Assert.AreEqual(167.709f, logScale.ToScaled(200f), 1e-3);
            Assert.AreEqual(1000f, logScale.ToScaled(1000f));

            Assert.AreEqual(150f, logScale.ToLinear(1));
            Assert.AreEqual(150f, logScale.ToLinear(2));
            Assert.AreEqual(278.895f, logScale.ToLinear(200), 1e-3);
            Assert.AreEqual(1000, logScale.ToLinear(1000), 1e-3);
        }

        [Test]
        public void LogarithmicScaleTest_Base10_Snap()
        {
            var logScale = new LogarithmicSliderScale(new Vector2(1, 1000), 10, true);

            Assert.AreEqual(1f, logScale.ToScaled(1f));
            Assert.AreEqual(1f, logScale.ToScaled(10f));
            Assert.AreEqual(1f, logScale.ToScaled(100f));
            Assert.AreEqual(1000f, logScale.ToScaled(1000f));

            Assert.AreEqual(1f, logScale.ToLinear(1f));
            Assert.AreEqual(100f, logScale.ToLinear(2f), 2);
            Assert.AreEqual(1000f, logScale.ToLinear(1000f));
        }

        [Test]
        public void LogarithmicScaleTest_Base2_NoSnap()
        {
            var logScale = new LogarithmicSliderScale(new Vector2(1, 2048), 2, false);

            Assert.AreEqual(1f, logScale.ToScaled(1f));
            Assert.AreEqual(1.114f, logScale.ToScaled(30f), 1e-3);
            Assert.AreEqual(2.528f, logScale.ToScaled(250f), 1e-3);
            Assert.AreEqual(41.307f, logScale.ToScaled(1000f), 1e-3);
            Assert.AreEqual(2048f, logScale.ToScaled(2048f), 1e-2);

            Assert.AreEqual(1, logScale.ToLinear(1f));
            Assert.AreEqual(187.09f, logScale.ToLinear(2f), 1e-3);
            Assert.AreEqual(991.362f, logScale.ToLinear(40f), 1e-3);
            Assert.AreEqual(2048f, logScale.ToLinear(2048f), 1e-2);
        }

        [Test]
        public void LogarithmicScaleTest_Base2_Snap()
        {
            var logScale = new LogarithmicSliderScale(new Vector2(1, 2048), 2, true);

            Assert.AreEqual(1f, logScale.ToScaled(1f));
            Assert.AreEqual(1f, logScale.ToScaled(30f));
            Assert.AreEqual(4f, logScale.ToScaled(300f));
            Assert.AreEqual(32f, logScale.ToScaled(1000f));
            Assert.AreEqual(2048f, logScale.ToScaled(2048f));

            Assert.AreEqual(1, logScale.ToLinear(1f));
            Assert.AreEqual(187.09f, logScale.ToLinear(2f), 1e-3);
            Assert.AreEqual(991.362f, logScale.ToLinear(40f), 1e-3);
            Assert.AreEqual(2048f, logScale.ToLinear(2048f), 1e-2);
        }
    }
}
#endif
