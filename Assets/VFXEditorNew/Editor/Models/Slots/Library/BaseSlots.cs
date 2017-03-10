using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(float))]
    class VFXSlotFloat : VFXSlot
    {

    }

    [VFXInfo(type = typeof(Vector2))]
    class VFXSlotFloat2 : VFXSlot
    {
        protected override VFXExpression ExpressionFromChildren()
        {
            return new VFXExpressionCombine(
                GetChild(0).expression,
                GetChild(1).expression);
        }

        protected override VFXExpression[] ExpressionToChildren()
        {
            return new VFXExpression[2] {
                new VFXExpressionExtractComponent(expression,0),
                new VFXExpressionExtractComponent(expression,1)};
        }
    }

    [VFXInfo(type = typeof(Vector3))]
    class VFXSlotFloat3 : VFXSlot 
    {
        protected override VFXExpression ExpressionFromChildren()
        {
            return new VFXExpressionCombine(
                GetChild(0).expression, 
                GetChild(1).expression,
                GetChild(2).expression);
        }

        protected override VFXExpression[] ExpressionToChildren()
        {
            return new VFXExpression[3] {
                new VFXExpressionExtractComponent(expression,0),
                new VFXExpressionExtractComponent(expression,1),
                new VFXExpressionExtractComponent(expression,2)};
        }
    }

    [VFXInfo(type = typeof(Vector4))]
    class VFXSlotFloat4 : VFXSlot
    {
        protected override VFXExpression ExpressionFromChildren()
        {
            return new VFXExpressionCombine(
                GetChild(0).expression,
                GetChild(1).expression,
                GetChild(2).expression,
                GetChild(3).expression);
        }

        protected override VFXExpression[] ExpressionToChildren()
        {
            return new VFXExpression[4] {
                new VFXExpressionExtractComponent(expression,0),
                new VFXExpressionExtractComponent(expression,1),
                new VFXExpressionExtractComponent(expression,2),
                new VFXExpressionExtractComponent(expression,3)};
        }
    }

    [VFXInfo(type = typeof(Color))]
    class VFXSlotColor : VFXSlotFloat4
    {

    }

    [VFXInfo(type = typeof(Texture2D))]
    class VFXSlotTexture2D : VFXSlot {}

    [VFXInfo(type = typeof(Texture3D))]
    class VFXSlotTexture3D : VFXSlot {}

    [VFXInfo(type = typeof(AnimationCurve))]
    class VFXSlotAnimationCurve : VFXSlot { }

    [VFXInfo(type = typeof(FloatN))]
    class VFXSlotFloatN : VFXSlot
    {
        protected override bool CanConvert(VFXExpression expression)
        {
            return expression == null || VFXExpression.IsFloatValueType(expression.ValueType);
        }
    }

    [VFXInfo(type = typeof(int))]
    class VFXSlotInt : VFXSlot { }

    [VFXInfo(type = typeof(Sphere))]
    class VFXSlotSphere : VFXSlot { }

    [VFXInfo(type = typeof(OrientedBox))]
    class VFXSlotOrientedBox : VFXSlot { }

    [VFXInfo(type = typeof(AABox))]
    class VFXSlotAABox : VFXSlot { }

    [VFXInfo(type = typeof(Plane))]
    class VFXSlotPlane : VFXSlot { }

    [VFXInfo(type = typeof(Cylinder))]
    class VFXSlotCylinder : VFXSlot { }

    [VFXInfo(type = typeof(Transform))]
    class VFXSlotTransform : VFXSlot { }

    [VFXInfo(type = typeof(Position))]
    class VFXSlotPosition : VFXSlot { }

    [VFXInfo(type = typeof(Vector))]
    class VFXSlotVector : VFXSlot { }

    [VFXInfo(type = typeof(FlipBook))]
    class VFXSlotFlipBook : VFXSlot { }

    [VFXInfo(type = typeof(bool))]
    class VFXSlotBool : VFXSlot { }

    [VFXInfo(type = typeof(Mesh))]
    class VFXSlotMesh : VFXSlot { }
}