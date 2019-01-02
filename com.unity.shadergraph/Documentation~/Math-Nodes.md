# Math Nodes

## Advanced

| [Absolute](Absolute-Node.md) | [Exponential](Absolute-Node.md) |
| :------- | :------ |
| ![Image](images/AbsoluteNodeThumb.png) | ![Image](images/ExponentialNodeThumb.png) |
| Returns the absolute value of input In. | Returns the exponential value of input In. |
| [**Length**](Length-Node.md) | [**Log**](Log-Node.md) |
| ![Image](images/LengthNodeThumb.png) | ![Image](images/LogNodeThumb.png) |
| Returns the length of input In. | Returns the logarithm of input In. |
| [**Modulo**](Modulo-Node.md) | [**Negate**](Negate-Node.md) |
| ![Image](images/ModuloNodeThumb.png) | ![Image](images/NegateNodeThumb.png) |
| Returns the remainder of input A divided by input B. | Returns the inverse value of input In. |
| [**Normalize**](Normalize-Node.md) | [**Posterize**](Posterize-Node.md) |
| ![Image](images/NormalizeNodeThumb.png) | ![Image](images/PosterizeNodeThumb.png) |
| Returns the normalized vector of input In. | Returns the input In converted into a number of values defined by input Steps. |
| [**Reciprocal**](Reciprocal-Node.md) | [**Reciprocal Square Root**](Reciprocal-Square-Root-Node.md) |
| ![Image](images/ReciprocalNodeThumb.png) | ![Image](images/ReciprocalSquareRootNodeThumb.png) |
| Returns the result of 1 divided by input In. | Returns the result of 1 divided by the square root of input In. |


## Basic

| [Add](Add-Node.md) | [Divide](Divide-Node.md) |
| :------- | :------ |
| ![Image](images/AddNodeThumb.png) | ![Image](images/DivideNodeThumb.png) |
| Returns the sum of the two input values. | Returns the result of input A divided by input B. |
| [**Multiply**](Multiply-Node.md) | [**Power**](Power-Node.md) |
| ![Image](images/MultiplyNodeThumb.png) | ![Image](images/PowerNodeThumb.png) |
| Returns the result of input A multiplied by input B. | Returns the result of input A to the power of input B. |
| [**Square Root**](Square-Root-Node.md) | [**Subtract**](Subtract-Node.md) |
| ![Image](images/SquareRootNodeThumb.png) | ![Image](images/SubtractNodeThumb.png) |
| Returns the square root of input In. | Returns the result of input A minus input B. |



## Derivative

| [DDX](DDX-Node.md) | [DDXY](DDXY-Node.md) |
| :------- | :------ |
| ![Image](images/DDXNodeThumb.png) | ![Image](images/DDXYNodeThumb.png) |
| Returns the partial derivative with respect to the screen-space x-coordinate. | Returns the sum of both partial derivatives. |
| [**DDY**](DDY-Node.md) ||
| ![Image](images/DDYNodeThumb.png) ||
| Returns the partial derivative with respect to the screen-space y-coordinate. ||



## Interpolation

| [Inverse Lerp](Inverse-Lerp-Node.md) | [Lerp](Lerp-Node.md) |
| :------- | :------ |
| ![Image](images/InverseLerpNodeThumb.png) | ![Image](images/LerpNodeThumb.png) |
| Returns the parameter that produces the interpolant specified by input T within the range of input A to input B. | Returns the result of linearly interpolating between input A and input B by input T. |
| [**Smoothstep**](Smoothstep-Node.md) ||
| ![Image](images/SmoothstepNodeThumb.png) ||
| Returns the result of a smooth Hermite interpolation between 0 and 1, if input In is between inputs Edge1 and Edge2. ||



## Matrix

| [Matrix Construction](Matrix-Construction-Node.md)           | [Matrix Determinant](Matrix-Determinant-Node.md)             |
| :----------------------------------------------------------- | :----------------------------------------------------------- |
| ![Image](images/MatrixConstructionNodeThumb.png)             | ![Image](images/MatrixDeterminantNodeThumb.png)              |
| Constructs square matrices from the four input vectors M0, M1, M2 and M3. | Returns the determinant of the matrix defined by input In.   |
| [**Matrix Split**](Matrix-Split-Node.md)                     | [**Matrix Transpose**](Matrix-Transpose-Node.md)                  |
| ![Image](images/MatrixSplitNodeThumb.png)                    | ![Image](images/MatrixTransposeNodeThumb.png)                |
| Splits a square matrix defined by input In into vectors.     | Returns the transposed value of the matrix defined by input In. |



