**Note:** This page is subject to change during the 2019.1 beta cycle.

# Configuring LWRP for use

To configure and use LWRP, you must first 

- create the Lightweight Render Pipeline Asset, and then
- add the Asset to the Graphics settings for your Project.

To read more about each step, see below.

## Creating the Lightweight Render Pipeline Asset

The Scriptable Render Pipeline Asset controls the [global rendering and quality settings](lwrp-asset.md) of your Project and creates the rendering pipeline instance. The rendering pipeline instance contains intermediate resources and the render pipeline implementation.

To create a Lightweight Render Pipeline Asset:

1. In the Editor, go to the Project window.
2. Right-click in the Project window, and select  __Create__ &gt; __Rendering__ > __Lightweight__ __Render Pipeline Asset__. Alternatively, navigate to the menu bar in top, and click __Assets__ > __Create__ > __Rendering__ > __Lightweight Render Pipeline Asset__.
3. Either leave the default name for the Asset, or type a new one. You've now created a LWRP Asset.

**Tip:** You can create multiple LWRP Assets to store settings for different platforms or for different testing environments. Once you've started using LWRP, try swapping out LWRP Assets under Graphics settings and test the combinations, to see what fits your Project or platforms best. You cannot swap LWRP Assets for other types of render pipeline assets, though.



## Adding the Asset to your Graphics settings

To use the Lightweight Render Pipeline, you have to add the newly created LWRP Asset to your Graphics settings in Unity. If you don't, Unity still tries to use the built-in render pipeline.

1. Navigate to __Edit__ > __Project Settings__ > __Graphics__. 
2. In the __Render Pipeline Settings__ field, add the LWRP Asset you created earlier.

**Note:** When you add the LWRP Asset, the available settings in LWRP immediately changes. This is because you've effectively instructed Unity to use the LWRP specific settings instead of those for the built-in render pipeline.
