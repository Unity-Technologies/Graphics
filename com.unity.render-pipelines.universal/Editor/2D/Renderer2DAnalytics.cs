using System;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal.Analytics
{
    struct AnalyticsDataTypes
    {
        public const string k_LightDataString = "u2drendererlights";
        public const string k_Renderer2DDataString = "u2drendererdata";
    }

    internal interface IAnalyticsData {};

    [Serializable]
    internal struct Light2DData : IAnalyticsData
    {
        [SerializeField]
        public bool was_create_event;
        [SerializeField]
        public int instance_id;
        [SerializeField]
        public Light2D.LightType light_type;
    };


    [Serializable]
    internal struct RendererAssetData : IAnalyticsData
    {
        [SerializeField]
        public bool was_create_event;
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
    internal class Renderer2DAnalytics : IAnalytics
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.renderpipelines.universal.editor";
        const int k_Version = 1;
        static Renderer2DAnalytics m_Instance = new Renderer2DAnalytics();
        static bool s_Initialize = false;
        public static Renderer2DAnalytics instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new Renderer2DAnalytics();

                return m_Instance;
            }
        }

        public AnalyticsResult SendData(string eventString, IAnalyticsData data)
        {
            //Debug.Log("Sent Data " + JsonUtility.ToJson(data));
            if (false == s_Initialize)
            {
                EditorAnalytics.RegisterEventWithLimit(AnalyticsDataTypes.k_LightDataString, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, k_Version);
                EditorAnalytics.RegisterEventWithLimit(AnalyticsDataTypes.k_Renderer2DDataString, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, k_Version);
                s_Initialize = true;
            }

            return EditorAnalytics.SendEventWithLimit(eventString, data, k_Version);
        }
    }
}
