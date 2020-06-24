using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.Universal
{
    public sealed class ShaderDebugPrintManager
    {
        private static readonly ShaderDebugPrintManager instance = new ShaderDebugPrintManager();

        private const int DebugUAVSlot = 7;
        private const int FramesInFlight = 4;
        private const int MaxBufferElements = 1024 * 1024; // 1M - must match the shader size definition

        private List<GraphicsBuffer> m_outputBuffers = new List<GraphicsBuffer>();

        private List<Rendering.AsyncGPUReadbackRequest> m_readbackRequests =
            new List<Rendering.AsyncGPUReadbackRequest>();

        private int m_FrameCounter = 0;
        private bool m_FrameCleared = false;

        private static readonly int m_ShaderPropertyIDInputMouse = Shader.PropertyToID("_ShaderDebugPrintInputMouse");
        private static readonly int m_ShaderPropertyIDInputFrame = Shader.PropertyToID("_ShaderDebugPrintInputFrame");

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
        };

        static ShaderDebugPrintManager()
        {
        }

        private ShaderDebugPrintManager()
        {
            for (int i = 0; i < FramesInFlight; i++)
            {
                m_outputBuffers.Add(new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxBufferElements, 4));
                m_readbackRequests.Add(new Rendering.AsyncGPUReadbackRequest());
            }
        }

        public static ShaderDebugPrintManager Instance
        {
            get { return instance; }
        }

        public void SetShaderDebugPrintInputConstants(CommandBuffer cmd)
        {
            var input = ShaderDebugPrintInput.Get();

            var mouse = new Vector4(input.Pos.x, input.Pos.y, input.LeftDown ? 1 : 0, input.RightDown ? 1 : 0);
            cmd.SetGlobalVector(m_ShaderPropertyIDInputMouse, mouse);
            cmd.SetGlobalInt(m_ShaderPropertyIDInputFrame, m_FrameCounter);
        }

        public void SetShaderDebugPrintBindings(CommandBuffer cmd)
        {
            int index = m_FrameCounter % FramesInFlight;
            if (!m_readbackRequests[index].done)
            {
                // We shouldn't end up here too often
                m_readbackRequests[index].WaitForCompletion();
            }

            cmd.SetRandomWriteTarget(DebugUAVSlot, m_outputBuffers[index]);

            ClearShaderDebugPrintBuffer();
        }

        private void ClearShaderDebugPrintBuffer()
        {
            // Only clear the buffer the first time this is called in each frame
            if (!m_FrameCleared)
            {
                int index = m_FrameCounter % FramesInFlight;
                NativeArray<uint> data = new NativeArray<uint>(1, Allocator.Temp);
                data[0] = 0;
                m_outputBuffers[index].SetData(data, 0, 0, 1);
                m_FrameCleared = true;
            }
        }

        private void BufferReadComplete(Rendering.AsyncGPUReadbackRequest request)
        {
            Assert.IsTrue(request.done);

            if (!request.hasError)
            {
                NativeArray<uint> data = request.GetData<uint>(0);

                Debug.Log("Frame #" + m_FrameCounter + ": ");

                uint count = data[0];

                if (count >= MaxBufferElements)
                {
                    count = MaxBufferElements;
                    Debug.LogWarning("Debug Shader Print Buffer Full!");
                }

                unsafe // Need to do ugly casts via pointers
                {
                    uint *ptr = (uint*)data.GetUnsafePtr();
                    for (int i = 1; i < count;)
                    {
                        DebugValueType type = (DebugValueType) data[i];
                        switch (type)
                        {
                            case DebugValueType.TypeUint:
                                Debug.Log(data[i + 1]);
                                i += 2;
                                break;
                            case DebugValueType.TypeInt:
                                int valueInt = *(int*) &ptr[i + 1];
                                Debug.Log(valueInt);
                                i += 2;
                                break;
                            case DebugValueType.TypeFloat:
                                float valueFloat = *(float*)&ptr[i + 1];
                                Debug.Log(valueFloat);
                                i += 2;
                                break;
                            case DebugValueType.TypeUint2:
                                uint2 valueUint2 = *(uint2*)&ptr[i + 1];
                                Debug.Log(valueUint2);
                                i += 3;
                                break;
                            case DebugValueType.TypeInt2:
                                int2 valueInt2 = *(int2*)&ptr[i + 1];
                                Debug.Log(valueInt2);
                                i += 3;
                                break;
                            case DebugValueType.TypeFloat2:
                                float2 valueFloat2 = *(float2*)&ptr[i + 1];
                                Debug.Log(valueFloat2);
                                i += 3;
                                break;
                            case DebugValueType.TypeUint3:
                                uint3 valueUint3 = *(uint3*)&ptr[i + 1];
                                Debug.Log(valueUint3);
                                i += 4;
                                break;
                            case DebugValueType.TypeInt3:
                                int3 valueInt3 = *(int3*)&ptr[i + 1];
                                Debug.Log(valueInt3);
                                i += 4;
                                break;
                            case DebugValueType.TypeFloat3:
                                float3 valueFloat3 = *(float3*)&ptr[i + 1];
                                Debug.Log(valueFloat3);
                                i += 4;
                                break;
                            case DebugValueType.TypeUint4:
                                uint4 valueUint4 = *(uint4*)&ptr[i + 1];
                                Debug.Log(valueUint4);
                                i += 5;
                                break;
                            case DebugValueType.TypeInt4:
                                int4 valueInt4 = *(int4*)&ptr[i + 1];
                                Debug.Log(valueInt4);
                                i += 5;
                                break;
                            case DebugValueType.TypeFloat4:
                                float4 valueFloat4 = *(float4*)&ptr[i + 1];
                                Debug.Log(valueFloat4);
                                i += 5;
                                break;
                            default:
                                i = (int)count; // Cannot handle the rest if there is an unknown type
                                break;
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Error at read back!");
            }
        }

        public void EndFrame()
        {
            int index = m_FrameCounter % FramesInFlight;
            m_readbackRequests[index] = Rendering.AsyncGPUReadback.Request(m_outputBuffers[index], BufferReadComplete);

            m_FrameCounter++;
            m_FrameCleared = false;
        }
    }

    public struct ShaderDebugPrintInput
    {
        // Mouse input
        // GameView bottom-left == (0,0) top-right == (surface.width, surface.height) where surface == game display surface/rendertarget
        // For screen pixel coordinates, game-view should be set to "Free Aspect".
        // Works only in PlayMode
        public Vector2 Pos { get; set; }
        public bool LeftDown { get; set; }
        public bool RightDown { get; set; }
        public bool MiddleDown { get; set; }

        static public ShaderDebugPrintInput Get()
        {
            var r = new ShaderDebugPrintInput();
            r.Pos = Input.mousePosition;
            r.LeftDown = Input.GetAxis("Fire1") > 0.5f;
            r.RightDown = Input.GetAxis("Fire2") > 0.5f;
            r.MiddleDown = Input.GetAxis("Fire3") > 0.5f;
            return r;
        }
        public string Log()
        {
            return $"Mouse: {Pos.x}x{Pos.y}  Btns: Left:{LeftDown} Right:{RightDown} Middle:{MiddleDown} ";
        }
    }
}
