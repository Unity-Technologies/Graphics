using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
#if UNITY_2020_2_OR_NEWER
    class VFXExpressionVertexBufferFromMesh : VFXExpression
    {
        public VFXExpressionVertexBufferFromMesh() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionVertexBufferFromMesh(VFXExpression mesh, VFXExpression channelFormatAndDimensionAndStream) : base(Flags.InvalidOnGPU, new VFXExpression[] { mesh, channelFormatAndDimensionAndStream })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.VertexBufferFromMesh; } }
    }

    class VFXExpressionIndexBufferFromMesh : VFXExpression
    {
        public VFXExpressionIndexBufferFromMesh() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionIndexBufferFromMesh(VFXExpression mesh) : base(Flags.InvalidOnGPU, new VFXExpression[] { mesh })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.IndexBufferFromMesh; } }
    }

    class VFXExpressionMeshIndexCount : VFXExpression
    {
        public VFXExpressionMeshIndexCount() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionMeshIndexCount(VFXExpression mesh) : base(Flags.InvalidOnGPU, new VFXExpression[1] { mesh })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshIndexCount; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var mesh = meshReduce.Get<Mesh>();
            return VFXValue.Constant(VFXExpressionMesh.GetIndexCount(mesh));
        }
    }

    class VFXExpressionMeshIndexFormat : VFXExpression
    {
        public VFXExpressionMeshIndexFormat() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionMeshIndexFormat(VFXExpression mesh) : base(Flags.InvalidOnGPU, new VFXExpression[1] { mesh })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshIndexFormat; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var mesh = meshReduce.Get<Mesh>();
            return VFXValue.Constant(VFXExpressionMesh.GetIndexFormat(mesh));
        }
    }

    class VFXExpressionSampleIndex : VFXExpression
    {
        public VFXExpressionSampleIndex() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {

        }

        public VFXExpressionSampleIndex(VFXExpression mesh, VFXExpression index, VFXExpression indexFormat) : base(Flags.None, new VFXExpression[] { mesh, index, indexFormat })
        {
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var vertexOffsetReduce = constParents[1];
            var channelFormatAndDimensionReduce = constParents[2];

            var mesh = meshReduce.Get<Mesh>();
            var index = vertexOffsetReduce.Get<uint>();
            var indexFormat = channelFormatAndDimensionReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetIndex(mesh, index, indexFormat));
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleMeshIndex; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshIndex({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleMeshFloat : VFXExpression
    {
        public VFXExpressionSampleMeshFloat() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
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

        public VFXExpressionSampleMeshFloat2(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
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

        public VFXExpressionSampleMeshFloat3(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
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

        public VFXExpressionSampleMeshFloat4(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
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

        public VFXExpressionSampleMeshColor(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, new VFXExpression[] { mesh, vertexOffset, channelFormatAndDimension })
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
#endif

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

#if UNITY_2020_2_OR_NEWER
    class VFXExpressionMeshChannelFormatAndDimension : VFXExpression
    {
        public VFXExpressionMeshChannelFormatAndDimension() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionMeshChannelFormatAndDimension(VFXExpression mesh, VFXExpression channelIndex) : base(Flags.InvalidOnGPU, new VFXExpression[2] { mesh, channelIndex })
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
        public VFXExpressionMeshVertexStride() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionMeshVertexStride(VFXExpression mesh, VFXExpression channelIndex) : base(Flags.InvalidOnGPU, new VFXExpression[] { mesh, channelIndex })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshVertexStride; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var channelIndexReduce = constParents[1];
            var mesh = meshReduce.Get<Mesh>();
            var channelIndex = channelIndexReduce.Get<uint>();
            return VFXValue.Constant(VFXExpressionMesh.GetVertexStride(mesh, channelIndex));
        }
    }
}
