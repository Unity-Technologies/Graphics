# Procedural Nodes

| [Checkerboard](Checkerboard-Node.md) |
| :-----------|
| ![A Checkerboard node. A UV0 value is connected to the UV(2) slot. A dark-gray value is connected to the Color A(3) slot. A light gray value is connected to the Color B(3) slot. A (1,1) Vector 2 is connected to the Frequency(2) slot. No value is connected to the Out(3) slot. A 2 by 2 checkerboard is displayed in the lower part of the node.](images/CheckerboardNodeThumb.png) |
| Generates a checkerboard of alternating colors between inputs Color A and Color B based on input UV. |

## Noise

| [Gradient Noise](Gradient-Noise-Node.md) | [Simple Noise](Simple-Noise-Node.md) |
| :------------------------ | :---------------------------- |
| ![A Gradient Noise node. A UV0 value is connected to the UV(2) slot. A 10 scalar is attached to the Scale(1) slot. No value is connected to the Out(1) slot. A textured, abstract grayscale pattern that resembles soft, cloudy noise is displayed in the lower part of the node.](images/GradientNoiseNodeThumb.png) | ![A Simple Noise node. A UV0 value is connected to the UV(2) slot. A 500 scalar is attached to the Scale(1) slot. No value is connected to the Out(1) slot. A TV static pattern is displayed in the lower part of the node.](images/SimpleNoiseNodeThumb.png) |
| Generates a gradient, or Perlin, noise based on input UV. | Generates a simple, or Value, noise based on input UV. |
| [**Voronoi**](Voronoi-Node.md) |           |
| ![A Voronoi node. A UV0 value is connected to the UV(2) slot. A 2 scalar is attached to the Angle Offset(1) slot. A 5 scalar is attached to the Cell Density(1) slot. No value is connected to the Out(1) slot or the Cells(1) slot. A pattern of cells is displayed in the lower part of the node.](images/VoronoiNodeThumb.png) ||
|Generates a Voronoi, or Worley, noise based on input UV.  ||

## Shape

| [Ellipse](Ellipse-Node.md) | [Polygon](Polygon-Node.md) |
| :----------------------------------------------------------- | :----------------------------------------------------------- |
| ![An Ellipse node. A UV0 value is connected to the UV(2) slot. 0.5 scalars are atttached to the Width(1) and Height(1) slots. No value is connected to the Out(1) slot. A solid white disk is displayed in the lower part of the node.](images/EllipseNodeThumb.png)                        | ![A Polygon node. A UV0 value is connected to the UV(2) slot. A 6 scalar is attached to the Sides(1) slot. 0.5 scalars are atttached to the Width(1) and Height(1) slots. No value is connected to the Out(1) slot. A solid white hexagon is displayed in the lower part of the node.](images/PolygonNodeThumb.png)                        |
| Generates an ellipse shape based on input UV at the size specified by inputs Width and Height. | Generates a regular polygon shape based on input UV at  the size specified by inputs Width and Height. The polygon's amount of  sides is determined by input Sides. |
| [**Rectangle**](Rectangle-Node.md) | [**Rounded Rectangle**](Rounded-Rectangle-Node.md) |
| ![A Rectangle node. A UV0 value is connected to the UV(2) slot. A 0.5 scalar is attached to the Width(1) slot. A 0.5 scalar is attached to the Height(1) slot. No value is connected to the Out(1) slot. The Fastest option is selected in a drop-down. A solid white square is displayed in the lower part of the node.](images/RectangleNodeThumb.png)                      | ![A Rounded Rectangle node. A UV0 value is connected to the UV(2) slot. 0.5 scalars are attached to the Width(1), Height(1), and Radius(1) slots. No value is connected to the Out(1) slot. A solid white rounded square is displayed in the lower part of the node.](images/RoundedRectangleNodeThumb.png)               |
| Generates a rectangle shape based on input UV at the size specified by inputs Width and Height. | Generates a rounded rectangle shape based on input UV at the size specified by inputs Width and Height. The input Radius defines the radius of each corner. |
| [](Rounded-Polygon-Node.md) ||
|![A Rounded Polygon node. A UV0 value is connected to the UV(2) slot. 0.5 scalars are atttached to the Width(1) and Height(1) slots.   A 5 scalar is attached to the Sides(1) slot. A 0.3 scalar is attached to the Roudness(1) slot. No value is connected to the Out(1) slot. A solid white rounded pentagon is displayed in the lower part of the node.](images/RoundedPolygonNodeThumb.png) ||
| Generates a rounded polygon shape based on input UV at the size specified by inputs Width and Height. The input Sides specifies the number of sides, and the input Roundness defines the roundness of each corner. ||
