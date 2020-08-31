# Procedural Sky

The **Procedural Sky Volume** component override lets you specify the type of sky that the High Definition Render Pipeline (HDRP) generates. For example, you can set up the sun size, ground color and sky tint.  

This sky is similar to the procedural sky in Unity’s built-in render pipeline. It differs as it includes extra properties that the built-in render pipeline lacks. HDRP’s **Procedural Sky** exposes a **Multiplier**, **Update Mode** properties, and the option to include the sun in HDRP’s baking process.

## Deprecation

HDRP deprecated **Procedural Sky** in 2019.3 and replaced it with [Physically Based Sky](Override-Physically-Based-Sky.md). To use Procedural Sky for HDRP Projects in Unity 2019.3 or later, follow the instructions on the [Upgrading from 2019.2 to 2019.3 guide](Upgrading-from-2019.2-to-2019.3.md#ProceduralSky).

## Using Procedural Sky

**Procedural Sky** uses the [Volume](Volumes.md) framework, so to enable and modify **Procedural Sky** properties, you must add a **Procedural Sky** override to a [Volume](Volumes.md) in your Scene. To add **Procedural Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **Procedural Sky**.

After you add a **Procedural Sky** override, you must set the Volume to use **Procedural  Sky**. The [Visual Environment](Override-Visual-Environment.md) override controls which type of sky the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Type** to **Procedural Sky**. HDRP now renders a **Procedural Sky** for any Camera this Volume affects.

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| Property                  | Description                                                  |
| ------------------------- | ------------------------------------------------------------ |
| **Enable Sun Disk**       | Enable the checkbox to make HDRP display the sun disk defined by the **Sky Size**, **Sun Size Convergence**, **Exposure**, and **Multiplier** . |
| **Sun Size**              | Use the slider to set the size modifier of the sun disk.     |
| **Sun Size Convergence**  | Use the slider to set the size convergence of the sun, smaller values make the sun appear larger. |
| **Atmospheric Thickness** | Use the slider to set the density of the atmosphere, an atmosphere of higher density absorbs more light. |
| **Sky Tint**              | Use the color picker to select the color of the sky.         |
| **Ground Color**          | Use the color picker to select the color of the ground (the area below the horizon). |
| **Exposure**              | Set the exposure for HDRP to apply to the Scene as environmental light. HDRP uses 2 to the power of your **Exposure** value to calculate the environment light in your Scene. |
| **Multiplier**            | Set the multiplier for HDRP to apply to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value. |
| **Update Mode**           | Use the drop-down to set the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes).<br />&#8226; **On Changed**: HDRP updates the sky environment when one of the sky properties changes.<br />&#8226; **On Demand**: HDRP waits until you manually call for a sky environment update from a script.<br />&#8226; **Realtime**: HDRP updates the sky environment at regular intervals defined by the **Update Period**. |
| - **Update Period**       | Set the period (in seconds) for HDRP to update the sky environment. Set the value to 0 if you want HDRP to update the sky environment every frame. This property only appears when you set the **Update Mode** to **Realtime**. |
