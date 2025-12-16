using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// This is a customer-reported render pass breaking DLSS & FSR2 output.
// The reason being the hdCamera used for the custom pass leading to
// global state in the DynamicResolutionHandler to be set by the custom pass
// and consumed by the upscaler passes right after, resulting in invalid
// output resolution leading to a black screen.
public class TestCustomRenderPassBreakingDLSSAndFSR2 : CustomPass
{
    private Camera _camera;
    [Header("View")] [SerializeField] private LayerMask _cullingMask;
    [SerializeField] private readonly CullMode _cullMode = CullMode.Front;

    private RenderTextureDescriptor _depthBufferDescriptor;
    [SerializeField] private bool _depthClip;

    private RTHandle _maskBuffer;

    [SerializeField] [Tooltip("Offset Geometry along normal")]
    private float _normalBias;

    [SerializeField] [Range(0, 1)] [Tooltip("Distance % from camera far plane.")]
    private readonly float _range = 0.5f;

    [Header("Rendering")] [SerializeField] private readonly TextureResolution _resolution = TextureResolution._256;
    [SerializeField] private Vector3 _rotation;

    [Header("Shadow Map")] [SerializeField]
    private readonly float _slopeBias = 2f;

    [SerializeField] private float _snapToGrid;
    [SerializeField] private float _varianceBias;

    protected override bool executeInSceneView => false;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        _depthBufferDescriptor = new RenderTextureDescriptor((int)_resolution, (int)_resolution, GraphicsFormat.None,
            GraphicsFormat.D32_SFloat)
        {
            autoGenerateMips = false,
            enableRandomWrite = false
        };

        _maskBuffer = RTHandles.Alloc((int)_resolution, (int)_resolution, colorFormat: GraphicsFormat.D32_SFloat,
            autoGenerateMips: false, isShadowMap: true);

        _camera = new GameObject { hideFlags = HideFlags.HideAndDontSave }.AddComponent<Camera>();
        _camera.cullingMask = _cullingMask;
        _camera.enabled = false;
        _camera.orthographic = true;
        _camera.targetTexture = _maskBuffer.rt;
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(_camera.gameObject);
        RTHandles.Release(_maskBuffer);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!UpdateCamera(ctx.hdCamera.camera)) return;
        if (!_camera.TryGetCullingParameters(out var cullingParameters)) return;

        cullingParameters.cullingOptions = CullingOptions.ShadowCasters;
        ctx.cullingResults = ctx.renderContext.Cull(ref cullingParameters);

        ctx.cmd.GetTemporaryRT(ShaderIDs._TemporaryDepthBuffer, _depthBufferDescriptor);
        CoreUtils.SetRenderTarget(ctx.cmd, ShaderIDs._TemporaryDepthBuffer, ClearFlag.Depth);
        ctx.cmd.SetGlobalDepthBias(1.0f, _slopeBias);
        CustomPassUtils.RenderDepthFromCamera(ctx, _camera, _camera.cullingMask,
            overrideRenderState: new RenderStateBlock(RenderStateMask.Depth | RenderStateMask.Raster)
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
                rasterState = new RasterState(_cullMode, 0, 0, _depthClip)
            });

        ctx.cmd.CopyTexture(ShaderIDs._TemporaryDepthBuffer, _maskBuffer);

        ctx.cmd.ReleaseTemporaryRT(ShaderIDs._TemporaryDepthBuffer);
    }

    private bool UpdateCamera(Camera camera)
    {
        if (camera.cameraType != CameraType.Game || !camera.CompareTag("MainCamera"))
            return false;

        float3 position = camera.transform.position;
        if (_snapToGrid > 0)
            position = math.round(position * _snapToGrid) / _snapToGrid;

        _camera.transform.position = position;
        _camera.orthographicSize = _range * camera.farClipPlane;
        _camera.nearClipPlane = -_range * camera.farClipPlane;
        _camera.farClipPlane = _range * camera.farClipPlane;
        _camera.transform.rotation =
            Quaternion.FromToRotation(Vector3.forward, Vector3.down) * Quaternion.Euler(_rotation);

        return true;
    }

    public static class ShaderIDs
    {
        public static readonly int _TemporaryDepthBuffer = Shader.PropertyToID("_TemporaryDepthBuffer");
    }

    private enum TextureResolution
    {
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }
}
