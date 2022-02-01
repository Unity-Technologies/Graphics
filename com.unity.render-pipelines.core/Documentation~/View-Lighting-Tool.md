## Light Anchor

The Light Anchor can help to place light sources around subjects, in relation to a Camera and an anchor point. It's particularly effective for cinematic lighting, which often requires multiple light sources orbiting a subject.

## Using the Light Anchor Component

To use the Light Anchor, you must set the Tag of at least one Camera to "MainCamera". By default, the Anchor's position will be the same as the position of the GameObject the Light Anchor Component is attached to.

Use the **Orbit** and **Elevation** to control the orientation of the light, in degrees, relative to the main Camera's and Anchor's positions. If the Light has a Cookie or an IES Profile, use the **Roll** to change their orientation. Use the **Distance** to control how far from the anchor, in meters, you want to place the Light.

Using the **Anchor Position Override**, you can provide a custom GameObject as an anchor point for the light. This is useful if you want the light to follow a specific GameObject in the Scene.

You can set a **Position Offset** for this custom Anchor. This is useful if the Transform position of the custom Anchor isn't centered appropriately for the light to orbit correctly around the custom Anchor.

![](Images/view-lighting-tool-light-anchor0.png)

The Light Anchor component also includes a list of **Presets** that you can use to set the Light's orientation relative to the main Camera.
