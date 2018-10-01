using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionValueNoise1D : VFXExpression
    {
        public VFXExpressionValueNoise1D() : this(VFXValue<float>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionValueNoise1D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<float>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateValueNoise1D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateValueNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionValueNoise2D : VFXExpression
    {
        public VFXExpressionValueNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionValueNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateValueNoise2D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateValueNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionValueNoise3D : VFXExpression
    {
        public VFXExpressionValueNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionValueNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateValueNoise3D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateValueNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionPerlinNoise1D : VFXExpression
    {
        public VFXExpressionPerlinNoise1D() : this(VFXValue<float>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionPerlinNoise1D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<float>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GeneratePerlinNoise1D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GeneratePerlinNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionPerlinNoise2D : VFXExpression
    {
        public VFXExpressionPerlinNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionPerlinNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GeneratePerlinNoise2D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GeneratePerlinNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionPerlinNoise3D : VFXExpression
    {
        public VFXExpressionPerlinNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionPerlinNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GeneratePerlinNoise3D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GeneratePerlinNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSimplexNoise1D : VFXExpression
    {
        public VFXExpressionSimplexNoise1D() : this(VFXValue<float>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionSimplexNoise1D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<float>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateSimplexNoise1D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateSimplexNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSimplexNoise2D : VFXExpression
    {
        public VFXExpressionSimplexNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionSimplexNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateSimplexNoise2D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateSimplexNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSimplexNoise3D : VFXExpression
    {
        public VFXExpressionSimplexNoise3D() : this(VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<int>.Default) {}
        public VFXExpressionSimplexNoise3D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector3>();
            var floatParams = constParents[1].Get<Vector3>();
            var octaveCount = constParents[2].Get<int>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateSimplexNoise3D(coordinate, floatParams.x, floatParams.y, octaveCount, floatParams.z));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateSimplexNoise({0}, {1}.x, {1}.y, {2}, {1}.z)", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionVoroNoise2D : VFXExpression
    {
        public VFXExpressionVoroNoise2D() : this(VFXValue<Vector2>.Default, VFXValue<Vector4>.Default) {}
        public VFXExpressionVoroNoise2D(params VFXExpression[] parents) : base(VFXExpression.Flags.InvalidOnCPU, parents) {}
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        /*sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var coordinate = constParents[0].Get<Vector2>();
            var floatParams = constParents[1].Get<Vector4>();

            return VFXValue.Constant(VFXExpressionNoise.GenerateVoroNoise2D(coordinate, floatParams));
        }*/

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GenerateVoroNoise({0}, {1}.x, {1}.y, {1}.z, {1}.w)", parents[0], parents[1]);
        }
    }
}
