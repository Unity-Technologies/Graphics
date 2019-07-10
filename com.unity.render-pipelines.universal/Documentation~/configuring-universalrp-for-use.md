# Configuring UniversalRP for use

To configure and use UniversalRP, you must first 

- create the Universal Render Pipeline Asset, and then
- add the Asset to the Graphics settings for your Project.

To read more about each step, see below.

## Creating the Universal Render Pipeline Asset

The [Universal Render Pipeline Asset](universalrp-asset.md) controls the [global rendering and quality settings](universalrp-asset.md) of your Project and creates the rendering pipeline instance. The rendering pipeline instance contains intermediate resources and the render pipeline implementation.

To create a Universal Render Pipeline Asset:

1. In the Editor, go to the Project window.
2. Right-click in the Project window, and select  __Create__ &gt; __Rendering__ > __Universal__ __Render Pipeline__ > __Pipeline Asset__. Alternatively, navigate to the menu bar in top, and click __Assets__ > __Create__ > __Rendering__ > __Universal Render Pipeline__ > __Pipeline Asset__.
3. Either leave the default name for the Asset, or type a new one. You've now created a UniversalRP Asset.

**Tip:** You can create multiple UniversalRP Assets to store settings for different platforms or for different testing environments. Once you've started using UniversalRP, try swapping out UniversalRP Assets under Graphics settings and test the combinations, to see what fits your Project or platforms best. You cannot swap UniversalRP Assets for other types of render pipeline assets, though.



## Adding the Asset to your Graphics settings

To use the Universal Render Pipeline, you have to add the newly created UniversalRP Asset to your Graphics settings in Unity. If you don't, Unity still tries to use the built-in render pipeline.

1. Navigate to __Edit__ > __Project Settings__ > __Graphics__. 
2. In the __Scriptable Render Pipeline Settings__ field, add the UniversalRP Asset you created earlier.

**Note:** When you add the UniversalRP Asset, the available settings in UniversalRP immediately changes. This is because you've effectively instructed Unity to use the UniversalRP specific settings instead of those for the built-in render pipeline.
