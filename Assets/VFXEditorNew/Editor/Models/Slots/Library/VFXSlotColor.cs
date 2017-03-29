using System;
using UnityEngine;

namespace UnityEditor.VFX
{
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
}

