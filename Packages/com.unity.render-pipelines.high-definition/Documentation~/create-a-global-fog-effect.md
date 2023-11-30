# Create a global fog effect

The High Definition Render Pipeline (HDRP) implements a multi-layered fog composed of an exponential component, whose density varies exponentially with distance from the Camera and height HDRP allows you to add an optional volumetric component to this exponential fog that realistically simulates the interaction of lights with fog, which allows for physically plausible rendering of glow and crepuscular rays, which are beams of light that stream through gaps in objects like clouds and trees from a central point.

## Using Fog

The **Fog** uses the [Volume](understand-volumes.md) framework, so to enable and modify **Fog** properties, you must add  **Fog** override to a [Volume](understand-volumes.md) in your Scene. To add **Fog** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Fog**.
3. In the override, set **State** to **Enabled**. HDRP now renders **Fog** for any Camera this Volume affects.

At this point, the Scene contains global fog. However, the effect might not suit your needs. To override the default property with your own chosen values, follow the steps in the [Customizing Global Fog](#CustomizingGlobalFog) section.

The High Definition Render Pipeline evaluates volumetric lighting on a 3D grid mapped to the volumetric section of the frustum. The resolution of the grid is quite low (it's 240x135x64 using the default quality setting at 1080p), so it's important to keep the dimensions of the frustum as small as possible to maintain high quality. Adjust the **Volumetric Fog Distance** parameter to define the maximum range for the volumetric fog relative to the Camera’s frustum.

The Fog may not work when using a custom camera projection matrix, like an off-axis projection.

<a name="CustomizingGlobalFog"></a>

## Customizing Global Fog

Use global volumetric fog, rather than local fog, because it provides the best performance and the best quality.

Global fog is a height fog which has two logical components:

- The region at a distance closer to the Camera than the **Base Height** is a constant (homogeneous) fog
- The region at a distance further than the **Base Height** is the exponential fog.

The **Fog** override of the active Volume controls the appearance of the global fog. It includes two main properties that you can use to override the default density.

* **Fog Attenuation Distance**: Controls the global density of the fog.
* **Maximum Height**: Controls the density falloff with height; allows you to have a greater density near the ground and a lower density higher up.

Refer to the [Fog Volume Override reference](fog-volume-override-reference.md) for more information.