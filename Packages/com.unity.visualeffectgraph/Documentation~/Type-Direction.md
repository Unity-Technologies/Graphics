# Direction

A world-space or local-space three-component direction vector. It indicates the initial orientation of the particle and is commonly used with blocks like [Set Velocity from Direction & Speed](Block-VelocityFromDirectionAndSpeed(ChangeSpeed).md) and [Set Position (Sphere)](Block-SetPosition(Sphere).md), [Set Position (Cone)](Block-SetPosition(Cone).md) where direction is set by shape's underlying surface normals. 

## Properties

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Direction** | The value of the direction. When you output this value, Unity normalizes it. |
