## View Lighting tool

The View Lighting tool allows you to set up lighting in camera space. It uses the main Camera (the one that renders to the Game view) to set the orientation of the Light using spherical coordinates, and it uses a target and distance to set the position of the Light.

Since the View Lighting tool uses camera-space, it is especially useful when setting up lighting for cinematics.

To be able to use the view lighting tool, we need to have at least one camera tagged as "MainCamera" (only one will be used).

## Using the View Lighting tool

The View Lighting tool uses the Light Anchor component to position and orient the Light. When this component is attached to a Light, you can use a custom Scene view gizmo to position the Light, and use the component's Inspector to orient the Light.

To get started with the View Lighting tool:

1. In the Scene view or Hierarchy, select a GameObject with an attached Light component.
2. In the Inspector click **Add Component > Rendering > Light Anchor**.
3. To use the Light Anchor gizmo, click the custom tool button then, in the drop-down, click **Light Anchor**.<br/>![](Images/view-lighting-tool-gizmo.png)
4. You can now use the gizmo to move the Light's target. This gizmo also shows you a visualization of the yaw, pitch, and roll from the target to the Light. To change the yaw, pitch, and roll, as well as the distance from the Light to the target, see [Light Anchor](#light-anchor).

### Light Anchor

The Light Anchor controls the Light's orientation and the distance it is from the target position. **Yaw** and **Pitch** control the orientation and **Distance** controls the distance between the Light and the target. If the Light has a cookie or uses [IES](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/IES-Profile.html), **Roll** controls the orientation of the cookie or IES profile.

To quickly reset the angle of a knob to zero. Right-click on it.

![](Images/view-lighting-tool-light-anchor0.png)

The Light Anchor component also includes a list of **Presets** that you can use to quickly set the Light's orientation relative to the Game view.
