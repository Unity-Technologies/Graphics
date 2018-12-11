Operators are used to perform computation for block properties and compute custom behavior based on mathematical expressions.

##Attribute Operator

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/attribute-operator.png)

The attribute operator is used to read a current attribute, or a source attribute. Current attribute values are those at the time of execution of the graph. Source attributes are not part of the simulation but instead part of the event that created these particles.

#### Attributes and Execution of the graph

While reading an attribute value in an operator graph, It is important to know which will be the value at a given time. 

The rule is simple : 

> Any attribute operator will fetch the attribute value at the time of execution of the block that reads it.

<u>Example 1:</u>

To illustrate this, here is an example where we set the color of particles depending on the speed of the particle.

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/attribute-execution-1.png)

In this example, the value of the velocity attribute will be fetch during the execution of the **Set color** block. **Set velocity (Random)** will already have been executed, and the velocity values already set.

If the Set Color was placed before the Set Velocity, the value read would be the default velocity, so particles would all have the same color.

<u>Example 2:</u>

Now, what if a part of graph is used for two nodeblocks? Given our rule, the graph will be executed once for the first set color, then another time for the next set color. But the velocity will have been modified beforehand, so it will be zero the second time.

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/execution-order-attribute.gif)

## Parameters

Parameter operators are references to parameters created into the Blackboard panel. 

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/operator-parameter.gif)

You can create them in various ways:

* By dragging an already created parameter into the graph
* By selecting `Parameter (Type)` into the node creation menu (this will also create a new entry in the blackboard)
* By converting an inline operator to a parameter (see Inline Operators)

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/operator-inline-to-parameter.gif)

The green dot next to a parameter name means it's exposed to the component level.

## Inline

Inline operators are constant values or proxies you can use to store or assemble data. 

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/operator-inline.png)

You can create them through the node creation menu. Or by converting a parameter node to an inline node.

