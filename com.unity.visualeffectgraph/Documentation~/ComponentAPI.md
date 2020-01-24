<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>
# Visual Effect Component API

 [Visual Effect Graphs](VisualEffectGraphAsset.md) are instantiated into scenes using the [Visual Effect Component](VisualEffectComponent.md) . This allows using different instances of effects that you can control independently,  make variations using Property overrides, and control the effect through a C# API. 

This document present common use cases and good practices in order to use the [Component API](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VisualEffect.html).

## Setting a Visual Effect Graph

The [Visual Effect Graph](VisualEffectGraphAsset.md) template can be changed at runtime using the `effect.visualEffectAsset ` property.

<u>Changing the Visual Effect Graph Asset will also reset the component:</u>

* Total Time is reset to 0.0 due to `Reset()` being called.
* Event Attributes are discarded.

<u>Some values are **not reset** while changing graphs:</u>

* Exposed Property Overrides (if new Graph Exposes properties of same name and type)
* Random Seed and Reset Seed On Play Value
* Default Event Override
* Rendering Settings overrides.

## Controlling Play State

### Common Controls

Effect playback can be controlled using the API using the following:

* Play : `effect.Play()` or `effect.Play(eventAttribute)` if needing Event Attributes.
* Stop : `effect.Stop()` or `effect.Stop(eventAttribute)` if needing Event Attributes.
* Pause : `effect.pause = true` or  `effect.pause = false`  (not serialized)
* Step : `effect.AdvanceOneFrame()` (only if `effect.pause == true`)
* Reset Effect : `effect.Reinit()` this also :
  * Resets TotalTime to 0.0
  * Sends again the **Default Event**
* Setting **Play Rate** : `effect.playRate = value` (not serialized)

### Default Event

Upon enabling the GameObject, or the Component, a default event is sent to the graph : by default it is `OnPlay` which is the implicit Start of [Spawn Contexts](Contexts.md#spawn).

You can override this property:

* on the [Visual Effect Inspector](VisualEffectComponent.md) using the **Initial Event Name** field.
* using the API : `initialEventName = "MyEventName"`
* using the API : `initialEventID = Shader.PropertyToID("MyEventName")`; 
* using the [ExposedProperty Helper Class](ExposedPropertyHelper.md) 

## Random Seed Control

Every instance has settings and controls about random seed and behavior when playing:

* `resetSeedOnPlay = true/false` : Controls whether a new random seed is computed every time the `Play()` function is called : Which cause each random value used by the graph to be different.
* `startSeed = intSeed` Sets manually the Random Number Generator Seed used for this Visual Effect Instance. (Ignored if `resetSeedOnPlay == true`)

## Property Interface

Exposed Properties state and values can be accessed using a variety of methods on the [Visual Effect Component](VisualEffectComponent.md) . Most of the API methods allow access to the property via:

* a `string` property Name : easy to use but less optimized.
* a `int` property ID that can be generated and cached using `Shader.PropertyToID(string name)`
* using the [ExposedProperty Helper Class](ExposedPropertyHelper.md) 

#### Checking Properties

You can check if the Visual Effect Component's Graph Asset exposes a specific property using a set of methods depending on the property type you want to check : `HasInt(property)`,  `HasUInt(property)`,`HasBool(property)`, `HasFloat(property)`, `HasVector2(property)`, `HasVector3(property)`, `HasVector4(property)`, `HasGradient(property)`,  `HasAnimationCurve(property)`, `HasMesh(property)`, `HasTexture(property)`, `HasMatrix4x4(property)`

Every Has method will return true if an Exposed property of the same name or ID has been found.

#### Getting Values

You can get an Exposed Property Value using a set of methods depending on the property type: `GetInt(property)`,  `GetUInt(property)`,`GetBool(property)`, `GetFloat(property)`, `GetVector2(property)`, `GetVector3(property)`, `GetVector4(property)`, `GetGradient(property)`, `GetAnimationCurve(property)`, `GetMesh(property)`, `GetTexture(property)`, `GetMatrix4x4(property)`

Every Get method will return the corresponding value or the type's default value if the property is not present.

#### Setting Values

You can set an Exposed Property Value using a set of methods depending on the property type: `SetInt(property,value)`,  `SetUInt(property,value)`,`SetBool(property,value)`, `SetFloat(property,value)`, `SetVector2(property,value)`, `SetVector3(property,value)`, `SetVector4(property,value)`, `SetGradient(property,value)`, `SetAnimationCurve(property,value)`, `SetMesh(property,value)`, `SetTexture(property,value)`, `SetMatrix4x4(property,value)`

Every Set method will set the corresponding value to the property and will set it as overridden.

#### Resetting Property Overrides and Default Values

Resetting Property Overrides on a  [Visual Effect Component](VisualEffectComponent.md) can be achieved using the `ResetOverride(property)`.

## Events

### Sending Events

You can send [Events](Events.md) to the Visual Effect instance using the following API:

* `SendEvent(eventNameOrId)`
* `SendEvent(eventNameOrId, eventAttribute)`

The parameter `eventNameOrId` can be of the following types:

- a `string` event Name : easy to use but less optimized.
- an `int` event ID that can be generated and cached using `Shader.PropertyToID(string name)`
- an [ExposedProperty Helper Class](ExposedPropertyHelper.md) that will cache the `int` value corresponding to the string name

The optional EventAttribute parameter attaches an **Event Attribute Payload** to the event, so it can be processed by the Graph.

> Events are sent to the API then Consumed in the next Visual Effect Component Update, happening the next frame.

### Event Attributes

Event Attributes are [Attributes](Attributes.md) attached to [Events](Events.md) and that can be processed by the graph. Event Attributes are stored in a `VFXEventAttribute` class, created from an instance of a [Visual Effect](VisualEffectComponent.md), based on its currently set  [Visual Effect Graph Asset](VisualEffectGraphAsset.md).

#### Creating Event Attributes

In order to Create and Use a `VFXEventAttribute` use the `CreateVFXEventAttribute()` method of the `VisualEffect` component. If you plan on sending multiple times events using attributes, you will preferably cache this object so you can reuse it.

#### Setting Attribute Payload

Once Created, you can access an API similar to Has/Get/Set Properties in order to set the Attribute Payload.

* Has : `HasBool`, `HasVector3`, `HasFloat`,... To check if attribute is present
* Get : `GetBool`, `GetVector3`, `GetFloat`,... To get attribute value
* Set: `SetBool`, `SetVector3`, `SetFloat`,... To get attribute value

The full API Reference is available on [Scripting API Documentation](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXEventAttribute.html).

The attribute name or ID can be of the following types:

- a `string` attribute Name : easy to use but less optimized.
- an `int` attribute ID that can be generated and cached using `Shader.PropertyToID(string name)`
- an [ExposedProperty Helper Class](ExposedPropertyHelper.md) that will cache the int value corresponding to the string name

#### Life Cycle and Compatibility

Event Attributes, when created, are compatible with the Visual Effect Graph Asset that is currently set on the Visual Effect Component. This means that you will be able to use the same `VFXEventAttribute` to send events to instances of the graph, as long as you do not change the `visualEffectAsset` property of the Component to another Graph.

If you manage multiple Visual Effect instances in scene and want to share event payloads, you can cache one VFXEventAttribute and use it on all the instances.

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

## Debug Functionality

Some debug Functionality values can be get on every component:

* `aliveParticleCount` : return a read-back value of the alive particles in the whole effect. Readback of this value happens asynchronously every second, and it does return the value of a previous frame.
* `culled` return whether the effect was culled from any camera at the previous frame.