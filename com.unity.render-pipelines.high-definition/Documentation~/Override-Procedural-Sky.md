# Procedural Sky

The **Procedural Sky Volume** component override lets you specify the type of sky that HDRP generates. For example, you can set up the sun size, ground color and sky tint.  

This sky is similar to the procedural sky in Unity’s built-in render pipeline. It differs as it includes extra properties that the built-in render pipeline lacks. HDRP’s **Procedural Sky** exposes a **Multiplier**, **Update Mode** properties, and the option to include the sun in HDRP’s baking process.

## Using Procedural Sky

**Procedural Sky** uses the [Volume](Volumes.html) framework, so to enable and modify **Procedural Sky** properties, you must add a **Procedural Sky** override to a [Volume](Volumes.html) in your Scene. To add **Procedural Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **Procedural Sky**.

After you add a **Procedural Sky** override, you must set the Volume to use **Procedural  Sky**. The [Visual Environment](Override-Visual-Environment.html) override controls which type of sky the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Type** to **Procedural Sky**. HDRP now renders a **Procedural Sky** for any Camera this Volume affects.

## Properties

![](Images/Override-ProceduralSky1.png)

| Property                  | Description                                                  |
| ------------------------- | ------------------------------------------------------------ |
| **Enable Sun Disk**       | Makes HDRP display the sun disk defined by the **Sky Size**, **Sun Size Convergence**, **Exposure**, and **Multiplier** . |
| **Sun Size**              | The size modifier of the sun disk.                           |
| **Sun Size Convergence**  | The size convergence of the sun, smaller values make the sun appear larger. |
| **Atmospheric Thickness** | The density of the atmosphere, an atmosphere of higher density absorbs more light. |
| **Sky Tint**              | The color of the sky.                                        |
| **Ground Color**          | The color of the ground (the area below the horizon).        |
| **Exposure**              | The exposure HDRP applies to the Scene as environmental light. HDRP calculates the environment light in your Scene using 2 to the power of your **Exposure** value. |
| **Multiplier**            | The multiplier HDRP applies to the Scene as environmental light. HDRP multiplies the environment light in your Scene by this value. |
| **Update Mode**           | Controls the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes).<br />&#8226; **On Changed**: Makes HDRP update the sky environment when one of the sky properties changes.<br />&#8226; **On Demand**: Makes HDRP wait until you manually call for a sky environment update from a script.<br />&#8226; **Realtime**: Makes HDRP update the sky environment at regular intervals defined by the **Update Period**. |
| - **Update Period**       | The period (in seconds) at which HDRP updates the sky environment when you set the **Update Mode** to **Realtime**. Set the value to 0 if you want HDRP to update the sky environment every frame. |