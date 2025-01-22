# Underwater view

To change the area where the camera displays an underwater view for a non-infinite water surface, use the **Volume Bounds** setting. Follow these steps:

1. Create a GameObject with a collider component, for example a cube with a **Box Collider** component.
2. Place the GameObject where you want the underwater view to be visible.
3. In the collider component, select **Edit Collider** to set the size of the visible underwater area. 
4. Select the water GameObject.
5. In the **Inspector** window, under **Appearance**, under **Underwater**, set **Volume Bounds** to the GameObject you created.

To set the area of the underwater view for an ocean, follow these steps:

1. Select the ocean GameObject.
2. In the **Inspector** window, under **Appearance**, enable **Underwater**.
3. Adjust **Volume Depth**.

If you look directly upward at the water surface from below, you may see a square border around the scene view. This is normal. It is because HDRP can only use screenspace data underwater.

# Additional resources
* [Settings and properties related to the Water System](WaterSystem-Properties.md)
