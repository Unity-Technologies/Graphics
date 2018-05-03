using UnityEngine;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine.TestTools;

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
                return string.Format("Convert {0} to type {1} is {2}",value == null ? (object)"null": value,targetType.UserFriendlyName(),expectedResult == null ? (object)"null": expectedResult);
            }
        }


        static Texture2D texture = new Texture2D(16,16);

        public static Conversion[] conversions =
        {
            // Vector3
            new Conversion{value = new Vector3(1,2,3),targetType=typeof(Vector2),expectedResult = new Vector2(1,2)},
            new Conversion{value = new Vector3(1,2,3),targetType=typeof(Vector3),expectedResult = new Vector3(1,2,3)},
            new Conversion{value = new Vector3(1,2,3),targetType=typeof(Vector4),expectedResult = new Vector4(1,2,3,0)},
            new Conversion{value = new Vector3(1,2,3),targetType=typeof(float),expectedResult = 1.0f},
            new Conversion{value = new Vector3(1,2,3),targetType=typeof(int),expectedResult = 1},
            new Conversion{value = new Vector3(1,2,3),targetType=typeof(uint),expectedResult = 1u},
            new Conversion{value = new Vector3(0.1f,0.2f,0.3f),targetType=typeof(Color),expectedResult = new Color(0.1f,0.2f,0.3f)},
            // Vector2
            new Conversion{value = new Vector2(1,2),targetType=typeof(Vector2),expectedResult = new Vector2(1,2)},
            new Conversion{value = new Vector2(1,2),targetType=typeof(Vector3),expectedResult = new Vector3(1,2,0)},
            new Conversion{value = new Vector2(1,2),targetType=typeof(Vector4),expectedResult = new Vector4(1,2,0,0)},
            new Conversion{value = new Vector2(1,2),targetType=typeof(float),expectedResult = 1.0f},
            new Conversion{value = new Vector2(1,2),targetType=typeof(int),expectedResult = 1},
            new Conversion{value = new Vector2(1,2),targetType=typeof(uint),expectedResult = 1u},
            new Conversion{value = new Vector2(0.1f,0.2f),targetType=typeof(Color),expectedResult = new Color(0.1f,0.2f,0.0f)},
            // Vector4
            new Conversion{value = new Vector4(1,2,3,4),targetType=typeof(Vector2),expectedResult = new Vector2(1,2)},
            new Conversion{value = new Vector4(1,2,3,4),targetType=typeof(Vector3),expectedResult = new Vector3(1,2,3)},
            new Conversion{value = new Vector4(1,2,3,4),targetType=typeof(Vector4),expectedResult = new Vector4(1,2,3,4)},
            new Conversion{value = new Vector4(1,2,3,4),targetType=typeof(float),expectedResult = 1.0f},
            new Conversion{value = new Vector4(1,2,3,4),targetType=typeof(int),expectedResult = 1},
            new Conversion{value = new Vector4(1,2,3,4),targetType=typeof(uint),expectedResult = 1u},
            new Conversion{value = new Vector4(0.1f,0.2f,0.3f,0.4f),targetType=typeof(Color),expectedResult = new Color(0.1f,0.2f,0.3f,0.4f)},
            // Color
            new Conversion{value = new Color(0.1f,0.2f,0.3f,0.4f),targetType=typeof(Vector2),expectedResult = new Vector2(0.1f,0.2f)},
            new Conversion{value = new Color(0.1f,0.2f,0.3f,0.4f),targetType=typeof(Vector3),expectedResult = new Vector3(0.1f,0.2f,0.3f)},
            new Conversion{value = new Color(0.1f,0.2f,0.3f,0.4f),targetType=typeof(Vector4),expectedResult = new Vector4(0.1f,0.2f,0.3f,0.4f)},
            new Conversion{value = new Color(0.1f,0.2f,0.3f,0.4f),targetType=typeof(float),expectedResult = 0.4f},
            new Conversion{value = new Color(0.1f,0.2f,0.3f,0.4f),targetType=typeof(Color),expectedResult = new Color(0.1f,0.2f,0.3f,0.4f)},

            new Conversion{value = texture,targetType=typeof(Texture2D),expectedResult = texture},
            new Conversion{value = texture,targetType=typeof(Texture),expectedResult = texture},

            new Conversion{value = 1.1f,targetType=typeof(int),expectedResult = 1},
            new Conversion{value = 1.1f,targetType=typeof(uint),expectedResult = 1u},
            new Conversion{value = -1.1f,targetType=typeof(uint),expectedResult = 0u},
            new Conversion{value = (uint)int.MaxValue + 2u,targetType=typeof(int),expectedResult = 0},
            new Conversion{value = (uint)int.MaxValue + 2u,targetType=typeof(uint),expectedResult = (uint)int.MaxValue + 2u},
            new Conversion{value = null,targetType=typeof(uint),expectedResult = null},
            new Conversion{value = null,targetType=typeof(Texture2D),expectedResult = null},
            new Conversion{value = null,targetType=typeof(Vector3),expectedResult = null},

        };

        [Test]
        public void SimpleConvertTest([ValueSource("conversions")] Conversion conversion)
        {
            Assert.AreEqual(conversion.expectedResult,VFXConverter.ConvertTo(conversion.value,conversion.targetType));
        }


        public static Conversion[] failingConversions =
        {
            new Conversion{value = texture,targetType=typeof(Mesh),expectedResult = null},
            new Conversion{value = texture,targetType=typeof(float),expectedResult = null},
            new Conversion{value = texture,targetType=typeof(int),expectedResult = null},
            new Conversion{value = texture,targetType=typeof(Vector3),expectedResult = null},
            new Conversion{value =  Matrix4x4.TRS(new Vector3(1,2,3),Quaternion.Euler(10,20,30),new Vector3(4,5,6)),targetType=typeof(float),expectedResult = null},
        };
        [Test]
        public void FailingConvertTest([ValueSource("failingConversions")] Conversion conversion)
        {
            Assert.IsNull(VFXConverter.ConvertTo(conversion.value,conversion.targetType));

            LogAssert.Expect(LogType.Error, string.Format("Cannot cast from {0} to {1}",conversion.value.GetType(),conversion.targetType));
        }

        [Test]
        public void MatrixToTransformTest()
        {
            Matrix4x4 value = Matrix4x4.TRS(new Vector3(1,2,3),Quaternion.Euler(10,20,30),new Vector3(4,5,6));

            Transform transform = VFXConverter.ConvertTo<Transform>(value);

            float epsilon = 0.00001f;

            Assert.AreEqual(transform.position.x,1,epsilon);
            Assert.AreEqual(transform.position.y,2,epsilon);
            Assert.AreEqual(transform.position.z,3,epsilon);

            Assert.AreEqual(transform.angles.x,10,epsilon);
            Assert.AreEqual(transform.angles.y,20,epsilon);
            Assert.AreEqual(transform.angles.z,30,epsilon);

            Assert.AreEqual(transform.scale.x,4,epsilon);
            Assert.AreEqual(transform.scale.y,5,epsilon);
            Assert.AreEqual(transform.scale.z,6,epsilon);
        }
    }
}
