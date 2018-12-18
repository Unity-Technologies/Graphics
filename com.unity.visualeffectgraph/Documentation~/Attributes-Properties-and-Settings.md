## Attributes

Attributes are the properties of every element of a simulation. For particles, attributes describes how each particle behaves during its simulation.

#### Reading and Writing attributes

Depending on the block you use. its configuration, and optionally operators connected to it, the system will require reading or writing attributes. If the context does not already require this attribute, it will be automatically required. 

> **Default Value :** If a block or an operator attempts to read the value of an attribute that has not been set, it will read its default value instead.
>

#### Storing Attributes

Attributes are not automatically stored as long as they are used in the simulation. The storage of attributes enable these to persist across time and be integrated into the simulation. For instance, particle position, if it depends on a velocity. 

<u>The attribute storage depends on the following:</u>

* Attributes written in initialize and/or update and read in update / output will result in being stored. 
* Attributes **read and written exclusively in initialize or update** but not read afterwards are **not stored.**
* Attributes **written exclusively in output** are **not stored** either.

#### Optimizing Attributes

There are some requirements for an attribute to be stored, and the graph compiler tries to store as few attributes as required, however the system will store an attribute if you use it under the right conditions, even if a more optimal solution exits. Also, memory gain has a performance tradeoff you can control by choosing where to compute and whether to store attributes or not.

> *For example:* In a mesh particle system, writing color and alpha (over life) in update will result in color and alpha being stored. As color and alpha are read by the particle mesh output to set color to particles.
>
> Using the Color and Alpha over Life in output will result in these two attributes being **not stored**, but instead Age and Lifetime attributes shall become part of the stored attributes (if they were not already). This helps using 2 floats instead of 4, per particle.
>
> In this case, The tradeoff for this memory gain is, the color over life will be computed, not for every particle, but for every vertex of every mesh particle. Which could result in degraded performance if too many vertices are present for each particle.

#### Commonly used Attributes

* **Position** (float3) : Position of the particle
* **Velocity** (float3) : Velocity vector of the particle
* **Color** (float3) / Alpha (float) : RGB Color of the particle / Transparency of the particle
* **Size** : Uniform size of the particle. Default is 0.1
* **Scale** : (variadic: float, float2 or float3) : Non-uniform scale of the particle. Applies as a multiplier to the Size attribute. Default is 1.
* **Age** (float) and **Lifetime** (float) of the particle, age is the time lived since the birth, lifetime is the expected age of death.
* **Angle** (float3) : Yaw, pitch and roll of the particle.

#### Other commonly used Attributes:

- **TargetPosition** (float3) : Expected end of the particle, can be used :
  - for **line output** to define the other end of the line, 
  - to define an expected target at birth and compute forces in **update**
  - to orient a particle using the **connect target** output block
- **OldPosition** (float3) : Container for old position, is not automatically set but can be used to store previous particle position
- **texIndex** (float) : animation progress used for flipbooks. Can be updated using the **flipbook player** update block

#### Custom Attributes

You can use the Experimental Operator : Get Custom Attribute and the Experimental Block : Set Custom Attribute to perform storage and reads from custom attributes.

### Attribute Location

Attributes can be referred to from their location. Depending on the location you can access attributes from the current simulation or attributes from events.

#### Current

Current Location is the default for the Attribute Operator, it will try to fetch the value from the current simulation. If the attribute is not used, it will be marked as read and its default value will be returned.

#### Source

Source refers to the event that triggered the spawn of new particles. As such, Source attributes are only available in *Initialize* contexts. In this context, source attribute can refer to :

* SpawnEvent attribute set in spawn context, or directly sent with an event with the `SendEvent()` component API.
* Some attribute from a system that triggered a GPU Event.

## Properties & Settings

Properties and settings are the user-side elements that help configure contexts, blocks and operators. Properties are connectable using the graph while settings are just static UI elements that will trigger more static behavior.

![](Images/settings-properties.png)

While properties are always exposed to the graph, some settings can be present in the graph, and some other (less used) can be present in the inspector, and be accessed while the element is selected in the graph.

> While changing values of a property is done seamlessly, changing the value of a Setting will trigger a system recompile.

