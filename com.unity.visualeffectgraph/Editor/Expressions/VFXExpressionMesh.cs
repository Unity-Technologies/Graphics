using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleMeshFloat : VFXExpression
    {
        public VFXExpressionSampleMeshFloat() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
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

        public VFXExpressionSampleMeshFloat2(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
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

        public VFXExpressionSampleMeshFloat3(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
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

        public VFXExpressionSampleMeshFloat4(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
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

        public VFXExpressionSampleMeshColor(VFXExpression mesh, VFXExpression vertexIndex, VFXExpression channelOffset, VFXExpression vertexStride) : base(Flags.None, new VFXExpression[4] { mesh, vertexIndex, channelOffset, vertexStride })
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

    class VFXExpressionMeshVertexCount : VFXExpression
    {
        public VFXExpressionMeshVertexCount() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionMeshVertexCount(VFXExpression mesh) : base(Flags.InvalidOnGPU, new VFXExpression[1] { mesh })
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

        public VFXExpressionMeshChannelOffset(VFXExpression mesh, VFXExpression channelIndex) : base(Flags.InvalidOnGPU, new VFXExpression[2] { mesh, channelIndex })
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

    class VFXExpressionMeshVertexStride : VFXExpression
    {
        public VFXExpressionMeshVertexStride() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionMeshVertexStride(VFXExpression mesh) : base(Flags.InvalidOnGPU, new VFXExpression[1] { mesh })
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
