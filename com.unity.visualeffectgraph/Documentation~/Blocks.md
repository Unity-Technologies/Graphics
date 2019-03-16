Here is a comprehensive list of blocks of the Visual Effects Standard Library

## Attribute Blocks

Attribute Blocks are the most basic blocks and also the most full-featured. They are aimed to serve general-purpose attribute computation from various cases. 

![](Images/block-attribute.PNG)

These blocks and their configuration are detailed in the [VFX Attribute Blocks](VFX-Blocks-Attribute) of this help.

## Spawn Blocks

Spawn blocks are dedicated to the spawn context and help process SpawnEvent attributes. 

These blocks and their configuration are detailed in the [VFX Spawn Blocks](VFX-Blocks-Spawn) of this help.



## Collision Blocks

Colliders enable particle collision on various primitives and elements. Particle Collisions share common features to alter behaviour of particles.

| Property/Setting                  | Description                                                  |
| --------------------------------- | ------------------------------------------------------------ |
| Mode (Enum setting)               | Primitive collision mode : Solid / Inverted. Interprets the primitive volume as a solid or an empty space. |
| Radius Mode (Enum setting)        | Enables auto computation of the collision detection radius or use of |
| Rough surface (Bool setting)      | Enables rough collision response (randomness in bounce direction) adds a Roughness float input property (0..1) |
| Elasticity (float input property) | Amount of bounciness and energy restitution after collision. 1.0 means 100% restitution. |
| Friction (float input property)   | Amount of energy absorption when sliding on surface.         |
| Lifetime loss                     | Percentage of lifetime automatically added to the age every time the particle collides with the primitive. |

#### Sphere Collider

Collides with a sphere primitive

#### Plane Collider

Collides with an infinite plane primitive

####AABox Collider

Collides with an axis-aligned box

#### Cylinder Collider

Collides with a cylinder

#### Signed Distance Field Collider

Collides with a signed DistanceField baked into a 3D Texture Distance fields can be transformed using a TRS Matrix

## Flipbook Blocks

#### Flipbook Player

Flipbook Player is an Update-only block that performs basic **texIndex** advance over time, based on a frame rate. 

Frame rate can be adjusted in two different ways based on the *Mode* setting : using a float Input property or a curve input Property sampled with the age/lifetime ratio.



## Force Blocks

Forces applies to particle velocity in different ways : **Absolute** forces apply directly without particle resistance, **Relative** forces apply to the particle until a stable point (such as wind), **Drag** forces apply to the particle so it resists all other forces (air resistance).

Forces depend mostly of the current velocity of the particle, and its **mass** attribute. 

#### Force

Force represents a linear force, applied to the particle, based on its **mass**. The Mode setting lets you choose between **Absolute** and **Relative** force.

The **Force** input property lets you determine the force vector applied to particle. Of course you can use operators to perform per-particle Force vector computation.

#### Gravity

Gravity is an **absolute force** that applies to particles *regardless of their **mass***. It represents the gravity vector in a vacuum environment. To reproduce gravity in atmospheric environment, use a **Drag** force alongside the gravity vector.

Default value is -9,81 squared units per second on Y axis, which represents earth gravity.

#### Linear Drag

Drag force represents the linear **resistance to the medium** (for instance, air) of the particle. It relies on the **mass** attribute and a *drag coefficient* input property to compute the amount of resistance applied to the particle and speed it down.

The *Use Particle Size* setting enables the use of particle **sizeX** attribute to alter the drag coefficient.

#### Turbulence

Turbulence applies procedural noise turbulence to particles. Noise field can be transformed using a TRS.

<u>Noise force is described by the following properties:</u>

* Field Transform : Transform applied to the noise field
* Num Octaves : Number of octaves used for the noise. The more octaves, the more processing will be required to compute the noise.
* Roughness : Influence of higher octaves (smaller noise) compared to the lower octaves.
* Intensity : Influence scale of the turbulence.
* Drag coefficient : Resistance of the particle to the turbulence field. 0 means absolute force. 

#### Vector Field Force

Vector field force applies forces fetched from a 3D Texture containing vector data. Depending on data encoding setting, you can choose **UnsignedNormalized** if your texture data is centered on 0.5 (middle gray), or **Signed** if your texture contains actual absolute, signed data.

> Unsigned normalized should be used if you encode your textures like normal maps, in 8 bits per-channel, linear textures. Signed should be used if you encode your texture as RGBAHalf or RGBAFloat 

<u>Vector field force is described by the following properties:</u>

- Field Transform : Transform applied to the noise field
- Intensity : Influence scale of the turbulence.
- Drag coefficient : Resistance of the particle to the turbulence field. 0 means absolute force.

#### Conform to Sphere

Conform to sphere applies an attraction force towards a sphere primitive and tries to retain particles onto the surface using a stick force.

<u>Properties:</u>

* **Sphere** : the sphere to conform to.
* **Attraction Speed** : relative speed used to attract particles to the sphere
* **Attraction Force** : relative force used to attract particles to the sphere
* **Stick Distance** : distance to the surface of the sphere where stick force applies
* **Stick Force** : force applied to particles within the range of the surface and the stick distance, that will try to retain particles to the surface

#### Conform to Signed Distance Field

