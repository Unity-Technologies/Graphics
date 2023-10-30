# Create a physically based sky

Physically Based Sky simulates a spherical planet with a two-part atmosphere that has an exponentially decreasing density based on its altitude. 

![](Images/Override-PhysicallyBasedSky2.png)

![](Images/Override-PhysicallyBasedSky3.png)

## Using Physically Based Sky

Physically Based Sky uses the [Volume](understand-volumes.md) framework. To enable and modify **Physically Based Sky** properties, add a **Physically Based Sky** override to a [Volume](understand-volumes.md) in your Scene. To add **Physically Based Sky** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override > Sky** and select **Physically Based Sky**.
3. If the Scene doesn't contain a Directional [Light](Light-Component.md), create one (menu: **GameObject > Light > Directional Light**). For physically correct results, set the Light's intensity to 130,000 lux.

Next, set the Volume to use **Physically Based Sky**. The [Visual Environment](Override-Visual-Environment.md) override controls which type of sky the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Type** to **Physically Based Sky**. HDRP now renders a **Physically Based Sky** for any Camera this Volume affects.

To change how much the atmosphere attenuates light, you can change the density of both air and aerosol molecules (participating media) in the atmosphere. You can also use aerosols to simulate real-world pollution or fog.

**Note**: HDRP only takes into account Lights that have **Affect Physically Based Sky** enabled. After Unity bakes the lighting in your project, Physically Based Sky ignores all Lights that have their **Mode** property set to **Baked**. To fix this, set the **Mode** to **Realtime** or **Mixed**.

**Note:** When Unity initializes a Physically Based Sky, it performs a resource-intensive operation which can cause the frame rate of your project to drop for a few frames. Once Unity has completed this operation, it stores the data in a cache to access the next time Unity initializes this volume. You might experience this frame rate drop if you have two Physically Based Sky volumes with different properties and switch between them.

Refer to the [Physically Based Sky Volume Override Reference](physically-based-sky-volume-override-reference.md) for more information.

[!include[](snippets/volume-override-api.md)]

## Set the surface of the planet

Where the surface of the planet is depends on whether you enable or disable the **Spherical Mode** property:

* If you enable **Spherical Mode**, the **Planetary Radius** and **Planet Center Position** properties define where the surface is. In this mode, the surface is at the distance set in **Planetary Radius** away from the position set in **Planet Center Position**.
* Otherwise, the **Sea Level** property defines where the surface is. In this mode, the surface stretches out infinitely on the xz plane and **Sea Level** sets its world space height.

The default values in either mode make it so the planet's surface is at **0** on the y-axis at the Scene origin. Since the default values for **Spherical Mode** simulate Earth, the radius is so large that, when you create your Scene environment, you can consider the surface to be flat. If you want some areas of your Scene environment to be below the current surface height, you can either vertically offset your Scene environment so that the lowest areas are above **0** on the y-axis, or decrease the surface height. To do the latter:

* If in **Spherical Mode**, either decrease the **Planetary  Radius**, or move the **Planet Center Position** down.

* If not in **Spherical Mode**, decrease the **Sea Level**.

The planet does not render in the depth buffer, this means it won't occlude lens flare and will not behave correctly when using motion blur.

Refer to the [Physically Based Sky Volume Override Reference](physically-based-sky-volume-override-reference.md) for more information.

