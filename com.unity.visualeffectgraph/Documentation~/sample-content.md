# Visual Effect Graph Sample Content

The Visual Effect Graph comes with a set of samples to help you get started.

A sample is a set of assets that you can import into your project and use as a base to build upon or learn how to use a feature. The Visual Effect Graph also includes some helpful Nodes

To find these samples, first [install the Visual Effect Graph](GettingStarted.md) then:

1. Go to **Windows > Package Manager.**

2. From the [Package list view](https://docs.unity3d.com/Manual/upm-ui-list.html), select **Visual Effect Graph**. If it is not there:

3. 1. From the [Packages drop-down menu](https://docs.unity3d.com/Manual/upm-ui.html), select **Unity Registry** or **In Project**.
   2. Go to **Edit** > **Project Settings** > **Package Manager**
   3. In the **Advanced Settings** drop-down, enable **Show Dependencies**. Visual Effect Graph should now appear in the Packages list view.

4. In the main window that shows the package's details, find the **Samples** section.

5. To import a sample into your project, click **Import**. This creates a **Samples** folder in your project and imports the sample you selected into it. This is also where Unity imports any future samples into.



## Output Event Handlers

This sample includes helper MonoBehaviour Scripts which you can attach to GameObjects that have a [VisualEffect](VisualEffectComponent.md) component. These scripts listen for Output Events of a given name and react by performing various actions. Some of the scripts support previews in the Editor and some do not. For those that do, the Inspector contains an **Execute in Editor** toggle. Otherwise, enter Play Mode to view the behavior.

The helper scripts this sample includes are:

- **VFXOutputEventCMCameraShake**: When it receives an Output Event with the name you specify, this helper script triggers a camera shake through the [Cinemachine Impulse Sources](https://docs.unity3d.com/Packages/com.unity.cinemachine@latest?subfolder=/manual/CinemachineImpulseSourceOverview.html) system.
- **VFXOutputEventPlayAudio**: When it receives an Output Event with the name you specify, this helper script plays a sound from an AudioSource
- **VFXOutputEventPrefabSpawn**: When it receives an Output Event with the name you specify, this helper script spawns an invisible Prefab from a pool of Prefabs. It spawns them at a given position and rotation. It also manages the life of the Prefab based on the Event's [lifetime attribute](Reference-Attributes.md). When the Prefabs spawns, you can use **VFXOutputEventPrefabAttributeHandler** scripts to configure the Prefab's child elements. For more information, see [Using VFXOutputEventPrefabSpawn ](#using-vfxoutputeventprefabspawn).
- **VFXOutputEventRigidBody**: When it receives an Output Event with the name you specify, this helper script applies a force to a [RigidBody](https://docs.unity3d.com/ScriptReference/Rigidbody.html).
- **VFXOutputEventRigidBody**: When it receives an Output Event with the name you specify, this helper script triggers a [UnityEvent](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html).

### Using VFXOutputEventPrefabSpawn

The **VFXOutputEventPrefabSpawn** MonoBehaviour component spawns Prefabs from a pool. When it instantiates these Prefabs, it makes them invisible. When you enable the component, the component disables ([SetActive(false)](https://docs.unity3d.com/ScriptReference/GameObject.SetActive.html)) each Prefab. Lastly, when you disable the component, the component destroys every Prefab instance. This also occurs when you destroy the GameObject that the component is attached to.

When this component receives an Output Event with the name you specify, it looks for a free (disabled) Prefab, and, if any are available:

1. It enables the Prefab.
2. If you enable **Use Position**, it sets the Prefab's position using the [position attribute](Reference-Attributes.md).
3. If you enable **Use Rotation**, it sets the Prefab's rotation from the [angle attribute](Reference-Attributes.md).
4. If you enable **Use Scale**, it sets the Prefab's scale from the [scale attribute](Reference-Attributes.md).
5. If you enable **Use Lifetime**, it starts a Coroutine with a delay based on the [lifetime attribute](Reference-Attributes.md), that disables (frees) the Prefab after the delay. This makes it available to spawn during a future OutputEvent.
6. It searches the Prefab instance for any `VFXOutputEventPrefabAttributeHandler` scripts and invokes each one to perform attribute binding.

`VFXOutputEventPrefabAttributeHandler` scripts configure parts of the Prefab based on the event that spawned the Prefab. This sample contains two example `VFXOutputEventPrefabAttributeHandler` scripts:

- **VFXOutputEventPrefabAttributeHandler_Light**: When the Prefab spawns, this sets the color and the brightness of the attached Light component based on the OutputEvent's [color attribute](Reference-Attributes.md) and the script's **Brightness Scale** property respectively.
- **VFXOutputEventPrefabAttributeHandler_RigidBodyVelocity**: When the Prefab spawns, this sets the velocity for the attached RigidBody based on the OutputEvent's [velocity attribute](Reference-Attributes.md).

## Visual Effect Graph Additions

This sample includes assets and example graphs that help you get started with the Visual Effect Graph. For example, this sample includes:

- A set of flipbook textures.
- Example graphs that demonstrate various [Nodes](GraphLogicAndPhilosophy.md).
- Shaders and subgraphs that you can use in your project.
- Sets of textures (licensed under CC0) which you can use for visual effects in your project.

This sample uses these assets and examples to reproduce many [Built-in Particle System](https://docs.unity3d.com/Manual/Built-inParticleSystem.html) behaviors. For example, it provides a helper to replicate soft particles and a helper that enables you to sample flipbooks with cuts either linearly, or with motion vectors.