## Range

| [Clamp](Clamp-Node.md) | [Fraction](Fraction-Node.md) |
| :------- | :------ |
| ![Image](images/ClampNodeThumb.png) | ![Image](images/FractionNodeThumb.png) |
| Returns the input In clamped between the minimum and maximum values defined by inputs Min and Max respectively. | Returns the fractional (or decimal) part of input In; which is greater than or equal to 0 and less than 1. |
| [**Maximum**](Maximum-Node.md) | [**Minimum**](Minimum-Node.md) |
| ![Image](images/MaximumNodeThumb.png) | ![Image](images/MinimumNodeThumb.png) |
| Returns the largest of the two inputs values A and B. | Returns the smallest of the two inputs values A and B. |
| [**One Minus**](One-Minus-Node.md) | [**Random Range**](Random-Range-Node.md) |
| ![Image](images/OneMinusNodeThumb.png) | ![Image](images/RandomRangeNodeThumb.png) |
| Returns the result of input In subtracted from 1. | Returns a pseudo-random number that is between the minimum and maximum values defined by inputs Min and Max. |
| [**Remap**](Remap-Node.md) | [**Saturate**](Saturate-Node.md) |
| ![Image](images/RemapNodeThumb.png) | ![Image](images/SaturateNodeThumb.png) |
| Remaps the value of input In from between the values of input Out Min Max to between the values of input In Min Max. | Returns the value of input In clamped between 0 and 1. |



## Round

| [Ceiling](Ceiling-Node.md) | [Floor](Floor-Node.md) |
| :------- | :------ |
| ![Image](images/CeilingNodeThumb.png) | ![Image](images/FloorNodeThumb.png) |
| Returns the smallest integer value, or whole number, that is greater than or equal to the value of input In. | Returns the largest integer value, or whole number, that is less than or equal to the value of input In. |
| [**Round**](Round-Node.md) | [**Sign**](Sign-Node.md) |
| ![Image](images/RoundNodeThumb.png) | ![Image](images/SignNodeThumb.png) |
| Returns the value of input In rounded to the nearest integer, or whole number. | Returns -1 if the value of input In is less than zero, 0 if equal to zero and 1 if greater than zero. |
| [**Step**](Step-Node.md) | [**Truncate**](Truncate-Node.md) |
| ![Image](images/StepNodeThumb.png) | ![Image](images/TruncateNodeThumb.png) |
| Returns 1 if the value of input In is greater than or equal to the value of input Edge, otherwise returns 0. | Returns the integer, or whole number, component of the value of input In. |

## Trigonometry 

| [Arccosine](Arccosine-Node) | [Arcsine](Arcsine-Node.md) |
| :------- | :------ |
| ![Image](images/ArccosineNodeThumb.png) | ![Image](images/ArcsineNodeThumb.png) |
| Returns the arccosine of each component the input In as a vector of equal length. | Returns the arcsine of each component the input In as a vector of equal length. |
| [**Arctangent**](Arctangent-Node.md) | [**Arctangent2**](Arctangent2-Node.md) |
| ![Image](images/ArctangentNodeThumb.png) | ![Image](images/Arctangent2NodeThumb.png) |
| Returns the arctangent of the value of input In. Each component should be within the range of -Pi/2 to Pi/2. | Returns the arctangent of the values of both input A and input B. |
| [**Cosine**](Cosine-Node.md) | [**Degrees to Radians**](Degrees-To-Radians-Node.md) |
| ![Image](images/CosineNodeThumb.png) | ![Image](images/DegreesToRadiansNodeThumb.png) |
| Returns the cosine of the value of input In. | Returns the value of input In converted from degrees to radians. |
| [**Hyperbolic Cosine**](Hyperbolic-Cosine-Node.md) | [**Hyperbolic Sine**](Hyperbolic-Sine-Node.md) |
| ![Image](images/HyperbolicCosineNodeThumb.png) | ![Image](images/HyperbolicSineNodeThumb.png) |
| Returns the hyperbolic cosine of input In. | Returns the hyperbolic sine of input In. |
| [**Hyperbolic Tangent**](Hyperbolic-Tangent-Node.md) | [**Radians to Degrees**](Radians-To-Degrees-Node.md) |
| ![Image](images/HyperbolicTangentNodeThumb.png) | ![Image](images/RadiansToDegreesNodeThumb.png) |
| Returns the hyperbolic tangent of input In. | Returns the value of input In converted from radians to degrees. |
| [**Sine**](Sine-Node.md) | [**Tangent**](Tangent-Node.md) |
| ![Image](images/SineNodeThumb.png) | ![Image](images/TangentNodeThumb.png) |
| Returns the sine of the value of input In. | Returns the tangent of the value of input In. |



