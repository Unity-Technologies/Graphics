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
        /*protected override VFXExpression FromChildren()
        {
            return new VFXExpressionCombine(GetChild(0).expression, GetChild(1).expression);
        }

        protected override void ToChildren()
        {
            for (int i = 0; i < GetNbChildren(); ++i )
                GetChild(i).expression = new VFXExpressionExtractComponent(this.expression, i);
        }*/
    }

    [VFXInfo(type = typeof(Vector3))]
    class VFXSlotFloat3 : VFXSlot {}

    [VFXInfo(type = typeof(Vector4))]
    class VFXSlotFloat4 : VFXSlot {}

    [VFXInfo(type = typeof(Color))]
    class VFXSlotColor : VFXSlot {}

    [VFXInfo(type = typeof(Texture2D))]
    class VFXSlotTexture2D : VFXSlot {}

    [VFXInfo(type = typeof(Texture3D))]
    class VFXSlotTexture3D : VFXSlot {}
}