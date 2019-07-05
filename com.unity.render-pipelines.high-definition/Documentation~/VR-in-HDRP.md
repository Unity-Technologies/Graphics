# Virtual Reality in the High Definition Render Pipeline

To use Virtual Reality (VR) in HDRP, you must enable VR in your Unity Project. For information on how to do this, see the [VR overview documentation](https://docs.unity3d.com/Manual/VROverview.html).

Some HDRP features are not compatible with VR. When you enable VR in your Project, HDRP automatically disables the features that it does not support . If you encounter an issue with a specific feature, you can disable it in your Project’s [HDRP Asset](HDRP-Asset.html).

## Supported features by Unity version

### Unity 2019.2

You must use Single Pass instancing for VR in HDRP.

### Unity 2019.1

You must use Single Pass Stereo rendering for VR in HDRP.

#### Not supported

- Multi-pass rendering
- Single-pass instancing
- Tile lighting
- Deferred rendering
- Volumetrics
- Render and viewport scale