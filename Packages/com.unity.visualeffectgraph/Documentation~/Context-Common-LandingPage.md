# Common Contexts reference

Spawn particles, set their starting values, and update them each frame.

These Contexts form the core simulation loop that every particle system relies on, regardless of how the particles are rendered.

| **Topic** | **Description** |
| --- | --- |
| [Initialize Particle Context](Context-Initialize.md) | Process an event and initialize new particle elements. |
| [Spawn Context](Context-Spawn.md) | Control the spawn rate of particles, or create a custom spawning behavior. |
| [Update Particle Context](Context-Update.md) | Manage the behavior of particles or particle strips from an Initialize Context. |

**Note**: The **Initialize Particle Strip** menu entry also opens the [Initialize Particle Context](Context-Initialize.md) page. Set its **Data Type** setting to **Particle Strip** to initialize particle strips instead of particles.

## Additional resources

- [Contexts](Contexts.md)
- [Blocks](Blocks.md)
