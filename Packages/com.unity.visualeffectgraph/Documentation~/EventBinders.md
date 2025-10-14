<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This feature is currently experimental and is subject to change in later major versions. To use this feature, enable <b>Experimental Operators/Blocks</b> in the <b>Visual Effects</b> tab of your Project's Preferences.</div>

# Event Binders

Event Binders refer to a set of **MonoBehaviour** Scripts that help you trigger [Events](Events.md) in [Visual Effects](VisualEffectComponent.md) when a particular event happens in the Scene. For example, when a Renderer becomes visible. Event Binders can also attach [Event Attribute Payloads](Events.md#eventattribute-payloads) to the Events that they send.

## Mouse Event Binder

The Mouse Event Binder triggers an event in the target Visual Effect based on actions that you perform with the mouse (for example, clicking, hovering, or dragging).

**Requires:** A Collider on the same GameObject as this component.

**Properties:**

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Target**                 | The Visual Effect instance to trigger the Event on.          |
| **Event Name**             | The name of the Event to trigger.                            |
| **Activation**             | Specifies when this component triggers the Event:<br/>&#8226; **OnMouseDown**: When you click down on the Collider.<br/>&#8226; **OnMouseUp**: When you release a click on the Collider.<br/>&#8226; **OnMouseEnter**: When the cursor enters the Collider's on-screen area.<br/>&#8226; **OnMouseExit**: When the cursor exits the Collider's on-screen area.<br/>&#8226; **OnMouseOver**: When the cursor hovers over the Collider's on-screen area.<br/>&#8226; **OnMouseDrag**: When you drag the mouse over the Collider's on-screen area. |
| **Raycast Mouse Position** | Specifies whether to use a `position ` EventAttribute as the result of a raycast towards the Collider. |

## Rigid Body Collision Event Binder

The Rigid Body Collision Event Binder triggers an Event in the target Visual Effect every time something collides with the Rigidbody attached to the same GameObject as this component. This binder also attaches the collision world position to the `position` EventAttribute, and the contact Normal to the `velocity ` EventAttribute.

**Requires:** A Rigidbody and a Collider on the same GameObject as this component.

**Properties:**

| **Properties** | **Description**                                     |
| -------------- | --------------------------------------------------- |
| **Target**     | The Visual Effect instance to trigger the Event on. |
| **Event Name** | The name of the event to trigger.                   |

## Trigger Event Binder

The Trigger Event Binder triggers an Event in the target Visual Effect every time a Collider from a list interacts with the attached trigger Collider. This binder also attaches the world position of the Collider instigator to the `position` EventAttribute.

**Requires:** A Collider with **Is Trigger** set to ` true` on the same GameObject as this component.

**Properties:**

| **Property**   | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **Target**     | The Visual Effect instance to trigger the Event on.          |
| **Event Name** | The name of the Event to trigger.                            |
| **Colliders**  | A list of Colliders that trigger the Event when something interacts with them. |
| **Activation** | Specifies which action triggers the Event:<br/>&#8226; **OnEnter**: Triggers the Event when any Collider enters the trigger.<br/>&#8226; **OnExit**: Triggers the Event when any Collider exits the trigger.<br/>&#8226; **OnStay**: Triggers the Event when any Collider stays in the trigger. |

## Visibility Event Binder

The Visibility Event Binder triggers an Event in the target Visual Effect every time the Renderer attached to this GameObject becomes visible or invisible.

**Requires:** A Renderer on the same GameObject as this component.

**Properties:**

| **Property**   | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **Target**     | The Visual Effect instance to trigger the Event on.          |
| **Event Name** | The name of the Event to trigger.                            |
| **Activation** | Specifies when to trigger the Event:<br/>&#8226; **OnBecameVisible**: Triggers the Event on the frame that the Renderer goes from invisible to visible.<br/>&#8226; **OnBecameInvisible**: Triggers the Event on the frame that the Renderer goes from visible to invisible. |
