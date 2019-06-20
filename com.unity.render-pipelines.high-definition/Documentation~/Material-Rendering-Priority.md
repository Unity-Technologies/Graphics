# Material rendering priority

HDRP uses Material Priority settings to sort transparent GameObjects.

The built-it Unity render pipeline sorts GameObjects according to their **Rendering Mode** and render queue. HDRP uses the render queue in a different way to Unity's built-in render pipeline, in that HDRP Materials do not expose the render queue directly. Instead, Materials with a **Transparent Surface Type** have a **Transparent Sort Priority** property:

![](Images/MaterialRenderingPriority1.png)

HDRP uses this priority to offset sorting inside a particular GameObject group (opaques, transparency). HDRP renders smaller values first.

Material priority works in conjunction with [Renderer priority](Renderer-Rendering-Priority). Material priority is higher than Renderer priority so HDRP sorts GameObjects first by Material Priority and then by Renderer Priority.