Conform to sphere applies an attraction force towards a Signed distance field and tries to retain particles onto the surface using a stick force.

<u>Properties:</u>

- **Distance Field** : A 3D Texture containing SDF data, normalized to the UVW range.
- **Field Transform** : the transform applied to the signed distance field.
- **Attraction Speed** : relative speed used to attract particles to the SDF
- **Attraction Force** : relative force used to attract particles to the SDF
- **Stick Distance** : distance to the SDF surface of the sphere where stick force applies
- **Stick Force** : force applied to particles within the range of the SDF surface and the stick distance, that will try to retain particles to the surface

## Kill Blocks

Kill blocks destroy particles based on different conditions. Blocks use a Mode similar to the Collision blocks to decide whether the primitive is **Solid** or **Inverted** (Hollow inside a solid world).

Kill blocks set the **alive** Attribute to false if the condition is met. So automatic reaping of particles is required.

#### Kill (AABox)

Kills particles if they are inside/outside an Axis-Oriented box

#### Kill (Plane)

Kills particles if they go below/above an infinite plane.

## Orientation Blocks

Orientation blocks are intended to control particle orientation.

#### Orient

Orient block is a general-purpose block intended to setup particle orientation for most common cases. It uses a Mode enum setting to setup with various behaviors:

* FaceCameraPlane : Standard camera plane facing, particles stay aligned to the near plane so they do not bend regardless of their position on screen. Up axis is cameraUp
* FaceCameraPosition : Camera position facing, particles look at camera position so they tend to orient themselves in a circular way. Up axis is cameraUp
* LookAtPosition : Particles orient themselves towards a given position input property, with camera Up Axis
* LookAtLine : Particles orient themselves towards a given position, which is the closest point to the particle on a given line input property, with camera Up Axis
* Fixed Orientation : Particles are oriented using a given front axis and a given up axis determined by input properties
* Fixed Axis : Particles orient themselves towards camera, with a given up axis as input property
* Along Velocity : Particles orient themselves towards camera with up axis corresponding to their normalized velocity.

#### Connect Target

Particles orient towards camera and scale themselves towards a given position (as input property), Also the pivot shift property enables setting the pivot at given relative position between the bottom and the top of the particle. (So applying a scale afterwards will change the Y pivot from the start position to the target position)

## Output Blocks

#### Camera Fade

Camera Fade alters the particle color and/or alpha (based on the *fadeMode* setting) depending on two distances:

* Faded Distance : distance from camera at which the particle will be totally faded out
* Visible Distance : distance from camera at which the particle will be totally not faded out.

Distances can be swapped to particles fade when they are near, or far from the camera. 

**Optimization** : Particles that are fully faded becomes culled if the *CullWhenFaded* bool setting in the inspector is activated.

#### Subpixel AA

Subpixel Anti Aliasing prevents small particles from becoming smaller than one pixel, and clamps them to one pixel size. If the particle needed to clamp it size, Alpha is modulated accordingly to simulate the loss of size.

## Position Blocks

Position blocks enable setting position for particles depending on configurable shapes. They share the following settings:

* **Mode** (enum) : this mode enables to configure the spawn mode for the shape
  * **Surface** : Positions are evenly computed from the surface of the shape
  * **Volume** : Position are evenly computed from the volume of the shape
  * **ThicknessRelative** : Positions are evenly computed from a thick surface relative to the size of the shape
  * **ThicknessAbsolute** : Positions are evenly computed from a thick surface with absolute thickness.
* **Spawn Mode** (enum) : Controls the order of spawn for particles
  * Randomize : Position is computed randomly
  * Custom : Position is computed from a sequencer (Eg arc sequencer)  so progression can be controlled.

#### Position : AABox

Positions are computed from an axis-oriented box

#### Position : Circle

Positions are computed from a circle / disc

####Position : Line

Positions are computed from a line

#### Position : Sphere

Positions are computed from a uniform sphere

#### Position : Cone

Positions are computed from a Cone / Cylinder

####Position : Torus

Positions are computed from a torus

## Size Blocks

#### Screen-Space Size

Screen-space size computes the size of the particle in screen-space units or screen-space ratio depending on a *sizeMode* setting:

* **PixelAbsolute** : SizeX is set to match actual pixel size, in any resolution.
* **PixelRelativeToResolution** : SizeX is set to match actual pixel size, at given resolution, and shall be resized according to actual resolution.
* **RatioRelativeToWidth** : Calculates a screen-space size for SizeX relative to the screen width (for instance 0.5 will create square particle which width and height equals half of the width of the screen)
* **RatioRelativeToHeight** : Calculates a screen-space size for SizeX relative to the screen height (for instance 0.5 will create square particle which width and height equals half of the height of the screen)
* **RatioRelativeToHeightAndWidth** : Calculates a screen-space size for SizeX relative to the width of the screen and a screen-space size for SizeY relative to the height of the screen.

## Velocity Blocks

Velocity blocks computes velocity for particles from various logic.

#### Velocity : Direction

Calculates velocity from direction attribute set by the Position : Shape blocks

#### Velocity : Randomize

Calculates random velocity and blends it to the current velocity

#### Velocity : Speed

Resets the speed of the current particle velocity

#### Velocity : Spherical

Sets the particle velocity towards or away from a given point.



