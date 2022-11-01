# Underwater view

To view non-infinite water surfaces from underwater, you have to specify a [collider](https://docs.unity3d.com/Manual/Glossary.html#Collider). You can either use the box collider HDRP automatically provides or select a box collider in the scene to use for this purpose.

To view infinite water surfaces from underwater, you have to specify a **Volume Depth**.

If you look directly upward at the water surface from below, you may see a square border around the scene view. This is normal. It is because HDRP can only use screenspace data underwater.

# Additional resources
* [Settings and properties related to the Water System](WaterSystem-Properties.md)
