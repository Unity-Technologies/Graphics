<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>
# Attributes

Attributes are data attached to elements present in Systems. For instance, the color of a particle, its position, or the amount of particles that a spawn system need to create.

Attributes can be read or written in systems in order to perform custom behavior and differentiate elements.

Attributes are stored in systems whenever needed so only the necessary data are stored in order to save memory. 

## Accessing Attributes

### Writing Attributes

Attributes can be written using [Blocks](Blocks.md). Blocks are the only graph elements that can write attributes to the system.

Attributes, when written, are stored into simulation data if needed in another, later context :

* Attributes written in Initialize / Update contexts will be stored only if read in Update / Output Contexts.
* Attributes written in Output Contexts do not store into simulation data and are only used for rendering.

### Reading Attributes

Reading attributes can be done through Operators and Blocks:

* Using a Get [Attribute] Operator.
* Using Different Composition Modes in Set [Attribute] Blocks (Add, Multiply, Blend) that depends on the previous value of the attribute.

> Reading an attribute that is not stored into the simulation will result in reading its default, constant value.

> **WARNING**: It is currently only possible to read attributes in Particle and ParticleStrip Systems. Reading attributes in Spawn Systems can only be achieved using [Spawner Callbacks](SpawnerCallbacks.md) .

## Attribute Locations

Attributes are stored in data containers specific to every system. However, Reading an attribute can be achieved on the current simulation data pool or in another data pool if the system depends on.

### Current

Current Attribute Location refers to the **current** system data where the value is read from. 

* Particle Data from a Particle System
* ParticleStrip Data from a ParticleStrip System
* SpawnEvent Data from a Spawn context or sent through [SendEvent](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VisualEffect.SendEvent.html) [EventAttribute](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXEventAttribute.html) Payload.

### Source

Source Attribute Location refers to the **previous system** data where the value is read from. 

Source attributes can be **read only in the first context of a system, after a system data change**. (For instance : EventAttributes and GPU EventAttributes can only be accessed in Particle / ParticleStrip System Initialize Contexts).

* In Initialize Particle / Initalize Particle Strips Contexts:
  * From incoming Spawn Contexts.
  * From other Particle Systems, through GPUEvent spawn.

## Variadic Attributes

Some attributes possess **Variadic** Properties : these attributes can be a scalar or a vector of different dimensions depending on the components you require for simulating and/or rendering.

In the case of a variadic attribute, all other implicit components will be read using their default values.

> For instance, the `scale` of a Quad particle can be expressed as a `Vector2` (width, and length of the quad), whereas the `scale` of a Box particle will be expressed as a `Vector3` (width, length and depth of the cube). When setting variadic attributes, a Drop Down of all channel combinations will enable you to write only the necessary channels.

> Another example is for the Rotation of a sprite around its normal, only the Z component of the angle attribute (`angleZ`) would be used, so `angleX`, and `angleY` are not stored.

