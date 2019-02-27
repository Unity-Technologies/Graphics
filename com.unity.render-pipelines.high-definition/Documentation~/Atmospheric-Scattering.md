# Atmospheric Scattering

Atmospheric scattering is the phenomena that occurs when particles suspended in the atmosphere diffuse (or scatter) a portion of the light passing through them in all directions.

Examples of natural effects that cause atmospheric scattering include fog, clouds, or mist. 

HDRP simulates a fog effect by overlaying a color onto objects, depending on their distance from the Camera. This is good for simulating fog or mist in outdoor environments. You can use it to hide the clipping of far away GameObjects, which is handy if you reduce a GameObject’s distance to a Camera’s far clip plane to enhance performance.

In HDRP, you can choose between different types of fog; Linear, Exponential, or [Volumetric](Volumetric-Fog.html). All Material types (Lit or Unlit) react correctly to the fog. HDRP calculates density differently, depending on the type of fog, the distance from the Camera, and the world space height.

Instead of using a constant color, Linear and Exponential fog can use the background sky as a source for color. In this case, HDRP samples the color from different mipmaps of the cubemap generated from the current sky settings. The chosen mip varies linearly between the lowest resolution and the highest resolution mipmaps, depending on the distance from the Camera and the values in the fog component’s **Mip Fog** properties. You can also choose to limit the resolution of the highest mip that HDRP uses. Doing this adds a volumetric effect to the fog and is much cheaper to use with Linear or Exponential fog than it is to use the Volumetric fog type.