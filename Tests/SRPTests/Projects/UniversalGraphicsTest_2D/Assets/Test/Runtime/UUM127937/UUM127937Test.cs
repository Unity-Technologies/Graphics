using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpriteRendererTests
{
    public class UUM127937Test
    {
        GameObject m_TestGo;
        SpriteRenderer m_SpriteRenderer;
        UUM127937 m_UUM127937;

        private GameObject m_BaseObj;
        private Camera m_BaseCamera;
        private UniversalAdditionalCameraData m_BaseCameraData;

        private bool m_OriginalUseSRPBatching = true;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_BaseObj = new GameObject();
            m_BaseObj.transform.position = new Vector3(0.0f, 0.0f, -10.0f);
            m_BaseObj.transform.forward = Vector3.forward;
            m_BaseCamera = m_BaseObj.AddComponent<Camera>();
            m_BaseCameraData = m_BaseObj.AddComponent<UniversalAdditionalCameraData>();

            m_BaseCamera.allowHDR = false;
            m_BaseCamera.clearFlags = CameraClearFlags.SolidColor;
            m_BaseCameraData.SetRenderer(2); // 2D Renderer. See the list of Renderers in CommonAssets/UniversalRPAsset.
            m_BaseCameraData.renderType = CameraRenderType.Base;
            m_BaseCameraData.renderPostProcessing = false;
            m_BaseCamera.targetTexture = new RenderTexture(m_BaseCamera.pixelWidth, m_BaseCamera.pixelHeight, 24);

#if UNITY_EDITOR
            EditorApplication.ExecuteMenuItem("Window/General/Game");
#endif
        }

        [SetUp]
        public void Setup()
        {
            m_TestGo = new GameObject("TestGo");
            m_SpriteRenderer = m_TestGo.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>("smallGreenStar");
            var material = Resources.Load<Material>("UUM127937SG");
            Assert.NotNull(sprite);
            Assert.NotNull(material);
            m_SpriteRenderer.sprite = sprite;
            m_SpriteRenderer.material = material;
            m_UUM127937 = m_TestGo.AddComponent<UUM127937>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(m_TestGo);
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            Object.DestroyImmediate(m_BaseObj);
        }

        [UnityTest]
        [UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor, RuntimePlatform.WindowsEditor)]
        public IEnumerator SpriteRenderer_ChangeSerializedProperty_Color_DoesNotClearMaterialPropertyBlock()
        {
            yield return null;
            yield return null;

            var startSaturation = m_UUM127937.GetSaturation();

            Assert.AreEqual(UUM127937.sDefault, startSaturation);

            yield return null;

#if UNITY_EDITOR
            SerializedObject so = new SerializedObject(m_SpriteRenderer);
            SerializedProperty sp = so.FindProperty("m_Color");
            Assert.NotNull(sp);

            so.Update();

            sp.colorValue = Color.white;
            sp.colorValue = Color.red;

            so.ApplyModifiedProperties();

            yield return null;

            Assert.AreEqual(Color.red, m_SpriteRenderer.color);
#endif

            yield return null;

            var endSaturation = m_UUM127937.GetSaturation();

            Assert.AreEqual(startSaturation, endSaturation);
        }

        [UnityTest]
        [UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor, RuntimePlatform.WindowsEditor)]
        public IEnumerator SpriteRenderer_ChangeSerializedProperty_Flip_DoesNotClearMaterialPropertyBlock()
        {
            yield return null;
            yield return null;

            var startSaturation = m_UUM127937.GetSaturation();

            Assert.AreEqual(UUM127937.sDefault, startSaturation);

            yield return null;

#if UNITY_EDITOR
            SerializedObject so = new SerializedObject(m_SpriteRenderer);
            SerializedProperty sp = so.FindProperty("m_FlipX");
            Assert.NotNull(sp);

            so.Update();

            sp.boolValue = !sp.boolValue;

            so.ApplyModifiedProperties();

            yield return null;

            Assert.AreEqual(true, m_SpriteRenderer.flipX);
#endif

            yield return null;

            var endSaturation = m_UUM127937.GetSaturation();

            Assert.AreEqual(startSaturation, endSaturation);
        }
    }
}
