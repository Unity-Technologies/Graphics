# Output Event Handlers

A VFXOutputEventAbstractHandler is an API helper that hooks into an [Output Event](Contexts.md#output-events) to allow you to execute scripts based on the event.

The Visual Effect Graph includes a set of example scripts as a sample. For information on how to install the sample, see [Installing sample scripts](#installing-sample-scripts).

## Output Event Handler API

To create your own output event handler, write a script that extends the `UnityEngine.VFX.Utility.VFXOutputEventAbstractHandler` class.

When you write a MonoBehaviour that extends this class, it reduces the  amount of code required to perform a hook. This is because the base  class does the job of filtering the event and calls the following method :

`override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)`

When you implement this method, Unity calls it every time the event triggers and passes in the event's attributes.

### Example

The following example teleports a Game Object to the Given position when it receives an event.

```c#
[RequireComponent(typeof(VisualEffect))]
public class VFXOutputEventTeleportObject : VFXOutputEventAbstractHandler
{
    public Transform target;

    static readonly int kPosition = Shader.PropertyToID("position");

    public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
    {
        if(target != null)
            target.position = eventAttribute.GetVector3(kPosition);
    }
}
```

## Installing Sample Scripts

To help you create your own VFXOutputEventAbstractHandler, the Visual Effect  Graph package includes a set of example scripts that you can install via the Package Manager. To do this, select the Visual Effect Graph package then, next to **OutputEvent Helpers Sample**, click the **Import** button.

### Using Output Event Helpers

The OutputEvent Helper scripts are deployed into the project as a Sample contained in the folder :  `Assets/Samples/Visual Effect Graph/(version)/OutputEvent Helpers/`.

These helpers are MonoBehaviour Scripts that you can add to Game Objects that hold a VisualEffect Component. These scripts will listen for OutputEvents of a given name, and will react to these events by performing various actions.

Some of these scripts can be safely previewed in editor, while other aren't. The Inspector shall display a **Execute in Editor** toggle if the script is able to be previewed. Otherwise, you can press the Play button to experience the behavior in play mode.

<u>The Sample Output Event Helpers are the following:</u>

* **VFXOutputEventCMCameraShake** : Upon receiving a given OutputEvent, Triggers a Camera Shake through the Cinemachine Impulse Sources system.
* **VFXOutputEventPlayAudio** : Upon receiving a given OutputEvent, Plays a sound from an AudioSource
* **VFXOutputEventPrefabSpawn** : Upon receiving a given OutputEvent, Spawns an invisible prefab game object from a pool of prefabs, at a given position, rotation and angle, and manages its life based on the Event lifetime attribute. Spawned Prefabs can also be customized upon spawn with the help of **VFXOutputEventPrefabAttributeHandler** scripts in order to configure child elements of the prefab (see below).
* **VFXOutputEventRigidBody** : Upon receiving a given OutputEvent, applies a force to a RigidBody.
* **VFXOutputEventRigidBody** : Upon receiving a given OutputEvent, triggers a UnityEvent

### Using Output Event Prefab Spawn

The **VFXOutputEventPrefabSpawn** script handles the spawn of hidden prefabs from a pool. These prefabs are instantiated as invisible, and disabled upon enabling this monobehaviour, and destroyed upon disabling the monobehaviour.

Upon receiving a given OutputEvent, the system will look for a free (disabled) prefab, and if any available will :

* enable it

* set its position from the position attribute,  if enabled in the inspector

* set its rotation from the angle attribute, if enabled in the inspector

* set its scale from the scale attribute, if enabled in the inspector

* start a coroutine with a delay (based on the lifetime attribute), that will disable (free) the prefab once elapsed, thus making it available for spawn upon a future OutputEvent

* search for any **VFXOutputEventPrefabAttributeHandler** scripts in the prefab instance in order to perform attribute binding.



**VFXOutputEventPrefabAttributeHandler** scripts are used to configure parts of the prefab, based on the event that spawned the prefab. Here are two examples bundled with the samples:

* **VFXOutputEventPrefabAttributeHandler_Light** : this script, upon prefab spawn, will set the color and the brightness of the light, based on the OutputEvent color attribute and a brightnessScale property.
* **VFXOutputEventPrefabAttributeHandler_RigidBodyVelocity** : this script, upon prefab spawn, will set the current rigid body velocity, based on the OutputEvent velocity attribute.
