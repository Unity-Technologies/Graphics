using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    //Temp Only (behavior)

    [VFXInfo]
    class VFXOperatorFloatOne : VFXOperator
    {
        override public string name { get { return "Temp_Float"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXValueFloat(1.0f, true) };
        }
    }

    [VFXInfo]
    class VFXOperatorFloat : VFXOperator
    {
        override public string name { get { return "Temp_Float"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValueFloat.Default };
        }
    }

    [VFXInfo]
    class VFXOperatorFloat2 : VFXOperator
    {
        override public string name { get { return "Temp_Float2"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValueFloat2.Default };
        }
    }

    [VFXInfo]
    class VFXOperatorFloat3 : VFXOperator
    {
        override public string name { get { return "Temp_Float3"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValueFloat3.Default };
        }
    }

    [VFXInfo]
    class VFXOperatorFloat4 : VFXOperator
    {
        override public string name { get { return "Temp_Float4"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValueFloat4.Default };
        }
    }

    [VFXInfo]
    class VFXOperatorExplodeFloatN : VFXOperator
    {
        public class Properties
        {
           public FloatN x = 0.0f;
        }

        override public string name { get { return "Temp_Explode"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();
        }

    }

}