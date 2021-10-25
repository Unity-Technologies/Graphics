using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDAnalytics : IPostprocessBuildWithReport
    {
        const int k_MaxEventsPerHour = 10;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.hdrp";
        const int k_UsageCurrentVersion = 2;
        const string k_UsageEventName = "uHDRPUsage";
        const string k_DefaultsEventName = "uHDRPDefaults";

        public int callbackOrder { get; }

        struct UsageEventData
        {
            // Naming convention for analytics data
            public string build_target;
            public string asset_guid;
            public string[] changed_settings;

            public UsageEventData(BuildTarget buildTarget, string assetGUID, Dictionary<string, string> diff)
            {
                build_target = $@"{buildTarget}";
                asset_guid = assetGUID;
                changed_settings = new string[diff.Count];

                int i = 0;
                foreach (var d in diff)
                    changed_settings[i++] = $@"{{""{d.Key}"":""{d.Value}""}}";
            }
        }

        struct DefaultsEventData
        {
            // Naming convention for analytics data
            public string[] default_settings;

            public DefaultsEventData(Dictionary<string, string> defaults)
            {
                default_settings = new string[defaults.Count];

                int i = 0;
                foreach (var d in defaults)
                    default_settings[i++] = $@"{{""{d.Key}"":""{d.Value}""}}";
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            SendEvent();
        }

        public static void SendEvent()
        {
            if (!EditorAnalytics.enabled)
                return;

            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            var qualityLevelCount = QualitySettings.names.Length;
            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                var hdrpAsset = QualitySettings.GetRenderPipelineAssetAt(i) as HDRenderPipelineAsset;
                if (hdrpAsset != null)
                {
                    if (EditorAnalytics.RegisterEventWithLimit(k_UsageEventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, k_UsageCurrentVersion) != AnalyticsResult.Ok)
                        continue;

                    RenderPipelineSettings settings = hdrpAsset.currentPlatformRenderPipelineSettings;
                    RenderPipelineSettings defaults = RenderPipelineSettings.NewDefault();

                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(hdrpAsset.GetInstanceID()));
                    var data = new UsageEventData(activeBuildTarget, guid, DiffSettings(settings, defaults));

                    EditorAnalytics.SendEventWithLimit(k_UsageEventName, data, k_UsageCurrentVersion);
                }
            }
        }

        public static void SendDefaultValuesEvent()
        {
            if (!EditorAnalytics.enabled)
                return;

            if (EditorAnalytics.RegisterEventWithLimit(k_DefaultsEventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey) != AnalyticsResult.Ok)
                return;

            RenderPipelineSettings defaults = RenderPipelineSettings.NewDefault();

            var data = new DefaultsEventData(AllSettings(defaults));
            EditorAnalytics.SendEventWithLimit(k_DefaultsEventName, data);
        }

        // Helpers to get changed settings as JSON
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

        static Dictionary<string, string> AllSettings(object a)
        {
            var allValues = new Dictionary<string, string>();
            var fields = a.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                var va = field.GetValue(a);

                var t = field.FieldType;
                if (t == typeof(string))
                    continue;
                if (t.IsPrimitive || t.IsEnum)
                {
                    allValues[field.Name] = va.ToString();
                }
                else if (t.IsArray)
                {
                    allValues[field.Name] = ArrayToJson(va);
                }
                else
                {
                    if (t == typeof(IntScalableSetting) || t == typeof(FloatScalableSetting))
                    {
                        var values = t.BaseType.GetField("m_Values", BindingFlags.NonPublic | BindingFlags.Instance);
                        va = values.GetValue(va);
                        allValues[field.Name] = ArrayToJson(va);
                    }
                    else if (t.IsClass || t.IsValueType)
                    {
                        var subdiff = AllSettings(va);
                        foreach (var d in subdiff)
                        {
                            allValues[field.Name + "." + d.Key] = d.Value;
                        }
                    }
                }
            }

            return allValues;
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

        // Commented out on purpose.
        // We only need to send this event manually when we add new members or change values of the HDRP asset.
        //[MenuItem("Edit/Rendering/Generate HDRP default values analytics", priority = CoreUtils.Sections.section4)]
        //static void GenerateDefaultValues()
        //{
        //    SendDefaultValuesEvent();
        //}
    }
}
