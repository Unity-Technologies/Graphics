using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

namespace Unity.Rendering.Universal.Tests
{
    public class MockHmdSetupAttribute : GraphicsPrebuildSetupAttribute
    {
        public MockHmdSetupAttribute(int order = 0) : base(order) { }

        public override void Setup()
        {
#if UNITY_EDITOR && USE_XR_MOCK_HMD && !OCULUS_SDK
            // Configure the project for XR tests by adding the MockHMD plugin if required.
            Unity.Testing.XR.Editor.SetupMockHMD.SetupLoader();
#endif
        }
    }
}