## Property Types

Properties in VFX Editor can be of native Unity types such as float, bool, Vector3, or Animation Curve. 

#### Spaceable Properties

Some properties also convey a "space" information so the graph is aware that this property's data is expressed in a certain reference space.  

> Example : Position property type contains a Vector3 data and a space. So a position that contains a local (1,0,0) data, that will be fed into a world-space particle system, will have its value automatically computed from local to world.

Spaces traverse the operator graph as long as the types permit it. So, as long as the output type conveys a space, the value can be converted. 

Spaces can be **local** (to the object that contains the visual effect instance) or **world**

Spaces also convey the type of transformation : **position**, **direction** or **matrix**

#### Base Property Types

| Type                         | Description                                   |
| ---------------------------- | --------------------------------------------- |
| bool                         | Boolean (1 bit)                               |
| float                        | 32 bit float                                  |
| Vector2, Vector3, Vector4    | floating point (32 bit) vectors of size 2,3,4 |
| Color                        | floating point 4-channel HDR Color (RGBA)     |
| int                          | 32 bit signed integer                         |
| uint                         | 32 bit unsigned integer                       |
| Texture2D                    | Texture 2D type                               |
| Texture3D                    | Texture 3D type                               |
| Cubemap                      | Cubemap Type                                  |
| AnimationCurve               | Animation Curve type (holds curve data)       |
| Gradient                     | Gradient type (holds color curve data)        |
| Texture2DArray, CubemapArray | Arrays of textures                            |
| Mesh                         | Mesh data                                     |

#### Compound Property Types

Some types used in the VFX Editor are composed from other types : for instance the AABox (Axis-Aligned Box) type is a Spaceable Box that aligns to XYZ axes and cannot be rotated. This box is defined by a center position and a size.

![](Images/expand-types.gif)

While using types, you can press the + next to the connector to expand the field and access sub-elements. It is a convenient way to connect sub-properties if you do not require to connect a full type.

| Type        | Spaceable? | Description                                               | Contents                                                     |
| ----------- | ---------- | --------------------------------------------------------- | ------------------------------------------------------------ |
| Position    | X          | Position                                                  | Vector3 position                                             |
| Direction   | X          | normalized Direction                                      | Vector3 direction                                            |
| Vector      | X          | Vector                                                    | Vector3 vector                                               |
| Circle      | X          | non-oriented Circle                                       | Vector3 center / float radius                                |
| ArcCircle   | X          | Arc from a non-oriented Circle                            | Circle circle / float arc (angle in radians)                 |
| Sphere      | X          | uniform Sphere                                            | Vector3 center / float radius                                |
| ArcSphere   | X          | uniform Arc of a Sphere                                   | Sphere sphere / float arc (angle in radians)                 |
| OrientedBox | X          | Oriented Box                                              | Vector3 center / Vector3 angles / Vector3 size               |
| AABox       | X          | Axis-Aligned Box                                          | Vector3 center / Vector3 size                                |
| Plane       | X          | Infinite Plane                                            | Vector3 position / Vector3 normal                            |
| Cylinder    | X          | non-Oriented Cylinder                                     | Vector3 center / float radius / float height                 |
| Cone        | X          | non-Oriented Cone                                         | Vector3 center/ float radius0 / float radius1 / height       |
| ArcCone     | X          | Arc from a non-oriented Cone                              | Cone cone / float arc (angle in radians)                     |
| Torus       | X          | Non-Oriented Torus                                        | Vector3 center / float majorRadius / float minorRadius       |
| ArcTorus    | X          | Arc from a Non-Oriented Torus                             | Torus torus / float arc (angle in radians)                   |
| Line        | X          | Line connecting two points                                | Vector3 start / Vector3 end                                  |
| Transform   | X          | Transformation TRS structure (Translation Rotation Scale) | Vector3 position / Vector3 angles / Vector3 scale            |
| CameraType  |            | Camera type                                               | Transform transform / float fieldOfView / float nearPlane / float farPlane / float aspectRatio / float pixelDimensions |
| Flipbook    |            | Rows and Columns for a flipbook texture sheet             | int x, int y                                                 |

