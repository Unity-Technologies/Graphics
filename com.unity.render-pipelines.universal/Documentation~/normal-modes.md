# Normal Modes
The lit shaders of URP all have configurable normal modes for back faces. This setting determines how the normals of the back face are calculated and can be set to one of three different modes:

 Normal Mode                  | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| Flip                        | This mode flips the normal direction on back faces. For surfaces seeming convex on the front face, this will create a concave impression on the back face and vice versa.
| Mirror                      | This mode mirrors the normal direction on back faces. This retains the perceived surface curvature and makes the front face indistinguishable from the back face.
| None                        | This mode does not modify the normal direction and keeps it the same as the front face.
