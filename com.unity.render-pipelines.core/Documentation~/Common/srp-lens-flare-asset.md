# SRP Lens Flare Asset

The SRP (HDRP & URP) includes an asset to describe Lens Flare, reused with the [SRP Lens Flare Override Component](srp-lens-flare-component.md).

### Properties

The SRP Lens Flare Asset is splited in two part. The common and per element.

## Type

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Type | The type of flare element, which can be [Image](#Image), [Circle](#Circle) or [Polygon](#Polygon) |

### Image
![](images/LensFlareShapeImage.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Flare Texture | Texture used to for this Lens Flare Element. |
| Preserve Aspect Ratio | Preserve aspect ratio of the image (width/height), can be modulated with [Distortion](#Distortion) parameters. |

### Circle
![](images/LensFlareShapeCircle.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Gradient | Gradient Offset used for the Procedural Flare. |
| Falloff | Fall of the gradient used for the Procedural Flare. |
| Inverse | Inverse the gradient direction. |

### Polygon
![](images/LensFlareShapePolygon.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Gradient | Gradient Offset used for the Procedural Flare. |
| Falloff | Fall of the gradient used for the Procedural Flare. |
| Side Count | Side count of the regular polygon generated. |
| Roundness | Roundness of the polygon flare (0: Sharp Polygon, 1: Circle). |
| Inverse | Inverse the gradient direction. |

## Common
![](images/LensFlareCommon.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Intensity | Intensity of this element. |
| Tint | Tint of the texture can be modulated by the light we are attached to. |
| Blend Mode | Blend mode used. Can be Additive, Screen, Premultiplied or Lerp. |
| Modulate By Light Color | Modulate by light color if the asset is used in a 'SRP Lens Flare Source Override' attached to a light. |
| Rotation | Local rotation of the texture. |
| Size | Scale applied to the element, not visible if [Preserve Aspect Ratio](#Image) is used with type is [Image](#Image). |
| Scale | Uniform scale applied to the flare. |
| Auto Rotate | Rotate the texture relative to the angle on the screen (the rotation will be added to the parameter 'rotation'). The Auto Rotate angle is added to **Rotation** parameter. The behavior is similar to a LookAt to lens flare source, which implies if the [Starting Position](#AxisTransform) is 0 the flare cannot rotate, a very small value can be use to fix that for instance 0.00001. |

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Intensity       | Modulate the whole lens flare. |
| Scale Curve     | Curve between 0 and 1 which describes the scale of each element, if the relative position is negative HDRP will read the negative part of the curve, the positive part otherwise. |
| Position Curve  | Curve between -1 and 1 which describes the scale of each element, if the relative position is negative HDRP will read the negative part of the curve, the positive part otherwise. |
| Elements        | List of SRPLensFlareDataElement. |

## AxisTransform
![](images/LensFlareAxisTransform.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Starting Position | Starting position relative to lens flare source. |
| Position Offset | Screen space offset relative to lens flare source. |
| Angular Offset | Angular offset, relative to the current position. |
| Translation Scale | Constraint on lens flare offset, for instance to have horizontal flare use (1, 0) and (0, 1) for a vertical one. This scale can modulate the speed of the flare in his original path (0.5, 0.5) make the lens flare element move twice slower. |

## Distortion
![](images/LensFlareRadialDistortion.png)

## Multiple Elements

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Count | Number of the same element generated. |
| Distribution | Describes the way we spread the elements generated can be [Uniform](#Uniform), parametrized by a [Curve](#Curve) or [Random](#Random). |
| Length Spread | The length of the spread for the **Count** Element. |

### Uniform
![](images/LensFlareMultileElementUniform.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Colors | Sequence of colors which will be used to modulate color of each elements on the spreaded flares. |

### Curve
![](images/LensFlareMultileElementCurve.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Colors | Sequence of colors which will be used to modulate color of each elements on the spreaded flares, **Position Spacing** will modulate which color will be used for each flare. |
| Position Spacing | A curve representing the placement of the flare element in the **Lens Spread**. |
| Scale Variation | Curve parametrize the size modulation of each element. |

### Random
![](images/LensFlareMultileElementRandom.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Seed | Seed used for the random. |
| Intensity Variation | Coefficient used to modulate the variation, too high value can make some element disapeared. |
| Colors | Collection of colors which will be selected randomly based on **Seed**. |
| Position Variation | Coefficients used to modulate the position. Scale X is along axis of **Length Spread** (0 mean no position variation) and Y is spread along vertical screen space axis (based on **Seed**). |
| Rotation Variation | Coefficient used to modulate the current rotation variation (based on **Seed**), this variation is added to **Rotation** parameter and **Auto Rotate**. |
| Scale Variation | Coefficient used modulate the scale based on **Seed**.. |
