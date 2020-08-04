using UnityEngine;

namespace com.unity.test.performance.runtimesettings
{
    public class CurrentSettings : ScriptableObject
    {
        public string PlayerGraphicsApi;
        public bool MtRendering;
        public bool GraphicsJobs;
        public bool EnableBurst;
        public string ColorSpace;
        public string Username;
        public string RenderPipeline;
        public int AntiAliasing;
        public string TestsRevision;
        public string TestsRevisionDate;
        public string TestsBranch;
        public string PackageUnderTestName;
        public string PackageUnderTestVersion;
        public string PackageUnderTestRevision;
        public string PackageUnderTestRevisionDate;
        public string PackageUnderTestPackageBranch;
        public string ScriptingBackend;
        public string JobLink;
        public int JobWorkerCount;
    }
}
