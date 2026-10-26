using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

// UUM-143532: URP counterpart to the BiRP Incompatible check in Tests/EditModeAndPlayModeTests/2D/Sprite.
// A SRP-batchable sprite, once rendered in URP with the SRP batcher on, must report GetSRPBatchingState
// as Compatible. The material is a project asset under Resources (not a runtime Material) so its
// SRP-batcher shader variant is included in player builds instead of being stripped.
[TestFixture]
class GetSRPBatchingStateTests
{
    GameObject m_CameraObj;
    Camera m_Camera;
    UniversalAdditionalCameraData m_CameraData;
    GameObject m_SpriteObj;
    bool m_OriginalUseSRPBatching;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        m_CameraObj = new GameObject("Camera");
        m_CameraObj.transform.position = new Vector3(0.0f, 0.0f, -10.0f);
        m_CameraObj.transform.forward = Vector3.forward;
        m_Camera = m_CameraObj.AddComponent<Camera>();
        m_CameraData = m_CameraObj.AddComponent<UniversalAdditionalCameraData>();
        m_Camera.allowHDR = false;
        m_Camera.clearFlags = CameraClearFlags.SolidColor;
        m_CameraData.SetRenderer(2); // 2D Renderer. See the Renderers list in CommonAssets/UniversalRPAsset.
        m_CameraData.renderType = CameraRenderType.Base;
        m_CameraData.renderPostProcessing = false;
        m_Camera.targetTexture = new RenderTexture(m_Camera.pixelWidth, m_Camera.pixelHeight, 24);
    }

    [SetUp]
    public void Setup()
    {
        m_OriginalUseSRPBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
        GraphicsSettings.useScriptableRenderPipelineBatching = true;

        var sprite = Resources.Load<Sprite>("smallGreenStar");
        var material = Resources.Load<Material>("UUM143532_SpriteLit");
        Assert.NotNull(sprite, "smallGreenStar sprite should be in Resources.");
        Assert.NotNull(material, "UUM143532_SpriteLit material should be in Resources.");

        m_SpriteObj = new GameObject("Sprite", typeof(SpriteRenderer));
        m_SpriteObj.transform.position = Vector3.zero;
        var sr = m_SpriteObj.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = material;
    }

    [TearDown]
    public void TearDown()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = m_OriginalUseSRPBatching;
        Object.Destroy(m_SpriteObj);
    }

    [OneTimeTearDown]
    public void OneTimeCleanup()
    {
        Object.DestroyImmediate(m_CameraObj);
    }

    // Editor-only: on a standalone player the synthetic render context does not activate SRP batching for a
    // code-created renderer (real scenes do), so Compatible cannot be reproduced there. Player-side batching is
    // covered by the scene image tests (e.g. 081_2D_SRPBatcher_GPUSkinning). Mirrors UUM127937Test.
    [UnityTest]
    [UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor, RuntimePlatform.WindowsEditor)]
    public IEnumerator GetSRPBatchingState_IsCompatible_ForRenderedSpriteInURP()
    {
        var sr = m_SpriteObj.GetComponent<SpriteRenderer>();

        // GetSRPBatchingState does not force the (expensive) compatibility computation; a render does.
        m_Camera.Render();
        yield return null;

        // 2 == Compatible (tri-state: 0 Undetermined, 1 Incompatible, 2 Compatible).
        Assert.AreEqual(2, sr.GetSRPBatchingState(),
            "A URP SRP-batchable sprite should report Compatible SRP-batching state once rendered.");
    }
}
