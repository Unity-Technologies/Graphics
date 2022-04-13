// The old version number system below is deprecated whith Graphic Packages that have move as core package of Unity.
// User should rely on the Macro UNITY_VERSION now to detect which version of Unity is coupled to the current set of pipeline shader
// Example of usage #if UNITY_VERSION >= 202120 to check if the version is above or equal 2021.2
#define SHADER_LIBRARY_VERSION_MAJOR 13
#define SHADER_LIBRARY_VERSION_MINOR 1

#define VERSION_GREATER_EQUAL(major, minor) ((SHADER_LIBRARY_VERSION_MAJOR > major) || ((SHADER_LIBRARY_VERSION_MAJOR == major) && (SHADER_LIBRARY_VERSION_MINOR >= minor)))
#define VERSION_LOWER(major, minor) ((SHADER_LIBRARY_VERSION_MAJOR < major) || ((SHADER_LIBRARY_VERSION_MAJOR == major) && (SHADER_LIBRARY_VERSION_MINOR < minor)))
#define VERSION_EQUAL(major, minor) ((SHADER_LIBRARY_VERSION_MAJOR == major) && (SHADER_LIBRARY_VERSION_MINOR == minor))
