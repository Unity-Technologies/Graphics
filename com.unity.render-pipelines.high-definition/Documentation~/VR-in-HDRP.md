# Virtual Reality in the High Definition Render Pipeline

To use Virtual Reality (VR) in HDRP, you must enable VR in your Unity Project. To do this, see the [VR tab](#Render-Pipeline-Wizard.html#VRTab) in the Render Pipeline Wizard.

Some HDRP features are not compatible with VR. When you enable VR in your Project, HDRP automatically disables the features that it does not support . If you encounter an issue with a specific feature, you can disable it in your Project’s [HDRP Asset](HDRP-Asset.html).

## Supported features by Unity version

### Unity 2019.3

You can use Multi-pass or Single-pass instancing for VR in HDRP.

#### Not supported

- Single-pass Stereo (double-wide)

### Unity 2019.2

You can use Multi-pass or Single Pass instancing for VR in HDRP.

#### Not supported

- Single-pass Stereo (double-wide)
- VFX Graph with Single-pass instancing

### Unity 2019.1

You must use Single-pass Stereo (double-wide) rendering for VR in HDRP.

#### Not supported

- Multi-pass rendering
- Single-pass instancing
- Tile lighting
- Deferred rendering
- Volumetrics
- Render and viewport scale
