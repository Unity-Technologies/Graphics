# Rounded Rectangle node

The Rounded Rectangle node generates a filled rounded rectangle shape with a border around it. The output is 1 for the rectangle and 0 for the border.

To move the rectangle within the space, do the following:

- To offset the shape, input a [Tiling And Offset node](Tiling-And-Offset-Node.md) and adjust the **Offset** property. 
- To tile the shape, input a [Tiling and Offset node](Fraction-Node.md) into a [Fraction node](Tiling-And-Offset-Node.md), then input the Fraction node into the Rounded Rectangle node.

You can only output this node into the Fragment Context.

## Ports

| Name | Direction | Type | Binding | Description |
|-|-|-|-|-|
| **UV** | Input | Vector 2 | UV | Sets the UV coordinates to use to sample the rounded rectangle shape and the border. |
| **Width** | Input | Float | None | Sets how much horizontal space the rectangle fills, where 1 is a full-width rectangle without a border on the left and right. |
| **Height** | Input | Float | None | Sets how much vertical space the rectangle fills, where 1 is a full height rectangle without a border above and below. |
| **Radius** | Input | Float | None | Sets the roundness of the corners of the rectangle. The range is 0 to 1, where 0 is right-angled corners. |
| **Out** | Output | Float | None | The rounded rectangle and the border, where 1 is the rectangle and 0 is the border. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_RoundedRectangle_float(float2 UV, float Width, float Height, float Radius, out float Out)
{
    Radius = max(min(min(abs(Radius * 2), abs(Width)), abs(Height)), 1e-5);
    float2 uv = abs(UV * 2 - 1) - float2(Width, Height) + Radius;
    float d = length(max(0, uv)) / Radius;
    Out = saturate((1 - d) / fwidth(d));
}
```
