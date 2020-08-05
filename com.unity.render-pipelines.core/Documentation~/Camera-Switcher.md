# Camera Switcher

The **CameraSwitcher** component allows you to define a List of Cameras in the Scene and then use the Debug Window to switch between them in Play Mode. This is useful when you want a set of different fixed views for profiling purposes where you need to guarantee that the Camera view is in the same position between sessions.

## Properties

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Cameras**  | Drag and drop GameObjects that have a Camera component attached to add them to this List of Cameras. The Debug Window can switch between the Cameras in this List. |