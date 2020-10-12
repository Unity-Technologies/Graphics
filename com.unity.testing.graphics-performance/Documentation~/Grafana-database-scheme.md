# Grafana Database Scheme

This is the scheme of the Grafana database. All Record types are an array. UNNEST the array to access its value. The performance package should populate every field automatically. If you encounter any issues with a value in this database, ask in #devs-performance-testing on slack.

| Field name                                        | Type      | Mode     |
| ------------------------------------------------- | --------- | -------- |
| VersionDate                                       | TIMESTAMP | NULLABLE |
| RunId                                             | STRING    | NULLABLE |
| TestSuite                                         | STRING    | NULLABLE |
| StartTime                                         | TIMESTAMP | NULLABLE |
| EndTime                                           | TIMESTAMP | NULLABLE |
| PlayerSystemInfo                                  | RECORD    | NULLABLE |
| PlayerSystemInfo. OperatingSystem                 | STRING    | NULLABLE |
| PlayerSystemInfo. DeviceModel                     | STRING    | NULLABLE |
| PlayerSystemInfo. DeviceName                      | STRING    | NULLABLE |
| PlayerSystemInfo. ProcessorType                   | STRING    | NULLABLE |
| PlayerSystemInfo. ProcessorCount                  | INTEGER   | NULLABLE |
| PlayerSystemInfo. GraphicsDeviceName              | STRING    | NULLABLE |
| PlayerSystemInfo. SystemMemorySize                | INTEGER   | NULLABLE |
| PlayerSystemInfo. XrModel                         | STRING    | NULLABLE |
| PlayerSystemInfo. XrDevice                        | STRING    | NULLABLE |
| EditorVersion                                     | RECORD    | NULLABLE |
| EditorVersion. FullVersion                        | STRING    | NULLABLE |
| EditorVersion. DateSeconds                        | INTEGER   | NULLABLE |
| EditorVersion. Branch                             | STRING    | NULLABLE |
| EditorVersion. RevisionValue                      | INTEGER   | NULLABLE |
| ProductVersion                                    | RECORD    | NULLABLE |
| ProductVersion. MajorVersion                      | INTEGER   | NULLABLE |
| ProductVersion. MinorVersion                      | INTEGER   | NULLABLE |
| ProductVersion. RevisionVersion                   | STRING    | NULLABLE |
| ProductVersion. RevisionVersionFirstNumber        | INTEGER   | NULLABLE |
| ProductVersion. RevisionVersionLetter             | STRING    | NULLABLE |
| ProductVersion. RevisionVersionSecondNumber       | INTEGER   | NULLABLE |
| ProductVersion. Changeset                         | STRING    | NULLABLE |
| ProductVersion. Revision                          | INTEGER   | NULLABLE |
| ProductVersion. Branch                            | STRING    | NULLABLE |
| ProductVersion. Date                              | TIMESTAMP | NULLABLE |
| BuildSettings                                     | RECORD    | NULLABLE |
| BuildSettings. Platform                           | STRING    | NULLABLE |
| BuildSettings. BuildTarget                        | STRING    | NULLABLE |
| BuildSettings. DevelopmentPlayer                  | BOOLEAN   | NULLABLE |
| BuildSettings. AndroidBuildSystem                 | STRING    | NULLABLE |
| ScreenSettings                                    | RECORD    | NULLABLE |
| ScreenSettings. ScreenWidth                       | INTEGER   | NULLABLE |
| ScreenSettings. ScreenHeight                      | INTEGER   | NULLABLE |
| ScreenSettings. ScreenRefreshRate                 | INTEGER   | NULLABLE |
| ScreenSettings. Fullscreen                        | BOOLEAN   | NULLABLE |
| QualitySettings                                   | RECORD    | NULLABLE |
| QualitySettings. Vsync                            | INTEGER   | NULLABLE |
| QualitySettings. AntiAliasing                     | INTEGER   | NULLABLE |
| QualitySettings. ColorSpace                       | STRING    | NULLABLE |
| QualitySettings. AnisotropicFiltering             | STRING    | NULLABLE |
| QualitySettings. BlendWeights                     | STRING    | NULLABLE |
| PlayerSettings                                    | RECORD    | NULLABLE |
| PlayerSettings. ScriptingBackend                  | STRING    | NULLABLE |
| PlayerSettings. VrSupported                       | BOOLEAN   | NULLABLE |
| PlayerSettings. MtRendering                       | BOOLEAN   | NULLABLE |
| PlayerSettings. GraphicsJobs                      | BOOLEAN   | NULLABLE |
| PlayerSettings. GpuSkinning                       | BOOLEAN   | NULLABLE |
| PlayerSettings. GraphicsApi                       | STRING    | NULLABLE |
| PlayerSettings. StereoRenderingPath               | STRING    | NULLABLE |
| PlayerSettings. RenderThreadingMode               | STRING    | NULLABLE |
| PlayerSettings. AndroidMinimumSdkVersion          | STRING    | NULLABLE |
| PlayerSettings. AndroidTargetSdkVersion           | STRING    | NULLABLE |
| PlayerSettings. AndroidSdkVersion                 | STRING    | NULLABLE |
| PlayerSettings. ScriptingRuntimeVersion           | STRING    | NULLABLE |
| PlayerSettings. EnabledXrTargets                  | STRING    | NULLABLE |
| ProjectVersions                                   | RECORD    | REPEATED |
| ProjectVersions. ProjectName                      | STRING    | NULLABLE |
| ProjectVersions. Changeset                        | STRING    | NULLABLE |
| ProjectVersions. Branch                           | STRING    | NULLABLE |
| ProjectVersions. Date                             | TIMESTAMP | NULLABLE |
| Results                                           | RECORD    | REPEATED |
| Results. TestName                                 | STRING    | NULLABLE |
| Results. TestCategories                           | RECORD    | REPEATED |
| Results.TestCategories. Name                      | STRING    | NULLABLE |
| Results. TestVersion                              | STRING    | NULLABLE |
| Results. StartTime                                | STRING    | NULLABLE |
| Results. EndTime                                  | STRING    | NULLABLE |
| Results. SampleGroups                             | RECORD    | REPEATED |
| Results.SampleGroups. Samples                     | RECORD    | REPEATED |
| Results.SampleGroups.Samples. Value               | FLOAT     | NULLABLE |
| Results.SampleGroups. AggregatedSampleValue       | FLOAT     | NULLABLE |
| Results.SampleGroups. Min                         | FLOAT     | NULLABLE |
| Results.SampleGroups. Max                         | FLOAT     | NULLABLE |
| Results.SampleGroups. Median                      | FLOAT     | NULLABLE |
| Results.SampleGroups. Average                     | FLOAT     | NULLABLE |
| Results.SampleGroups. StandardDeviation           | FLOAT     | NULLABLE |
| Results.SampleGroups. PercentileValue             | FLOAT     | NULLABLE |
| Results.SampleGroups. Sum                         | FLOAT     | NULLABLE |
| Results.SampleGroups. Zeroes                      | INTEGER   | NULLABLE |
| Results.SampleGroups. SampleCount                 | INTEGER   | NULLABLE |
| Results.SampleGroups. Definition                  | RECORD    | NULLABLE |
| Results.SampleGroups.Definition. Name             | STRING    | NULLABLE |
| Results.SampleGroups.Definition. SampleUnit       | STRING    | NULLABLE |
| Results.SampleGroups.Definition. AggregationType  | STRING    | NULLABLE |
| Results.SampleGroups.Definition. Threshold        | FLOAT     | NULLABLE |
| Results.SampleGroups.Definition. IncreaseIsBetter | BOOLEAN   | NULLABLE |
| Results.SampleGroups.Definition. Percentile       | FLOAT     | NULLABLE |
| TestProject                                       | STRING    | NULLABLE |
| JobMetaData                                       | RECORD    | NULLABLE |
| JobMetaData. Bokken                               | RECORD    | NULLABLE |
| JobMetaData.Bokken. ResourceGroupName             | STRING    | NULLABLE |
| JobMetaData.Bokken. DeviceResourceId              | STRING    | NULLABLE |
| JobMetaData.Bokken. DeviceId                      | STRING    | NULLABLE |
| JobMetaData.Bokken. DeviceIp                      | STRING    | NULLABLE |
| JobMetaData.Bokken. DeviceType                    | STRING    | NULLABLE |
| JobMetaData.Bokken. HostIp                        | STRING    | NULLABLE |
| JobMetaData.Bokken. RuntimeOs                     | STRING    | NULLABLE |
| JobMetaData. Yamato                               | RECORD    | NULLABLE |
| JobMetaData.Yamato. JobFriendlyName               | STRING    | NULLABLE |
| JobMetaData.Yamato. JobName                       | STRING    | NULLABLE |
| JobMetaData.Yamato. JobId                         | STRING    | NULLABLE |
| JobMetaData.Yamato. ProjectId                     | STRING    | NULLABLE |
| JobMetaData.Yamato. ProjectName                   | STRING    | NULLABLE |
| JobMetaData.Yamato. WorkDir                       | STRING    | NULLABLE |
| JobMetaData.Yamato. JobOwnerEmail                 | STRING    | NULLABLE |
| Dependencies                                      | RECORD    | NULLABLE |
| Dependencies. Packages                            | STRING    | REPEATED |