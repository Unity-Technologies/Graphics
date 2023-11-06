# Create simple clouds (Cloud Layer)

A Cloud Layer is a simple representation of clouds in the High Definition Render Pipeline (HDRP). The cloud layer is a 2D texture rendered on top of the sky that can be animated using a flowmap. You can also project cloud shadows on the ground.

Refer to [Understand clouds](understand-clouds.md) for more information about clouds in the High Definition Render Pipeline (HDRP).

![](Images/HDRPFeatures-CloudLayer.png)

## Using the Cloud Layer

The **Cloud Layer** uses the [Volume](understand-volumes.md) framework, so to enable and modify **Cloud Layer** properties, you must add a **Cloud Layer** override to a [Volume](understand-volumes.md) in your Scene. To add **Cloud Layer** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **Cloud Layer**.

After you add a **Cloud Layer** override, you must set the Volume to use **Cloud Layer**. The [Visual Environment](visual-environment-volume-override-reference.md) override controls which type of clouds the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Cloud Type** to **Cloud Layer**. HDRP now renders a **Cloud Layer** for any Camera this Volume affects.
To enable the **Cloud Layer** override, you must assign a cloud map. For information about the cloud map's format or how to find the example cloud map texture, see [about the cloud map](#about-the-cloud-map) section.

The Cloud Layer will bake the cloud map to an intermediate texture, which is recomputed everytime a parameter changes. The resolution of the baked texture is determined by the **Resolution** parameter in the advanced settings of the inspector.
Clouds shadows are also baked to a separate texture whose resolution is set by the **Shadow Resolution** parameter.

Refer to the [Cloud Layer Volume Override reference](cloud-layer-volume-override-reference.md) for more information.

[!include[](snippets/volume-override-api.md)]

## About the cloud map

The cloud map is a 2D RGBA texture in LatLong layout (sometimes called Cylindrical or Equirectangular) where each channel contains a cloud opacity. For rendering, HDRP mixes the four channels together using the **Opacity RGBA** parameters of the Volume override. This allows you to change the aspects of the clouds using a single texture and the volume framework.
If you enable **Upper Hemisphere Only**, the map is interpreted as containing only the upper half of a LatLong texture. This means that clouds will only cover the sky above the horizon.

By default, HDRP uses a cloud map named `DefaultCloudMap`. This texture contains cumulus clouds in the red channel, stratus clouds in the green channel, cirrus clouds in the blue channel and wispy clouds in the alpha channel.

**Note**: This cloud map is formatted differently to the cloud map that the [Volumetric Clouds](create-realistic-clouds-volumetric-clouds.md) feature uses.

## Controlling cloud movement

The Cloud Layer override provides a way to move clouds at runtime, using a flowmap. A flowmap has the same layout as the [cloud map]create-simple-clouds-cloud-layer#about-the-cloud-map), in that it is a LatLong layout 2D texture, and also uses the **Upper Hemisphere Only** property to determine the area it affects.

A flowmap only uses the red and green channels and they represent horizontal and vertical displacement respectively. For each of these channels, a value of `0.5` means no displacement, a value of `0` means a negative displacement and a value of `1` means a positive displacement.
