# Set Position Shape Block reference

The Set Position Shape Block calculates particle positions based on an input shape, then stores them in the [position attribute](Reference-Attributes.md) of particles.

<video title="Particles spawn in the shape of a cone. The particles adjust as the wide base of the cone shrinks and grows, and the length of the arc of the cone is reduced." src="Images/Block-SetPosition(Cone)Main.mp4" width="auto" height="auto" autoplay="true" loop="true" controls></video>

<video title="Particles spawn in the shape of a signed distance field (SDF) in the shape of a teapot." src="Images/Block-SetPosition(SDF)Example.mp4" width="auto" height="auto" autoplay="true" loop="true" controls></video>

This Block also stores direction vectors in the [direction attribute](Reference-Attributes.md) of particles. To set the velocity of a particle from its direction attribute, use a [Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed(ChangeSpeed).md) Block.

The direction vector depends on the shape:

- Sphere: The normalized vector from the center of the sphere to the calculated position.
- Box: The normal of the face the calculated particle position is on. Unity selects the face based on six pyramids, each with a face as its base and the center of the box as its tip.
- Cone: The normalized vector from the calculated position to the top of the cone.
- Torus: The normalized vector from the center of the torus to the calculated position.
- Circle: The normalized vector from the center of the circle to the calculated position.
- Line: The normalized vector from the start of the line to the end of the line.

## Block compatibility

You can add the Set Position Shape Block to the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

