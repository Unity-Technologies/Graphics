# Known issues

This page contains information on known issues you may encounter when using URP.

## Renaming a URP Renderer asset to a name matching one of the Renderer Feature names causes erroneous behavior

If a URP Renderer asset has any Renderer Features assigned, renaming the Renderer asset to a name matching one of the Renderer Feature names causes erroneous behavior: the URP Renderer and the Renderer Feature switch places.

The following scenario shows how the error occurs:

* Let's assume that the URP Renderer in your project is called `ForwardRenderer`.
* The Renderer has a Renderer Feature called `NewRenderObjects` assigned.

    ![ForwardRenderer with Renderer Feature assigned to it.](Images/known-issues/urp-10-renaming-renderer.png)

* Renaming `ForwardRenderer` to `NewRenderObjects` causes erroneous behavior:<br/>The Renderer switches places with the Renderer Feature and does not behave correctly.

To avoid the issue, do not give the URP Renderer asset the same name as the Renderer Feature asset.

To see updates on this issue, refer to the [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/parent-and-child-nested-scriptable-object-assets-switch-places-when-parent-scriptable-object-asset-is-renamed).

## Warning about \_AdditionalLights property when upgrading the URP package

In certain cases, you might see the following warning when upgrading the URP package to a newer version:

```
Property (_AdditionalLights<...>) exceeds previous array size (256 vs 16). Cap to previous size.
```

This warning does not cause issues with the project, the warning disappears if you restart the Editor.

## In XR projects, the SSAO effect does not match between eyes in stereo rendering modes

In XR projects, the SSAO effect does not match between eyes in stereo rendering modes.

To see updates on this issue, refer to the [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/urp-xr-ssao-renderer-mismatch-between-eyes-when-using-multiview-rendering-on-quest).

## On Oculus Quest OpenGL ES 3, URP does not render VFX Graph Particle Systems correctly

On Oculus Quest OpenGL ES 3, VFX Graph Particle effects might look stretched, or might be missing.

To see updates on this issue, refer to the [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/srp-urp-particles-missing-or-very-stretched-using-urp-10-dot-1-on-quest-gles3).
