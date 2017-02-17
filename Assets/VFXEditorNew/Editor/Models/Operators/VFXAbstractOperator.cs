using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXOperatorUnaryFloatOperation : VFXOperator
    {
        public class Properties
        {
            public float input = 0.0f;
        }

        protected override ModeFlags Flags { get { return ModeFlags.kUnaryFloatOperator; } }
    }

    abstract class VFXOperatorBinaryFloatOperation : VFXOperator
    {
        protected override ModeFlags Flags { get { return ModeFlags.kBinaryFloatOperator; } }
    }

    abstract class VFXOperatorBinaryFloatOperationOne : VFXOperatorBinaryFloatOperation
    {
        public class Properties
        {
            public float right = 1.0f;
            public float left = 1.0f;
        }
    }

    abstract class VFXOperatorBinaryFloatOperationZero : VFXOperatorBinaryFloatOperation
    {
        public class Properties
        {
            public float right = 0.0f;
            public float left = 0.0f;
        }
    }
}