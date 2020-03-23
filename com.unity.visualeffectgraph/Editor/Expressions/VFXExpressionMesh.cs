using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class MeshFlags
    {
        //Mesh API can modify the vertex count & layout.
        //Thus, all mesh related expression should never been constant folded while generating code.
        //If you still want to allow constant compilation, replace this following line with "Flags.None"
        public static readonly VFXExpression.Flags kCommonFlag = VFXExpression.Flags.InvalidConstant;
    }

#if UNITY_2020_2_OR_NEWER
    class VFXExpressionSampleMeshFloat : VFXExpression
    {
        public VFXExpressionSampleMeshFloat() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshVertexFloat; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var vertexOffsetReduce = constParents[1];
            var channelFormatAndDimensionReduce = constParents[2];

            var mesh = meshReduce.Get<Mesh>();
            var vertexOffset = vertexOffsetReduce.Get<uint>();
            var channelFormatAndDimension = channelFormatAndDimensionReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetFloat(mesh, vertexOffset, channelFormatAndDimension));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleMeshFloat2 : VFXExpression
    {
        public VFXExpressionSampleMeshFloat2() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat2(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshVertexFloat2; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var vertexOffsetReduce = constParents[1];
            var channelFormatAndDimensionReduce = constParents[2];

            var mesh = meshReduce.Get<Mesh>();
            var vertexOffset = vertexOffsetReduce.Get<uint>();
            var channelFormatAndDimension = channelFormatAndDimensionReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetFloat2(mesh, vertexOffset, channelFormatAndDimension));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat2({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleMeshFloat3 : VFXExpression
    {
        public VFXExpressionSampleMeshFloat3() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat3(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelOffset) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[] { mesh, vertexOffset, channelOffset })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshVertexFloat3; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var vertexOffsetReduce = constParents[1];
            var channelFormatAndDimensionReduce = constParents[2];

            var mesh = meshReduce.Get<Mesh>();
            var vertexOffset = vertexOffsetReduce.Get<uint>();
            var channelFormatAndDimension = channelFormatAndDimensionReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetFloat3(mesh, vertexOffset, channelFormatAndDimension));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat3({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleMeshFloat4 : VFXExpression
    {
        public VFXExpressionSampleMeshFloat4() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat4(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshVertexFloat4; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var vertexOffsetReduce = constParents[1];
            var channelFormatAndDimensionReduce = constParents[2];

            var mesh = meshReduce.Get<Mesh>();
            var vertexOffset = vertexOffsetReduce.Get<uint>();
            var channelFormatAndDimension = channelFormatAndDimensionReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetFloat4(mesh, vertexOffset, channelFormatAndDimension));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat4({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleMeshColor : VFXExpression
    {
        public VFXExpressionSampleMeshColor() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshColor(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshVertexColor; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var vertexOffsetReduce = constParents[1];
            var channelFormatAndDimensionReduce = constParents[2];

            var mesh = meshReduce.Get<Mesh>();
            var vertexOffset = vertexOffsetReduce.Get<uint>();
            var channelFormatAndDimension = channelFormatAndDimensionReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetColor(mesh, vertexOffset, channelFormatAndDimension));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshColor({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }
#else
    class VFXExpressionSampleMeshFloat : VFXExpression
    {
        public VFXExpressionSampleMeshFloat() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshFloat; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var vertexIndexReduce = constParents[1];
            var channelOffsetReduce = constParents[2];
            var vertexStrideReduce = constParents[3];

            var mesh = meshReduce.Get<Mesh>();
            var vertexIndex = vertexIndexReduce.Get<uint>();
            var channelOffset = channelOffsetReduce.Get<uint>();
            var vertexStride = vertexStrideReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetFloat(mesh, vertexIndex, channelOffset, vertexStride));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat({0},{1}, {2}, {3})", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionSampleMeshFloat2 : VFXExpression
    {
        public VFXExpressionSampleMeshFloat2() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat2(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshFloat2; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var mesh = constParents[0].Get<Mesh>();
            var vertexIndex = constParents[1].Get<uint>();
            var channelOffset = constParents[2].Get<uint>();
            var vertexStride = constParents[3].Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetFloat2(mesh, vertexIndex, channelOffset, vertexStride));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat2({0},{1}, {2}, {3})", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionSampleMeshFloat3 : VFXExpression
    {
        public VFXExpressionSampleMeshFloat3() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat3(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshFloat3; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var mesh = constParents[0].Get<Mesh>();
            var vertexIndex = constParents[1].Get<uint>();
            var channelOffset = constParents[2].Get<uint>();
            var vertexStride = constParents[3].Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetFloat3(mesh, vertexIndex, channelOffset, vertexStride));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat3({0},{1}, {2}, {3})", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionSampleMeshFloat4 : VFXExpression
    {
        public VFXExpressionSampleMeshFloat4() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat4(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshFloat4; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var mesh = constParents[0].Get<Mesh>();
            var vertexIndex = constParents[1].Get<uint>();
            var channelOffset = constParents[2].Get<uint>();
            var vertexStride = constParents[3].Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetFloat4(mesh, vertexIndex, channelOffset, vertexStride));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat4({0},{1}, {2}, {3})", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionSampleMeshColor : VFXExpression
    {
        public VFXExpressionSampleMeshColor() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshColor(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(MeshFlags.kCommonFlag | Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshColor; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var mesh = constParents[0].Get<Mesh>();
            var vertexIndex = constParents[1].Get<uint>();
            var channelOffset = constParents[2].Get<uint>();
            var vertexStride = constParents[3].Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetColor(mesh, vertexIndex, channelOffset, vertexStride));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshColor({0}, {1}, {2}, {3})", parents[0], parents[1], parents[2], parents[3]);
        }
    }
#endif

    class VFXExpressionMeshVertexCount : VFXExpression
    {
        public VFXExpressionMeshVertexCount() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionMeshVertexCount(VFXExpression mesh) : base(MeshFlags.kCommonFlag | Flags.InvalidOnGPU, new VFXExpression[1] { mesh })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshVertexCount; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var mesh = meshReduce.Get<Mesh>();
            return VFXValue.Constant(VFXExpressionMesh.GetVertexCount(mesh));
        }
    }

    class VFXExpressionMeshChannelOffset : VFXExpression
    {
        public VFXExpressionMeshChannelOffset() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionMeshChannelOffset(VFXExpression mesh, VFXExpression channelIndex) : base(MeshFlags.kCommonFlag | Flags.InvalidOnGPU, new VFXExpression[2] { mesh, channelIndex })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshChannelOffset; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var channelIndexReduce = constParents[1];

            var mesh = meshReduce.Get<Mesh>();
            var channelIndex = channelIndexReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetChannelOffset(mesh, channelIndex));
        }
    }

#if UNITY_2020_2_OR_NEWER
    class VFXExpressionMeshChannelFormatAndDimension : VFXExpression
    {
        public VFXExpressionMeshChannelFormatAndDimension() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionMeshChannelFormatAndDimension(VFXExpression mesh, VFXExpression channelIndex) : base(MeshFlags.kCommonFlag | Flags.InvalidOnGPU, new VFXExpression[2] { mesh, channelIndex })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshChannelFormatAndDimension; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var channelIndexReduce = constParents[1];

            var mesh = meshReduce.Get<Mesh>();
            var channelIndex = channelIndexReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetChannelFormatAndDimension(mesh, channelIndex));
        }
    }
#endif

    class VFXExpressionMeshVertexStride : VFXExpression
    {
        public VFXExpressionMeshVertexStride() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionMeshVertexStride(VFXExpression mesh) : base(MeshFlags.kCommonFlag | Flags.InvalidOnGPU, new VFXExpression[1] { mesh })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshVertexStride; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var mesh = meshReduce.Get<Mesh>();
            return VFXValue.Constant(VFXExpressionMesh.GetVertexStride(mesh));
        }
    }
}
