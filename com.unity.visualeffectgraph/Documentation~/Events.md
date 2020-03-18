# Events

Events define the inputs for a Visual Effect Graph's [**processing** workflow](GraphLogicAndPhilosophy.md#processing-workflow-(vertical-logic)). The Spawn and Initialize [Contexts](Contexts.md) use Events as their inputs. Through Events, a Visual Effect Graph can :

* Start and stop spawning particles.
* Read [Event Attribute payloads](#eventattribute-payloads) sent from C#.

## Creating Events

![](Images/EventContexts.png)

In general, an Event is just a string that contains the name of the Event. To send an Event in the Visual Effect Graph, create an Event [Context](Contexts.md) and type the name of the Event in the **Event Name** property. Event Contexts have no flow input and can only connect their flow output to Spawn or Initialize Contexts.

To create an Event Context:

1. In the [Visual Effect Graph window](VisualEffectGraphWindow.md), right-click in an empty space.
2. From the menu, click **Create Node**.
3. In the Node Creation menu, click **Contexts > Event (Context)**.
4. In the **Event Name** input field, type the name of your Event.

## Default Events

The Visual Effect Graph provide two default Events:

* **OnPlay**: To enable the spawning of particles. If you do not assign an Event to a Spawn Context's **Start** flow input, the Visual Effect Graph implicitly binds this Event to that flow input instead.
* **OnStop**: To disable the spawning of particles. If you do not assign an Event to a Spawn Context's **Stop** flow input, the Visual Effect Graph implicitly binds this Event to that flow input instead.

If you connect an Event Context to a Spawn Context's **Start** or **Stop** flow input, this removes the implicit binding to the **OnPlay** and **OnStop** Events.

## Custom Events

If you do not want to use the default Events, you can use an Event Context to create your own custom Event.

To create a custom event, create an event using the **Create Node** menu, then change its name in the **Event Name** field

## EventAttribute Payloads

Event Attribute payloads are attributes attached on one event. You can set these attributes in Visual Effect Graph using the **Set [Attribute] Event Attribute>** Blocks in Spawn Contexts, but you can also attach them to events sent from the scene using the [Component API](ComponentAPI.md#event-attributes) .

EventAttribute Payloads are attributes that will implicitly travel through the graph from Events, through Spawn Systems, and that can be caught in Initialize Contexts using **Get Source Attribute Operators** and **Inherit [Attribute] Blocks**

## Default VisualEffect Event

The default Visual Effect Event defines the name of the event that is implicitly sent when a `Reset` is performed on a [Visual Effect](VisualEffectComponent.md) instance (this can happen at first start or any restart of the effect).

Default VisualEffect Event is defined in the [Visual Effect Graph Asset Inspector](VisualEffectGraphAsset.md) but can be overridden in any [Visual Effect Inspector](VisualEffectComponent.md) for any instance in the scene.

## GPU Events

GPU Events is an **Experimental feature** of Visual Effect Graph : It enables particle spawn based on other Particles.  You can enable this option in [Visual Effect Preferences](VisualEffectPreferences.md) .

![](Images/GPUEvent.png)

GPU Events are Event Contexts that relies on Data sent from other Systems, for instance when a particle dies, or other conditions. The following Update Blocks can send GPU Event Data:

* **Trigger Event On Die** :  Spawns N Particles on another system when a particle dies
* **Trigger Event Rate** : Spawn N Particles per second (or per distance travelled), based on a particle from a system
* **Trigger Event Always** :  Spawns N Particles every Frame.

These Blocks connect to a **GPUEvent** Context. This context does not handle any Blocks but instead connects to a Initialize Context of a child system.

In order to gather data from the parent particle, the child system must refer to [Source Attributes](Attributes.md) in its Initialize Context, by using **Get Source Attribute Operator**, or **Inherit Attribute Block**,as shown as in the example above : The child System inherits the source position of the particle that created it, and inherits roughly 50% of its speed.

