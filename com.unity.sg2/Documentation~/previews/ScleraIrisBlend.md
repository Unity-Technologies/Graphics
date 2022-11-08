## Description
Combines the seperate components of the eye into unified material parameters.

## Input
**Sclera Color** - Color of the sclera at the target fragment.
**Sclera Normal** - Normal of the sclera at the target fragment.
**Sclera Smoothness** - Smoothness of the sclera at the target fragment.
**Iris Color** - Color of the iris at the target fragment.
**Iris Normal** - Normal of the iris at the target fragment.
**Cornea Smoothness** - Smoothness of the cornea at the target fragment.
**Iris Radius** -  The radius of the Iris in the model.
**Position OS** - Position of the current fragment to shade in object space
**Diffusion Profile Sclera** - Diffusion profile used to compute the subsurface scattering of the sclera.
**Diffusion Profile Iris** - Diffusion profile used to compute the subsurface scattering of the iris.

## Output
**Eye Color** - Final Diffuse color of the Eye.
**Surface Mask** - Linear, normalized value that defines where the fragment is.
**Diffuse Normal** - Normal of the diffuse lobes.
**Specular Normal** - Normal of the specular lobes.
**Eye Smoothness** - Final smoothness of the Eye.
**Surface Diffusion Profile** - Diffusion profile of the target fragment.