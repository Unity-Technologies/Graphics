using NUnit.Framework;
using UnityEditor.Experimental.Rendering.TestFramework;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public partial class HDAdditionalReflectionDataTests
    {
        public class MigrateReflectionProbeFromVersion_ModeAndTextures
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
                public Vector3 capturePositionWS;
                public Quaternion captureRotationWS;
                public Vector3 influenceOffset;
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
                    capturePositionWS = new Vector3(3, 5.24f, 64.2f),
                    captureRotationWS = Quaternion.Euler(62.34f, 185.53f, 323.563f),
                    influenceOffset = new Vector3(3, -3.23f, 7.34f)
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
                    capturePositionWS = new Vector3(3, 5.24f, 64.2f),
                    captureRotationWS = Quaternion.Euler(135.34f, 24.683f, 176.323f),
                    influenceOffset = new Vector3(3, -3.23f, 7.34f)
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
                    capturePositionWS = new Vector3(3, 5.24f, 64.2f),
                    captureRotationWS = Quaternion.Euler(341.35f, 165.2f, 12.25f),
                    influenceOffset = new Vector3(3, -3.23f, 7.34f)
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
                        legacyProbeData.capturePositionWS,
                        legacyProbeData.captureRotationWS,
                        Vector3.one
                    );
                    var influencePositionWS = mat.MultiplyPoint(legacyProbeData.influenceOffset);
                    var influenceRotationWS = mat.rotation;

                    // No custom proxy here, so proxyToWorld = influenceToWorld
                    var proxyToWorld = Matrix4x4.TRS(influencePositionWS, influenceRotationWS, Vector3.one);
                    var capturePositionPS = (Vector3)proxyToWorld.inverse.MultiplyPoint(legacyProbeData.capturePositionWS);

                    var instance = Object.Instantiate(prefab);
                    m_ToClean = instance;

                    var probe = instance.GetComponent<HDAdditionalReflectionData>()
                        ?? instance.AddComponent<HDAdditionalReflectionData>();
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
                    Assert.IsTrue((capturePositionPS - settings.proxySettings.capturePositionProxySpace).sqrMagnitude < 0.001f);
                    Assert.AreEqual(ProbeSettings.ProbeType.ReflectionProbe, settings.type);
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
--- !u!1 &3102262843427888416
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 3102262843427888420}}
  - component: {{fileID: 3102262843427888421}}
  - component: {{fileID: 3102262843427888418}}
  - component: {{fileID: 3102262843427888419}}
  m_Layer: 5
  m_Name: Reflection Probe
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &3102262843427888420
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3102262843427888416}}
  m_LocalRotation: {legacyProbeData.captureRotationWS.ToYAML()}
  m_LocalPosition: {legacyProbeData.capturePositionWS.ToYAML()}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children: []
  m_Father: {{fileID: 0}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 30.95}}
--- !u!215 &3102262843427888421
ReflectionProbe:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3102262843427888416}}
  m_Enabled: 1
  serializedVersion: 2
  m_Type: 0
  m_Mode: 1
  m_RefreshMode: 0
  m_TimeSlicingMode: 0
  m_Resolution: 128
  m_UpdateFrequency: 0
  m_BoxSize: {{x: 6, y: 6, z: 6}}
  m_BoxOffset: {{x: 0.32623026, y: 1.5948586, z: 1.3}}
  m_NearClip: 2.76
  m_FarClip: 5
  m_ShadowDistance: 100
  m_ClearFlags: 2
  m_BackGroundColor: {{r: 0.1882353, g: 0.023529412, b: 0.13529739, a: 0}}
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 310
  m_IntensityMultiplier: 1
  m_BlendDistance: 0
  m_HDR: 1
  m_BoxProjection: 0
  m_RenderDynamicObjects: 0
  m_UseOcclusionCulling: 1
  m_Importance: 1
  m_CustomBakedTexture: {{fileID: 8900000, guid: b7a0288be1440c140849eb49d3b12573,
    type: 3}}
