# Miscellaneous shaders
#### Blockout Grid
Apply this simple shader to a 1 meter cube.  You can then scale and stretch the cube to block out your level.  The grid projected on the cube doesn't stretch but maintains its world-space projection so it's easy to see distances and heights.  It's a great way to block out traversable paths, obstacles, and level layouts. Turn on the **EnableSlopeWarning** parameter to shade meshes red where they’re too steep to traverse.
#### Ice
This ice shader uses up to three layers of parallax mapping to create the illusion that the cracks and bubbles are embedded in the volume of the ice below the surface though there is no transparency or actual volume. It also uses a Fresnel effect to brighten the edges and create a frosted look.