To add a Set Position Shape Block to your graph, [open the menu for adding a graph element](VisualEffectGraphWindow.md#adding-graph-elements) then select **Position Shape** > **Set Position Shape**.

## Block settings

| **Property** | **Type** | **Description** |
|-|-|-|
| **Shape** | Enum | The shape to use to calculate positions. For more information, refer to the [**Shape dropdown**](#shape-dropdown) section. |
| **Height Mode** | Enum | Specifies which part of the cone Unity uses to calculate positions. This property is available only if you set **Shape** to **Cone**. The options are: <ul><li><strong>Volume</strong>: Uses the entire cone to calculate positions.</li><li><strong>Base</strong>: Uses the circle at the base of the cone to calculate positions. |
| **Position Mode** | Enum | Specifies which part of the shape Unity uses to calculate positions. The options are:<ul><li><strong>Surface</strong>: Uses positions only on the surface of the shape.</li><li><strong>Volume</strong>: Uses positions inside the entire volume of the shape.</li><li><strong>Thickness Absolute</strong>: Uses positions on the surface of the shape, and within an inner layer defined in meters by **Thickness**.</li><li><strong>Thickness Relative</strong>: Uses positions on the surface of the shape, and within an inner layer defined as a percentage of the size of the shape by **Thickness**.</li></ul> |
| **Spawn Mode** | Enum | Specifies where particles spawn along the arc of the shape, or the length of the line. This property isn't available if you set **Shape** to **Oriented Box**. The options are: <ul><li><strong>Random</strong>: Spawns particles at random positions.</li><li><strong>Custom</strong>: Spawns particles at the position set in **Arc Sequencer** or **Line Sequencer**.</li></ul> |

<a name="shape-dropdown"></a>
### Shape dropdown

| **Shape** | **Description** |
|-|-|
| **Sphere**| Sets the shape as a spherical volume. |
| **Oriented Box** | Sets the shape as an axis-aligned box volume. |
| **Cone** | Sets the shape as a truncated cone volume.|
| **Torus** | Sets the shape as a 3D ring volume. |
| **Circle** | Sets the shape as a circle. |
| **Line** | Sets the shape as a flat plane with infinite width and height. |
| **Signed Distance Field** | Sets the shape as a signed distance field (SDF), so you can create positions with existing assets. To generate a signed distance field asset, use the [SDF Bake Tool](sdf-bake-tool.md) or an external digital content creation (DCC) tool. |

## Block properties

| **Input** | **Type** | **Description** |
| ------------------- | ---------------------- | ------------------------------------------------------------ |
| **Arc Sphere** | [ArcSphere](Type-ArcSphere.md) | The sphere to use for positions. This property is available only if you set **Shape** to **Sphere**. |
| **Box** | [AABox](Type-AABox.md) | The axis-aligned box to use for positions. This property is available only if you set **Shape** to **Oriented Box**. |
| **Arc Cone** | [ArcCone](Type-ArcCone.md) | The cone to use for positions. This property is available only if you set **Shape** to **Cone**. |
| **Arc Torus** | [ArcTorus](Type-ArcTorus.md) | The torus to use for positions. This property is available only if you set **Shape** to **Torus**. |
| **Arc Circle** | [ArcCircle](Type-ArcCircle.md) | The circle to use for positions. This property is available only if you set **Shape** to **Circle**. |
| **Line** | [Line](Type-Line.md) | The line to use for positions. This property is available only if you set **Shape** to **Line**. |
| **SDF**| Signed distance field | The signed distance field asset to use for positions. This property appears only if you set **Shape** to **Signed Distance Field**. |
| **Height Sequencer** | Float | Sets where particles appear on the shape, as a percentage of its height. This property is available only if you set **Shape** to **Sphere**, **Cone**, or **Torus** and **Spawn Mode** to **Custom**. |
| **Arc Sequencer** | Float | Sets where particles appear on the arc, as a percentage of its length. This property is available only if you set **Shape** to **Sphere**, **Cone**, **Torus**, **Circle**, or **Signed Distance Field**, and **Spawn Mode** to **Custom**. |
| **Line Sequencer** | Float | Sets where particles appear on the line, as a percentage of its length. This property is available only if you set **Shape** to **Line** and **Spawn Mode** to **Custom**. |
| **Field Transform** | [Transform](Type-Transform.md) | The transform that determines the position, size, and rotation of the **Distance Field**. This property appears only if you set **Shape** to **Signed Distance Field**. |
| **Thickness** | Float | Sets the thickness of the inner layer for particle positions. This property is available only if you set **Position Mode** to **Thickness Relative** or **Thickness Absolute**. This property isn't available if you set **Shape** to **Line**.<br/><br/>If you use a signed distance field, Unity calculates the relative thickness based on the largest axis of the SDF, which might not be the size of the object the SDF represents. As a result, particles might be inside the volume even if you use **Thickness Relative** and a **Thickness** value of less than 1.|
| **Blend Position** | Float | Sets the blend percentage between the current position attribute value and the new position value. This property is available only if you set **Composition Position** to **Blend** in the Inspector window. |
| **Blend Direction** | Float | Sets the blend percentage between the current direction attribute value and the new direction value. This property is available only if you set **Composition Direction** to **Blend** in the Inspector window. |

## Inspector window properties

| **Property** | **Type** | **Description** |
|-|-|-|
| **Apply Orientation** | Enum | Aligns particles so they match the orientation of the geometry of the shape. The options are: <ul><li><strong>None</strong>: Doesn't use the shape to align particles.</li><li><strong>Everything</strong>: Aligns both the Direction attributes, and the AxisX, AxisY, and AxisZ attributes that set the particle's orientation.</li><li><strong>Direction</strong>: Aligns only the Direction attribute.</li><li><strong>Axis</strong>: Aligns only the AxisX, AxisY, and AxisZ attributes.</li></ul>
| **Kill Outliers** | Boolean | Specifies whether to destroy particles if their position is outside the surface or volume. This property is available only if you set **Shape** to **Signed Distance Field**. |
| **Projection Steps** | Uint | Sets the number of steps Unity uses to project the particle onto the surface of the SDF, to reduce the number of outlier particles. This might have a performance impact. This property is available only if you set **Shape** to **Signed Distance Field**. |
| **Composition Position** | Enum | Specifies how Unity updates the position attribute. The options are:<ul><li><strong>Overwrite</strong>: Overwrites the position attribute value with the new value.</li><li><strong>Add</strong>: Adds the position attribute value to the new value.</li><li><strong>Multiply</strong>: Multiplies the position attribute value by the new value.</li><li><strong>Blend</strong>: Interpolates between the position attribute value and the new value. To set the blend factor, set **Blend Position** in the Block properties.</li></ul> |
| **Composition Direction** | Enum | Specifies how Unity updates the direction attribute. The options are:<ul><li><strong>Overwrite</strong>: Overwrites the direction attribute value with the new value.</li><li><strong>Add</strong>: Adds the direction attribute value to the new value.</li><li><strong>Multiply</strong>: Multiplies the direction attribute value by the new value.</li><li><strong>Blend</strong>: Interpolates between the direction attribute value and the new value. To set the blend factor, set **Blend Direction** in the Block properties.</li></ul> |

