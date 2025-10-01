# Soften shadows

Make your shadows appear softer in HDRP by adjusting settings such as [Filtering Quality property](HDRP-Asset.md#lighting-shadows), sample counts, and light shape properties.

For more information on shadows in HDRP, refer to [Shadows](shadows.md).

You can soften your shadows in HDRP to make them appear less sharp and more diffuse. HDRP controls shadow softness primarily through the **Filtering Quality** property, which determines the shadow filtering algorithm and sample count.

## Filtering quality levels

| **Filtering quality** | **Algorithm for punctual light** | **Algorithm for directional light** | **Algorithm for area light** |
| :---- | :---- | :---- | :---- |
| Low | PCF 3x3 (4 taps) | PCF Tent 5x5 (9 taps) | (N/A) |
| Medium | PCF 5x5 (9 taps) | PCF Tent 5x5 (9 taps) | EVSM |
| High | PCSS | PCSS | PCSS |

**Low** and **Medium** filtering quality levels use Percentage Closer Filtering (PCF), which applies a fixed size blur.

**High** filtering quality uses Percentage Closer Soft Shadow (PCSS), which applies a different blur size depending on the distance between the shadowed pixel and the shadow caster. This creates a more natural penumbra effect and results in a more realistic shadow. However, this is more resource-intensive to compute so it's not recommended for consoles. 

## Optimize soft shadows

For the softest shadows, set the **Filtering Quality** for your shadow type to **High**. When you use the **High** filtering quality, you can also fine-tune softness and performance in the Light Inspector for your needs by adjusting the following properties:

- **Filter Sample Count** and **Blocker Sample Count**. A lower sample count decreases the resource intensity of PCSS, which reduces shadow quality but improves performance.
- **Radius Scale for Softness** or **Angular Diameter Scale for Softness**.
- Point and Spot Lights: **Radius** (**Light** > **Shape** > **Radius**).
- Directional Lights: **Angular Diameter** (**Light** > **Shape** > **Angular Diameter**).
- Area Lights: **Near Plane** (**Light** > **Shadows** > **Shadow Map** > **Near Plane**).

## Additional resources

- [Additional shadow detail](shadows-additional-detail.md)
- [Control shadow resolution and quality](Shadows-in-HDRP.md)