using System;
using System.IO;
using UnityEngine.Networking.PlayerConnection;
#if UNITY_EDITOR
using UnityEditor.Networking.PlayerConnection;
#endif
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    class DebugMessageHandler : ScriptableObject
    {
        // Render Graph Viewer Debug message protocol version.
        // This number is sent from the player with the MessageType.DebugData message. The editor UI is only compatible
        // with an equal version - if the version is not the same, a "not supported" message is displayed in the UI.
        // As DebugData is currently serialized as json, it's quite resilient to adding/removing fields, so it's not
        // always necessary to bump this version in this case. However, be aware that this leads to a situation where
        // players built without this new code will not send these values. If you are unsure, bump the version.
        // If you modify the streams in SerializeMessage/DeserializeMessage, you should definitely bump the version.
        // ------------------------
        // Version history:
        // 1 - Initial version
        internal const int k_Version = 1;

        // These were generated using GUID.NewGuid and hard-coded
        static readonly Guid s_EditorToPlayerGuid = new Guid("df519969-f421-4397-b2a1-1740abc989a0");
        static readonly Guid s_PlayerToEditorGuid = new Guid("98d0787d-3917-4c48-8393-e313498046e6");

        public enum MessageType : byte
        {
            Activate = 0,
            DebugData = 1,
            AnalyticsData = 2
        }

        public abstract class IPayload
        {
            public int version;

            public bool isCompatible => version == k_Version;
        }

        public class DebugDataPayload : IPayload
        {
            public string graphName;
            public EntityId executionId;
            public DebugData debugData;
        }

        public class AnalyticsPayload : IPayload
        {
            public GraphicsDeviceType graphicsDeviceType;
            public DeviceType deviceType;
            public string deviceModel;
            public string gpuVendor;
            public string gpuName;

            public AnalyticsPayload()
            {
                deviceModel = SystemInfo.deviceModel;
                deviceType = SystemInfo.deviceType;
                graphicsDeviceType = SystemInfo.graphicsDeviceType;
                gpuVendor = SystemInfo.graphicsDeviceVendor;
                gpuName = SystemInfo.graphicsDeviceName;
            }
        }

        Action<MessageType, IPayload> m_UserCallback;

        // Note: The callback we register to must be a method of a class inheriting from UnityEngine.Object for the
        // persistent callback registration to work.
        void InternalCallback(MessageEventArgs msg)
        {
            var (messageType, payload) = DeserializeMessage(msg.data);
            m_UserCallback.Invoke(messageType, payload);
        }

        public void Register(Action<MessageType, IPayload> callback)
        {
            m_UserCallback = callback;
#if UNITY_EDITOR
            EditorConnection.instance.Register(s_PlayerToEditorGuid, InternalCallback);
#else
            PlayerConnection.instance.Register(s_EditorToPlayerGuid, InternalCallback);
#endif
        }

        public void UnregisterAll()
        {
#if UNITY_EDITOR
            EditorConnection.instance.Unregister(s_PlayerToEditorGuid, InternalCallback);
#else
            PlayerConnection.instance.Unregister(s_EditorToPlayerGuid, InternalCallback);
#endif
        }

        public void Send(MessageType messageType, IPayload payload = null)
        {
#if UNITY_EDITOR
            EditorConnection.instance.Send(s_EditorToPlayerGuid, SerializeMessage(messageType, payload));
#else
            PlayerConnection.instance.Send(s_PlayerToEditorGuid, SerializeMessage(messageType, payload));
#endif
        }


        internal static byte[] SerializeMessage(MessageType type, IPayload payload = null)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);

            writer.Write((byte)type);

            if (type == MessageType.DebugData)
            {
                writer.Write(k_Version);

                if (payload is not DebugDataPayload debugDataPayload)
                    throw new InvalidOperationException("No valid payload provided");

                writer.Write(debugDataPayload.graphName);
                writer.Write(debugDataPayload.executionId);
                writer.Write(DebugDataSerialization.ToJson(debugDataPayload.debugData));
            }
            else if (type == MessageType.AnalyticsData)
            {
                writer.Write(k_Version);

                if (payload is not AnalyticsPayload analyticsPayload)
                    throw new InvalidOperationException("No valid payload provided");

                writer.Write((int)analyticsPayload.graphicsDeviceType);
                writer.Write((int)analyticsPayload.deviceType);
                writer.Write(analyticsPayload.deviceModel);
                writer.Write(analyticsPayload.gpuVendor);
                writer.Write(analyticsPayload.gpuName);
            }

            return memoryStream.ToArray();
        }

        internal static (MessageType, IPayload) DeserializeMessage(byte[] data)
        {
            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var type = (MessageType)reader.ReadByte();
            if (type == MessageType.DebugData)
            {
                var payload = new DebugDataPayload();
                payload.version = reader.ReadInt32();
                if (!payload.isCompatible)
                {
                    Debug.LogWarning($"Render Graph Viewer message version mismatch (expected {k_Version}, received {payload.version})");
                    return (type, payload);
                }

                payload.graphName = reader.ReadString();
                payload.executionId = reader.ReadInt32();
                payload.debugData = DebugDataSerialization.FromJson(reader.ReadString());
                return (type, payload);
            }
            else if (type == MessageType.AnalyticsData)
            {
                var payload = new AnalyticsPayload();
                payload.version = reader.ReadInt32();
                if (!payload.isCompatible)
                {
                    return (type, payload);
                }

                payload.graphicsDeviceType = (GraphicsDeviceType)reader.ReadInt32();
                payload.deviceType = (DeviceType)reader.ReadInt32();
                payload.deviceModel = reader.ReadString();
                payload.gpuVendor = reader.ReadString();
                payload.gpuName = reader.ReadString();

                return (type, payload);
            }

            return (type, null);
        }
    }
}
