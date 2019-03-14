using NUnit.Framework;
using UnityEditor.Experimental.Rendering.TestFramework;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    //duplicate formerly used enum RenderingPath here for migration test
    enum LegacyRenderingPath
    {
        UseGraphicsSettings,
        Custom,
        FullscreenPassthrough
    }

    public partial class PlanarReflectionProbeTests
    {
        public class MigratePlanarProbeFromVersion_ModeAndTextures
        {
            public class LegacyProbeData
            {
                public int clearColorMode;
                public Color backgroundColorHDR;
                public bool clearDepth;
                public int cullingMask;
                public bool useOcclusionCulling;
                public int volumeLayerMask;
                public int projection;
                public float nearClipPlane;
                public float farClipPlane;
                public float fieldOfview;
                public float orthographicSize;
                public int renderingPath;
                public float shadowDistance;
                public Vector3 mirrorPositionWS;
                public Quaternion mirrorRotationWS;
                public int captureSettingsOverride;
                public float influenceYOffset;
            }

            static object[] s_LegacyProbeDatas =
            {
                new LegacyProbeData
                {
                    clearColorMode = (int)HDAdditionalCameraData.ClearColorMode.Color,
                    backgroundColorHDR = new Color(1.5f, 0.56234f, 62.523f, 0.123f),
                    clearDepth = false,
                    cullingMask = 101,
                    useOcclusionCulling = false,
                    volumeLayerMask = 302,
                    projection = (int)CameraProjection.Perspective,
                    nearClipPlane = 1.34f,
                    farClipPlane = 734.0f,
                    fieldOfview = 86.75f,
                    orthographicSize = 4,
                    renderingPath = (int)LegacyRenderingPath.UseGraphicsSettings,
                    shadowDistance = 151,
                    mirrorPositionWS = new Vector3(3, 5.24f, 64.2f),
                    mirrorRotationWS = Quaternion.Euler(15.3f, 93.3f, 243.34f),
                    influenceYOffset = 0.34f
                },
                new LegacyProbeData
                {
                    clearColorMode = (int)HDAdditionalCameraData.ClearColorMode.Sky,
                    backgroundColorHDR = new Color(1.5f, 0.56234f, 62.523f, 0.123f),
                    clearDepth = true,
                    cullingMask = 101,
                    useOcclusionCulling = true,
                    volumeLayerMask = 302,
                    projection = (int)CameraProjection.Orthographic,
                    nearClipPlane = 1.34f,
                    farClipPlane = 734.0f,
                    fieldOfview = 86.75f,
                    orthographicSize = 4,
                    renderingPath = (int)LegacyRenderingPath.FullscreenPassthrough,
                    shadowDistance = 151,
                    mirrorPositionWS = new Vector3(3, 5.24f, 64.2f),
                    mirrorRotationWS = Quaternion.Euler(165.3f, 21.678f, 345.214f),
                    influenceYOffset = 15.2f
                },
                new LegacyProbeData
                {
                    clearColorMode = (int)HDAdditionalCameraData.ClearColorMode.None,
                    backgroundColorHDR = new Color(1.5f, 0.56234f, 62.523f, 0.123f),
                    clearDepth = true,
                    cullingMask = 101,
                    useOcclusionCulling = true,
                    volumeLayerMask = 302,
                    projection = (int)CameraProjection.Orthographic,
                    nearClipPlane = 1.34f,
                    farClipPlane = 734.0f,
                    fieldOfview = 86.75f,
                    orthographicSize = 4,
                    renderingPath = (int)LegacyRenderingPath.Custom,
                    shadowDistance = 151,
                    mirrorPositionWS = new Vector3(3, 5.24f, 64.2f),
                    mirrorRotationWS = Quaternion.Euler(84.134f, 352.4f, 167.36f),
                    influenceYOffset = 2.4f
                },
            };

            Object m_ToClean;

            [Test, TestCaseSource(nameof(s_LegacyProbeDatas))]
            public void Test(LegacyProbeData legacyProbeData)
            {
                using (new PrefabMigrationTests(
                    GetType().Name,
                    GeneratePrefabYAML(legacyProbeData),
                    out GameObject prefab
                ))
                {
                    var mat = Matrix4x4.TRS(
                        legacyProbeData.mirrorPositionWS,
                        legacyProbeData.mirrorRotationWS,
                        Vector3.one
                    );
                    var influencePositionWS = mat.MultiplyPoint(Vector3.up * legacyProbeData.influenceYOffset);
                    var influenceRotationWS = mat.rotation;

                    // No custom proxy here, so proxyToWorld = influenceToWorld
                    var proxyToWorld = Matrix4x4.TRS(influencePositionWS, influenceRotationWS, Vector3.one);
                    var mirrorPositionPS = (Vector3)proxyToWorld.inverse.MultiplyPoint(legacyProbeData.mirrorPositionWS);

                    var instance = Object.Instantiate(prefab);
                    m_ToClean = instance;

                    var probe = instance.GetComponent<PlanarReflectionProbe>();
                    prefab.SetActive(true);
                    probe.enabled = true;

                    var settings = probe.settings;
                    Assert.AreEqual((HDAdditionalCameraData.ClearColorMode)legacyProbeData.clearColorMode, settings.camera.bufferClearing.clearColorMode);
                    Assert.AreEqual(legacyProbeData.backgroundColorHDR, settings.camera.bufferClearing.backgroundColorHDR);
                    Assert.AreEqual(legacyProbeData.clearDepth, settings.camera.bufferClearing.clearDepth);
                    Assert.AreEqual(legacyProbeData.cullingMask, (int)settings.camera.culling.cullingMask);
                    Assert.AreEqual(legacyProbeData.useOcclusionCulling, settings.camera.culling.useOcclusionCulling);
                    Assert.AreEqual(legacyProbeData.volumeLayerMask, (int)settings.camera.volumes.layerMask);
                    Assert.AreEqual(legacyProbeData.nearClipPlane, settings.camera.frustum.nearClipPlane);
                    Assert.AreEqual(legacyProbeData.farClipPlane, settings.camera.frustum.farClipPlane);
                    Assert.AreEqual(legacyProbeData.fieldOfview, settings.camera.frustum.fieldOfView);
                    Assert.AreEqual(legacyProbeData.renderingPath == (int)LegacyRenderingPath.Custom, settings.camera.customRenderingSettings);
                    Assert.IsTrue((influencePositionWS - probe.transform.position).sqrMagnitude < 0.001f);
                    Assert.IsTrue((mirrorPositionPS - settings.proxySettings.mirrorPositionProxySpace).sqrMagnitude < 0.001f);
                    Assert.AreEqual(ProbeSettings.ProbeType.PlanarProbe, settings.type);
                }
            }

            [TearDown]
            public void TearDown()
            {
                if (m_ToClean != null)
                    CoreUtils.Destroy(m_ToClean);
            }

            string GeneratePrefabYAML(LegacyProbeData legacyProbeData)
                => $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &6171638715142251291
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 6171638715142251289}}
  - component: {{fileID: 6171638715142251288}}
  m_Layer: 0
  m_Name: Planar Reflection
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &6171638715142251289
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6171638715142251291}}
  m_LocalRotation: {legacyProbeData.mirrorRotationWS.ToYAML()}
  m_LocalPosition: {legacyProbeData.mirrorPositionWS.ToYAML()}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children: []
  m_Father: {{fileID: 0}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &6171638715142251288
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6171638715142251291}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: a4ee7c3a3b205a14a94094d01ff91d6b, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_ProxyVolume: {{fileID: 0}}
  m_InfiniteProjection: 1
  m_InfluenceVolume:
    m_Shape: 1
    m_Offset: {{x: 0, y: {legacyProbeData.influenceYOffset}, z: 0}}
    m_BoxSize: {{x: 7, y: 8, z: 9}}
    m_BoxBlendDistancePositive: {{x: 0.1, y: 0.2, z: 0.3}}
    m_BoxBlendDistanceNegative: {{x: 0.4, y: 0.5, z: 0.6}}
    m_BoxBlendNormalDistancePositive: {{x: 0, y: 0, z: 0}}
    m_BoxBlendNormalDistanceNegative: {{x: 0, y: 0, z: 0}}
    m_BoxSideFadePositive: {{x: 1, y: 1, z: 1}}
    m_BoxSideFadeNegative: {{x: 1, y: 1, z: 1}}
    m_EditorAdvancedModeBlendDistancePositive: {{x: 0.1, y: 0.2, z: 0.3}}
    m_EditorAdvancedModeBlendDistanceNegative: {{x: 0.4, y: 0.5, z: 0.6}}
    m_EditorSimplifiedModeBlendDistance: 0.9213414
    m_EditorAdvancedModeBlendNormalDistancePositive: {{x: 0, y: 0, z: 0}}
    m_EditorAdvancedModeBlendNormalDistanceNegative: {{x: 0, y: 0, z: 0}}
    m_EditorSimplifiedModeBlendNormalDistance: 0
    m_EditorAdvancedModeEnabled: 1
    m_EditorAdvancedModeFaceFadePositive: {{x: 1, y: 1, z: 1}}
    m_EditorAdvancedModeFaceFadeNegative: {{x: 1, y: 1, z: 1}}
    m_SphereRadius: 5
    m_SphereBlendDistance: 1
    m_SphereBlendNormalDistance: 0
    m_Version: 1
    m_ObsoleteSphereBaseOffset: {{x: 0, y: 0, z: 0}}
  m_FrameSettings:
    overrides: 125691895
    enableShadow: 0
    enableContactShadows: 0
    enableShadowMask: 0
    enableSSR: 0
    enableSSAO: 1
    enableSubsurfaceScattering: 1
    enableTransmission: 1
    enableAtmosphericScattering: 0
    enableVolumetrics: 1
    enableReprojectionForVolumetrics: 1
    enableLightLayers: 0
    diffuseGlobalDimmer: 1
    specularGlobalDimmer: 1
    shaderLitMode: 0
    enableDepthPrepassWithDeferredRendering: 0
    enableTransparentPrepass: 0
    enableMotionVectors: 0
    enableObjectMotionVectors: 1
    enableDecals: 1
    enableRoughRefraction: 0
    enableTransparentPostpass: 1
    enableDistortion: 0
    enablePostprocess: 1
    enableAsyncCompute: 1
    enableOpaqueObjects: 0
    enableTransparentObjects: 1
    enableRealtimePlanarReflection: 0
    enableMSAA: 0
    lightLoopSettings:
      overrides: 31
      enableTileAndCluster: 1
      enableComputeLightEvaluation: 1
      enableComputeLightVariants: 1
      enableComputeMaterialVariants: 1
      enableFptlForForwardOpaque: 1
      enableBigTilePrepass: 0
      isFptlEnabled: 1
  m_CaptureSettings:
    overrides: {legacyProbeData.captureSettingsOverride}
    clearColorMode: {legacyProbeData.clearColorMode}
    backgroundColorHDR: {legacyProbeData.backgroundColorHDR.ToYAML()}
    clearDepth: {(legacyProbeData.clearDepth ? 1 : 0)}
    cullingMask:
      serializedVersion: 2
      m_Bits: {legacyProbeData.cullingMask}
    useOcclusionCulling: {(legacyProbeData.useOcclusionCulling ? 1 : 0)}
    volumeLayerMask:
      serializedVersion: 2
      m_Bits: {legacyProbeData.volumeLayerMask}
    volumeAnchorOverride: {{fileID: 0}}
    projection: {legacyProbeData.projection}
    nearClipPlane: {legacyProbeData.nearClipPlane}
    farClipPlane: {legacyProbeData.farClipPlane}
    fieldOfView: {legacyProbeData.fieldOfview}
    orthographicSize: {legacyProbeData.orthographicSize}
    renderingPath: {legacyProbeData.renderingPath}
    shadowDistance: {legacyProbeData.shadowDistance}
  m_Multiplier: 98.21
  m_Weight: 0.839
  m_Mode: 1
  m_RefreshMode: 1
  m_CustomTexture: {{fileID: 0}}
  m_BakedTexture: {{fileID: 0}}
  m_RenderDynamicObjects: 0
  lightLayers: 9
  m_CaptureLocalPosition: {{x: 0, y: 0, z: 0}}
  m_CapturePositionMode: 1
  m_CaptureMirrorPlaneLocalPosition: {{x: 0, y: 0, z: 0}}
  m_CaptureMirrorPlaneLocalNormal: {{x: 0, y: 1, z: 0}}
  m_PlanarProbeVersion: 3
  m_ObsoleteOverrideFieldOfView: 0
  m_ObsoleteFieldOfViewOverride: 90
  m_ObsoleteCaptureNearPlane: 0.3
  m_ObsoleteCaptureFarPlane: 1000";
        }
    }
}
