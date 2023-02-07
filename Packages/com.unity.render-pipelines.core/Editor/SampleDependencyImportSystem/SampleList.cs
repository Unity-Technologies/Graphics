using System;
using UnityEditor.PackageManager.UI;
using System.Collections.Generic;

/// <remarks>
/// This is an extension on the built-in information related to samples.
/// Information related to common dependencies and doc links can be defined in this class.
/// </remarks>

/// <summary>
/// This class defines the informations and dependencies for a specific sample.
/// </summary>
[Serializable]
internal class SampleInformation
{
    public string displayName;
    public string description;
    public string path;
    public string[] dependencies;
}

/// <summary>
/// A configuration class defining information related to samples for the package.
/// </summary>
[Serializable]
class SampleList
{
    public SampleInformation[] samples;
}
