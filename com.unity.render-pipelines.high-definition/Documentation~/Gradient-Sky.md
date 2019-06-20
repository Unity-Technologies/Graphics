# Gradient Sky

The Gradient Sky Volume component override controls settings relevant to rendering a simple representation of the sky. It allows you to define three colors, the **Top**, **Middle**, and **Bottom**, which HDRP interpolates between to create a gradient sky.

You can alter these values at run time. This component also exposes options that enable you to define how HDRP updates the indirect lighting the sky generates in the Scene.

To use this type of sky. You must first add it to a Volume and set that Volume’s **Visual Environment** component override’s **Sky Type** to **Gradient**. For details on how to do this, see the [Visual Environment](Visual-Environment.html) documentation.

![](Images/SceneSettingsGradientSky1.png)

| Property               | Description                                                  |
| ---------------------- | ------------------------------------------------------------ |
| **Top**                | Sets the color of the upper hemisphere of the sky.           |
| **Middle**             | Sets the color at the horizon.                               |
| **Bottom**             | Sets the color of the lower hemisphere of the sky. This is below the horizon. |
| **Gradient Diffusion** | The size of the **Middle** property in the Skybox. Higher values make the gradient thinner, shrinking the size of the **Middle** section. Low values make the gradient thicker, increasing the size of the **Middle** section. |
| **Update Mode**        | Controls the rate at which HDRP updates the sky environment (using Ambient and Reflection Probes). |
| - **On Changed**       | Makes HDRP update the sky environment when one of its properties change. |
| - **On Demand**        | Makes HDRP wait until you manually call for a sky environment update from a script. |
| - **Realtime**         | Makes HDRP update the sky environment at regular intervals defined by the **Update Period**. |
| - - **Update Period**  | The period (in seconds) at which HDRP updates the sky environment when you set the **Update Mode** to **Realtime**. Set the value to 0 if you want HDRP to update the sky environment every frame. |