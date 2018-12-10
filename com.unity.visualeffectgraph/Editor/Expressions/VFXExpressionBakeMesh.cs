using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionBakeMesh : VFXExpression
    {
        public VFXExpressionBakeMesh() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionBakeMesh(VFXExpression mesh) : base(Flags.InvalidOnGPU, new VFXExpression[1] { mesh })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.BakeMesh; } }
    }
}
