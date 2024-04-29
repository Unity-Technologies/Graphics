using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    class VFXTexture2DValue : VFXObjectValue
    {
        public VFXTexture2DValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode, VFXValueType.Texture2D)
        {
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTexture2DValue(Get(), mode);
            return copy;
        }
    }

    class VFXTexture3DValue : VFXObjectValue
    {
        public VFXTexture3DValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode, VFXValueType.Texture3D)
        {
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTexture3DValue(Get(), mode);
            return copy;
        }
    }

    class VFXTextureCubeValue : VFXObjectValue
    {
        public VFXTextureCubeValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode, VFXValueType.TextureCube)
        {
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTextureCubeValue(Get(), mode);
            return copy;
        }
    }

    class VFXTexture2DArrayValue : VFXObjectValue
    {
        public VFXTexture2DArrayValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode, VFXValueType.Texture2DArray)
        {
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTexture2DArrayValue(Get(), mode);
            return copy;
        }
    }

    class VFXTextureCubeArrayValue : VFXObjectValue
    {
        public VFXTextureCubeArrayValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode, VFXValueType.TextureCubeArray)
        {
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXTextureCubeArrayValue(Get(), mode);
            return copy;
        }
    }

    class VFXCameraBufferValue : VFXValue<int>
    {
        public VFXCameraBufferValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode)
        {
        }

        public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXCameraBufferValue(Get(), mode);
            return copy;
        }

        public override VFXValueType valueType { get { return VFXValueType.CameraBuffer; } }

        sealed protected override int[] additionnalOperands { get { return new int[] { (int)valueType }; } }

        public override T Get<T>()
        {
            CameraBuffer cameraBuffer = base.Get();

            object value = cameraBuffer;

            if (typeof(T) == typeof(int))
                value = (int)cameraBuffer;

            if (typeof(T).IsAssignableFrom(typeof(Texture)))
                value = (Texture)cameraBuffer;

            return (T)value;
        }

        public override object GetContent()
        {
            return Get();
        }

        public override void SetContent(object value)
        {
            m_Content = (int)(CameraBuffer)value;
        }
    }

    class VFXMeshValue : VFXObjectValue
    {
        public VFXMeshValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode, VFXValueType.Mesh)
        {
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXMeshValue(Get(), mode);
            return copy;
        }
    }

    class VFXSkinnedMeshRendererValue : VFXObjectValue
    {
        public VFXSkinnedMeshRendererValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode, VFXValueType.SkinnedMeshRenderer)
        {
        }

        sealed public override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXSkinnedMeshRendererValue(Get(), mode);
            return copy;
        }
    }

    class VFXGraphicsBufferValue : VFXObjectValue
    {
        public VFXGraphicsBufferValue(int instanceID = 0, Mode mode = Mode.FoldableVariable) : base(instanceID, mode, VFXValueType.Buffer)
        {
        }
        public sealed override VFXValue CopyExpression(Mode mode)
        {
            var copy = new VFXGraphicsBufferValue(Get(), mode);
            return copy;
        }
    }

    struct BufferUsage : IEquatable<BufferUsage>
    {
        public enum Container
        {
            StructuredBuffer,
            RWStructuredBuffer,
            ByteAddressBuffer,
            RWByteAddressBuffer,
            Buffer,
            RWBuffer,
            AppendStructuredBuffer,
            ConsumeStructuredBuffer

            //Can be extended to integrate RWTexture2D here
        }

        public Container container { get; private set; }
        public Type actualType { get; private set; }
        public string verbatimType { get; private set; }
        public bool valid => actualType != null;

        public BufferUsage(Container container, string verbatimType, Type actualType)
        {
            this.container = container;
            this.actualType = actualType;
            this.verbatimType = verbatimType;
        }

        public bool Equals(BufferUsage other)
        {
            return container == other.container && actualType == other.actualType && verbatimType == other.verbatimType;
        }

        public override bool Equals(object obj)
        {
            return obj is BufferUsage other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(container, actualType, verbatimType);
        }

        public static bool operator ==(BufferUsage lhs, BufferUsage rhs) => lhs.Equals(rhs);
        public static bool operator !=(BufferUsage lhs, BufferUsage rhs) => !(lhs == rhs);
    }

#pragma warning disable 0659
    sealed class VFXExpressionBufferWithType : VFXExpression
    {
        public VFXExpressionBufferWithType() : this(new BufferUsage(), VFXValue<GraphicsBuffer>.Default)
        {
        }

        public VFXExpressionBufferWithType(BufferUsage usage, VFXExpression graphicsBuffer) : base(Flags.None, new[] { graphicsBuffer })
        {
            this.usage = usage;
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.None;

        public override VFXValueType valueType => parents[0].valueType;

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var reduced = (VFXExpressionBufferWithType)base.Reduce(reducedParents);
            reduced.usage = usage;
            return reduced;
        }

        protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            //Can be invoked from VFXViewControllerExpressions
            return constParents[0];
        }

        protected override int GetInnerHashCode()
        {
            return HashCode.Combine(base.GetInnerHashCode(), usage.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var other = obj as VFXExpressionBufferWithType;
            if (other == null)
                return false;

            return other.usage == usage;
        }

        public BufferUsage usage { get; private set; }
    }
#pragma warning restore 0659
}
