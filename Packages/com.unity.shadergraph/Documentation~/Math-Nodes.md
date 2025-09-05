# Math nodes

Perform a mathematical operations, from basic arithmetic to advanced functions like trigonometry, vectors, matrices, interpolation, and waves.

## Advanced

| **Topic**                                                | **Description**                                                                |
|----------------------------------------------------------|--------------------------------------------------------------------------------|
| [Absolute](Absolute-Node.md)                             | Returns the absolute value of input In.                                        |
| [Exponential](Exponential-Node.md)                       | Returns the exponential value of input In.                                     |
| [Length](Length-Node.md)                                 | Returns the length of input In.                                                |
| [Log](Log-Node.md)                                       | Returns the logarithm of input In.                                             |
| [Modulo](Modulo-Node.md)                                 | Returns the remainder of input A divided by input B.                           |
| [Negate](Negate-Node.md)                                 | Returns the inverse value of input In.                                         |
| [Normalize](Normalize-Node.md)                           | Returns the normalized vector of input In.                                     |
| [Posterize](Posterize-Node.md)                           | Returns the input In converted into a number of values defined by input Steps. |
| [Reciprocal](Reciprocal-Node.md)                         | Returns the result of 1 divided by input In.                                   |
| [Reciprocal Square Root](Reciprocal-Square-Root-Node.md) | Returns the result of 1 divided by the square root of input In.                |

## Basic

| **Topic**                          | **Description**                                        |
|------------------------------------|--------------------------------------------------------|
| [Add](Add-Node.md)                 | Returns the sum of the two input values.               |
| [Divide](Divide-Node.md)           | Returns the result of input A divided by input B.      |
| [Multiply](Multiply-Node.md)       | Returns the result of input A multiplied by input B.   |
| [Power](Power-Node.md)             | Returns the result of input A to the power of input B. |
| [Square Root](Square-Root-Node.md) | Returns the square root of input In.                   |
| [Subtract](Subtract-Node.md)       | Returns the result of input A minus input B.           |

## Derivative

| **Topic**            | **Description**                                                               |
|----------------------|-------------------------------------------------------------------------------|
| [DDX](DDX-Node.md)   | Returns the partial derivative with respect to the screen-space x-coordinate. |
| [DDXY](DDXY-Node.md) | Returns the sum of both partial derivatives.                                  |
| [DDY](DDY-Node.md)   | Returns the partial derivative with respect to the screen-space y-coordinate. |

## Interpolation

| **Topic**                            | **Description**                                                                                                      |
|--------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| [Inverse Lerp](Inverse-Lerp-Node.md) | Returns the parameter that produces the interpolant specified by input T within the range of input A to input B.     |
| [Lerp](Lerp-Node.md)                 | Returns the result of linearly interpolating between input A and input B by input T.                                 |
| [Smoothstep](Smoothstep-Node.md)     | Returns the result of a smooth Hermite interpolation between 0 and 1, if input In is between inputs Edge1 and Edge2. |

## Matrix

| **Topic**                                          | **Description**                                                           |
|----------------------------------------------------|---------------------------------------------------------------------------|
| [Matrix Construction](Matrix-Construction-Node.md) | Constructs square matrices from the four input vectors M0, M1, M2 and M3. |
| [Matrix Determinant](Matrix-Determinant-Node.md)   | Returns the determinant of the matrix defined by input In.                |
| [Matrix Split](Matrix-Split-Node.md)               | Splits a square matrix defined by input In into vectors.                  |
| [Matrix Transpose](Matrix-Transpose-Node.md)       | Returns the transposed value of the matrix defined by input In.           |

## Range

| **Topic**                            | **Description**                                                                                                      |
|--------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| [Clamp](Clamp-Node.md)               | Returns the input In clamped between the minimum and maximum values defined by inputs Min and Max respectively.      |
| [Fraction](Fraction-Node.md)         | Returns the fractional (or decimal) part of input In; which is greater than or equal to 0 and less than 1.           |
| [Maximum](Maximum-Node.md)           | Returns the largest of the two inputs values A and B.                                                                |
| [Minimum](Minimum-Node.md)           | Returns the smallest of the two inputs values A and B.                                                               |
| [One Minus](One-Minus-Node.md)       | Returns the result of input In subtracted from 1.                                                                    |
| [Random Range](Random-Range-Node.md) | Returns a pseudo-random number that is between the minimum and maximum values defined by inputs Min and Max.         |
| [Remap](Remap-Node.md)               | Remaps the value of input In from between the values of input Out Min Max to between the values of input In Min Max. |
| [Saturate](Saturate-Node.md)         | Returns the value of input In clamped between 0 and 1.                                                               |

## Round

