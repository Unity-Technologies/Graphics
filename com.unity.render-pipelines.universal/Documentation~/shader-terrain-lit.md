# **Terrain Lit shader**

URP uses the Terrain Lit shader for Unity Terrain. This shader is a simpler version of the [Lit shader](lit-shader.md). A Terrain can use a Terrain Lit Material with up to eight [Terrain Layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html).

![A Terrain GameObject rendered with the Terrain Lit shader.](Images/terrain/terrain-rendered-with-terrain-lit.png)

## How to create a Terrain Lit Material

To create a Material compatible with a Terrain GameObject:

1. Create a new Material (**Assets** > **Create** > **Material**).
2. Select the new Material.
3. In the Inspector, click the **Shader** drop-down, and select **Universal&#160;Render&#160;Pipeline** > **Terrain** > **Lit**.

## How to assign a Terrain Lit Material to a Terrain GameObject

To assign a Terrain Lit Material to a Terrain GameObject:

1. Select a Terrain GameObject.
2. In the Inspector, click the gear icon on the right side of the Terrain Inspector toolbar to open the **Terrain Settings** section.
3. In the **Material** property, select a Terrain Lit Material. Either use the Object picker (circle icon), or drag and drop the Material onto the property.

![Terrain GameObject Inspector, Terrain Settings.](Images/terrain/terrain-lit-shader-inspector.png)

## Using the Paint Holes Tool

To use the **Paint Holes** tool on a Terrain, ensure that the **Terrain Holes** check box in your project's URP Asset is checked. Otherwise, the Terrain holes are absent when you build the application.

![URP Asset, Terrain Holes check box.](Images/terrain/urp-asset-terrain-holes.png)

## Terrain Lit Material properties

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Enable Height-based Blend** | Specifies whether URP should only render the Terrain Layer with the greatest height value for a particular pixel. When enabled, URP takes the height values from the blue channel of the **Mask Map** Texture. When disabled, URP blends the Terrain Layers based on the weights painted in the splatmap Textures. URP automatically ignores this feature when more than four Terrain Layers are on the Terrain. When this option is disabled and the Terrain Lit Shader Material is assigned to a Terrain, URP will provide an additional option to enable **Opacity as Density Blend** for each on each Terrain Layer that is added to that Terrain in the Paint Texture Tool Inspector. |
| **- Height Transition**       | Controls how much URP blends the terrain if multiple Terrain Layers are approximately the same height. |
| **Enable Per-pixel Normal**   | Specifies whether URP should sample the normal map Texture on a per-pixel level.  When enabled, Unity preserves more geometry details for distant terrain parts. Unity generates a geometry normal map at runtime from the heightmap, rather than the Mesh geometry. This means you can have high-resolution Mesh normals, even if your Mesh is low resolution. It only works if you enable **Draw Instanced** on the terrain. |
