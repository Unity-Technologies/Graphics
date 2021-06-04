# **Terrain Lit Shader**

The Universal Render Pipeline (URP) uses the Terrain Lit Shader for Unity Terrain. This Shader is a simpler version of the [Lit Shader](lit-Shader.md). A Terrain can use a Terrain Lit Material with up to eight [Terrain Layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html).

![](Images/URPFeatures-TerrainShader.png)

## Creating a Terrain Lit Material

To create a new Terrain Lit Shader Material:

1. Go to your Project window and right-click in the **Assets** folder
2. Select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder.
3. Click on the Material to view it in the Inspector.
4. Click on the **Shader** drop-down and select **URP > Terrain > Lit**.

## Using a Terrain Lit Material

To use a Terrain Lit Material, you must assign it to a Terrain:

1. View the Terrain in the Inspector and click on the gear button on the right side of the Terrain Inspector toolbar to go to the **Terrain Settings** section.
2. Either drag and drop or use the radio button to assign your Terrain Lit Material to the **Material** property.

![](Images/TerrainLitShader1.png)

## Using the Paint Holes Tool

If you use the **Paint Holes** tool on your terrain, make sure to enable the **Terrain Holes** feature in your Project's URP Asset. Otherwise, the holes do not appear when you build your application.

![](Images/TerrainPaintHoles.png)

## Material properties

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Enable Height-based Blend** | Specifies whether URP should only render the Terrain Layer with the greatest height value for a particular pixel. When enabled, URP takes the height values from the blue channel of the **Mask Map** Texture. When disabled, URP blends the Terrain Layers based on the weights painted in the splatmap Textures. URP automatically ignores this feature when more than four Terrain Layers are on the Terrain. When this option is disabled and the Terrain Lit Shader Material is assigned to a Terrain, URP will provide an additional option to enable **Opacity as Density Blend** for each on each Terrain Layer that is added to that Terrain in the Paint Texture Tool Inspector. |
| **- Height Transition**       | Controls how much URP blends the terrain if multiple Terrain Layers are approximately the same height. |
| **Enable Per-pixel Normal**   | Specifies whether URP should sample the normal map Texture on a per-pixel level.  When enabled, Unity preserves more geometry details for distant terrain parts. Unity generates a geometry normal map at runtime from the heightmap, rather than the Mesh geometry. This means you can have high-resolution Mesh normals, even if your Mesh is low resolution. It only works if you enable **Draw Instanced** on the terrain. |
