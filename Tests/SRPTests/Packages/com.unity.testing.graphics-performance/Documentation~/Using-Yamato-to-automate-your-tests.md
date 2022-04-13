# Using Yamato to automate your tests
[Yamato](https://internaldocs.cds.internal.unity3d.com/yamato/) is Unity’s continuous integration (CI) solution. You can use Yamato with the Graphics performance testing package to run your tests automatically.
<a name="yamato-setup"></a>
## Yamato setup
To automate your tests using Yamato, you need to set up Yamato for your project. For more information, see the [Yamato Quickstart guide](https://internaldocs.cds.internal.unity3d.com/yamato/workflows/quick-start/).
<a name="creating-yamato-pipelines"></a>

## Creating Yamato pipelines
Each test you perform in Unity requires its own Yamato pipeline. For example, if you are running a test for timings, build profiling, and static shader analysis, you need to create a new pipeline for each one.

To create a new pipeline in Yamato, modify the Unified Test Runner (utr) command line to support performance testing:

In the command line, add`--performance-project-id=ProjectName`. Replace `ProjectName` with the name of your project in the performance database.

1. Add `--report-performance-data` in the command line to enable performance reporting. 

Here's an example of a utr command line:

```
utr/utr --timeout=2400 --loglevel=verbose --scripting-backend=Il2Cpp --suite={{ suite.mode }} --testfilter={{ suite.filter }} --platform={{ platform.name }} --testproject=C:/Link/TestProjects/{{ project.folder }} --editor-location=.Editor  --report-performance-data --performance-project-id=HDRP --artifacts_path=test-results --player-connection-ip=%BOKKEN_HOST_IP%
```

You can also instruct Yamato to automatically run daily updates to the performance metrics on a certain branch. To do this, add the following to your Yamato job description:

```
triggers:
recurring:
​       -branch: master
​     frequency: daily
```

If you have any issues during the data reporting, you can check what Yamato sends to the database in the **Artifacts** field after the reporting has finished. The default path for the file is located in the **Artifacts** field in the Yamato build: **results/test-results/PerformanceTestReport.html.**