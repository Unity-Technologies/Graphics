# Configuring the Universal Render Pipeline

To configure and use the Universal Render Pipeline (URP), you must first:

- Create the Universal Render Pipeline Asset.

- Add the Asset to the Graphics settings for your Project.

**Note:** A Scriptable Render Pipeline asset specified for any of the project's quality levels (**Project Settings** > **Quality**) has priority over the settings that you specify in the **Graphics** section.

To read more about each step, see below.

## Creating the Universal Render Pipeline Asset

The [Universal Render Pipeline Asset](universalrp-asset.md) controls the [global rendering and quality settings](universalrp-asset.md) of your Project and creates the rendering pipeline instance. The rendering pipeline instance contains intermediate resources and the render pipeline implementation.

To create a Universal Render Pipeline Asset:

1. In the Editor, go to the Project window.
2. Right-click in the Project window, and select  __Create > Rendering > URP Asset__. Alternatively, navigate to the menu bar in top, and click __Assets > Create > Rendering > URP Asset__.
3. Either leave the default name for the Asset, or type a new one. You've now created a URP Asset.

**Tip:** You can create multiple URP Assets to store settings for different platforms or for different testing environments. Once you've started using URP, try swapping out URP Assets under Graphics settings and test the combinations, to see what fits your Project or platforms best. You cannot swap URP Assets for other types of render pipeline assets, though.



## Adding the Asset to your Graphics settings

To use the Universal Render Pipeline, you have to add the newly created URP Asset to your Graphics settings in Unity. If you don't, Unity still tries to use the built-in render pipeline.

1. Navigate to __Edit > Project Settings > Graphics__.
2. In the __Scriptable Render Pipeline Settings__ field, add the URP Asset you created earlier.

**Note:** When you add the UP Asset, the available settings in URP immediately changes. This is because you've effectively instructed Unity to use the URP specific settings instead of those for the built-in render pipeline.

## Scriptable Render Pipelines and Quality settings

A Scriptable Render Pipeline (SRP) asset contains settings that often need to be different for different hardware. To specify different settings, you can configure different SRP assets for different Quality levels (**Project Settings** > **Quality**). Unity searches for an SRP asset in the quality level first, and uses the SRP asset in **Project Settings** > **Graphics** if one if not configured for the quality level. If an SRP asset is not defined neither in a quality level, nor in the Graphics section, Unity falls back to the Built-in Render Pipeline.