| **Topic**                    | **Description**                                                                                              |
|------------------------------|--------------------------------------------------------------------------------------------------------------|
| [Ceiling](Ceiling-Node.md)   | Returns the smallest integer value, or whole number, that is greater than or equal to the value of input In. |
| [Floor](Floor-Node.md)       | Returns the largest integer value, or whole number, that is less than or equal to the value of input In.     |
| [Round](Round-Node.md)       | Returns the value of input In rounded to the nearest integer, or whole number.                               |
| [Sign](Sign-Node.md)         | Returns -1 if the value of input In is less than zero, 0 if equal to zero and 1 if greater than zero.        |
| [Step](Step-Node.md)         | Returns 1 if the value of input In is greater than or equal to the value of input Edge, otherwise returns 0. |
| [Truncate](Truncate-Node.md) | Returns the integer, or whole number, component of the value of input In.                                    |

## Trigonometry

| **Topic**                                        | **Description**                                                                                              |
|--------------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| [Arccosine](Arccosine-Node.md)                   | Returns the arccosine of each component the input In as a vector of equal length.                            |
| [Arcsine](Arcsine-Node.md)                       | Returns the arcsine of each component the input In as a vector of equal length.                              |
| [Arctangent](Arctangent-Node.md)                 | Returns the arctangent of the value of input In. Each component should be within the range of -Pi/2 to Pi/2. |
| [Arctangent2](Arctangent2-Node.md)               | Returns the arctangent of the values of both input A and input B.                                            |
| [Cosine](Cosine-Node.md)                         | Returns the cosine of the value of input In.                                                                 |
| [Degrees to Radians](Degrees-To-Radians-Node.md) | Returns the value of input In converted from degrees to radians.                                             |
| [Hyperbolic Cosine](Hyperbolic-Cosine-Node.md)   | Returns the hyperbolic cosine of input In.                                                                   |
| [Hyperbolic Sine](Hyperbolic-Sine-Node.md)       | Returns the hyperbolic sine of input In.                                                                     |
| [Hyperbolic Tangent](Hyperbolic-Tangent-Node.md) | Returns the hyperbolic tangent of input In.                                                                  |
| [Radians to Degrees](Radians-To-Degrees-Node.md) | Returns the value of input In converted from radians to degrees.                                             |
| [Sine](Sine-Node.md)                             | Returns the sine of the value of input In.                                                                   |
| [Tangent](Tangent-Node.md)                       | Returns the tangent of the value of input In.                                                                |

## Vector

| **Topic**                                      | **Description**                                                                                                                                                 |
|------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [Cross Product](Cross-Product-Node.md)         | Returns the cross product of the values of the inputs A and B.                                                                                                  |
| [Distance](Distance-Node.md)                   | Returns the Euclidean distance between the values of the inputs A and B.                                                                                        |
| [Dot Product](Dot-Product-Node.md)             | Returns the dot product, or scalar product, of the values of the inputs A and B.                                                                                |
| [Fresnel Effect](Fresnel-Effect-Node.md)       | Fresnel Effect is the effect of differing reflectance on a surface depending on viewing angle, where as you approach the grazing angle more light is reflected. |
| [Projection](Projection-Node.md)               | Returns the result of projecting the value of input A onto a straight line parallel to the value of input B.                                                    |
| [Reflection](Reflection-Node.md)               | Returns a reflection vector using input In and a surface normal Normal.                                                                                         |
| [Rejection](Rejection-Node.md)                 | Returns the result of the projection of the value of input A onto the plane orthogonal, or perpendicular, to the value of input B.                              |
| [Rotate About Axis](Rotate-About-Axis-Node.md) | Rotates the input vector In around the axis Axis by the value of Rotation.                                                                                      |
| [Sphere Mask](Sphere-Mask-Node.md)             | Creates a sphere mask originating from input Center.                                                                                                            |
| [Transform](Transform-Node.md)                 | Returns the result of transforming the value of input In from one coordinate space to another.                                                                  |

## Wave

| **Topic**                                    | **Description**                                                                                                   |
|----------------------------------------------|-------------------------------------------------------------------------------------------------------------------|
| [Noise Sine Wave](Noise-Sine-Wave-Node.md)   | Returns the sine of the value of input In. For variance, random noise is added to the amplitude of the sine wave. |
| [Sawtooth Wave](Sawtooth-Wave-Node.md)       | Returns a sawtooth wave from the value of input In.                                                               |
| [Matrix Split](Matrix-Split-Node.md)         | Splits a square matrix defined by input In into vectors.                                                          |
| [Matrix Transpose](Matrix-Transpose-Node.md) | Returns the transposed value of the matrix defined by input In.                                                   |
