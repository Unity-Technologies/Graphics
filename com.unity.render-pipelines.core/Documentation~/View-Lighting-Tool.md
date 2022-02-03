## Light Anchor

The Light Anchor can help to place light sources around subjects, in relation to a Camera and an anchor point. It's particularly effective for cinematic lighting, which often requires multiple light sources orbiting a subject.

## Using the Light Anchor Component

To add a Light Anchor component to a GameObject in your Scene:

1. Select a Light GameObject in the hierarchy to open its Inspector window.
2. Go to **Add Component** > **Rendering** > **Light Anchor**

By default, the Anchor's position is the same as the position of the GameObject the Light Anchor Component is attached to.

**Note**: To use the Light Anchor, you must set the Tag of at least one Camera to "MainCamera".

Use the **Orbit** and **Elevation** to control the orientation of the light, in degrees, relative to the main Camera's and Anchor's positions. If the Light has a Cookie or an IES Profile, use the **Roll** to change their orientation. Use the **Distance** to control how far from the anchor, in meters, you want to place the Light.

You can use the **Anchor Position Override** to provide a GameObject’s [Transform](https://docs.unity3d.com/ScriptReference/Transform.html) as an anchor point for the Light. This is useful if you want the Light to follow a specific GameObject in the Scene.

![](Images/LightAnchorAnimation.gif)

**Note**: The above example uses the Main Camera as the reference Camera that adjusts the light rotation. The Common presets might create a different result in the Scene View if your view isn't aligned with the Main Camera.

You can set a **Position Offset** for this custom Anchor. This is useful if the Transform position of the custom Anchor isn't centered appropriately for the light to orbit correctly around the custom Anchor.

![](Images/LightAnchor0.png)


The Light Anchor component also includes a list of **Presets** that you can use to set the Light's orientation relative to the main Camera.

## Properties

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Orbit** | Use the left icon to control the Orbit of the light. This tool becomes green when you move the icon. |
| **Elevation** | Use the middle icon to control the Elevation of the light. This tool becomes blue when you move the icon. |
| **Roll** | Use the right icon to control the Roll of the light. This tool becomes gray when you move the icon. This is useful if the light has an IES or a Cookie. |
| **Distance** | Controls the distance between the light and its anchor in world space. |
| **Up Direction** | Defines the space of the up direction of the anchor. When you set this value to Local, the Up Direction is relative to the Camera. |
| **Anchor Position Override** | Allows you to use a GameObject's [Transform](https://docs.unity3d.com/ScriptReference/Transform.html) as anchor position instead of the LightAnchor's Transform. When the Transform of the GameObject you assigned to this property changes, the Light Anchor's Transform also changes. |
| **Common** | Assigns a preset to the light component based on the behavior of studio lights. |
