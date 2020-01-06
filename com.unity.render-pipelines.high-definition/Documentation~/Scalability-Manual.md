# Scalability in HDRP

The High Definition Render Pipeline (HDRP) allocates resources when the application initializes to better manage memory. HDRP allows you to specify which effects it allocates memory for as well as which algorithm it uses to process these effects.

These settings define which effects are available in your HDRP Project and impact GPU performance and the amount of graphics memory that the application uses. If you enable fewer effects, HDRP uses less memory. If you use a less precise algorithm for an effect, HDRP processes the effect faster.

To specify which effects HDRP allocates memory for, as well as the resource intensity of the algorithm it uses to process them, see the [HDRP Asset](HDRP-Asset.html). 

Different platforms and computers have different capabilities in terms of GPU performance and available graphics memory. With this in mind, HDRP allows you to define multiple HDRP Assets for your Project. Each HDRP Asset can target a specific platform or quality tier to maintain a balance between performance and rendering quality. For example, you can define separate HDRP Assets for:

* Xbox One.
* Xbox One X.
* PlayStation 4.
* PlayStation 4 Pro.
* PC - Low.
* PC - Medium.
* PC - High.

## Using the HDRP Asset and Quality Settings in Unity

### The default HDRP Asset

To use HDRP, you need to create an HDRP Asset and assign it as the **Scriptable Render Pipeline** for your Project. To do this, see [Creating an HDRP Asset](HDRP-Asset.html#CreatingAnHDRPAsset). When you assign an HDRP Asset as the **Scriptable Render Pipeline**, it acts as the **default** HDRP Asset for your Project and contains all of the default HDRP settings.

### Overriding settings for a quality level

To override HDRP settings for different hardware and computer processing ability, you need to create an additional HDRP Asset to contain the overridden values.

To guarantee scalability, you should define a separate HDRP Asset for each console as well as an HDRP Asset for a low-end, average, and high-end computer.

![Quality Settings Panel](Images/QualitySettingsPanel.png)
_A Quality Settings Panel with several quality level with an associated HDRP Asset_

After you create an HDRP Asset, create a Quality Level for it. To do this, go to **Project Settings > Quality** and click **Add Quality Level**. Now, select the new Quality Level and assign the HDRP Asset to its **Render Pipeline Asset** property.

The HDRP Asset that you assign to a Quality Level is the Quality Level's **current** HDRP Asset.

**Note**: You can use the same HDRP Asset for multiple Quality Levels. If you do not assign an HDRP Asset to a Quality Level, the Quality Level uses the **default** HDRP Asset as its **current** HDRP Asset.

### Editing Quality Levels

To edit the HDRP Asset for a Quality Level, you can use the **HDRP** panel in the Project Settings window.

At the top, there is a list of HDRP Assets that contains the **default** HDRP Asset as well as every HDRP Asset that you have assigned to a Quality Level.
To edit an HDRP Asset, select it from the list and use the inspector that appears below.

![HDRP Quality Settings Panel](images/HDRPQualitySettingsPanel.png)
_The HDRP quality settings panel allows you to edit values for specific HDRP Assets_

## Using the current quality settings parameters

The settings defined in both the default HDRP Asset and the one in the quality settings are used during the rendering.

### Predefined values for different Quality Levels

In a single frame, some elements can require differing resources. For example, a Spot Light closer to the Camera may have a greater shadow resolution than a Light further away.

For this kind of settings, you can either:

* Use a custom value to always use.
* Use a value from those defined in the current HDRP Asset.

For the example above, the shadow resolution of a Light can be either:
- A custom value that you set in the Inspector for the Light.
- One of the predefined values in the current Quality Level's HDRP Asset (either **Low**, **Medium**, **High**, **Ultra**).

![Shadow Resolution Scalability](Images/ShadowResolutionScalability.png)
_The shadow resolution of the Directional Light uses the **Medium** predefined value in the Quality Levels HDRP Asset._

### Material Quality Node in Shader Graph

You can use the Material Quality Node in Shader Graphs to decide which code to execute for a specific quality. The current quality level is in the **Material** section of the HDRP Asset.

It is important that, for a given quality level, all Materials have the same Material Quality Level applied. If you need to have a different shader quality in a single rendering (like a Shader LOD system), you should author a dedicated Shader and use a different Shader with the appropriate complexity for this rendering.

### Ray tracing Node in Shader Graph

For Shader Graphs that use ray tracing, you can use the Raytracing Node to provide a fast and a slow implementation.

HDRP uses the fast implementation to increase the performance of ray-traced rendering features where accuracy is less important.

HDRP activates the Raytracing Node depending on the settings defined in the current HDRP Asset.
