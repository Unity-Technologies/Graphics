# Known issues

This page contains information on known issues you may encounter when using URP.

## When importing the URP package samples, Unity does not set the necessary URP asset in Quality > Render Pipeline Asset<a name="urp-samples-known-issue-1"></a>

When importing the URP package samples, Unity does not set the necessary URP asset in **Quality** > **Render Pipeline Asset**, and certain sample rendering effects do not work.

To fix this issue:

In **Project Settings** > **Quality** > **Render Pipeline Asset**, select `SamplesPipelineAsset`.

![In Project Settings > Quality > Render Pipeline Asset, select SamplesPipelineAsset](Images/known-issues/urp-12-package-samples.png)

## Renaming a URP Renderer asset to a name matching one of the Renderer Feature names causes erroneous behavior

If a URP Renderer asset has any Renderer Features assigned, renaming the Renderer asset to a name matching one of the Renderer Feature names causes erroneous behavior: the URP Renderer and the Renderer Feature switch places.

The following scenario shows how the error occurs:

* Let's assume that the URP Renderer in your project is called `UniversalRenderer`.
* The Renderer has a Renderer Feature called `NewRenderObjects` assigned.

    ![UniversalRenderer with Renderer Feature assigned to it.](Images/known-issues/urp-10-renaming-renderer.png)

* Renaming `UniversalRenderer` to `NewRenderObjects` causes erroneous behavior:<br/>The Renderer switches places with the Renderer Feature and does not behave correctly.

To avoid the issue, do not give the URP Renderer asset the same name as the Renderer Feature asset.

To see updates on this issue, refer to the [Unity Issue Tracker](https://issuetracker.unity3d.com/issues/parent-and-child-nested-scriptable-object-assets-switch-places-when-parent-scriptable-object-asset-is-renamed).

## Warning about \_AdditionalLights property when upgrading the URP package

In certain cases, you might see the following warning when upgrading the URP package to a newer version:

```
Property (_AdditionalLights<...>) exceeds previous array size (256 vs 16). Cap to previous size.
```

This warning does not cause issues with the project, the warning disappears if you restart the Editor.
