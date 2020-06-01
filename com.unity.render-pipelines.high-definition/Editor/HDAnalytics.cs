using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDAnalytics
    {
        const int k_MaxEventsPerHour = 1;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.hdrp";
        const string k_EventName = "HDRPBuildSettings";

        struct AnalyticsData
        {
            public string[] changed_settings;

            public AnalyticsData(Dictionary<string, string> diff)
            {
                changed_settings = new string[diff.Count];

                int i = 0;
                foreach (var d in diff)
                    changed_settings[i++] = $@"{{""name"":""{d.Key}"",""value"":""{d.Value}""}}";
            }
            
        }

        [MenuItem("Test/Send")]
        public static void SendEvent()
        {
            if (!EditorAnalytics.enabled)
                return;

            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset == null)
                return;

            if (EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey) != AnalyticsResult.Ok)
            {
                Debug.Log("Event not registered");
                return;
            }

            RenderPipelineSettings settings = hdrpAsset.currentPlatformRenderPipelineSettings;
            RenderPipelineSettings defaults = RenderPipelineSettings.NewDefault();

            var data = new AnalyticsData(DiffSettings(settings, defaults));

            Debug.Log(string.Join(",\n", data.changed_settings));

            if (EditorAnalytics.SendEventWithLimit(k_EventName, data) != AnalyticsResult.Ok)
            {
                Debug.Log("Event not sent");
                return;
            }
        }

        static Dictionary<string, string> DiffSettings(object a, object b)
        {
            var diff = new Dictionary<string, string>();
            var fields = a.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                var va = field.GetValue(a);
                var vb = field.GetValue(b);

                var t = field.FieldType;
                if (t == typeof(string))
                    continue;
                if (t.IsPrimitive || t.IsEnum)
                {
                    if (!va.Equals(vb))
                        diff[field.Name] = va.ToString();
                }
                else if (t.IsArray)
                {
                    if (DiffArray(va, vb))
                        diff[field.Name] = ArrayToJson(va);
                }
                else
                {
                    if (t == typeof(IntScalableSetting) || t == typeof(FloatScalableSetting))
                    {
                        var values = t.BaseType.GetField("m_Values", BindingFlags.NonPublic | BindingFlags.Instance);
                        va = values.GetValue(va);
                        vb = values.GetValue(vb);
                        if (DiffArray(va, vb))
                            diff[field.Name] = ArrayToJson(va);
                    }
                    else if (t.IsClass || t.IsValueType)
                    {
                        var subdiff = DiffSettings(va, vb);
                        foreach (var d in subdiff)
                        {
                            diff[field.Name + "." + d.Key] = d.Value;
                        }
                    }
                }
            }

            return diff;
        }

        static bool DiffArray(object a, object b)
        {
            var va = (System.Collections.IList)a;
            var vb = (System.Collections.IList)b;

            if (va.Count != vb.Count)
                return true;
            for (int i = 0; i < va.Count; i++)
            {
                if (!va[i].Equals(vb[i]))
                    return true;
            }
            return false;
        }

        static string ArrayToJson(object array)
        {
            var a = (System.Collections.IList)array;
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < a.Count; i++)
            {
                sb.Append(a[i].ToString());
                sb.Append(i == a.Count - 1 ? "]" : ",");
            }
            return sb.ToString();
        }
    }
}
