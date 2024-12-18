# Use contact shadows
Contact Shadows are shadows that HDRP [ray marches](Glossary.md#RayMarching) in screen space, inside the depth buffer, at a close range. They provide small, detailed, shadows for details in geometry that shadow maps can't usually capture.

The Contact Shadows [Volume Override](volume-component.md) specifies properties which control the behavior of Contact Shadows. Contact Shadows are shadows that The High Definition Render Pipeline (HDRP) [ray marches](Glossary.md#RayMarching) in screen space inside the depth buffer. The goal of using Contact Shadows is to capture small details that regular shadow mapping algorithms fail to capture.

24 Lights (Direction, Point or Spot) can cast Contact Shadows at a time.

For information about contact shadow properties, refer to [Contact shadows override reference](reference-contact-shadows-override.md).

<a name="enable-contact-shadows"><a>

## Enable contact shadows

[!include[](snippets/Volume-Override-Enable-Override.md)]

Enable the following properties:

- In the HDRP Asset: **Lighting > Shadows > Use Contact Shadows**.
- In the Frame Settings window: **Lighting > Contact Shadows**.


<a name="use-contact-shadows"></a>

## Use contact shadows

**Contact Shadows** use the [Volume](understand-volumes.md) framework, so to enable and modify **Contact Shadow** properties, you must add a **Contact Shadows** override to a [Volume](understand-volumes.md) in your Scene. To add **Contact Shadows** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Shadowing** and click on **Contact Shadows**. HDRP now applies **Contact Shadows** to any Camera this Volume affects.

You can enable Contact Shadows on a per Light basis for Directional, Point, and Spot Lights. Tick the **Enable** checkbox under the **Contact Shadows** drop-down in the **Shadows** section of each Light to indicate that HDRP should calculate Contact Shadows for that Light.

**Note**: A Light casts Contact Shadows for every Mesh Renderer that uses a Material that writes to the depth buffer. This is regardless of whether you enable or disable the **Cast Shadows** property on the Mesh Renderer. This means that you can disable **Cast Shadows** on small GameObjects/props and still have them cast Contact Shadows. This is good if you do not want HDRP to render these GameObjects in shadow maps. If you do not want this behavior, use Shader Graph to author a Material that does not write to the depth buffer.

[!include[](snippets/volume-override-api.md)]


## Details

* For Mesh Renderer components, setting __Cast Shadows__ to __Off__ does not apply to Contact Shadows.

* Contact shadow have a variable cost between 0.5 and 1.3 ms on the base PS4 at 1080p.
