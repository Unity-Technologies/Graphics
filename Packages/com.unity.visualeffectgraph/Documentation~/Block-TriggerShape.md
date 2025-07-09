# Trigger Shape Block reference

The Trigger Shape Block defines a shape that detects particle collisions without physically interacting with the particles. Instead of blocking or altering particle movement, it triggers specific events when particles collide with it. You can use this block in combination with a [Trigger Event Block](Block-Trigger-Event.md) set to **Collide** mode to spawn child particles or perform other actions.

If you change the **Behavior** property of the block, the Block changes to the following:

- A [Collision Shape Block](Block-CollisionShape.md) if you set **Behavior** to **Collision**.
- A [Kill Shape Block](Block-KillShape.md) if you set **Behavior** to **Kill**.

## Block compatibility

You can add the Trigger Shape Block to the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)

To add a Trigger Shape Block to your graph, [open the menu for adding a graph element](VisualEffectGraphWindow.md#adding-graph-elements) then select **Collision** > **Trigger Shape**.

## Block settings

| **Property** | **Type** | **Description** |
|-|-|-|
| **Shape** | Enum | Sets the shape for particles to collide with. For more information, refer to the [Shape dropdown](#shape-dropdown) section. |
| **Mode** | Enum | Specifies how particles interact with the collider. The options are:<ul><li><strong>Solid</strong>: Destroys particles when they enter the shape. If you set <strong>Shape</strong> to <strong>Plane</strong>, particles collide with the plane when they travel away from the normal of the plane.</li><li><strong>Inverted</strong>: Destroys particles when they leave the shape. If you set <strong>Shape</strong> to <strong>Plane</strong>, particles collide with the plane when they travel in the same direction as the normal of the plane.</li></ul> |
| **Radius Mode** | Enum | Sets the collision radius of the particles. The options are:<ul><li><strong>None</strong>: Sets the collision radius to zero.</li><li><strong>From Size</strong>: Sets the collision radius for each particle to its individual size.</li><li><strong>Custom</strong>: Sets the collision radius to the value of **Radius** in the [Block properties](#block-properties).</li></ul> |
| **Collision Attributes** | Enum | Specifies whether Unity stores data in the collision attributes of particles. The options are:<ul><li><strong>No Write</strong>: Doesn't write or store collision attributes.</li><li><strong>Write Punctual Contact only</strong>: Updates the collision attribute only when a specific, single-point collision occurs. This setting has no effect in a Trigger Shape Block.</li><li><strong>Write Always</strong>: Updates the collision attribute every time a collision occurs.</li></ul> |

<a name="shape-dropdown"></a>
### Shape dropdown

| **Shape** | **Description** |
|-|-|
| **Sphere**| Sets the collision shape as a spherical volume. |
| **Oriented Box** | Sets the collision shape as a box volume. |
| **Cone**| Sets the collision shape as truncated cone volume.|
| **Plane** | Sets the collision shape as a flat plane with infinite length and width. |
| **Signed Distance Field** | Sets the collision shape as a signed distance field (SDF), so you can create precise complex collision with an existing asset. To generate a signed distance field asset, use the [SDF Bake Tool](sdf-bake-tool.md) or an external digital content creation (DCC) tool. |

## Block properties

| **Input** | **Type** | **Description**|
|-|-|-|
| **Sphere**| [Sphere](Type-Sphere.md) | Sets the sphere that particles collide with. This property is available only if you set **Shape** to **Sphere**. |
| **Box** | [OrientedBox](Type-OrientedBox.md) | Sets the box that particles collide with. This property is available only if you set **Shape** to **Box**. |
| **Cone**| [Cone](Type-Cone.md) | Sets the cone that particles collide with. This property is available only if you set **Shape** to **Cone**. |
| **Plane** | [Plane](Type-Plane.md) | Sets the plane that particles collide with. This property is available only if you set **Shape** to **Plane**. |
| **Distance Field**| Signed distance field | Sets the signed distance field (SDF) that particles collide with. This property is available only if you set **Shape** to **Signed Distance Field**. |
| **Field Transform** | [Transform](Type-Transform.md) | Sets the position, size, and rotation of the **Distance Field**. This property is available only if you set **Shape** to **Signed Distance Field**. |
| **Radius**| Float | Sets the collision radius of the particles. This property is available only if you set **Radius Mode** to **Custom**. |

## Inspector window properties

| **Property** | **Type** | **Description** |
|-|-|-|
| **Behavior** | Enum | Specifies how particles behave when they collide with the shape. The options are: <ul><li><strong>None</strong>: Changes the Block to a Trigger Shape Block, so particles don't bounce off the shape.</li><li><strong>Collision</strong>: Changes the Block to a [Collision Shape Block](Block-CollisionShape.md), so particles bounce off the shape.</li><li><strong>Kill</strong>: Destroys a particle when it collides with the shape.</li></ul> |
