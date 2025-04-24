# Understand eyes

## Eye anatomy

When rendering eyes, it’s helpful to become familiar with their biological structure to produce a realistic outcome.
* The **Iris** is the flat, colored, ring that surrounds the Pupil. It sits underneath the Cornea.
* The **Cornea** is the transparent lens on top of the Iris. It reflects and focuses light into the Pupil.
* The **Pupil** is the opening in the Iris that allows light to pass into the eye and reach the retina.
* The **Limbus**, or the Limbal Ring, is the darkened bordering region between the Cornea and the Sclera.
* The **Sclera** is the opaque, protective outer layer of the eye.

![Front and side views of an eye, with the five areas labelled.](Images/eye-shader-anatomy.png)

## Eye materials

Use the Eye shader or the Eye Shader Graph as the starting point for rendering eyes in the High Definition Render Pipeline (HDRP). They model a two-layer material, in which the first layer describes the cornea and fluids on the surface, and the second layer describes the sclera and the iris, visible through the first layer. They supports various effects, such as cornea refraction, caustics, pupil dilation, limbal darkening, and subsurface scattering.

![Six floating eyeballs, each with a different-colored iris and a different-sized limbus.](Images/HDRPFeatures-EyeShader.png)

Under the hood, the Eye shader is a pre-configured Shader Graph. 

Refer to [Create an eye material](create-an-eye-material.md) for more information.
