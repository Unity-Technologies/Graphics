# What's new in Core version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the Core Render Pipeline package, embedded in Unity 2021.2.

## Improvements

### RTHandle System and MSAA

The RTHandle System no longer requires users to specify the number of MSAA samples at initialization time. This means that the number of samples can be specified on a per texture basis, making the system much more flexible in this regard.
In practice this means that the initialization APIs no longer require MSAA related parameters. The Alloc functions have also replaced the "enableMSAA" parameter by an explicit number of samples.