![](https://raw.githubusercontent.com/wiki/Unity-Technologies/ScriptableRenderPipeline/Pages/VFXEditor/img/operator-parameter-to-inline.gif)

## Operator Library

Operator Library contains a variety of nodes to process data. Here is a list with a brief description:

| Category          | Name                      | Description                                                  |
| ----------------- | ------------------------- | ------------------------------------------------------------ |
| Bitwise           | And                       | Performs a bitwise AND to the values                         |
|                   | Complement                | Invert bits to the value                                     |
|                   | Left Shift                | Shift bits leftwards                                         |
|                   | Or                        | Performs a bitwise OR to the values                          |
|                   | Right Shift               | Shift bits rightwards                                        |
|                   | XOR                       | Performs a bitzise XOR (Exclusive OR) to the values          |
| Builtin           | DeltaTime                 | Returns the current deltaTime for the execution of the context |
|                   | LocalToWorld              | Returns the LocalToWorld matrix for the gameObject that holds this Visual Effect |
|                   | MainCamera                | Returns a Camera Info structure with the main camera currently used |
|                   | SystemSeed                | Returns the current System Seed                              |
|                   | TotalTime                 | Returns the current effect TotalTime (time since the last Play() event) |
|                   | WorldToLocal              | Returns the WorldToLocal matrix for the gameObject that holds this visual Effect |
| Color             | Color Luma                | Returns the luminance of the input color                     |
|                   | HSV to RGB                | Converts a HSV vector to RGB value                           |
|                   | RGB to HSV                | Converts a RGB value to HSV vector                           |
| Logic             | And                       | Performs a and between two booleans                          |
|                   | Branch                    | Selects one branch or another based on a boolean condition   |
|                   | Compare                   | Compares values and returns a boolean value corresponding to the test result. |
|                   | Nand                      | Performs a not-and between two booleans                      |
|                   | Nor                       | Performs a not-or between two booleans                       |
|                   | Not                       | Performs a not to the boolean input                          |
|                   | Or                        | Performs a or between two booleans                           |
| Math/Arithmetic   | Absolute                  | Returns the absolute value of the input                      |
|                   | Add                       | Adds the inputs together                                     |
|                   | Divide                    | Divides the inputs together                                  |
|                   | Fraction                  | Returns the fractional part of the input                     |
|                   | Lerp                      | Performs a linear interpolation between two values, using an interpolation value |
|                   | Modulo                    | Returns the value of A modulo B                              |
|                   | Multiply                  | Multiplies the inputs together                               |
|                   | OneMinus                  | Performs a 1-X operation                                     |
|                   | Power                     | Performs a A exponent B operation                            |
|                   | Reciprocal (1/x)          | Returns the reciprocal of the value (1/x)                    |
|                   | Sign                      | Returns the sign of the value (-1 if negative, 0 if null, 1 if positive) |
|                   | Smoothstep                | Performs a smoothstep operation over the value               |
|                   | Square Root               | Returns the square root of the value                         |
|                   | Step                      | Return 0 if A < B, 1 otherwise                               |
|                   | Subtract                  | Subtracts inputs together                                    |
| Math/Clamp        | Ceiling                   | Returns the first integer greater or equal to the value      |
|                   | Clamp                     | Clamps the value to a Minimum and a Maximum                  |
|                   | Discretize                | Discretizes the value to a given steps                       |
|                   | Floor                     | Returns the first integer lesser or equal to the value       |
|                   | Maximum                   | Returns the maximum between two values                       |
|                   | Minimum                   | Returns the minimum between two values                       |
|                   | Round                     | Rounds the value to the closest integer                      |
|                   | Saturate                  | Clamps the value to the 0 .. 1 range                         |
| Math/Constants    | Epsilon                   | Returns the Epsilon constant (really small value)            |
|                   | Pi                        | Returns various multiple of PI                               |
| Math/Coordinates  | Polar to Rectangular      | Converts a 2D or 3D polar coordinate to 2D or 3D rectangular coordinate |
|                   | Rectangular to Polar      | Converts a 2D or 3D rectangular coordinate to 2D or 3D polar coordinate |
|                   | Rectangular to Spherical  | Converts a 2D or 3D rectangular coordinate to 2D or 3D spherical coordinate |
|                   | Spherical to Rectangular  | Converts a 2D or 3D spherical coordinate to 2D or 3D rectangular coordinate |
| Math/Geometry     | Area (Circle)             | Computes the area of a circle                                |
|                   | Distance (Line)           | Computes the shortest distance of a point to a line          |
|                   | Distance (Plane)          | Computes the shortest distance of a point to a plane         |
|                   | Distance (Sphere)         | Computes the shortest distance of a point to a sphere        |
|                   | Transform (Direction)     | Transforms a direction vector with a matrix                  |
|                   | Transform (Position)      | Transforms a position with a matrix                          |
|                   | Transform (Vector)        | Transforms a vector with a matrix                            |
|                   | Volume (AABox)            | Returns the volume of a axis-aligned box                     |
|                   | Volume (Cone)             | Returns the volume of a cone                                 |
|                   | Volume (Cylinder)         | Returns the volume of a cylinder                             |
|                   | Volume (Oriented Box)     | Returns the volume of an oriented box                        |
|                   | Volume (Sphere)           | Returns the volume of a sphere                               |
|                   | Volume (Torus)            | Returns the volume of a torus                                |
| Math/Remap        | Remap                     | Remaps a value from an input range to an output range        |
|                   | Remap [0..1]=>[-1..1]     | Remaps a value from 0..1 range to a -1..1 range (corresponds to a (X*2)-1) |
|                   | Remap [-1..1]=>[0..1]     | Remaps a value from 0..1 range to a -1..1 range (corresponds to a (X*0.5)+0.5) |
| Math/Trigonometry | Cosine                    | Returns the cosine of the input value (in radians)           |
|                   | Sine                      | Returns the sine of the input value (in radians)             |
|                   | Tangent                   | Returns the tangent of the input value                       |
| Math/Vector       | AppendVector              | Builds multi-component vectors from any number of scalars or vectors |
|                   | CrossProduct              | Returns the cross product between two vectors                |
|                   | Distance                  | Returns the distance between two points                      |
|                   | Dot Product               | Returns the dot product between two vectors                  |
|                   | Length                    | Returns the length (magnitude) of a vector                   |
|                   | Normalize                 | Normalizes the input vector (Returns a vector of same direction and of length 1) |
|                   | Rotate 2D                 | Rotates a 2D position of a given angle around a given position |
|                   | Rotate 3D                 | Rotates a 3D position of a given angle, around an axis and center of rotation |
|                   | Sample Bezier             | Returns an interpolated position around 4 bezier points      |
|                   | Squared Distance          | Returns the squared distance between two points              |
|                   | Squared Length            | Returns the squared length of a vector                       |
|                   | Swizzle                   | Use a swizzle pattern (eg `xyz`, `zyxx` ) to build a vector from another vector components. |
| Math/Wave         | Sawtooth wave             | Samples a sawtooth wave of given period and amplitude bounds |
|                   | Sine wave                 | Samples a sine wave of given period and amplitude bounds     |
|                   | Square wave               | Samples a square wave of given period and amplitude bounds   |
|                   | Triangle wave             | Samples a triangle wave of given period and amplitude bounds |
| Random            | Random Number             | Performs a random number computation between a min and a max value, with various seed options. |
| Sampling          | Sample Curve              | Samples an AnimationCurve at given time and returns its value |
|                   | Sample Gradient           | Samples a gradient at given position and returns its color value |
|                   | Sample Texture2D          | Samples a texture 2d at given position and LOD, and returns the corresponding value |
|                   | Sample Texture3D          | Samples a texture 3d at given position and LOD, and returns the corresponding value |
|                   | Sample TextureCube        | Samples a cubemap at given position and LOD, and returns the corresponding value |
|                   | Sample Texture2DArray     | Samples a texture 2d array at given index, position and LOD, and returns the corresponding value |
|                   | Sample TextureCubeArray   | Samples a cubemap array at given index, position and LOD, and returns the corresponding value |
| Time              | Periodic Total Time       | Returns a recurring lapse of time every N seconds, with given range. |
|                   | Total-time (per particle) | Returns the total time with a fraction of the delta time randomized per-particle. Use this time node to randomize sub-frame position computations and prevent discrete stepping. |
