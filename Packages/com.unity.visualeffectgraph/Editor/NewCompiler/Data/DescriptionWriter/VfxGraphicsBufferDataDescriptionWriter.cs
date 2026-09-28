using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Declares a bound graphics buffer in the generated shader: a raw <c>ByteAddressBuffer</c> unless a block
    /// declared an element type for it, in which case the typed buffer plus the element struct it reads.
    /// </summary>
    class VfxGraphicsBufferDataDescriptionWriter : IDataDescriptionWriter<VfxGraphicsBufferData>
    {
        const string RawBufferType = "ByteAddressBuffer";

        public void WriteDescription(ShaderWriter shaderWriter, DataView dataView, VfxGraphicsBufferData dataDescription,
            string name, CompilationContext context)
        {
            if (context.data.GetOrCreate<ResourceTypeContainer>().TryGetShape(dataView.Id, out var bufferType))
            {
                VfxBufferElementTypeWriter.WriteDeclarations(shaderWriter, bufferType);
            }
        }

        public bool WriteView(ShaderWriter shaderWriter, DataView usedDataView, DataNode dataNode, string name,
            string sourceName, CompilationContext context)
        {
            context.data.GetOrCreate<ResourceTypeContainer>().TryGetBufferType(usedDataView, dataNode, out var bufferType);
            shaderWriter.WriteVariableDeclaration(VfxBufferTypeHlsl.GetDeclaration(bufferType) ?? RawBufferType, name);

            // Nothing to build a view over: the buffer is what the generated code names.
            return false;
        }
    }

    /// <summary>
    /// The element type a task declared for the buffer behind a data view, when it declared one: a
    /// <c>StructuredBuffer&lt;Foo&gt;</c> rather than a raw buffer.
    /// </summary>
    class ResourceTypeContainer
    {
        // Keyed on the shape alone - the element type and container kind, read-write stripped off it: that
        // part is shared by every reader of the buffer, since it is the same memory laid out one way.
        // Read-only versus read-write is per task and never lives here (see TryGetBufferType).
        readonly Dictionary<DataViewId, BufferType> m_Shapes = new();

        /// <summary>
        /// The shape declared for a view - the element type and container kind, read-write stripped off -
        /// for the struct declaration every reader shares. See <see cref="TryGetBufferType"/> for a
        /// specific task's own read-only-or-read-write declaration.
        /// </summary>
        public bool TryGetShape(DataViewId dataViewId, out BufferType bufferType)
            => m_Shapes.TryGetValue(dataViewId, out bufferType);

        /// <summary>
        /// The type a task declared for the buffer behind this view, with the read-write-ness the given
        /// task node itself needs: the shape is shared by every reader, but whether a task reads or writes
        /// it is per task, resolved off the generic usage its binding registered.
        /// </summary>
        public bool TryGetBufferType(DataView usedDataView, DataNode dataNode, out BufferType bufferType)
        {
            if (!TryGetShape(usedDataView.Id, out var shape))
            {
                bufferType = default;
                return false;
            }

            bufferType = VfxBufferTypeHlsl.WithReadWrite(shape, dataNode.IsWritten(usedDataView.Id));
            return true;
        }

        /// <summary>
        /// Records the shape declared for a view: the element type and container kind, read-write stripped
        /// off since that is per task, not per resource. Two contexts declaring different shapes for one
        /// buffer collide here: the first one wins and the conflict is reported rather than silently
        /// miscompiled.
        /// </summary>
        public void Add(DataViewId dataViewId, BufferType bufferType)
        {
            if (!bufferType.valid)
                return;

            var shape = VfxBufferTypeHlsl.WithReadWrite(bufferType, isReadWrite: false);
            if (m_Shapes.TryAdd(dataViewId, shape))
                return;

            var declared = m_Shapes[dataViewId];
            if (declared != shape)
            {
                Debug.LogError($"Diverging type usage for a GPU buffer: declared as both {declared.verbatimType} and {bufferType.verbatimType}.");
            }
        }
    }

    /// <summary>
    /// Writes the element type a buffer is read as: the struct itself and the helper that samples a buffer
    /// of it, matching what <see cref="VFXExpressionSampleBuffer"/> generates.
    /// </summary>
    static class VfxBufferElementTypeWriter
    {
        public static void WriteDeclarations(ShaderWriter shaderWriter, BufferType bufferType)
        {
            var elementType = bufferType.actualType;

            // HLSLParser resolves a template argument it does not know to void: the buffer is still usable,
            // there is just no type to declare for it.
            if (elementType == null || elementType == typeof(void))
                return;

            // Several containers can be read as the same element type, and each contributes its own include
            // file to the shader, so the struct and its helper are guarded by the type they declare.
            var typeName = HlslCodeHelper.GetTypeName(elementType);
            shaderWriter.WriteLine($"#ifndef VFX_BUFFER_TYPE_{typeName.ToUpperInvariant()}");
            shaderWriter.WriteLine($"#define VFX_BUFFER_TYPE_{typeName.ToUpperInvariant()}");
            shaderWriter.NewLine();

            WriteStructDeclaration(shaderWriter, elementType, new HashSet<string>());
            WriteSampleFunction(shaderWriter, elementType);

            shaderWriter.WriteLine($"#endif // VFX_BUFFER_TYPE_{typeName.ToUpperInvariant()}");
        }

        static void WriteStructDeclaration(ShaderWriter shaderWriter, Type type, HashSet<string> declaredStructs)
        {
            var typeName = HlslCodeHelper.GetTypeName(type);

            // A builtin maps to an HLSL type of its own, so there is nothing to declare. Anything else falls
            // back to its own name, which is the struct this declares.
            if (typeName != type.Name || !declaredStructs.Add(typeName))
                return;

            var fields = new List<VFXLibrary.VFXFieldType>(VFXLibrary.GetFieldFromType(type));

            // Nested structs have to be declared before the struct using them.
            foreach (var field in fields)
            {
                if (field.valueType == VFXValueType.None)
                    WriteStructDeclaration(shaderWriter, field.type, declaredStructs);
            }

            shaderWriter.WriteLine($"struct {typeName}");
            shaderWriter.OpenBlock();
            foreach (var field in fields)
            {
                shaderWriter.WriteVariableDeclaration(HlslCodeHelper.GetTypeName(field.type), field.name);
            }
            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            shaderWriter.NewLine();
        }

        static void WriteSampleFunction(ShaderWriter shaderWriter, Type type)
        {
            var typeName = HlslCodeHelper.GetTypeName(type);
            var expectedStride = Marshal.SizeOf(type);

            shaderWriter.WriteLine(
                $"{typeName} SampleStructuredBuffer(StructuredBuffer<{typeName}> buffer, uint index, uint actualStride, uint actualCount)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine($"{typeName} read = ({typeName})0;");
            shaderWriter.WriteLine("[branch]");
            shaderWriter.WriteLine($"if (actualStride == (uint){expectedStride} && index < actualCount)");
            shaderWriter.OpenBlock();
            shaderWriter.WriteLine("read = buffer[index];");
            shaderWriter.CloseBlock();
            shaderWriter.WriteLine("return read;");
            shaderWriter.CloseBlock();
            shaderWriter.NewLine();
        }
    }
}