--- !u!114 &3102262843427888418
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3102262843427888416}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: d0ef8dc2c2eabfa4e8cb77be57a837c0, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_ProxyVolume: {{fileID: 0}}
  m_InfiniteProjection: 1
  m_InfluenceVolume:
    m_Shape: 1
    m_Offset: {legacyProbeData.influenceOffset.ToYAML()}
    m_BoxSize: {{x: 7, y: 8, z: 9}}
    m_BoxBlendDistancePositive: {{x: 1, y: 2, z: 3}}
    m_BoxBlendDistanceNegative: {{x: 1.5, y: 2.5, z: 3.5}}
    m_BoxBlendNormalDistancePositive: {{x: 0.5, y: 0.4, z: 0.3}}
    m_BoxBlendNormalDistanceNegative: {{x: 0.2, y: 0.1, z: 0.6}}
    m_BoxSideFadePositive: {{x: 0.1, y: 0.2, z: 0.3}}
    m_BoxSideFadeNegative: {{x: 0.15, y: 0.25, z: 0.35}}
    m_EditorAdvancedModeBlendDistancePositive: {{x: 1, y: 2, z: 3}}
    m_EditorAdvancedModeBlendDistanceNegative: {{x: 1.5, y: 2.5, z: 3.5}}
    m_EditorSimplifiedModeBlendDistance: 3.5
    m_EditorAdvancedModeBlendNormalDistancePositive: {{x: 0.5, y: 0.4, z: 0.3}}
    m_EditorAdvancedModeBlendNormalDistanceNegative: {{x: 0.2, y: 0.1, z: 0.6}}
    m_EditorSimplifiedModeBlendNormalDistance: 4.5
    m_EditorAdvancedModeEnabled: 1
    m_EditorAdvancedModeFaceFadePositive: {{x: 0.1, y: 0.2, z: 0.3}}
    m_EditorAdvancedModeFaceFadeNegative: {{x: 0.15, y: 0.25, z: 0.35}}
    m_SphereRadius: 6
    m_SphereBlendDistance: 2
    m_SphereBlendNormalDistance: 1
    m_Version: 1
    m_ObsoleteSphereBaseOffset: {{x: 0, y: 0, z: 0}}
  m_FrameSettings:
    overrides: 121169911
    enableShadow: 1
    enableContactShadows: 0
    enableShadowMask: 1
    enableSSR: 0
    enableSSAO: 1
    enableSubsurfaceScattering: 1
    enableTransmission: 0
    enableAtmosphericScattering: 0
    enableVolumetrics: 0
    enableReprojectionForVolumetrics: 1
    enableLightLayers: 1
    diffuseGlobalDimmer: 1
    specularGlobalDimmer: 1
    shaderLitMode: 0
    enableDepthPrepassWithDeferredRendering: 0
    enableTransparentPrepass: 1
    enableMotionVectors: 1
    enableObjectMotionVectors: 1
    enableDecals: 1
    enableRoughRefraction: 1
    enableTransparentPostpass: 1
    enableDistortion: 0
    enablePostprocess: 0
    enableAsyncCompute: 1
    enableOpaqueObjects: 0
    enableTransparentObjects: 0
    enableRealtimePlanarReflection: 1
    enableMSAA: 0
    lightLoopSettings:
      overrides: 31
      enableTileAndCluster: 1
      enableComputeLightEvaluation: 1
      enableComputeLightVariants: 0
      enableComputeMaterialVariants: 1
      enableFptlForForwardOpaque: 1
      enableBigTilePrepass: 0
      isFptlEnabled: 1
  m_CaptureSettings:
    overrides: 0
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
  m_Multiplier: 20
  m_Weight: 0.66
  m_Mode: 1
  m_RefreshMode: 1
  m_CustomTexture: {{fileID: 0}}
  m_BakedTexture: {{fileID: 0}}
  m_RenderDynamicObjects: 0
  lightLayers: 9
  m_ReflectionProbeVersion: 6
  m_ObsoleteInfluenceShape: 0
  m_ObsoleteInfluenceSphereRadius: 3
  m_ObsoleteBlendDistancePositive: {{x: 0, y: 0, z: 0}}
  m_ObsoleteBlendDistanceNegative: {{x: 0, y: 0, z: 0}}
  m_ObsoleteBlendNormalDistancePositive: {{x: 0, y: 0, z: 0}}
  m_ObsoleteBlendNormalDistanceNegative: {{x: 0, y: 0, z: 0}}
  m_ObsoleteBoxSideFadePositive: {{x: 1, y: 1, z: 1}}
  m_ObsoleteBoxSideFadeNegative: {{x: 1, y: 1, z: 1}}
