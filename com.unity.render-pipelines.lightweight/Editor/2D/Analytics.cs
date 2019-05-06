using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP.Analytics
{
    struct AnalyticsDataTypes
    {
        public const string k_LightDataString = "u2drendererlights";
        public const string k_2DRendererDataString = "u2drendererdata";
    }

    internal interface IAnalyticsData { };

    [Serializable]
    internal struct Light2DData : IAnalyticsData
    {
        public enum EventType
        {
            Created,
            Modified
        }

        [SerializeField]
        public EventType event_type;
        [SerializeField]
        public int instance_id;
        [SerializeField]
        public Light2D.LightType light_type;
    };


    [Serializable]
    internal struct RendererAssetData : IAnalyticsData
    {
        public enum EventType
        {
            Created,
            Modified
        }

        [SerializeField]
        public EventType event_type;
        [SerializeField]
        public int instance_id;
        [SerializeField]
        public int blending_layers_count;
        [SerializeField]
        public int blending_modes_used;
    }


    interface IAnalytics
    {
        AnalyticsResult SendData(string eventString, IAnalyticsData data);
    }

    [InitializeOnLoad]
    internal class Analytics : IAnalytics
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "Unity.RenderPipelines.Lightweight.Editor";
        const int k_Version = 1;
        static Analytics m_Instance = new Analytics();
        public static Analytics instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new Analytics();

                return m_Instance;
            }
        }

        static Analytics()
        {
            Debug.Log("Registered Event");
            EditorAnalytics.RegisterEventWithLimit(AnalyticsDataTypes.k_LightDataString, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, k_Version);
            EditorAnalytics.RegisterEventWithLimit(AnalyticsDataTypes.k_2DRendererDataString, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, k_Version);
        }

        public AnalyticsResult SendData(string eventString, IAnalyticsData data)
        {
            Debug.Log("Sent Data " + JsonUtility.ToJson(data, true));
            return EditorAnalytics.SendEventWithLimit(eventString, data, k_Version);
        }
    }
}
