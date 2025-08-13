using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor.LightBaking;

namespace UnityEngine.Rendering
{
    partial class AdaptiveProbeVolumes
    {
        // This class is used to (1) access the internal class UnityEditor.LightBaking.BakePipelineDriver and (2) provide a slightly higher level API.
        private sealed class BakePipelineDriver : IDisposable
        {
            // Keep in sync with the enum BakePipeline::Run::StageName
            public enum StageName : int
            {
                Initialized,
                Preprocess,
                Bake,
                PostProcess,
                AdditionalBake,
                Done
            }

            private readonly object _bakePipelineDriver;
            private readonly Type _bakePipelineDriverType;

            internal BakePipelineDriver()
            {
                _bakePipelineDriverType = Type.GetType("UnityEditor.LightBaking.BakePipelineDriver, UnityEditor");
                bool newed = _bakePipelineDriverType != null;
                Debug.Assert(newed, "Unexpected, could not find the type UnityEditor.LightBaking.BakePipelineDriver");
                _bakePipelineDriver = newed ? Activator.CreateInstance(_bakePipelineDriverType) : null;
                Debug.Assert(_bakePipelineDriver != null, "Unexpected, could not new up a BakePipelineDriver");
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

            private void SetEnableBakedLightmaps(bool enable) =>
                InvokeMethod(new object[] { enable }, out _);

            private void SetEnablePatching(bool enable) =>
                InvokeMethod(new object[] { enable }, out _);

            private void Update(bool isOnDemandBakeInProgress, bool isOnDemandBakeAsync, bool shouldBeRunning,
                out float progress, out StageName stage)
            {
                object[] parameters = { isOnDemandBakeInProgress, isOnDemandBakeAsync, shouldBeRunning, -1.0f, -1 };
                InvokeMethod(parameters, out _);
                progress = (float)parameters[3];
                stage = (StageName)parameters[4];
            }

            public void Dispose() =>
                InvokeMethod(new object[] { }, out _);

            private bool InvokeMethod(object[] parameters, out object result, [CallerMemberName] string methodName = "")
            {
                MethodInfo methodInfo = _bakePipelineDriverType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                bool gotMethod = methodInfo != null;
                Debug.Assert(gotMethod, $"Unexpected, could not find {methodName} on BakePipelineDriver");
                if (!gotMethod)
                {
                    result = null;
                    return false;
                }

                result = methodInfo.Invoke(_bakePipelineDriver, parameters);

                return true;
            }
        }
    }
}
