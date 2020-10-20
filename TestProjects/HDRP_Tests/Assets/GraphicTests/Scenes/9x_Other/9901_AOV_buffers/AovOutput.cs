using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(HDAdditionalCameraData))]
public class AovOutput : MonoBehaviour
{
    [SerializeField] RenderTexture _outputTexture = null;
    [SerializeField] RenderTexture _colorTexture = null;
    [SerializeField] RenderTexture _depthTexture = null;
    [SerializeField] RenderTexture _normalTexture = null;
    [SerializeField] RenderTexture _motionVectorsTexture = null;

    Material _material;
    MaterialPropertyBlock _props;

    (RTHandle output, RTHandle color, RTHandle depth, RTHandle normal, RTHandle motionvector) _rt;

    RTHandle RTAllocator(AOVBuffers bufferID)
    {
        if (bufferID == AOVBuffers.Output)
            return _rt.output ??
                (_rt.output = RTHandles.Alloc(
                    _outputTexture.width, _outputTexture.height, 1,
                    DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB));

        if (bufferID == AOVBuffers.Color)
            return _rt.color ??
                (_rt.color = RTHandles.Alloc(
                    _colorTexture.width, _colorTexture.height, 1,
                    DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB));

        if (bufferID == AOVBuffers.DepthStencil)
            return _rt.depth ??
                (_rt.depth = RTHandles.Alloc(
                    _depthTexture.width, _depthTexture.height, 1,
                    DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB));


        if (bufferID == AOVBuffers.Normals)
            return _rt.normal ??
                (_rt.normal = RTHandles.Alloc(
                    _normalTexture.width, _normalTexture.height, 1,
                    DepthBits.None, GraphicsFormat.R8G8B8A8_UNorm));

        return _rt.motionvector ??
            (_rt.motionvector = RTHandles.Alloc(
                _motionVectorsTexture.width, _motionVectorsTexture.height, 1,
                DepthBits.None, GraphicsFormat.R8G8B8A8_SRGB));
    }

    void AovCallback(
    CommandBuffer cmd,
    List<RTHandle> buffers,
    RenderOutputProperties outProps
    )
    {
        // Shader objects instantiation
        if (_material == null)
        {
            var shader = Shader.Find("Hidden/HdrpAovTest/AovShader");
            _material = new Material(shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_props == null) _props = new MaterialPropertyBlock();

        // AOV buffers
        _props.SetTexture("_ColorTexture", buffers[0]);

        // Full screen triangle
        CoreUtils.DrawFullScreen(
            cmd, _material, _outputTexture, _props
        );

        _props.SetTexture("_ColorTexture", buffers[1]);

        CoreUtils.DrawFullScreen(
            cmd, _material, _colorTexture, _props
        );

        _props.SetTexture("_ColorTexture", buffers[2]);

        CoreUtils.DrawFullScreen(
            cmd, _material, _depthTexture, _props
        );

        _props.SetTexture("_ColorTexture", buffers[3]);

        CoreUtils.DrawFullScreen(
            cmd, _material, _normalTexture, _props
        );

        _props.SetTexture("_ColorTexture", buffers[4]);

        CoreUtils.DrawFullScreen(
            cmd, _material, _motionVectorsTexture, _props
        );
    }

    AOVRequestDataCollection BuildAovRequest()
    {
        return new AOVRequestBuilder().Add(
            AOVRequest.NewDefault(),
            RTAllocator,
            null, // lightFilter
            new[] {
                    AOVBuffers.Output,
                    AOVBuffers.Color,
                    AOVBuffers.DepthStencil,
                    AOVBuffers.Normals,
                    AOVBuffers.MotionVectors
            },
            AovCallback
        ).Build();
    }

    void OnDisable()
    {
        RTHandles.Release(_rt.output);
        RTHandles.Release(_rt.color);
        RTHandles.Release(_rt.depth);
        RTHandles.Release(_rt.normal);
        RTHandles.Release(_rt.motionvector);
    }

    void Start()
    {
        GetComponent<HDAdditionalCameraData>().SetAOVRequests(BuildAovRequest());
    }
}
