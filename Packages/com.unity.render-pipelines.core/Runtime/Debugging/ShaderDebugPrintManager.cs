using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Internal development tool.
    /// Manages gpu-buffers for shader debug printing.
    /// </summary>

    public sealed class ShaderDebugPrintManager
    {
        private static readonly ShaderDebugPrintManager s_Instance = new ShaderDebugPrintManager();

        private const int k_FramesInFlight = 4;
        private const int k_MaxBufferElements = 1024 * 16; // Must match the shader size definition

        private List<GraphicsBuffer> m_OutputBuffers = new List<GraphicsBuffer>();

        private List<Rendering.AsyncGPUReadbackRequest> m_ReadbackRequests =
            new List<Rendering.AsyncGPUReadbackRequest>();

        // Cache Action to avoid delegate allocation
        private Action<AsyncGPUReadbackRequest> m_BufferReadCompleteAction;

        private int m_FrameCounter = 0;
        private bool m_FrameCleared = false;

        private string m_OutputLine = "";
        private Action<string> m_OutputAction;

        private static readonly int m_ShaderPropertyIDInputMouse = Shader.PropertyToID("_ShaderDebugPrintInputMouse");
        private static readonly int m_ShaderPropertyIDInputFrame = Shader.PropertyToID("_ShaderDebugPrintInputFrame");
        private static readonly int m_shaderDebugOutputData = Shader.PropertyToID("shaderDebugOutputData");

        // A static "container" for all profiler markers.
        private static class Profiling
        {
            // Uses nameof to avoid aliasing
            public static readonly ProfilingSampler BufferReadComplete = new ProfilingSampler($"{nameof(ShaderDebugPrintManager)}.{nameof(BufferReadComplete)}");
        }

        // Should match: com.unity.render-pipelines.core/ShaderLibrary/ShaderDebugPrint.hlsl
        enum DebugValueType
        {
            TypeUint = 1,
            TypeInt = 2,
            TypeFloat = 3,
            TypeUint2 = 4,
            TypeInt2 = 5,
            TypeFloat2 = 6,
            TypeUint3 = 7,
            TypeInt3 = 8,
            TypeFloat3 = 9,
            TypeUint4 = 10,
            TypeInt4 = 11,
            TypeFloat4 = 12,
            TypeBool = 13,
        };
        private const uint k_TypeHasTag = 128;

        private int DebugValueTypeToElemSize(DebugValueType type)
        {
            switch (type)
            {
                case DebugValueType.TypeUint:
                case DebugValueType.TypeInt:
                case DebugValueType.TypeFloat:
                case DebugValueType.TypeBool:
                    return 1;
                case DebugValueType.TypeUint2:
                case DebugValueType.TypeInt2:
                case DebugValueType.TypeFloat2:
                    return 2;
                case DebugValueType.TypeUint3:
                case DebugValueType.TypeInt3:
                case DebugValueType.TypeFloat3:
                    return 3;
                case DebugValueType.TypeUint4:
                case DebugValueType.TypeInt4:
                case DebugValueType.TypeFloat4:
                    return 4;
                default:
                    return 0;
            }
        }

        private ShaderDebugPrintManager()
        {
            for (int i = 0; i < k_FramesInFlight; i++)
            {
                m_OutputBuffers.Add(new GraphicsBuffer(GraphicsBuffer.Target.Structured, k_MaxBufferElements, 4));
                m_ReadbackRequests.Add(new Rendering.AsyncGPUReadbackRequest());
            }

            m_BufferReadCompleteAction = BufferReadComplete;
            m_OutputAction = DefaultOutput;
        }

        /// <summary>
        /// Get the current instance.
        /// </summary>
        public static ShaderDebugPrintManager instance => s_Instance;

        /// <summary>
        /// Set shader input constants.
        /// </summary>
        /// <param name="cmd">CommandBuffer to store the commands.</param>
        /// <param name="input">Input parameters for the constants.</param>
        public void SetShaderDebugPrintInputConstants(CommandBuffer cmd, ShaderDebugPrintInput input)
        {
            var mouse = new Vector4(input.pos.x, input.pos.y, input.leftDown ? 1 : 0, input.rightDown ? 1 : 0);
            cmd.SetGlobalVector(m_ShaderPropertyIDInputMouse, mouse);
            cmd.SetGlobalInt(m_ShaderPropertyIDInputFrame, m_FrameCounter);
        }

        /// <summary>
        /// Binds the gpu-buffers for current frame.
        /// </summary>
        /// <param name="cmd">CommandBuffer to store the commands.</param>
        public void SetShaderDebugPrintBindings(CommandBuffer cmd)
        {
            int index = m_FrameCounter % k_FramesInFlight;
            if (!m_ReadbackRequests[index].done)
            {
                // We shouldn't end up here too often
                m_ReadbackRequests[index].WaitForCompletion();
            }

            cmd.SetGlobalBuffer(m_shaderDebugOutputData, m_OutputBuffers[index]);

            ClearShaderDebugPrintBuffer();
        }

        private void ClearShaderDebugPrintBuffer()
        {
            // Only clear the buffer the first time this is called in each frame
            if (!m_FrameCleared)
            {
                int index = m_FrameCounter % k_FramesInFlight;
                NativeArray<uint> data = new NativeArray<uint>(1, Allocator.Temp);
                data[0] = 0;
                m_OutputBuffers[index].SetData(data, 0, 0, 1);
                m_FrameCleared = true;
            }
        }

        private void BufferReadComplete(Rendering.AsyncGPUReadbackRequest request)
        {
            using var profScope = new ProfilingScope(Profiling.BufferReadComplete);

            Assert.IsTrue(request.done);

            if (!request.hasError)
            {
                NativeArray<uint> data = request.GetData<uint>(0);

                uint count = data[0];

                if (count >= k_MaxBufferElements)
                {
                    count = k_MaxBufferElements;
                    // Shader print buffer is full, some data is lost!
                    Debug.LogWarning("Debug Shader Print Buffer Full!");
                }

                string newOutputLine = "";
                if (count > 0)
                    newOutputLine += "Frame #" + m_FrameCounter + ": ";

                unsafe // Need to do ugly casts via pointers
                {
                    uint* ptr = (uint*)data.GetUnsafePtr();
                    for (int i = 1; i < count;)
                    {
                        DebugValueType type = (DebugValueType)(data[i] & 0x0f);
                        bool hasTag = (data[i] & k_TypeHasTag) == k_TypeHasTag;

                        // ensure elem for tag after the header
                        if (hasTag && i + 1 < count)
                        {
                            uint tagEncoded = data[i + 1];
                            i++;

                            for (int j = 0; j < 4; j++)
                            {
                                char c = (char)(tagEncoded & 255);
                                // skip '\0', for low-level output (avoid string termination)
                                if (c == 0)
                                    continue;
                                newOutputLine += c;
                                tagEncoded >>= 8;
                            }

                            newOutputLine += " ";
                        }

                        // ensure elem for payload after the header/tag
                        int elemSize = DebugValueTypeToElemSize(type);
                        if (i + elemSize > count)
                            break;

                        i++; // [i] == payload

                        switch (type)
                        {
                            case DebugValueType.TypeUint:
                            {
                                newOutputLine += $"{data[i]}u";
                                break;
                            }
                            case DebugValueType.TypeInt:
                            {
                                int valueInt = *(int*)&ptr[i];
                                newOutputLine += valueInt;
                                break;
                            }
                            case DebugValueType.TypeFloat:
                            {
                                float valueFloat = *(float*)&ptr[i];
                                newOutputLine += $"{valueFloat}f";
                                break;
                            }
                            case DebugValueType.TypeUint2:
                            {
                                uint* valueUint2 = &ptr[i];
                                newOutputLine += $"uint2({valueUint2[0]}, {valueUint2[1]})";
                                break;
                            }
                            case DebugValueType.TypeInt2:
                            {
                                int* valueInt2 = (int*)&ptr[i];
                                newOutputLine += $"int2({valueInt2[0]}, {valueInt2[1]})";
                                break;
                            }
                            case DebugValueType.TypeFloat2:
                            {
                                float* valueFloat2 = (float*)&ptr[i];
                                newOutputLine += $"float2({valueFloat2[0]}, {valueFloat2[1]})";
                                break;
                            }
                            case DebugValueType.TypeUint3:
                            {
                                uint* valueUint3 = &ptr[i];
                                newOutputLine += $"uint3({valueUint3[0]}, {valueUint3[1]}, {valueUint3[2]})";
                                break;
                            }
                            case DebugValueType.TypeInt3:
                            {
                                int* valueInt3 = (int*)&ptr[i];
                                newOutputLine += $"int3({valueInt3[0]}, {valueInt3[1]}, {valueInt3[2]})";
                                break;
                            }
                            case DebugValueType.TypeFloat3:
                            {
                                float* valueFloat3 = (float*)&ptr[i];
                                newOutputLine += $"float3({valueFloat3[0]}, {valueFloat3[1]}, {valueFloat3[2]})";
                                break;
                            }
                            case DebugValueType.TypeUint4:
                            {
                                uint* valueUint4 = &ptr[i];
                                newOutputLine += $"uint4({valueUint4[0]}, {valueUint4[1]}, {valueUint4[2]}, {valueUint4[3]})";
                                break;
                            }
                            case DebugValueType.TypeInt4:
                            {
                                int* valueInt4 = (int*)&ptr[i];
                                newOutputLine += $"int4({valueInt4[0]}, {valueInt4[1]}, {valueInt4[2]}, {valueInt4[3]})";
                                break;
                            }
                            case DebugValueType.TypeFloat4:
                            {
                                float* valueFloat4 = (float*)&ptr[i];
                                newOutputLine += $"float4({valueFloat4[0]}, {valueFloat4[1]}, {valueFloat4[2]}, {valueFloat4[3]})";
                                break;
                            }
                            case DebugValueType.TypeBool:
                            {
                                newOutputLine += ((data[i] == 0) ? "False" : "True");
                                break;
                            }
                            default:
                                i = (int)count;  // Cannot handle the rest if there is an unknown type
                                break;
                        }

                        i += elemSize;

                        newOutputLine += " ";
                    }
                }

                if (count > 0)
                {
                    m_OutputLine = newOutputLine;
                    m_OutputAction(newOutputLine);
                }
            }
            else
            {
                const string errorMsg = "Error at read back!";
                m_OutputLine = errorMsg;
                m_OutputAction(errorMsg);
            }
        }

        /// <summary>
        /// Initiate async read-back of the GPU buffer to the CPU.
        /// Prepare a new GPU buffer for the next frame.
        /// </summary>
        public void EndFrame()
        {
            int index = m_FrameCounter % k_FramesInFlight;
            m_ReadbackRequests[index] = Rendering.AsyncGPUReadback.Request(m_OutputBuffers[index], m_BufferReadCompleteAction);

            m_FrameCounter++;
            m_FrameCleared = false;
        }

        /// <summary>
        /// Initiate synchronous read-back of the GPU buffer to the CPU and executes output action. By default prints to the debug log.
        /// </summary>
        public void PrintImmediate()
        {
            int index = m_FrameCounter % k_FramesInFlight;
            var request = Rendering.AsyncGPUReadback.Request(m_OutputBuffers[index]);
            request.WaitForCompletion();
            m_BufferReadCompleteAction(request);

            m_FrameCounter++;
            m_FrameCleared = false;
        }

        // Custom output API

        /// <summary>
        /// Get current print line.
        /// </summary>
        public string outputLine { get => m_OutputLine; }

        /// <summary>
        /// Action taken for each print line. By default prints to the debug log.
        /// </summary>
        public Action<string> outputAction { set => m_OutputAction = value; }

        /// <summary>
        /// The default output action. Print to the debug log.
        /// </summary>
        /// <param name="line">Line to be printed.</param>
        public void DefaultOutput(string line)
        {
            Debug.Log(line);
        }
    }

    /// <summary>
    /// Shader constant input parameters.
    /// </summary>
    public struct ShaderDebugPrintInput
    {
        // Mouse input for the shader

        /// <summary>
        /// Mouse position.
        /// GameView bottom-left == (0,0) top-right == (surface.width, surface.height) where surface == game display surface/rendertarget
        /// For screen pixel coordinates, game-view should be set to "Free Aspect".
        /// Works only in PlayMode.
        /// </summary>
        public Vector2 pos { get; set; }

        /// <summary>
        /// Left mouse button is pressed.
        /// </summary>
        public bool leftDown { get; set; }

        /// <summary>
        /// Right mouse button is pressed.
        /// </summary>
        public bool rightDown { get; set; }

        /// <summary>
        /// Middle mouse button is pressed.
        /// </summary>
        public bool middleDown { get; set; }

        /// <summary>
        /// Pretty print parameters for debug purposes.
        /// </summary>
        /// <returns>A string containing debug information</returns>
        // NOTE: Separate from ToString on purpose.
        public string String()
        {
            return $"Mouse: {pos.x}x{pos.y}  Btns: Left:{leftDown} Right:{rightDown} Middle:{middleDown} ";
        }
    }

    /// <summary>
    /// Reads system input to produce ShaderDebugPrintInput parameters.
    /// </summary>
    public static class ShaderDebugPrintInputProducer
    {
        /// <summary>
        /// Read system input.
        /// </summary>
        /// <returns>Input parameters for ShaderDebugPrintManager.</returns>
        static public ShaderDebugPrintInput Get()
        {
            var r = new ShaderDebugPrintInput();
#if ENABLE_LEGACY_INPUT_MANAGER
            r.pos = Input.mousePosition;
            r.leftDown = Input.GetMouseButton(0);
            r.rightDown = Input.GetMouseButton(1);
            r.middleDown = Input.GetMouseButton(2);
#endif
#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
            // NOTE: needs Unity.InputSystem asmdef reference.
            var mouse = InputSystem.Mouse.current;
            r.pos = mouse.position.ReadValue();
            r.leftDown = mouse.leftButton.isPressed;
            r.rightDown = mouse.rightButton.isPressed;
            r.middleDown = mouse.middleButton.isPressed;
#endif
            return r;
        }
    }
}
