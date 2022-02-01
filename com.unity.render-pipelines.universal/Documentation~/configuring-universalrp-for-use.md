# Configuring the Universal Render Pipeline

To configure and use the Universal Render Pipeline (URP), you must first:

- Create the Universal Render Pipeline Asset
- Add the Asset to the Graphics settings for your Project

**Note:** If a scriptable render pipeline is assigned in any of the projets quality levels, they will override the Graphics settings. If this is the case, you will not see any changes when updating the pipeline asset in Graphics settings.

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


## SRPs and quality settings
The pipeline asset contains settings that will often need to be configures based on device performance. This is achieved by having unique pipeline assets for each quality level. A Unity project will always look to the quality settings first for a pipeline asset. If no pipeline is assigned to the current quality level, Unity falls back to the pipeline asset assigned in project settings (built-in render pipeline if nothing is assigned).
