using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering
{
    partial class AdaptiveProbeVolumes
    {
        // This class is used to (1) access the internal class UnityEditor.LightBaking.BakePipelineDriver and (2) provide a slightly higher level API.
        sealed class BakePipelineDriver : IDisposable
        {
            readonly object m_BakePipelineDriver;
            readonly Type m_BakePipelineDriverType;
            readonly Type m_StageNameType;

            internal BakePipelineDriver()
            {
                Debug.Assert(UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread());

                m_BakePipelineDriverType = Type.GetType("UnityEditor.LightBaking.BakePipelineDriver, UnityEditor");
                Debug.Assert(m_BakePipelineDriverType != null, "Unexpected, could not find the type UnityEditor.LightBaking.BakePipelineDriver");
                m_StageNameType = m_BakePipelineDriverType.GetNestedType("StageName", BindingFlags.NonPublic | BindingFlags.Public);
                Debug.Assert(m_StageNameType is { IsEnum: true }, "Unexpected, could not find the nested enum StageName on BakePipelineDriver");
                Debug.Assert(IsStageNameEnumConsistent(m_StageNameType), "Unexpected, StageName enum is not consistent with BakePipelineDriver.StageName enum");
                m_BakePipelineDriver = Activator.CreateInstance(m_BakePipelineDriverType, nonPublic: true);
                Debug.Assert(m_BakePipelineDriver != null, "Unexpected, could not new up a BakePipelineDriver");
            }

            internal void StartBake(bool enablePatching, ref float progress, ref StageName stage)
            {
                SetEnableBakedLightmaps(false); // Additional only
                SetEnablePatching(enablePatching);
                Update(false, true, true, out progress, out stage);
            }

            internal bool RunInProgress()
            {
                if (!InvokeMethod(new object[] { }, out object result))
                    return false;

                return result is true;
            }

            internal void Step(ref float progress, ref StageName stage) =>
                Update(true, true, true, out progress, out stage);

            void SetEnableBakedLightmaps(bool enable) =>
                InvokeMethod(new object[] { enable }, out _);

            void SetEnablePatching(bool enable) =>
                InvokeMethod(new object[] { enable }, out _);

            void Update(bool isOnDemandBakeInProgress, bool isOnDemandBakeAsync, bool shouldBeRunning,
                out float progress, out StageName stage)
            {
                progress = -1.0f;
                stage = StageName.Invalid;
                object[] parameters = { isOnDemandBakeInProgress, isOnDemandBakeAsync, shouldBeRunning, progress,
                    Enum.ToObject(m_StageNameType, (int)stage) };
                InvokeMethod(parameters, out _);
                progress = (float)parameters[3];
                stage = (StageName)Convert.ToInt32(parameters[4]);
            }

            public void Dispose() =>
                InvokeMethod(new object[] { }, out _);

            bool InvokeMethod(object[] parameters, out object result, [CallerMemberName] string methodName = "")
            {
                MethodInfo methodInfo = m_BakePipelineDriverType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                bool gotMethod = methodInfo != null;
                Debug.Assert(gotMethod, $"Unexpected, could not find {methodName} on BakePipelineDriver");

                result = methodInfo.Invoke(m_BakePipelineDriver, parameters);

                return true;
            }

            // Keep this in sync with the enum in Editor\Src\GI\BakePipeline\BakePipeline.bindings.h
            public enum StageName
            {
                Invalid = -1,
                Initialized = 0,
                Preprocess = 1,
                PreprocessProbes = 2,
                Bake = 3,
                PostProcess = 4,
                AdditionalBake = 5,
                Done = 6
            }

            // If StageName is not kept in sync, this should return false
            static bool IsStageNameEnumConsistent(Type otherType)
            {
                string[] ourNames = Enum.GetNames(typeof(StageName));
                string[] otherNames = Enum.GetNames(otherType);

                if (ourNames.Length != otherNames.Length)
                    return false;

                Array ourValues = Enum.GetValues(typeof(StageName));
                Array otherValues = Enum.GetValues(otherType);

                // Brute-force compare each local name against the external by lookup
                for (int i = 0; i < ourNames.Length; i++)
                {
                    string name = ourNames[i];

                    int otherIndex = Array.IndexOf(otherNames, name);
                    if (otherIndex < 0)
                        return false;

                    int ourVal = Convert.ToInt32(ourValues.GetValue(i));
                    int otherVal = Convert.ToInt32(otherValues.GetValue(otherIndex));

                    if (ourVal != otherVal)
                        return false;
                }

                return true;
            }
        }
    }
}
