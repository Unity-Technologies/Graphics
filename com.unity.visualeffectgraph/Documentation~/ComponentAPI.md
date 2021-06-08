# Visual Effect component API

To create an instance of a [Visual Effect Graph](VisualEffectGraphAsset.md) in a Scene, Unity uses the [Visual Effect component](VisualEffectComponent.md). The Visual Effect component attaches to GameObjects in your Scene and references a Visual Effect Graph which defines the visual effect. This allows you to create different instances of effects at various positions and orientations, and control each effect independently. To control an effect at runtime, Unity provides C# API that you can use to modify the Visual Effect component and set [Property](Properties.md) overrides.

This document presents common use cases and describes good practices to consider when you use the [component API](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VisualEffect.html).

## Setting a Visual Effect Graph

To change the [Visual Effect Graph](VisualEffectGraphAsset.md) at runtime, assign a new Visual Effect Graph Asset to the `effect.visualEffectAsset ` property. When you change the Visual Effect Graph, the component resets the value of some of its properties.

The values that reset are:

* **Total Time**: When you change the graph, the API calls the `Reset()` function which sets this value to 0.0f.
* **Event Attributes**: The component discards all Event [Attribues](Attributes.md).

The values that do **not** reset are:

* **Exposed Property Overrides**: If the new Visual Effect Graph Asset exposes a property that has the same name and type as a property from the previous Asset, the value for this property does not reset.
* **Random Seed** and **Reset Seed On Play Value**.
* **Default Event Override**.
* **Rendering Settings overrides**.

## Controlling play state

You can use the API to control effect playback.

### Common controls

* **Play** : `effect.Play()` or `effect.Play(eventAttribute)` if needing Event Attributes.
* **Stop** : `effect.Stop()` or `effect.Stop(eventAttribute)` if needing Event Attributes.
* **Pause** : `effect.pause = true` or  `effect.pause = false`. Unity does not serialize this change.
* **Step** : `effect.AdvanceOneFrame()`. This only works if `effect.pause` is set to `true`.
* **Reset Effect** : `effect.Reinit()` this also :
  * Resets `TotalTime` to 0.0f.
  * Re-sends the **Default Event** to the Visual Effect Graph.
* **Play Rate** : `effect.playRate = value`. Unity does not serialize this change.

### Default Event

