# Lens Flare (SRP) Data Asset

Unity’s [Scriptable Render Pipeline (SRP)](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html) includes the **Lens Flare Data** asset. You can use this asset to control the appearance of [Lens Flares](lens-flare-component.md) in your scene. This is the SRP equivalent of the Built-in Render Pipeline's [Flare](https://docs.unity3d.com/Manual/class-Flare.html) asset, which is incompatible with SRPs.

For examples of how to use Lens Flares, refer to the [Lens Flare samples in URP Package Samples](../../package-sample-urp-package-samples.md#lens-flares).

To create a Lens Flare Data asset, select **Assets** &gt; **Create** &gt; **Lens Flare (SRP)**. To use this asset, assign it to the **Lens Flare Data** property of a [Lens Flare (SRP) component](lens-flare-component.md).

## Properties

The Lens Flare Element asset has the following properties:

* [Type](#type)
  * [Image](#image)
  * [Circle](#circle)
  * [Polygon](#polygon)
* [Color](#color)
* [Transform](#transform)
* [AxisTransform](#axistransform)
* [Distortion](#Distortion)
* [Multiple Elements](#Multiple-Elements)
  * [Uniform](#uniform)
  * [Curve](#Curve)
  * [Random](#random)

### <a name="type"></a>Type

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| Type         | Select the type of Lens Flare Element this asset creates: <br />&#8226; [Image](#image) <br />&#8226; [Circle](#circle) <br />&#8226; [Polygon](#polygon) |

#### <a name="image"></a>Image

#### Image

| **Property**          | **Description**                                              |
| --------------------- | ------------------------------------------------------------ |
| Flare Texture         | The Texture this lens flare element uses.                    |
| Preserve Aspect Ratio | Fixes the width and height (aspect ratio) of the **Flare Texture**. You can use [Distortion](#Distortion) to change this property. |

#### <a name="circle"></a>Circle

#### Circle

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| Gradient     | Controls the offset of the circular flare's gradient. This value ranges from 0 to 1. |
| Falloff      | Controls the falloff of the circular flare's gradient. This value ranges from 0 to 1, where 0 has no falloff between the tones and 1 creates a falloff that is spread evenly across the circle. |
| Inverse      | Enable this property to reverse the direction of the gradient. |

#### <a name="polygon"></a>Polygon

#### Polygon

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| Gradient     | Controls the offset of the polygon flare's gradient. This value ranges from 0 to 1. |
| Falloff      | Controls the falloff of the polygon flare's gradient. This value ranges from 0 to 1, where 0 has no falloff between the tones and 1 creates a falloff that is spread evenly across the polygon. |
| Side Count   | Determines how many sides the polygon flare has.             |
| Roundness    | Defines how smooth the edges of the polygon flare are. This value ranges from 0 to 1, where 0 is a sharp polygon and 1 is a circle. |
| Inverse      | Enable this property to reverse the direction of the gradient |

## <a name="color"></a>Color

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| Tint                    | Changes the tint of the lens flare. If this asset is attached to the light, this property is based on the light tint. |
| Modulate By Light Color | Allows light color to affect this Lens Flare Element. This only applies when the asset is used in a [Lens Flare (SRP) component](lens-flare-component.md) that is attached to a point, spot, or area light. |
| Intensity               | Controls the intensity of this element.                      |
| Blend Mode              | Select the blend mode of the Lens Flare Element this asset creates:<br />• Additive  <br />• Screen  <br />• Premultiplied <br />• Lerp |

## <a name="transform"></a>Transform

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| Position Offset   | Defines the offset of the lens flare's position in screen space, relative to its source. |
| Auto Rotate             | Enable this property to automatically rotate the Lens Flare Texture relative to its angle on the screen. Unity uses the **Auto Rotate** angle to override the **Rotation** parameter. <br/><br/> To ensure the Lens Flare can rotate, assign a value greater than 0 to the [**Starting Position**](#axistransform)  property. |
| Rotation                | Rotates the lens flare. This value operates in degrees of rotation. |
| Size                    | Use this to adjust the scale of this lens flare element. <br/><br/> This property is not available when the [Type](#type) is set to [Image](#image) and **Preserve Aspect Ratio** is enabled. |
| Scale                   | The size of this lens flare element in world space.          |

## <a name="axistransform"></a>AxisTransform

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| Starting Position | Defines the starting position of the lens flare relative to its source. This value operates in screen space. |
| Angular Offset    | Controls the angular offset of the lens flare, relative to its current position. This value operates in degrees of rotation. |
| Translation Scale | Limits the size of the lens flare offset. For example, values of (1, 0) create a horizontal lens flare, and (0, 1) create a vertical lens flare. <br/><br/>You can also use this property to control how quickly the lens flare appears to move. For example, values of (0.5, 0.5) make the lens flare element appear to move at half the speed. |

<a name="Distortion"></a>

## Distortion

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Enable | Set this property to True to enable distortion. |
| Radial Edge Size | Controls the size of the distortion effect from the edge of the screen. |
| Radial Edge Curve | Blends the distortion effect along a curve from the center of the screen to the edges of the screen. |
| Relative To Center | Set this value to True to make distortion relative to the center of the screen. Otherwise, distortion is relative to the screen position of the lens flare. |

<a name="Multiple-Elements"></a>

## Multiple Elements

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Enable | Enable this to allow multiple lens flare elements in your scene. |
| Count | Determines the number of identical lens flare elements Unity generates.<br/>A value of **1** appears the same as a single lens flare element. |
| Distribution | Select the method that Unity uses to generate multiple lens flare elements:<br/>•[Uniform](#uniform)<br/>•[Curve](#Curve)<br/>•[Random](#random) |
| Length Spread | Controls how spread out multiple lens flare elements appear. |
| Relative To Center | If true the distortion is relative to center of the screen otherwise relative to lensFlare source screen position. |

### <a name="uniform"></a>Uniform
| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Colors | The range of colors that this asset applies to the lens flares. |
| Rotation | The angle of rotation (in degrees) applied to each element incrementally. |

### <a name="Curve"></a>Curve

#### Curve

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| Colors           | The range of colors that this asset applies to the lens flares. You can use the **Position Spacing** curve to determine how this range affects each lens flare. |
| Position Variation | Adjust this curve to change the placement of the lens flare elements in the **Lens Spread**. |
| Rotation | The uniform angle of rotation (in degrees) applied to each element distributed along the curve. This value ranges from -180° to 180°. |
| Scale | Adjust this curve to control the size range of the lens flare elements. |

### <a name="random"></a>Random

#### Random

| **Property**        | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| Seed                | The base value that this asset uses to generate randomness.  |
| Intensity Variation | Controls the variation of brightness across the lens flare elements. A high value can make some elements might invisible. |
| Colors              | The range of colors that this asset applies to the lens flares. This property is based on the **Seed** value. |
| Position Variation  | Controls the position of the lens flares. The **X** value is spread along the same axis as **Length Spread**. A value of 0 means there is no change in the lens flare position. The **Y** value is spread along the vertical screen space axis based on the **Seed** value. |
| Rotation Variation  | Controls the rotation variation of the lens flares, based on the **Seed** value. The **Rotation** and **Auto Rotate** parameters inherit from this property. |
| Scale Variation     | Controls the scale of the lens flares based on the **Seed** value. |
