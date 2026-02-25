using System.Text;

namespace UnityEditor.VFX
{
    static class VFXGraphLegacyCompilationOutputExtensions
    {
        internal static string ToDetailedString(this VfxGraphLegacyCompilationOutput output)
        {
            var sb = new StringBuilder();

            sb.AppendLine("VFXGraphLegacyCompilationOutput:");
            sb.AppendLine($"  Version: {output.Version}");
            sb.AppendLine($"  Compilation Mode: {output.CompilationMode}");

            sb.AppendLine("  Expression Sheet:");
            if (output.SheetExpressions != null)
            {
                sb.AppendLine($"    Expressions Count: {output.SheetExpressions.Count}");
                for (int i = 0; i < output.SheetExpressions.Count; i++)
                {
                    var expr = output.SheetExpressions[i];
                    sb.AppendLine($"      Expression {i}: {expr.op}");
                }
            }
            else
            {
                sb.AppendLine("    Expressions: null");
            }

            if (output.SheetExpressionsPerSpawnEventAttribute != null)
            {
                sb.AppendLine($"    PerSpawnEventAttribute Count: {output.SheetExpressionsPerSpawnEventAttribute.Count}");
                for (int i = 0; i < output.SheetExpressionsPerSpawnEventAttribute.Count; i++)
                {
                    var expr = output.SheetExpressionsPerSpawnEventAttribute[i];
                    sb.AppendLine($"      Expression {i}: {expr.op}");
                }
            }
            else
            {
                sb.AppendLine("    PerSpawnEventAttribute: null");
            }

            if (output.SheetValues != null)
            {
                sb.AppendLine($"    Values Count: {output.SheetValues.Count}");
                for (int i = 0; i < output.SheetValues.Count; i++)
                {
                    var value = output.SheetValues[i];
                    sb.AppendLine($"      Value {i}: {value} - {value.expressionIndex}");
                }
            }
            else
            {
                sb.AppendLine("    Values: null");
            }

            if (output.SheetExposed != null)
            {
                sb.AppendLine($"    Exposed Count: {output.SheetExposed.Count}");
                for (int i = 0; i < output.SheetExposed.Count; i++)
                {
                    var value = output.SheetExposed[i];
                    sb.AppendLine($"      Exposed Value {i}: {value.mapping}");
                }
            }
            else
            {
                sb.AppendLine("    Exposed Values: null");

            }

            if (output.SystemDescs != null)
            {
                sb.AppendLine("  System Descriptions:");
                for (int i = 0; i < output.SystemDescs.Count; i++)
                {
                    var systemDesc = output.SystemDescs[i];
                    sb.AppendLine($"    System {i}:");
                    sb.AppendLine($"      Name: {systemDesc.name}");
                    sb.AppendLine($"      Type: {systemDesc.type}");
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

                    if (systemDesc.tasks != null)
                    {
                        sb.AppendLine($"      Tasks Count: {systemDesc.tasks.Length}");
                        for (int j = 0; j < systemDesc.tasks.Length; j++)
                        {
                            var task = systemDesc.tasks[j];
                            sb.AppendLine(
                                $"        Task {j}: Type={task.type}, ShaderSourceIndex={task.shaderSourceIndex}");
                            sb.AppendLine($"        Processor: {task.processor?.GetType().Name ?? "None"}");

                            //Add task buffers mapping
                            if (task.buffers != null)
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
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("  System Descriptions: null");
            }

            if (output.EventDescs != null)
            {
                sb.AppendLine("  Event Descriptions:");
                for (int i = 0; i < output.EventDescs.Count; i++)
                {
                    var eventDesc = output.EventDescs[i];
                    sb.AppendLine($"    Event {i}: Name={eventDesc.name}");
                    sb.AppendLine($"      Init Systems Count: {eventDesc.initSystems?.Length ?? 0}");
                    sb.AppendLine($"      Start Systems Count: {eventDesc.startSystems?.Length ?? 0}");
                    sb.AppendLine($"      Stop Systems Count: {eventDesc.stopSystems?.Length ?? 0}");
                }
            }
            else
            {
                sb.AppendLine("  Event Descriptions: null");
            }

            if (output.GpuBufferDescs != null)
            {
                sb.AppendLine("  GPU Buffer Descriptions:");
                for (int i = 0; i < output.GpuBufferDescs.Count; i++)
                {
                    var bufferDesc = output.GpuBufferDescs[i];
                    sb.AppendLine($"    Buffer {i}: Target={bufferDesc.target}, Size={bufferDesc.size}, Capacity={bufferDesc.capacity}, Stride={bufferDesc.stride}");
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
                sb.AppendLine("  GPU Buffer Descriptions: null");
            }

            if (output.CpuBufferDescs != null)
            {
                sb.AppendLine("  CPU Buffer Descriptions:");
                for (int i = 0; i < output.CpuBufferDescs.Count; i++)
                {
                    var bufferDesc = output.CpuBufferDescs[i];
                    sb.AppendLine($"    Buffer {i}: Capacity={bufferDesc.capacity}, Stride={bufferDesc.stride}");
                }
            }
            else
            {
                sb.AppendLine("  CPU Buffer Descriptions: null");
            }

            if (output.TemporaryBufferDescs != null)
            {
                sb.AppendLine("  Temporary GPU Buffer Descriptions:");
                for (int i = 0; i < output.TemporaryBufferDescs.Count; i++)
                {
                    var tempBufferDesc = output.TemporaryBufferDescs[i];
                    var bufferDesc = tempBufferDesc.desc;
                    sb.AppendLine($"    Buffer {i}: FrameCount={tempBufferDesc.frameCount},Target={bufferDesc.target}, Capacity={bufferDesc.capacity}, Size={bufferDesc.size}, Stride={bufferDesc.stride}");
                }
            }
            else
            {
                sb.AppendLine("  Temporary GPU Buffer Descriptions: null");
            }

            if (output.ShaderSourceDescs != null)
            {
                sb.AppendLine("  Shader Source Descriptions:");
                for (int i = 0; i < output.ShaderSourceDescs.Count; i++)
                {
                    var shaderDesc = output.ShaderSourceDescs[i];
                    sb.AppendLine($"    Shader {i}: Name={shaderDesc.name}, Compute={shaderDesc.compute}");
                    // Be careful with large shader sources!
                    // sb.AppendLine($"    Shader {i} Source:\n{shaderDesc.source}\n");
                }
            }
            else
            {
                sb.AppendLine("  Shader Source Descriptions: null");
            }

            if (output.Objects != null)
            {
                sb.AppendLine("  Objects:");
                for (int i = 0; i < output.Objects.Count; ++i)
                {
                    var obj = output.Objects[i];
                    sb.AppendLine($"    Object {i}: Name={obj?.name}, Type={obj?.GetType().Name}");
                }
            }
            else
            {
                sb.AppendLine("  Objects: null");
            }

            return sb.ToString();
        }
    }
}
