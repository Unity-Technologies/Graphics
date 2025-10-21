# Generate six-way lightmap textures for visual effects

Create six-way lightmap textures in your preferred VFX tool for use in Unity.

To create and export six-way lightmap textures for Unity, follow these steps:

1. Simulate or paint the smoke or cloud effect in your preferred VFX tool.
2. Set six lighting directions: top, bottom, left, right, front, and back.
3. Render the lighting result from each direction.
4. Pack the lighting data into two textures:
    - In the first texture, set the red, green, and blue channels to store lighting data, and set the alpha channel to store transparency.
    - In the second texture, set the red, green, and blue channels to store lighting data, and set the alpha channel to store emissive or scattering effects.
5. Export the textures in a format that Unity supports.

After you complete these steps, you can use the six-way lightmap textures in Unity.
