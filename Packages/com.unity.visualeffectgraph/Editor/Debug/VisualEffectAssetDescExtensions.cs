using System;
using System.Reflection;
using System.Text;
using UnityEditor.VFX;

static class VisualEffectAssetDescExtensions
{
    internal static string ToDetailedString(this VisualEffectAssetDesc assetDesc)
    {
        var sb = new StringBuilder();

        sb.AppendLine("VisualEffectAssetDesc:");
        sb.AppendLine("  Expression Sheet:");
        if (assetDesc.sheet.expressions != null)
        {
            sb.AppendLine($"    Expressions Count: {assetDesc.sheet.expressions.Length}");
            for (int i = 0; i < assetDesc.sheet.expressions.Length; i++)
            {
                var expr = assetDesc.sheet.expressions[i];
                sb.AppendLine($"      Expression {i}: Op={expr.op}, Data={FormatExpressionData(expr)}");
            }
        }
        else
        {
            sb.AppendLine("    Expressions: null");
        }

        if (assetDesc.sheet.expressionsPerSpawnEventAttribute != null)
        {
            sb.AppendLine($"    PerSpawnEventAttribute Count: {assetDesc.sheet.expressionsPerSpawnEventAttribute.Length}");
            for (int i = 0; i < assetDesc.sheet.expressionsPerSpawnEventAttribute.Length; i++)
            {
                var expr = assetDesc.sheet.expressionsPerSpawnEventAttribute[i];
                sb.AppendLine($"      Expression {i}: Op={expr.op}, Data={FormatExpressionData(expr)}");
            }
        }
        else
        {
            sb.AppendLine("    PerSpawnEventAttribute: null");
        }

        if (assetDesc.sheet.values != null)
        {
            sb.AppendLine($"    Values Count: {assetDesc.sheet.values.Length}");
            for (int i = 0; i < assetDesc.sheet.values.Length; i++)
            {
                var value = assetDesc.sheet.values[i];
                sb.AppendLine($"      Value {i}: ExpressionIndex={value.expressionIndex}, Value={FormatValueContainer(value)}");
            }
        }
        else
        {
            sb.AppendLine("    Values: null");
        }

        if (assetDesc.sheet.exposed != null)
        {
            sb.AppendLine($"    Exposed Values Count: {assetDesc.sheet.exposed.Length}");
            for (int i = 0; i < assetDesc.sheet.exposed.Length; i++)
            {
                var value = assetDesc.sheet.exposed[i];
                sb.AppendLine($"      Exposed Value {i}: Name={value.mapping.name}, Index={value.mapping.index}, Space={value.space}");
            }
        }
        else
        {
            sb.AppendLine("    Exposed Values: null");
        }

        if (assetDesc.systemDesc != null)
        {
            sb.AppendLine("  System Descriptions:");
            for (int i = 0; i < assetDesc.systemDesc.Length; i++)
            {
                var systemDesc = assetDesc.systemDesc[i];
                sb.AppendLine($"    System {i}:");
                sb.AppendLine($"      Name: {systemDesc.name}");
                sb.AppendLine($"      Type: {systemDesc.type}");
                sb.AppendLine($"      Capacity: {systemDesc.capacity}");
                sb.AppendLine($"      Flags: {systemDesc.flags}");
                sb.AppendLine($"      Layer: {systemDesc.layer}");

                if (systemDesc.buffers != null)
                {
                    sb.AppendLine($"      Buffers Count: {systemDesc.buffers.Length}");
                    for (int j = 0; j < systemDesc.buffers.Length; j++)
                    {
                        var buffer = systemDesc.buffers[j];
                        sb.AppendLine($"        Buffer {j}: Name={buffer.name}, Index={buffer.index}");
                    }
                }
                else
                {
                    sb.AppendLine("      Buffers: null");
                }

                if (systemDesc.values != null)
                {
                    sb.AppendLine($"      Values Count: {systemDesc.values.Length}");
                    for (int j = 0; j < systemDesc.values.Length; j++)
                    {
                        var value = systemDesc.values[j];
                        sb.AppendLine($"        Value {j}: Name={value.name}, Index={value.index}");
                    }
                }
                else
                {
                    sb.AppendLine("      Values: null");
                }

                if (systemDesc.instanceSplitDescs != null)
                {
                    sb.AppendLine($"      InstanceSplits Count: {systemDesc.instanceSplitDescs.Length}");
                    for (int j = 0; j < systemDesc.instanceSplitDescs.Length; j++)
                    {
                        var splitDesc = systemDesc.instanceSplitDescs[j];
                        sb.AppendLine($"        InstanceSplit {j}: Values= {{{string.Join(", ", splitDesc.values)}}}");
                    }
                }
                else
                {
                    sb.AppendLine("      InstanceSplits: null");
                }

                if (systemDesc.tasks != null)
                {
                    sb.AppendLine($"      Tasks Count: {systemDesc.tasks.Length}");
                    for (int j = 0; j < systemDesc.tasks.Length; j++)
                    {
                        var task = systemDesc.tasks[j];
                        sb.AppendLine($"        Task {j}: Type={task.type}, ShaderSourceIndex={task.shaderSourceIndex}, InstanceSplitIndex={task.instanceSplitIndex}, ModelId={task.modelId}, UsesMaterialVariant={task.usesMaterialVariant}");
                        sb.AppendLine($"        Processor: {task.processor?.GetType().Name ?? "None"}");

                        //Add task buffers mapping
                        if(task.buffers != null)
                        {
                            sb.AppendLine($"          Task Buffers Count: {task.buffers.Length}");
                            for (int k = 0; k < task.buffers.Length; k++)
                            {
                                var buffer = task.buffers[k];
                                sb.AppendLine($"            Task Buffer {k}: Name={buffer.name}, Index={buffer.index}");
                            }
                        }
                        else
                        {
                            sb.AppendLine("          Task Buffers: null");
                        }

                        if (task.temporaryBuffers != null)
                        {
                            sb.AppendLine($"          Task Temporary Buffers Count: {task.temporaryBuffers.Length}");
                            for (int k = 0; k < task.temporaryBuffers.Length; k++)
                            {
                                var tempMapping = task.temporaryBuffers[k];
                                sb.AppendLine($"            Task Temporary Buffer {k}: Name={tempMapping.mapping.name}, Index={tempMapping.mapping.index}, PastFrameIndex={tempMapping.pastFrameIndex}, PerCameraBuffer={tempMapping.perCameraBuffer}");
                            }
                        }
                        else
                        {
                            sb.AppendLine("          Task Temporary Buffers: null");
                        }

                        if (task.values != null)
                        {
                            sb.AppendLine($"          Task Values Count: {task.values.Length}");
                            for (int k = 0; k < task.values.Length; k++)
                            {
                                var value = task.values[k];
                                sb.AppendLine($"            Task Value {k}: Name={value.name}, Index={value.index}");
                            }
                        }
                        else
                        {
                            sb.AppendLine("          Task Values: null");
                        }

                        if (task.parameters != null)
                        {
                            sb.AppendLine($"          Task Parameters Count: {task.parameters.Length}");
                            for (int k = 0; k < task.parameters.Length; k++)
                            {
                                var param = task.parameters[k];
                                sb.AppendLine($"            Task Parameter {k}: Name={param.name}, Index={param.index}");
                            }
                        }
                        else
                        {
                            sb.AppendLine("          Task Parameters: null");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("      Tasks: null");
                }
            }
        }
        else
        {
            sb.AppendLine("  System Descriptions: null");
        }

        if (assetDesc.eventDesc != null)
        {
            sb.AppendLine("  Event Descriptions:");
            for (int i = 0; i < assetDesc.eventDesc.Length; i++)
            {
                var eventDesc = assetDesc.eventDesc[i];
                sb.AppendLine($"    Event {i}: Name={eventDesc.name}");
                sb.AppendLine($"      Init Systems: {{{string.Join(", ", eventDesc.initSystems ?? Array.Empty<uint>())}}}");
                sb.AppendLine($"      Start Systems: {{{string.Join(", ", eventDesc.startSystems ?? Array.Empty<uint>())}}}");
                sb.AppendLine($"      Stop Systems: {{{string.Join(", ", eventDesc.stopSystems ?? Array.Empty<uint>())}}}");
            }
        }
        else
        {
            sb.AppendLine("  Event Descriptions: null");
        }

        if (assetDesc.gpuBufferDesc != null)
        {
            sb.AppendLine("  GPU Buffer Descriptions:");
            for (int i = 0; i < assetDesc.gpuBufferDesc.Length; i++)
            {
                sb.AppendLine($"    Buffer {i}:");
                AppendGpuBufferDesc(sb, "      ", assetDesc.gpuBufferDesc[i]);
            }
        }
        else
        {
            sb.AppendLine("  GPU Buffer Descriptions: null");
        }

        if (assetDesc.cpuBufferDesc != null)
        {
            sb.AppendLine("  CPU Buffer Descriptions:");
            for (int i = 0; i < assetDesc.cpuBufferDesc.Length; i++)
            {
                var bufferDesc = assetDesc.cpuBufferDesc[i];
                sb.AppendLine($"    Buffer {i}: DebugName={bufferDesc.debugName}, Capacity={bufferDesc.capacity}, Stride={bufferDesc.stride}, HasInitialData={bufferDesc.initialData != null}");
                if (bufferDesc.layout != null && bufferDesc.layout.Length > 0)
                {
                    sb.AppendLine($"      Layout elements Count: {bufferDesc.layout.Length}");
                    foreach (var layoutDesc in bufferDesc.layout)
                    {
                        sb.AppendLine($"        Name={layoutDesc.name}, Type={layoutDesc.type}, Offset (bucket, structure, element) ={layoutDesc.offset.bucket}, {layoutDesc.offset.structure}, {layoutDesc.offset.element}");
                    }
                }
            }
        }
        else
        {
            sb.AppendLine("  CPU Buffer Descriptions: null");
        }

        if (assetDesc.temporaryBufferDesc != null)
        {
            sb.AppendLine("  Temporary GPU Buffer Descriptions:");
            for (int i = 0; i < assetDesc.temporaryBufferDesc.Length; i++)
            {
                var tempBufferDesc = assetDesc.temporaryBufferDesc[i];
                sb.AppendLine($"    Buffer {i}: FrameCount={tempBufferDesc.frameCount}");
                AppendGpuBufferDesc(sb, "      ", tempBufferDesc.desc);
            }
        }
        else
        {
            sb.AppendLine("  Temporary GPU Buffer Descriptions: null");
        }

        if (assetDesc.shaderSourceDesc != null)
        {
            sb.AppendLine("  Shader Source Descriptions:");
            for (int i = 0; i < assetDesc.shaderSourceDesc.Length; i++)
            {
                var shaderDesc = assetDesc.shaderSourceDesc[i];
                sb.AppendLine($"    Shader {i}: Name={shaderDesc.name}, Compute={shaderDesc.compute}, SourceLength={shaderDesc.source?.Length ?? 0}");
            }
        }
        else
        {
            sb.AppendLine("  Shader Source Descriptions: null");
        }

        sb.AppendLine($"  Renderer Settings: ShadowCastingMode={assetDesc.rendererSettings.shadowCastingMode}, MotionVectorGenerationMode={assetDesc.rendererSettings.motionVectorGenerationMode}");
        sb.AppendLine($"  Instancing Disabled Reason: {assetDesc.instancingDisabledReason}");
        sb.AppendLine($"  Compilation Mode: {assetDesc.compilationMode}");

        return sb.ToString();
    }

    static void AppendGpuBufferDesc(StringBuilder sb, string indent, VFXGPUBufferDesc bufferDesc)
    {
        sb.AppendLine($"{indent}DebugName={bufferDesc.debugName}, Target={bufferDesc.target}, Size={bufferDesc.size}, Capacity={bufferDesc.capacity}, Stride={bufferDesc.stride}, Mode={bufferDesc.mode}");
        if (bufferDesc.layout != null && bufferDesc.layout.Length > 0)
        {
            sb.AppendLine($"{indent}Layout elements Count: {bufferDesc.layout.Length}");
            foreach (var layoutDesc in bufferDesc.layout)
            {
                sb.AppendLine($"{indent}  Name={layoutDesc.name}, Type={layoutDesc.type}, Offset (bucket, structure, element) ={layoutDesc.offset.bucket}, {layoutDesc.offset.structure}, {layoutDesc.offset.element}");
            }
        }
    }

    // VFXExpressionDesc.data is a fixed buffer, only reachable from unsafe code.
    static unsafe string FormatExpressionData(VFXExpressionDesc expr)
    {
        return $"[{expr.data[0]}, {expr.data[1]}, {expr.data[2]}, {expr.data[3]}]";
    }

    // VFXExpressionValueContainerDesc only exposes its payload on its generic subclasses
    // (VFXExpressionValueContainerDesc<T>.value or VFXExpressionObjectValueContainerDesc<T>.entityId),
    // so the concrete value is read back via reflection to keep this dump type-agnostic.
    static string FormatValueContainer(VFXExpressionValueContainerDesc value)
    {
        var valueField = value.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance);
        if (valueField != null)
            return valueField.GetValue(value)?.ToString() ?? "null";

        var entityIdField = value.GetType().GetField("entityId", BindingFlags.Public | BindingFlags.Instance);
        if (entityIdField != null)
            return entityIdField.GetValue(value)?.ToString() ?? "null";

        return "<unknown>";
    }
}
