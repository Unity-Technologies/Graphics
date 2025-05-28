using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uRenderGraphViewerSessionCreated_v1
    // taxonomy = editor.analytics.uRenderGraphViewerSessionCreated.v1
    internal class RenderGraphViewerSessionCreatedAnalytic
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.srp";
        const string k_EventName = "uRenderGraphViewerSessionCreated";

        public enum SessionType
        {
            Local = 0,
            Remote = 1
        }

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey, maxEventsPerHour: k_MaxEventsPerHour, maxNumberOfElements: k_MaxNumberOfElements)]
        class Analytic : IAnalytic
        {
            public Analytic(SessionType sessionType, DebugMessageHandler.AnalyticsPayload payload)
            {
                using (GenericPool<Data>.Get(out var data))
                {
                    data.session_type = sessionType.ToString();
                    data.graphics_device_type = payload.graphicsDeviceType.ToString();
                    data.device_type = payload.deviceType.ToString();
                    data.device_model = payload.deviceModel;
                    data.gpu_vendor = payload.gpuVendor;
                    data.gpu_name = payload.gpuName;

                    m_Data = data;
                }
            }

            [Serializable]
            class Data : IAnalytic.IData
            {
                // Naming convention for analytics data
                public string session_type;
                public string graphics_device_type;
                public string device_type;
                public string device_model;
                public string gpu_vendor;
                public string gpu_name;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            Data m_Data;
        };

        public static void Send(SessionType sessionType, DebugMessageHandler.AnalyticsPayload payload)
        {
            Analytic analytic = new Analytic(sessionType, payload);
            AnalyticsUtils.SendData(analytic);
        }
    }
}
