using System;
using UnityEngine;

namespace UnityEditor.VFX
{
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
}