## Vector

| [Cross Product](Cross-Product-Node.md) | [Distance](Distance-Node.md) |
| :------- | :------ |
| ![Image](images/CrossProductNodeThumb.png) | ![Image](images/DistanceNodeThumb.png) |
| Returns the cross product of the values of the inputs A and B. | Returns the Euclidean distance between the values of the inputs A and B. |
| [**Dot Product**](Dot-Product-Node.md) | [**Fresnel Effect**](Fresnel-Effect-Node.md) |
| ![Image](images/DotProductNodeThumb.png) | ![Image](images/FresnelEffectNodeThumb.png) |
| Returns the dot product, or scalar product, of the values of the inputs A and B. | Fresnel Effect is the effect of differing reflectance on a surface depending on viewing angle, where as you approach the grazing angle more light is reflected. |
| [**Projection**](Projection-Node.md) | [**Reflection**](Reflection-Node.md) |
| ![Image](images/ProjectionNodeThumb.png) | ![Image](images/ReflectionNodeThumb.png) |
| Returns the result of projecting the value of input A onto a straight line parallel to the value of input B. | Returns a reflection vector using input In and a surface normal Normal. |
| [**Rejection**](Rejection-Node.md) | [**Rotate About Axis**](Rotate-About-Axis-Node.md) |
| ![Image](images/RejectionNodeThumb.png) | ![Image](images/RotateAboutAxisNodeThumb.png) |
| Returns the result of the projection of the value of  input A onto the plane orthogonal, or perpendicular, to the value of  input B. | Rotates the input vector In around the axis Axis by the value of Rotation. |
| [**Projection**](Projection-Node.md) | [**Rejection**](Rejection-Node.md) |
| ![Image](images/ProjectionNodeThumb.png) | ![Image](images/RejectionNodeThumb.png) |
| Returns the result of projecting the value of input A onto a straight line parallel to the value of input B. | Returns the result of the projection of the value of  input A onto the plane orthogonal, or perpendicular, to the value of  input B. |
| [**Sphere Mask**](Sphere-Mask-Node.md) | [**Transform**](Transform-Node.md) |
| ![Image](images/SphereMaskNodeThumb.png)| ![Image](images/TransformNodeThumb.png) |
| Creates a sphere mask originating from input Center. | Returns the result of transforming the value of input In from one coordinate space to another. |

## Wave

| [Noise Sine Wave](Noise-Sine-Wave-Node.md)           | [Sawtooth Wave](Sawtooth-Wave-Node.md)             |
| :----------------------------------------------------------- | :----------------------------------------------------------- |
| ![Image](images/NoiseSineWaveNodeThumb.png)             | ![Image](images/SawtoothWaveNodeThumb.png)              |
| Returns the sine of the value of input In. For variance, random noise is added to the amplitude of the sine wave. | Returns a sawtooth wave from the value of input In. |
| [**Matrix Split**](Matrix-Split-Node.md)                     | [**Matrix Transpose**](Matrix-Transpose-Node.md)                  |
| ![Image](images/MatrixSplitNodeThumb.png)                    | ![Image](images/MatrixTransposeNodeThumb.png)                |
| Splits a square matrix defined by input In into vectors.     | Returns the transposed value of the matrix defined by input In. |

Noise Sine Wave
Sawtooth Wave
Square Wavve
Triangle Wave