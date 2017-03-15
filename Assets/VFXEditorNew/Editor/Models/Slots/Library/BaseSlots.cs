using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(float))]
    class VFXSlotFloat : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValueFloat(0.0f, false);
        }
    }

    [VFXInfo(type = typeof(Vector2))]
    class VFXSlotFloat2 : VFXSlot
    {
        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1]);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[2] {
                new VFXExpressionExtractComponent(expr,0),
                new VFXExpressionExtractComponent(expr,1)};
        }
    }

    [VFXInfo(type = typeof(Vector3))]
    class VFXSlotFloat3 : VFXSlot 
    {
        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1],
                expr[2]);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[3] {
                new VFXExpressionExtractComponent(expr,0),
                new VFXExpressionExtractComponent(expr,1),
                new VFXExpressionExtractComponent(expr,2)};
        }
    }

    [VFXInfo(type = typeof(Vector4))]
    class VFXSlotFloat4 : VFXSlot
    {
        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1],
                expr[2],
                expr[3]);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[4] {
                new VFXExpressionExtractComponent(expr,0),
                new VFXExpressionExtractComponent(expr,1),
                new VFXExpressionExtractComponent(expr,2),
                new VFXExpressionExtractComponent(expr,3)};
        }
    }

    [VFXInfo(type = typeof(Color))]
    class VFXSlotColor : VFXSlotFloat4
    {

    }

    [VFXInfo(type = typeof(Texture2D))]
    class VFXSlotTexture2D : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValueTexture2D(Texture2D.whiteTexture, false);
        }
    }

    [VFXInfo(type = typeof(Texture3D))]
    class VFXSlotTexture3D : VFXSlot 
    {
        /*protected override VFXValue DefaultExpression()
        {
            return new VFXValueTexture3D(null, false);
        }*/
    }

    [VFXInfo(type = typeof(AnimationCurve))]
    class VFXSlotAnimationCurve : VFXSlot { }

    [VFXInfo(type = typeof(FloatN))]
    class VFXSlotFloatN : VFXSlot
    {
        protected override bool CanConvertFrom(VFXExpression expression)
        {
            return expression == null || VFXExpression.IsFloatValueType(expression.ValueType);
        }

        protected virtual VFXExpression ConvertExpression(VFXExpression expression)
        {
           /* if (expression == null)
            {
                PropagateToChildren(c => c.UnlinkAll());
                RemoveAllChildren();
            }
            else
            {
                var nbComponents = VFXExpression.TypeToSize(expression.ValueType);
                var nbChildren = GetNbChildren();

                if (nbChildren > nbComponents)
                {
                    for (int i = nbComponents; i < nbChildren; ++i)
                    {
                        var child = GetChild(GetNbChildren() - 1);
                        child.UnlinkAll();
                        GetChild(GetNbChildren() - 1).Detach();
                    }
                }

                if (GetNbChildren() != nbComponents)
                {

                }
            }*/


            return expression;
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