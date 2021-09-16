# Motion vectors in HDRP

Motion vectors capture the per-pixel, screen-space motion of GameObjects from one frame to the next. To compute motion vectors for a GameObject, HDRP uses the difference between the GameObject’s position in the current and previous frame.

HDRP uses motion vectors for various effects such as [temporal anti-aliasing (TAA)](Glossary.md#TemporalAntiAliasing) and motion blur.

HDRP calculates motion vectors in two stages:

1. HDRP first calculates object motion vectors. Object motion vectors are motion vectors that HDRP calculates based on the screen-space movement of GameObjects.
2. HDRP then calculates camera motion vectors. These are motion vectors caused by the movement of the Camera. HDRP calculates camera motion vectors for pixels that did not write motion vectors during the first stage.

## Using motion vectors

To use motion vectors in HDRP, you must first enable them:

1. In your Unity Project’s [HDRP Asset](HDRP-Asset.md):
   1. Select the HDRP Asset and view it in the Inspector window.
   2. Go to the **Rendering** section and enable **Motion Vectors**.
2. In [Frame Settings](Frame-Settings.md). You can either enable motion vectors for all Cameras or on an individual, per-Camera level.
   1. To enable motion vectors for all Cameras, open the [HDRP Default Settings](Default-Settings-Window.md) Project Settings tab (menu: **Edit** > **Project Settings** > **HDRP Default Settings**), then set **Default Frame Settings For** to **Camera**. To enable motion vectors for a particular Camera, select the Camera and, in the Inspector, enable **Custom Frame Settings**.
   3. In the **Rendering Section**, enable **Motion Vectors**. This enables camera motion vectors.
   3. To enable motion vectors for opaque GameObjects, enable **Opaque Object Motion**. To enable motion vectors for transparent GameObjects, enable **Transparent Object Motion**.


HDRP can now render motion vectors. If you enabled object motion vectors, be aware that, by default, new Mesh Renderers write object motion vectors. To change this behavior, select the Mesh Renderer and, in the Inspector, change the value of the **Motion Vectors** property.<br/>![](Images/MotionVectors1.png)
The options are:

* **Camera Motion Only**: HDRP only calculates camera motion vectors for the area of the screen this GameObject fills.
* **Per Object Motion**: HDRP calculates motion vectors for this GameObject if:
  * The GameObject moves and the camera does not.
  * The camera moves and the GamaObject does not.
  * Both the GameObject and the camera move.
* **Force No Motion**: HDRP does not calculate any motion vectors for the area of the screen this GameObject fills.

## Motion vectors for transparent objects

By default, HDRP does not render motion vectors for transparent Materials. This is because motion vectors from transparent GameObjects overwrite motion vectors for GameObjects behind them. For example, a window would overwrite the motion vectors for a bird flying behind it. In this example, since the bird's motion vectors are now invalid, if you used [temporal anti-aliasing](Anti-Aliasing.md#temporal-anti-aliasing-taa), the bird would produce ghosting.

To make HDRP render motion vectors for transparent Materials, see the steps in [Using motion vectors](#using-motion-vectors) and enable **Transparent Object Motion**.

When transparent objects write motion vectors on a given pixel, they replace that pixel’s previous motion vectors. This is particularly useful for Materials that use alpha clipping, such as hair.

If you use motion blur in conjunction with transparent GameObjects, be aware that motion blur also uses depth information. This means that you should make the Material write depth information too. To do this, go to **Surface Options** and enable the **Transparent Depth Postpass** checkbox.
