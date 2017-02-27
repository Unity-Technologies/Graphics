using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    // Test blocks only !
    // TODO Remove that!

    [VFXInfo]
    class VFXInitBlockTest : VFXBlock
    {
        public override string name                         { get { return "Init Block"; }}
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInit; } }
    }

    [VFXInfo]
    class VFXUpdateBlockTest : VFXBlock
    {
        public override string name                         { get { return "Update Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kUpdate; } }
    }

    [VFXInfo]
    class VFXOutputBlockTest : VFXBlock
    {
        public override string name                         { get { return "Output Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kOutput; } }
    }

    [VFXInfo(category = "test")]
    class VFXInitAndUpdateTest : VFXBlock
    {
        public override string name                         { get { return "Init And Update Block"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInitAndUpdate; } }
    }

    [VFXInfo]
    class VFXRotate : VFXBlock
    {
        public override string name                         { get { return "Rotate"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kInitAndUpdate; } }

        public class InputProperties
        {
            public float angle = 30;
            public Vector3 axis = Vector3.forward;
        }
    }

    [VFXInfo]
    class VFXAllType : VFXBlock
    {
        public override string name                         { get { return "Test"; } }
        public override VFXContextType compatibleContexts   { get { return VFXContextType.kAll; } }

        public class InputProperties
        {
            public float aFloat = 123.456f;
            public Vector2 aVector2 = Vector2.left;
            public Vector3 aVector3 = Vector3.forward;
            public Vector4 aVector4 = Vector4.one;
            public Color aColor = Color.gray;
            public Texture2D aTexture2D = null;
            public Texture3D aTexture3D = null;
            public Sphere aSphere;
            public Vector aVector = new Vector { space = CoordinateSpace.Local, vector = Vector3.one };
            public Position aPosition = new Position { space = CoordinateSpace.Local, position = Vector3.forward };
            public int anInt;
            public FlipBook aFlipBook;
            public bool aBool;
            public AnimationCurve curve = new AnimationCurve();
            public Mesh aMesh;
        }
    }
}
