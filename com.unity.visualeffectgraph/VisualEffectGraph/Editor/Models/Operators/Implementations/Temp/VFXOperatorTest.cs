using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Test")]
    class VFXOperatorTest : VFXOperator
    {
        override public string name { get { return "Test"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] {};
        }

        public class InputProperties
        {
            public float aFloat = 123.456f;
            [Range(-32, 150)]
            public float aRange = 123.456f;
            //public Gradient aGradient = new Gradient();
            public Vector2 aVector2 = Vector2.left;
            public Vector3 aVector3 = Vector3.forward;
            public Vector4 aVector4 = Vector4.one;
            public Color aColor = Color.gray;
            //public Texture2D aTexture2D = null;
            //public Texture3D aTexture3D = null;
            public Sphere aSphere = new Sphere();
            public Vector aVector = new Vector { vector = Vector3.one };
            public Position aPosition = new Position { position = Vector3.forward };
            //public int anInt;
            //public uint anUint;
            //public FlipBook aFlipBook;
            //public bool aBool;
            //public AnimationCurve curve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1)});
            //public Mesh aMesh;
        }


        public enum RotateMode
        {
            Quaternion,
            Euler
        }

        [VFXSetting]
        public RotateMode mode;
        [VFXSetting]
        public bool rotateTwice;
    }
}
