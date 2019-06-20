# Gradient Sky

The Gradient Sky Volume component override controls settings relevant to rendering a simple representation of the sky. It allows you to define three colors, the **Top**, **Middle**, and **Bottom**, which HDRP interpolates between to create a gradient sky.

You can alter these values at run time. This component also exposes options that enable you to define how HDRP updates the indirect lighting the sky generates in the Scene.

##  Using Gradient Sky

**Gradient Sky** uses the [Volume](Volumes.html) framework, so to enable and modify **Gradient Sky** properties, you must add a **Gradient Sky** override to a [Volume](Volumes.html) in your Scene. To add **Gradient  Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **Gradient Sky**.

After you add a **Gradient Sky** override, you must set the Volume to use **Gradient Sky**. The [Visual Environment](Override-Visual-Environment.html) override controls which type of sky the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Type** to **Gradient Sky**. HDRP now renders a **Gradient Sky** for any Camera this Volume affects.

## Properties

![](Images/Override-GradientSky1.png)

| Property               | Description                                                  |
| ---------------------- | ------------------------------------------------------------ |
| **Top**                | Sets the color of the upper hemisphere of the sky.           |
| **Middle**             | Sets the color at the horizon.                               |
| **Bottom**             | Sets the color of the lower hemisphere of the sky. This is below the horizon. |
| **Gradient Diffusion** | The size of the **Middle** property in the Skybox. Higher values make the gradient thinner, shrinking the size of the **Middle** section. Low values make the gradient thicker, increasing the size of the **Middle** section. |
| **Update Mode**        | Controls the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes).<br />&#8226; **On Changed**: Makes HDRP update the sky environment when one of the sky properties changes.<br />&#8226; **On Demand**: Makes HDRP wait until you manually call for a sky environment update from a script.<br />&#8226; **Realtime**: Makes HDRP update the sky environment at regular intervals defined by the **Update Period**. |
| - **Update Period**    | The period (in seconds) at which HDRP updates the sky environment when you set the **Update Mode** to **Realtime**. Set the value to 0 if you want HDRP to update the sky environment every frame. |