# Renderer and Material Priority

A render pipeline must sort objects before rendering them to make sure that they appear on the screen in the correct order. The render pipeline must draw objects that are far away from the Camera first, so that it can draw closer objects over the top of them later. If the order is not correct, objects further away from the Camera can appear in front of closer objects.

The built-it Unity render pipeline sorts GameObjects according to their [Rendering Mode](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) and [renderQueue](https://docs.unity3d.com/ScriptReference/Material-renderQueue.html). HDRP uses the render queue in a different way, in that HDRP Materials do not expose the render queue directly. Instead, HDRP introduces two methods of control. Sorting by [Material](#SortingByMaterial) and sorting by [Renderer](#SortingByRenderer).

HDRP uses these two sorting methods together to control the render queue. To calculate the order of the render queue, HDRP:

1. Sorts Meshes into groups that share Materials
2. Uses each Materials’ **Priority** to calculate the rendering order of these Material groups
3.  Sorts the Material groups using each Mesh Renderer’s **Priority** property. 

The resulting queue is a list of GameObjects that are first sorted by their Material’s **Priority**, then by their individual Mesh Renderer’s **Priority**.

<a name="SortingByMaterial"></a>

## Sorting by Material

Materials with a **Transparent Surface Type** have a **Sorting Priority** property that you can use to sort groups of Meshes that use different Materials. This property is an integer value clamped between -100 and 100.

![](Images/RendererAndMaterialPriority1.png)

HDRP supports negative values so that you can easily assign new Materials to the lowest priority. This is helpful if you want to assign a new Material to the lowest priority when the lowest priority is already being used for another Material. In this case, you can just assign the new Material’s priority to a negative value, instead of increasing every other Material’s sorting priority by one to accommodate the new Material.

HDRP uses the **Sorting Priority** to sort GameObjects that use different Materials in your Scene. HDRP renders Materials with lower **Sorting Priority** values first. This means that Meshes using Materials with a higher **Sorting Priority** value appear in front of those using Materials with lower ones, even if Meshes using the first Material are further away from the Camera.

For example, the following Scene includes two spheres (**Sphere 1** and **Sphere 2**) that use two different Materials. As you can see, **Sphere 1** is closer to the **Camera** than **Sphere 2**.


![](Images/RendererAndMaterialPriority2.png)

When the **Sort Priority** of each Material is the same, HDRP treats them with equal importance, and bases the rendering order on the Material's distance from the Camera. In this example, the **Sort Priority** of both Materials is set to **0**, so HDRP renders them in the order defined by their distance from the Camera, which means **Sphere 1** appears in front of **Sphere 2**.

![](Images/RendererAndMaterialPriority3.png)

When the **Sort Priority** properties of different Materials are not the same, HDRP displays Meshes using Materials with a higher priority in front of those using Materials with a lower priority. To achieve this, HDRP draws Meshes using lower priority Materials first and draws Meshes using the higher priority Materials later, on top of the Meshes it’s already drawn. In the example, setting the **Sort Priority** of **Sphere 2** to **1** means that HDRP renders **Sphere 1** first, then renders **Sphere 2** (drawing it over **Sphere 1**). This makes **Sphere 2** appear in front of **Sphere 1**, despite **Sphere 1** being closer to the **Camera**.

![](Images/RendererAndMaterialPriority4.png)

**⚠ Note that when the Depth Write is enabled on the material, the Sort Priority is ignored.** This is because the **Depth Test** performed in the Shader overwrites the **Sort Priority** of the material.

<a name="SortingByRenderer"></a>

## Sorting by Renderer

Mesh Renderers have a **Priority** property to sort Renderers using the same Material in your Scene.

![](Images/RendererAndMaterialPriority5.png)

When you want to modify the render order for GameObjects using the same Material, use the **Priority** property in the Mesh Renderer’s Inspector. The **Priority** is a per-Renderer property that allows you to influence the rendering order for Renderers in your Scene.

HDRP displays Renderers with higher **Priority** values in front of those with lower **Priority** values.

You can also edit the Renderer **Priority** for Mesh Renderers in scripts by setting the [rendererPriority](https://docs.unity3d.com/2018.3/Documentation/ScriptReference/Renderer-rendererPriority.html) value.

## Example usage

The following Scene includes two spheres (**Sphere 1** and **Sphere 2**) that use the same Material. As you can see, **Sphere 1** is closer to the **Camera** than **Sphere 2**.


![](Images/RendererAndMaterialPriority6.png)

When the Renderer **Priority** of each Mesh Renderer is the same, HDRP treats them with equal importance, and bases the rendering order on each Mesh Renderer’s distance from the Camera. In this example, the Renderer **Priority** of both Mesh Renderers is set to **0**, so HDRP renders them in the order defined by their distance from the Camera, which means **Sphere 1** appears in front of **Sphere 2**.

![](Images/RendererAndMaterialPriority7.png)

When the **Renderer Priority** properties of different Mesh Renderers are not the same, HDRP displays Mesh Renderers with a higher priority in front of those with a lower Priority. To achieve this, HDRP draws lower priority Meshes first and then draws higher priority Meshes on top of the Meshes it’s already drawn. In the example, setting the **Renderer Priority** of **Sphere 2** to **1** means that HDRP renders **Sphere 1** first, then renders **Sphere 2** (drawing it over **Sphere 1**). This makes **Sphere 2** appear in front of **Sphere 1** despite **Sphere 1** being closer to the **Camera**.

![](Images/RendererAndMaterialPriority8.png)