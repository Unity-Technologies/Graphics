# Virtual Reality in the High Definition Render Pipeline

To use Virtual Reality (VR) in HDRP, you must enable VR in your Unity Project. For information on how to do this, see the [VR overview documentation](https://docs.unity3d.com/Manual/VROverview.html).

Some HDRP features are not compatible with VR. When you enable VR in your Project, HDRP automatically disables the features that it does not support . If you encounter an issue with a specific feature, you can disable it in your Project’s [HDRP Asset](HDRP-Asset.html).

For rendering in VR, HDRP only supports [Forward](Forward-And-Deferred-Rendering.html) [Single Pass Stereo Rendering](https://docs.unity3d.com/Manual/SinglePassStereoRendering.html). HDRP also sets the rendering path to Forward in the HDRP Asset by default.

## Supported features by Unity version

### Unity 2019.2

You must use Single Pass Stereo rendering for VR in HDRP.

#### Not supported

- Multi-pass rendering
- Deferred rendering
- Volumetrics
- Depth-of-Field
- Render and viewport scale

### Unity 2019.1

You must use Single Pass Stereo rendering for VR in HDRP.

#### Not supported

- Multi-pass rendering
- Single-pass instancing
- Clustered lighting
- Deferred rendering
- Volumetrics
- Post-processing
- Render and viewport scale