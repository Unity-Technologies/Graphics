using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
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

    class VFXExpressionVertexBufferFromSkinnedMeshRenderer : VFXExpression
    {
        public VFXExpressionVertexBufferFromSkinnedMeshRenderer() : this(VFXValue<SkinnedMeshRenderer>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionVertexBufferFromSkinnedMeshRenderer(VFXExpression mesh, VFXExpression channelFormatAndDimensionAndStream) : base(Flags.InvalidOnGPU, new VFXExpression[] { mesh, channelFormatAndDimensionAndStream })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.VertexBufferFromSkinnedMeshRenderer; } }
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

    class VFXExpressionMeshFromSkinnedMeshRenderer : VFXExpression
    {
        public VFXExpressionMeshFromSkinnedMeshRenderer() : this(VFXValue<SkinnedMeshRenderer>.Default)
        {
        }

        public VFXExpressionMeshFromSkinnedMeshRenderer(VFXExpression skinnedMesh) : base(Flags.InvalidOnGPU, new VFXExpression[] { skinnedMesh })
        {
            if (skinnedMesh.valueType != VFXValueType.SkinnedMeshRenderer)
                throw new InvalidOperationException("Unexpected input type in VFXExpressionMeshFromSkinnedMeshRenderer : " + skinnedMesh.valueType);
        }

        public sealed override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshFromSkinnedMeshRenderer; } }
        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var skinnedMeshReduce = constParents[0];
            var skinnedMesh = skinnedMeshReduce.Get<SkinnedMeshRenderer>();
            Mesh result = skinnedMesh != null ? skinnedMesh.sharedMesh : null;
            return VFXValue.Constant(result);
        }
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

    abstract class VFXExpressionSampleBaseFloat : VFXExpression
    {
        public VFXExpressionSampleBaseFloat(Flags flags, VFXExpression source, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(flags, new VFXExpression[] { source, vertexOffset, channelFormatAndDimension })
        {
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleSkinnedMeshRendererFloat : VFXExpressionSampleBaseFloat
    {
        public VFXExpressionSampleSkinnedMeshRendererFloat() : this(VFXValue<SkinnedMeshRenderer>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleSkinnedMeshRendererFloat(VFXExpression skinnedMeshRenderer, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.InvalidOnCPU, skinnedMeshRenderer, vertexOffset, channelFormatAndDimension)
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }
    }

    class VFXExpressionSampleMeshFloat : VFXExpressionSampleBaseFloat
    {
        public VFXExpressionSampleMeshFloat() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, mesh, vertexOffset, channelFormatAndDimension)
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
    }

    abstract class VFXExpressionSampleBaseFloat2 : VFXExpression
    {
        public VFXExpressionSampleBaseFloat2(Flags flags, VFXExpression source, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(flags, new VFXExpression[] { source, vertexOffset, channelFormatAndDimension })
        {
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat2({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleSkinnedMeshRendererFloat2 : VFXExpressionSampleBaseFloat2
    {
        public VFXExpressionSampleSkinnedMeshRendererFloat2() : this(VFXValue<SkinnedMeshRenderer>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleSkinnedMeshRendererFloat2(VFXExpression skinnedMeshRenderer, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.InvalidOnCPU, skinnedMeshRenderer, vertexOffset, channelFormatAndDimension)
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        sealed public override VFXValueType valueType { get { return VFXValueType.Float2; } }
    }

    class VFXExpressionSampleMeshFloat2 : VFXExpressionSampleBaseFloat2
    {
        public VFXExpressionSampleMeshFloat2() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat2(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, mesh, vertexOffset, channelFormatAndDimension)
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
    }

    abstract class VFXExpressionSampleBaseFloat3 : VFXExpression
    {
        public VFXExpressionSampleBaseFloat3(Flags flags, VFXExpression source, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(flags, new VFXExpression[] { source, vertexOffset, channelFormatAndDimension })
        {
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat3({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleSkinnedMeshRendererFloat3 : VFXExpressionSampleBaseFloat3
    {
        public VFXExpressionSampleSkinnedMeshRendererFloat3() : this(VFXValue<SkinnedMeshRenderer>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleSkinnedMeshRendererFloat3(VFXExpression skinnedMeshRenderer, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.InvalidOnCPU, skinnedMeshRenderer, vertexOffset, channelFormatAndDimension)
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }
    }

    class VFXExpressionSampleMeshFloat3 : VFXExpressionSampleBaseFloat3
    {
        public VFXExpressionSampleMeshFloat3() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat3(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, mesh, vertexOffset, channelFormatAndDimension)
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
    }

    abstract class VFXExpressionSampleBaseFloat4 : VFXExpression
    {
        public VFXExpressionSampleBaseFloat4(Flags flags, VFXExpression source, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(flags, new VFXExpression[] { source, vertexOffset, channelFormatAndDimension })
        {
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshFloat4({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleSkinnedMeshRendererFloat4 : VFXExpressionSampleBaseFloat4
    {
        public VFXExpressionSampleSkinnedMeshRendererFloat4() : this(VFXValue<SkinnedMeshRenderer>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleSkinnedMeshRendererFloat4(VFXExpression skinnedMeshRenderer, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.InvalidOnCPU, skinnedMeshRenderer, vertexOffset, channelFormatAndDimension)
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }
    }

    class VFXExpressionSampleMeshFloat4 : VFXExpressionSampleBaseFloat4
    {
        public VFXExpressionSampleMeshFloat4() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshFloat4(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, mesh, vertexOffset, channelFormatAndDimension)
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
    }

    abstract class VFXExpressionSampleBaseColor : VFXExpression
    {
        public VFXExpressionSampleBaseColor(Flags flags, VFXExpression source, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(flags, new VFXExpression[] { source, vertexOffset, channelFormatAndDimension })
        {
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleMeshColor({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionSampleSkinnedMeshRendererColor : VFXExpressionSampleBaseColor
    {
        public VFXExpressionSampleSkinnedMeshRendererColor() : this(VFXValue<SkinnedMeshRenderer>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleSkinnedMeshRendererColor(VFXExpression skinnedMeshRenderer, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.InvalidOnCPU, skinnedMeshRenderer, vertexOffset, channelFormatAndDimension)
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }
    }

    class VFXExpressionSampleMeshColor : VFXExpressionSampleBaseColor
    {
        public VFXExpressionSampleMeshColor() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleMeshColor(VFXExpression mesh, VFXExpression vertexOffset, VFXExpression channelFormatAndDimension) : base(Flags.None, mesh, vertexOffset, channelFormatAndDimension)
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
    }

    class VFXExpressionMeshVertexCount : VFXExpression
    {
        public VFXExpressionMeshVertexCount() : this(VFXValue<Mesh>.Default)
        {
        }

        public VFXExpressionMeshVertexCount(VFXExpression mesh) : base(Flags.InvalidOnGPU, new VFXExpression[1] { mesh })
        {
            if (mesh.valueType != VFXValueType.Mesh)
                throw new InvalidOperationException("Unexpected type in VFXExpressionMeshVertexCount : " + mesh.valueType);
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

    class VFXExpressionMeshChannelInfos : VFXExpression
    {
        public VFXExpressionMeshChannelInfos() : this(VFXValue<Mesh>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionMeshChannelInfos(VFXExpression mesh, VFXExpression channelIndex) : base(Flags.InvalidOnGPU, new VFXExpression[2] { mesh, channelIndex })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.MeshChannelInfos; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var meshReduce = constParents[0];
            var channelIndexReduce = constParents[1];

            var mesh = meshReduce.Get<Mesh>();
            var channelIndex = channelIndexReduce.Get<uint>();

            return VFXValue.Constant(VFXExpressionMesh.GetChannelInfos(mesh, channelIndex));
        }
    }

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
