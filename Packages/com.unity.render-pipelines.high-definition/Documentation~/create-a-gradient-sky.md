# Create a gradient sky

A gradient sky is a simple representation of the sky, where the High Definition Render Pipeline (HDRP) interpolates between the following three colors:

* **Top**
* **Middle**
* **Bottom**

You can alter these values at runtime.

## Using Gradient Sky

**Gradient Sky** uses the [Volume](understand-volumes.md) framework, so to enable and modify **Gradient Sky** properties, you must add a **Gradient Sky** override to a [Volume](understand-volumes.md) in your Scene. To add **Gradient  Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Sky** and select on **Gradient Sky**.

After you add a **Gradient Sky** override, you must set the Volume to use **Gradient Sky**. The [Visual Environment](visual-environment-volume-override-reference.md) override controls which type of sky the Volume uses. To set the Volume to use **Gradient Sky**:

1. In the **Visual Environment** override, go to **Sky** > **Sky Type**
2. Set **Sky Type** to **Gradient Sky**.

HDRP now renders a **Gradient Sky** for any Camera this Volume affects.

Refer to the [Gradient Sky Volume Override Reference](gradient-sky-volume-override-reference.md) for more information.

[!include[](snippets/volume-override-api.md)]
