# Light Anchor

![](Images/LightAnchor0.png)

You can use a Light Anchor to light a scene in rendered Camera Space. You can also attach a transform to the Light Anchor through the **Anchor Position Override** field so the light will automatically move and rotate to match the selected lighting setup.

![](Images/LightAnchorAnimation.gif)

Note that the reference camera used to adjust the light rotation is the Main Camera. It means that using the Common presets may lead to a different result in the Scene View if you're view is not aligned with the Main Camera.

## Properties

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Orbit | Use the left icon to control the Orbit of the light. This tool becomes green when you move the icon. |
| Elevation | Use the middle icon to control the Elevation of the light. This tool becomes blue when you move the icon. |
| Roll | Use the right icon to control the Rollof the light. This tool becomes gray when you move the icon. This is especially useful if the light has an IES or a Cookie. |
| Distance | Controls the distance between the light and its anchor in world space. |
| Up Direction | Defines the space of the up direction of the anchor. When this value is set to Local, the Up Direction is relative to the camera. |
| Anchor Position Override | Allows you to use a GameObject's [Transform](https://docs.unity3d.com/ScriptReference/Transform.html) as anchor position instead of the LightAnchor's Transform. When the Transform of the GameObject you assigned to this property changes, the Light Anchor's Transform also changes. |
| Common | Assigns a preset to the light component based on the behaviour of studio lights. |
