# Atmospheric Scattering

Atmospheric scattering is the phenomena that occurs when particles suspended in the atmosphere diffuse (or scatter) a portion of the light passing through them in all directions.

Examples of natural effects that cause atmospheric scattering include fog, clouds, or mist. 

The High Definition Render Pipeline (HDRP) simulates a [fog](Override-Fog.md) effect by overlaying a color onto objects, depending on their distance from the Camera. This is good for simulating fog or mist in outdoor environments. You can use it to hide the clipping of far away GameObjects, which is useful if you reduce a Camera’s far clip plane to enhance performance.

HDRP implements an exponential fog, where density varies exponentially with distance from the Camera. All Material types (Lit or Unlit) react correctly to the fog. HDRP calculates fog density depending on the distance from the Camera, and the world space height.

Instead of using a constant color, fog can use the background sky as a source for color. In this case, HDRP samples the color from different mipmaps of the cubemap generated from the current sky settings. The chosen mip varies linearly between the lowest resolution and the highest resolution mipmaps, depending on the distance from the Camera and the values in the fog component’s **Mip Fog** properties. You can also choose to limit the resolution of the highest mip that HDRP uses. Doing this adds a volumetric effect to the fog and is much cheaper to use than actual volumetric fog.

Optionally, you can enabled volumetric fog for GameObjects close to the camera. It realistically simulates the interaction of lights with fog, which allows for physically-plausible rendering of glow and crepuscular rays, which are beams of light that stream through gaps in objects like clouds and trees from a central point.