When the Visual Effect component (or the GameObject it attaches to) enables, it sends an [Event](Events.md) to the graph. By default, this Event is `OnPlay` which is the standard start for [Spawn Contexts](Contexts.md#spawn).

You can change the default Event in the following ways:

* On the [Visual Effect Inspector](VisualEffectComponent.md), change the **Initial Event Name** field.
* In the component API : `initialEventName = "MyEventName";`.
* In the component API : `initialEventID = Shader.PropertyToID("MyEventName");`.
* Using the [ExposedProperty Helper Class](ExposedPropertyHelper.md).

## Random seed control

Every effect instance has settings and controls for its random seed. You can modify the seed to influence the random values the Visual Effect Graph uses.

* `resetSeedOnPlay = true/false`: Controls whether Unity computes a new random seed every time you call the `ReInit()` function. This causes each random value the Visual Effect Graph uses to be different to what it was in previous simulations.
* `startSeed = intSeed`: Sets a manual seed that the **Random Number** Operator uses to create random values for this Visual Effect. Unity ignores this value if `resetSeedOnPlay` is set to `true`.

<a name="PropertyInterface"></a>

## Property interface

To access the state and values of Exposed Properties, you can use multiple methods in the [Visual Effect component](VisualEffectComponent.md). Most of the API methods allow access to the property via the following methods:

* A `string` property name. This is easy to use, but is the least optimized method.
* An `int` property ID. To generate this ID from a string property name, use `Shader.PropertyToID(string name)`. This is the most optimized method.
* The [ExposedProperty Helper Class](ExposedPropertyHelper.md). This combines the ease of use the string property name provides with the efficiency of the integer property ID.

### Checking for exposed properties

You can check if the component's Visual Effect Graph contains a specific exposed property. To do this, you can use the method from the following group that corresponds to the property's type:

* `HasInt(property)`
* `HasUInt(property)`
* `HasBool(property)`
* `HasFloat(property)`
* `HasVector2(property)`
* `HasVector3(property)`
* `HasVector4(property)`
* `HasGradient(property)`
* `HasAnimationCurve(property)`
* `HasMesh(property)`
* `HasTexture(property)`
* `HasMatrix4x4(property)`

For each method, if the Visual Effect Graph contains an exposed property of the correct type with the same name or ID you pass in, the method returns `true`. Otherwise the method returns `false`.

### Getting the values of exposed properties

The component API allows you to get the value of an exposed property in the component's Visual Effect Graph. To do this, you can use the method from the following group that corresponds to the property's type:

* `GetInt(property)`
* `GetUInt(property)`
* `GetBool(property)`
* `GetFloat(property)`
* `GetVector2(property)`
* `GetVector3(property)`
* `GetVector4(property)`
* `GetGradient(property)`
* `GetAnimationCurve(property)`
* `GetMesh(property)`
* `GetTexture(property)`
* `GetMatrix4x4(property)`

For each method, if the Visual Effect Graph contains an exposed property of the correct type with the same name or ID you pass in, the method returns the property's value. Otherwise the method returns the default value for the property type.

### Setting the values of exposed properties

The component API allows you to set the value of an exposed property in the component's Visual Effect Graph. To do this, you can use the method from the following group that corresponds to the property's type:

* `SetInt(property,value)`
* `SetUInt(property,value)`
* `SetBool(property,value)`
* `SetFloat(property,value)`
* `SetVector2(property,value)`
* `SetVector3(property,value)`
* `SetVector4(property,value)`
* `SetGradient(property,value)`
* `SetAnimationCurve(property,value)`
* `SetMesh(property,value)`
* `SetTexture(property,value)`
* `SetMatrix4x4(property,value)`

Each method overrides the value of the corresponding property with the value you pass in.

### Resetting property overrides and default values

The component API allows you to reset property overrides back to their original values. To do this, use the `ResetOverride(property)` method.

## Events

### Sending Events

The component API allows you to send [Events](Events.md) to the component's Visual Effect Graph at runtime. To do this, use either of the following methods:

* `SendEvent(eventNameOrId)`
* `SendEvent(eventNameOrId, eventAttribute)`

The `eventNameOrId` parameter can be one of the following types:

* A `string` property name. This is easy to use, but is the least optimized method.
* An `int` property ID. To generate this ID from a string property name, use `Shader.PropertyToID(string name)`. This is the most optimized method.
* The [ExposedProperty Helper Class](ExposedPropertyHelper.md). This combines the ease of use the string property name provides with the efficiency of the integer property ID.

The optional `eventAttribute` parameter attaches an **Event Attribute Payload** to the Event. They payload provides data that the Graph processes with the Event.

**Note**: When you send an Event, the Visual Effect component processes it in its next Update(), which happens during the next frame.

### Event Attributes

Event Attributes are [Attributes](Attributes.md) that attach to [Events](Events.md) and can be processed by the Visual Effect Graph. To create and store Event Attributes, use the `VFXEventAttribute` class. The Visual Effect component is responsible for creating instances of the `VFXEventAttribute` class and creates them based on the currently assigned [Visual Effect Graph](VisualEffectGraphAsset.md).

#### Creating Event Attributes

To create a `VFXEventAttribute`, use the `CreateVFXEventAttribute()` method of the Visual Effect component. If you want to send the same Event multiple times with the same attributes, store the `VFXEventAtrribute`, rather than create a new one every time you send the Event. When you send an Event to a Visual Effect Graph, Unity creates a copy of the EventAttribute in its current state and sends the copy. This means that, after you send the Event, you can safely modify the EventAttribute without affecting the information sent to the Visual Effect Graph.

#### Setting the Attribute's payload

After you create an Event Attribute, you use API similar to the Has/Get/Set property methods described in the [Property interface section](#PropertyInterface) to set the Attribute Payload.

* Has : `HasBool`, `HasVector3`, `HasFloat`,... To check if an Attribute exists.
* Get : `GetBool`, `GetVector3`, `GetFloat`,... To get the value of an Attribute.
* Set : `SetBool`, `SetVector3`, `SetFloat`,... To set the value of an Attribute.

For the full Attribute API documentation, see [VFXEventAttribute](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXEventAttribute.html) in the Unity Script Reference.

The attribute name or ID can be one of the following types:

- A `string` property name. This is easy to use, but is the least optimized method.
- An `int` property ID. To generate this ID from a string property name, use `Shader.PropertyToID(string name)`. This is the most optimized method.
- The [ExposedProperty Helper Class](ExposedPropertyHelper.md). This combines the ease of use the string property name provides with the efficiency of the integer property ID.

#### Life cycle and compatibility

When you create an Event Attribute, it is compatible with the Visual Effect Graph Asset currently assigned to the Visual Effect component. This means that you are able to use the same `VFXEventAttribute` to send Events to other instances of the same graph. If you change the `visualEffectAsset` property of a Visual Effect component to another graph, you can no longer use the same `VFXEventAttribute` to send Events to it.

If you want to manage multiple Visual Effect instances in the same Scene and want to share Event payloads, you can store one `VFXEventAttribute` and use it on all the instances.

#### Example (in a MonoBehaviour)

```c#
VisualEffect visualEffect;
VFXEventAttribute eventAttribute;

static readonly ExposedProperty positionAttribute = "Position"
static readonly ExposedProperty enteredTriggerEvent = "EnteredTrigger"

void Start()
{
    visualEffect = GetComponent<VisualEffect>();
    // Caches an Event Attribute matching the
    // visualEffect.visualEffectAsset graph.
    eventAttribute = visualEffect.CreateVFXEventAttribute();
}

void OnTriggerEnter()
{
    // Sets some Attributes
    eventAttribute.SetVector3(positionAttribute, player.transform.position);
    // Sends the Event
    visualEffect.SendEvent(enteredTriggerEvent, eventAttribute);
}
```

## Debugging

Each Visual Effect component contains the following debug properties:

* `aliveParticleCount`: The number of alive particles in the entire effect.<br/>**Note**: The component calculates this value asynchronously every second which means that the result may be the number of alive particles during a frame rendered up to a second before you access this property.
* `culled`: Indicates whether any Camera culled the effect in the previous frame.
