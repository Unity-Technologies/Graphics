using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    public sealed class ShaderDebugPrintManager
    {
        private static readonly ShaderDebugPrintManager s_Instance = new ShaderDebugPrintManager();

        private const int k_DebugUAVSlot = 7;
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

        public static ShaderDebugPrintManager instance => s_Instance;

        public void SetShaderDebugPrintInputConstants(CommandBuffer cmd, ShaderDebugPrintInput input)
        {
            var mouse = new Vector4(input.pos.x, input.pos.y, input.leftDown ? 1 : 0, input.rightDown ? 1 : 0);
            cmd.SetGlobalVector(m_ShaderPropertyIDInputMouse, mouse);
            cmd.SetGlobalInt(m_ShaderPropertyIDInputFrame, m_FrameCounter);
        }

        public void SetShaderDebugPrintBindings(CommandBuffer cmd)
        {
            int index = m_FrameCounter % k_FramesInFlight;
            if (!m_ReadbackRequests[index].done)
            {
                // We shouldn't end up here too often
                m_ReadbackRequests[index].WaitForCompletion();
            }

            cmd.SetRandomWriteTarget(k_DebugUAVSlot, m_OutputBuffers[index]);

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
                        if ((data[i] & k_TypeHasTag) == k_TypeHasTag)
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

                        switch (type)
                        {
                            case DebugValueType.TypeUint:
                            {
                                newOutputLine += data[i + 1];
                                i += 2;
                                break;
                            }
                            case DebugValueType.TypeInt:
                            {
                                int valueInt = *(int*)&ptr[i + 1];
                                newOutputLine += valueInt;
                                i += 2;
                                break;
                            }
                            case DebugValueType.TypeFloat:
                            {
                                float valueFloat = *(float*)&ptr[i + 1];
                                newOutputLine += valueFloat;
                                i += 2;
                                break;
                            }
                            case DebugValueType.TypeUint2:
                            {
                                uint* valueUint2 = &ptr[i + 1];
                                newOutputLine += $"({valueUint2[0]}, {valueUint2[1]})";
                                i += 3;
                                break;
                            }
                            case DebugValueType.TypeInt2:
                            {
                                int* valueInt2 = (int*)&ptr[i + 1];
                                newOutputLine += $"({valueInt2[0]}, {valueInt2[1]})";
                                i += 3;
                                break;
                            }
                            case DebugValueType.TypeFloat2:
                            {
                                float* valueFloat2 = (float*)&ptr[i + 1];
                                newOutputLine += $"({valueFloat2[0]}, {valueFloat2[1]})";
                                i += 3;
                                break;
                            }
                            case DebugValueType.TypeUint3:
                            {
                                uint* valueUint3 = &ptr[i + 1];
                                newOutputLine += $"({valueUint3[0]}, {valueUint3[1]}, {valueUint3[2]})";
                                i += 4;
                                break;
                            }
                            case DebugValueType.TypeInt3:
                            {
                                int* valueInt3 = (int*)&ptr[i + 1];
                                newOutputLine += $"({valueInt3[0]}, {valueInt3[1]}, {valueInt3[2]})";
                                i += 4;
                                break;
                            }
                            case DebugValueType.TypeFloat3:
                            {
                                float* valueFloat3 = (float*)&ptr[i + 1];
                                newOutputLine += $"({valueFloat3[0]}, {valueFloat3[1]}, {valueFloat3[2]})";
                                i += 4;
                                break;
                            }
                            case DebugValueType.TypeUint4:
                            {
                                uint* valueUint4 = &ptr[i + 1];
                                newOutputLine += $"({valueUint4[0]}, {valueUint4[1]}, {valueUint4[2]}, {valueUint4[3]})";
                                i += 5;
                                break;
                            }
                            case DebugValueType.TypeInt4:
                            {
                                int* valueInt4 = (int*)&ptr[i + 1];
                                newOutputLine += $"({valueInt4[0]}, {valueInt4[1]}, {valueInt4[2]}, {valueInt4[3]})";
                                i += 5;
                                break;
                            }
                            case DebugValueType.TypeFloat4:
                            {
                                float* valueFloat4 = (float*)&ptr[i + 1];
                                newOutputLine += $"({valueFloat4[0]}, {valueFloat4[1]}, {valueFloat4[2]}, {valueFloat4[3]})";
                                i += 5;
                                break;
                            }
                            case DebugValueType.TypeBool:
                            {
                                newOutputLine += ((data[i + 1] == 0) ? "False" : "True");
                                i += 2;
                                break;
                            }
                            default:
                                i = (int)count;  // Cannot handle the rest if there is an unknown type
                                break;
                        }

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

        public void EndFrame()
        {
            int index = m_FrameCounter % k_FramesInFlight;
            m_ReadbackRequests[index] = Rendering.AsyncGPUReadback.Request(m_OutputBuffers[index], m_BufferReadCompleteAction);

            m_FrameCounter++;
            m_FrameCleared = false;
        }

        // Custom output API
        public string outputLine { get => m_OutputLine; }
        public Action<string> outputAction { set => m_OutputAction = value; }
        public void DefaultOutput(string line)
        {
            Debug.Log(line);
        }
    }

    public struct ShaderDebugPrintInput
    {
        // Mouse input
        // GameView bottom-left == (0,0) top-right == (surface.width, surface.height) where surface == game display surface/rendertarget
        // For screen pixel coordinates, game-view should be set to "Free Aspect".
        // Works only in PlayMode
        public Vector2 pos { get; set; }
        public bool leftDown { get; set; }
        public bool rightDown { get; set; }
        public bool middleDown { get; set; }

        public string String()
        {
            return $"Mouse: {pos.x}x{pos.y}  Btns: Left:{leftDown} Right:{rightDown} Middle:{middleDown} ";
        }
    }

    public static class ShaderDebugPrintInputProducer
    {
        static public ShaderDebugPrintInput Get()
        {
            var r = new ShaderDebugPrintInput();
#if ENABLE_LEGACY_INPUT_MANAGER
            r.pos = Input.mousePosition;
            r.leftDown = Input.GetMouseButton(0);
            r.rightDown = Input.GetMouseButton(1);
            r.middleDown = Input.GetMouseButton(2);
#endif
#if ENABLE_INPUT_SYSTEM
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
