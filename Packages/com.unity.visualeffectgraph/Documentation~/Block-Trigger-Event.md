# Trigger Event Block reference

The Trigger Event Block spawns particles using a [GPU Event](Context-GPUEvent.md).

## Block compatibility

You can add the Trigger Event Block to the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)

To add a Trigger Event Block to your graph, [open the menu for adding a graph element](VisualEffectGraphWindow.md#adding-graph-elements) then select **GPUEvent** > **Trigger Event**.

Trigger Blocks always execute at the end of the Update Context, regardless of where you place the Block.

## Block settings

| **Property** | **Type** | **Description** |
|-|-|-|
| **Mode** | Enum | The options are: <ul><li><strong>Always</strong>: Spawns particles each frame.</li><li><strong>Over Time</strong>: Spawns particles at a specified rate per second.</li><li><strong>Over Distance</strong>: Spawns a set number of particles over the distance a parent particle moves.</li><li><strong>On Die</strong>: Spawns particles when a parent particle dies.</li><li><strong>On Collide</strong>: Spawns particles when a particle collides with another particle.</li></ul> |

## Block properties

| **Input** | **Type** | **Description** |
|-|-|-|
| **Count** | Uint | Sets the number of particles to spawn. This property is available only if you set **Mode** to **Always**, **On Die**, or **On Collide**. |
| **Rate** | Float | Sets the rate at which to spawn particles. This property is available only if you set **Mode** to **Over Time** or **Over Distance**. If you set **Mode** to **Over Time**, **Rate** sets the number of particles to spawn per second. If you set **Mode** to **Over Distance**, **Rate** sets the number of particles to spawn over the distance the parent particle moves. |

## Block output

| **Output** | **Type** | **Description** |
|-|-|-|
| **Evt** | [GPU Event](Context-GPUEvent.md) | The GPU Event to trigger. |

## Inspector window properties

| **Property** | **Type** | **Description** |
|-|-|-|
| **Clamp To One** | Bool | Limits GPU Events to one per frame. This property is available only if you set **Mode** to **Over Time** or **Over Distance**. |
