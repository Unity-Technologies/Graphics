using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.Core
{
    internal class UnityComputeDeviceContext : IDeviceContext
    {
        private readonly Dictionary<BufferID, GraphicsBuffer> _buffers = new();
        private readonly HashSet<EventID> _inProgressRequests = new();
        private readonly HashSet<EventID> _failedRequests = new();
        private readonly HashSet<EventID> _successfulRequests = new();
        private uint _nextFreeBufferId;
        private uint _nextFreeEventId;
        private CommandBuffer _cmdBuffer;

        private List<BufferID> _temporaryBuffers = new();

        private void CreateCommandBuffer()
        {
            _cmdBuffer?.Dispose();
            _cmdBuffer = new CommandBuffer();
            _cmdBuffer.name = "UnityComputeDeviceContextCommandBuffer";
        }

        public BufferID CreateBuffer(ulong count, ulong stride)
        {
            Debug.Assert(count != 0, "Buffer element count cannot be zero.");
            Debug.Assert(stride != 0, "Stride cannot be zero.");
            Debug.Assert(stride % 4 == 0, "Stride must be a multiple of 4.");
            Debug.Assert(stride <= 2048, "Stride must be 2048 or less.");
            GraphicsBuffer buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)count, (int)stride);
            Debug.Assert(buffer.IsValid(), "Buffer was not successfully created.");
            var zeros = new NativeArray<byte>((int)(count * stride), Allocator.Temp, NativeArrayOptions.ClearMemory);
            buffer.SetData(zeros);
            zeros.Dispose();

            var idInteger = _nextFreeBufferId++;
            var id = new BufferID(idInteger);
            _buffers[id] = buffer;
            return id;
        }

        public void DestroyBuffer(BufferID id)
        {
            Debug.Assert(_buffers.ContainsKey(id), "Invalid buffer ID given.");

            _buffers[id].Release();
            _buffers.Remove(id);
        }

        public void Dispose()
        {
            ReleaseTemporaryBuffers();
            _cmdBuffer?.Dispose();
        }

        public bool Flush()
        {
            Debug.Assert(_cmdBuffer != null);
            Graphics.ExecuteCommandBuffer(_cmdBuffer);

            // TODO(pema.malling): Don't block here https://jira.unity3d.com/browse/LIGHT-1699
            // Ideally we shouldn't need this, but if we don't do it, read-backs will never finish unless explicitly waited on.
            AsyncGPUReadback.WaitAllRequests();

            ReleaseTemporaryBuffers();

            CreateCommandBuffer();
            return true;
        }

        public bool Initialize()
        {
            CreateCommandBuffer();
            return true;
        }

        public bool IsCompleted(EventID id)
        {
            return _successfulRequests.Contains(id) || _failedRequests.Contains(id);
        }

        public bool Wait(EventID id)
        {
            AsyncGPUReadback.WaitAllRequests();

            if (_failedRequests.Contains(id))
            {
                return false;
            }
            return true;
        }

        public void ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> result) where T : struct
        {
            Debug.Assert(_buffers.ContainsKey(src.Id), "Invalid buffer ID given.");

            int stride = UnsafeUtility.SizeOf<T>();
            int offset = (int)src.Offset * stride;
            int size = result.Length * stride;
            _cmdBuffer.RequestAsyncReadbackIntoNativeArray(ref result, _buffers[src.Id], size, offset, delegate { });
        }

        public void ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> result, EventID id) where T : struct
        {
            Debug.Assert(_buffers.ContainsKey(src.Id), "Invalid buffer ID given.");

            int stride = UnsafeUtility.SizeOf<T>();
            int offset = (int)src.Offset * stride;
            int size = result.Length * stride;
            _cmdBuffer.RequestAsyncReadbackIntoNativeArray(ref result, _buffers[src.Id], size, offset, request =>
            {
                Debug.Assert(request.done);
                // The user may have destroyed the event before the readback was completed, so we check if its still there.
                if (_inProgressRequests.Remove(id))
                {
                    if (request.hasError)
                    {
                        _failedRequests.Add(id);
                    }
                    else
                    {
                        _successfulRequests.Add(id);
                    }
                }
            });
            _inProgressRequests.Add(id);
        }

        public void WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src)
            where T : struct
        {
            Debug.Assert(_buffers.ContainsKey(dst.Id), "Invalid buffer ID given.");

            _cmdBuffer.SetBufferData(_buffers[dst.Id], src, 0, (int)dst.Offset, src.Length);
        }

        public void WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src, EventID id)
            where T : struct
        {
            Debug.Assert(_buffers.ContainsKey(dst.Id), "Invalid buffer ID given.");

            _cmdBuffer.SetBufferData(_buffers[dst.Id], src, 0, (int)dst.Offset, src.Length);

            _successfulRequests.Add(id);
        }

        public EventID CreateEvent()
        {
            var eventIdInteger = _nextFreeEventId++;
            var eventId = new EventID(eventIdInteger);
            return eventId;
        }

        public void DestroyEvent(EventID id)
        {
            if (_inProgressRequests.Contains(id))
            {
                _inProgressRequests.Remove(id);
            }
            if (_failedRequests.Contains(id))
            {
                _failedRequests.Remove(id);
            }
            if (_successfulRequests.Contains(id))
            {
                _successfulRequests.Remove(id);
            }
        }

        public GraphicsBuffer GetComputeBuffer(BufferID id)
        {
            Debug.Assert(_buffers.ContainsKey(id), "Invalid buffer ID given.");
            return _buffers[id];
        }

        public CommandBuffer GetCommandBuffer()
        {
            return _cmdBuffer;
        }

        // Temporary buffers are valid until the next call to Flush().
        public BufferID GetTemporaryBuffer(ulong count, ulong stride)
        {
            BufferID bufferID = CreateBuffer(count, stride);
            _temporaryBuffers.Add(bufferID);
            return bufferID;
        }

        private void ReleaseTemporaryBuffers()
        {
            foreach (var bufferId in _temporaryBuffers)
            {
                if (_buffers.ContainsKey(bufferId))
                {
                    DestroyBuffer(bufferId);
                }
            }
        }
    }
}