--- !u!114 &3102262843427888419
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3102262843427888416}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 172515602e62fb746b5d573b38a5fe58, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  isGlobal: 1
  priority: 0
  blendDistance: 0
  weight: 1
  sharedProfile: {{fileID: 11400000, guid: cc8be05cdf24e1748a0d99d50a681853, type: 2}}";
        }

        public class MigrateFromLegacyProbe
        {
            public class LegacyProbeData
            {
                public Vector3 boxOffset;
                public Vector3 capturePositionWS;
                public Quaternion captureRotationWS;
                public Vector3 boxSize;
                public float blendDistance;
                public float importance;
                public float intensity;
                public bool boxProjection;
                public int cullingMask;
                public bool useOcclusionCulling;
                public float nearClipPlane;
                public float farClipPlane;
                public int resolution;
                public int mode;
                public int refreshMode;
            }

            static object[] s_LegacyProbeDatas =
            {
                new LegacyProbeData
                {
                    blendDistance = 1.2f,
                    boxOffset = new Vector3(2, 3, 4),
                    boxProjection = true,
                    boxSize = new Vector3(1, 2, 3),
                    capturePositionWS = new Vector3(2, 3.5f, 6),
                    captureRotationWS = Quaternion.Euler(341.35f, 165.2f, 12.25f),
                    cullingMask = 308,
                    farClipPlane = 850,
                    nearClipPlane = 1.5f,
                    importance = 12,
                    intensity = 1.4f,
                    mode = (int)ReflectionProbeMode.Realtime,
                    refreshMode = (int)ReflectionProbeRefreshMode.EveryFrame,
                    resolution = 256,
                    useOcclusionCulling = false
                },
                new LegacyProbeData
                {
                    blendDistance = 1.2f,
                    boxOffset = new Vector3(8, 3, 2),
                    boxProjection = true,
                    boxSize = new Vector3(4, 1, 8),
                    capturePositionWS = new Vector3(4, 6, 3),
                    captureRotationWS = Quaternion.Euler(341.35f, 165.2f, 12.25f),
                    cullingMask = 308,
                    farClipPlane = 850,
                    nearClipPlane = 1.5f,
                    importance = 12,
                    intensity = 1.4f,
                    mode = (int)ReflectionProbeMode.Realtime,
                    refreshMode = (int)ReflectionProbeRefreshMode.OnAwake,
                    resolution = 256,
                    useOcclusionCulling = false
                },
                new LegacyProbeData
                {
                    blendDistance = 1.5f,
                    boxOffset = new Vector3(2, 6, -1),
                    boxProjection = true,
                    boxSize = new Vector3(3.5f, 7, 2),
                    capturePositionWS = new Vector3(1.2f, 4, 5.12f),
                    captureRotationWS = Quaternion.Euler(341.35f, 165.2f, 12.25f),
                    cullingMask = 308,
                    farClipPlane = 850,
                    nearClipPlane = 1.35f,
                    importance = 11,
                    intensity = 1.4f,
                    mode = (int)ReflectionProbeMode.Baked,
                    refreshMode = (int)ReflectionProbeRefreshMode.EveryFrame,
                    resolution = 256,
                    useOcclusionCulling = true
                },
                new LegacyProbeData
                {
                    blendDistance = 1.5f,
                    boxOffset = new Vector3(3, 0, -10),
                    boxProjection = true,
                    boxSize = new Vector3(3.5f, 7, 2),
                    capturePositionWS = new Vector3(1.2f, 4, 5.12f),
                    captureRotationWS = Quaternion.Euler(341.35f, 165.2f, 12.25f),
                    cullingMask = 308,
                    farClipPlane = 870,
                    nearClipPlane = 5.5f,
                    importance = 13,
                    intensity = 1.4f,
                    mode = (int)ReflectionProbeMode.Custom,
                    refreshMode = (int)ReflectionProbeRefreshMode.EveryFrame,
                    resolution = 128,
                    useOcclusionCulling = true
                }
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
                    var influencePositionWS = legacyProbeData.capturePositionWS + legacyProbeData.boxOffset;
                    var proxyToWorld = Matrix4x4.TRS(influencePositionWS, Quaternion.identity, Vector3.one);
                    var capturePositionPS = (Vector3)proxyToWorld.inverse.MultiplyPoint(legacyProbeData.capturePositionWS);

                    var instance = Object.Instantiate(prefab);
                    m_ToClean = instance;

                    var probe = instance.GetComponent<HDAdditionalReflectionData>()
                        ?? instance.AddComponent<HDAdditionalReflectionData>();
                    prefab.SetActive(true);
                    probe.enabled = true;

                    var settings = probe.settings;
                    Assert.AreEqual(influencePositionWS, probe.transform.position);
                    Assert.AreEqual(capturePositionPS, settings.proxySettings.capturePositionProxySpace);
                    Assert.AreEqual(Vector3.one * legacyProbeData.blendDistance, settings.influence.boxBlendDistancePositive);
                    Assert.AreEqual(Vector3.one * legacyProbeData.blendDistance, settings.influence.boxBlendDistanceNegative);
                    Assert.AreEqual(legacyProbeData.importance, settings.lighting.weight);
                    Assert.AreEqual(legacyProbeData.intensity, settings.lighting.multiplier);
                    Assert.AreEqual(legacyProbeData.boxSize, settings.influence.boxSize);
                    Assert.AreEqual(legacyProbeData.boxProjection, settings.proxySettings.useInfluenceVolumeAsProxyVolume);
                    Assert.AreEqual(legacyProbeData.useOcclusionCulling, settings.camera.culling.useOcclusionCulling);
                    Assert.AreEqual(legacyProbeData.nearClipPlane, settings.camera.frustum.nearClipPlane);
                    Assert.AreEqual(legacyProbeData.farClipPlane, settings.camera.frustum.farClipPlane);
                    Assert.AreEqual(ProbeSettings.ProbeType.ReflectionProbe, settings.type);

                    var targetMode = ProbeSettings.Mode.Baked;
                    switch ((ReflectionProbeMode)legacyProbeData.mode)
                    {
                        case ReflectionProbeMode.Baked: targetMode = ProbeSettings.Mode.Baked; break;
                        case ReflectionProbeMode.Custom: targetMode = ProbeSettings.Mode.Custom; break;
                        case ReflectionProbeMode.Realtime: targetMode = ProbeSettings.Mode.Realtime; break;
                    }
                    Assert.AreEqual(targetMode, settings.mode);

                    var targetRealtimeMode = ProbeSettings.RealtimeMode.EveryFrame;
                    switch ((ReflectionProbeRefreshMode)legacyProbeData.refreshMode)
                    {
                        case ReflectionProbeRefreshMode.EveryFrame:
                        case ReflectionProbeRefreshMode.ViaScripting: targetRealtimeMode = ProbeSettings.RealtimeMode.EveryFrame; break;
                        case ReflectionProbeRefreshMode.OnAwake: targetRealtimeMode = ProbeSettings.RealtimeMode.OnEnable; break;
                    }
                    Assert.AreEqual(targetRealtimeMode, settings.realtimeMode);
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
--- !u!1 &4579176910221717176
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 6741578724909752953}}
  - component: {{fileID: 1787267906489536894}}
  m_Layer: 0
  m_Name: Reflection Probe
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &6741578724909752953
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4579176910221717176}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {legacyProbeData.capturePositionWS.ToYAML()}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children: []
  m_Father: {{fileID: 0}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!215 &1787267906489536894
ReflectionProbe:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 4579176910221717176}}
  m_Enabled: 1
  serializedVersion: 2
  m_Type: 0
  m_Mode: {legacyProbeData.mode}
  m_RefreshMode: {legacyProbeData.refreshMode}
  m_TimeSlicingMode: 0
  m_Resolution: {legacyProbeData.resolution}
  m_UpdateFrequency: 0
  m_BoxSize: {legacyProbeData.boxSize.ToYAML()}
  m_BoxOffset: {legacyProbeData.boxOffset.ToYAML()}
  m_NearClip: {legacyProbeData.nearClipPlane}
  m_FarClip: {legacyProbeData.farClipPlane}
  m_ShadowDistance: 100
  m_ClearFlags: 1
  m_BackGroundColor: {{r: 0.20, g: 0.30, b: 0.50, a: 0}}
  m_CullingMask:
    serializedVersion: 2
    m_Bits: {legacyProbeData.cullingMask}
  m_IntensityMultiplier: {legacyProbeData.intensity}
  m_BlendDistance: {legacyProbeData.blendDistance}
  m_HDR: 1
  m_BoxProjection: {(legacyProbeData.boxProjection ? 1 : 0)}
  m_RenderDynamicObjects: 0
  m_UseOcclusionCulling: {(legacyProbeData.useOcclusionCulling ? 1 : 0)}
  m_Importance: {legacyProbeData.importance}
  m_CustomBakedTexture: {{fileID: 0}}
";
        }
    }
}
