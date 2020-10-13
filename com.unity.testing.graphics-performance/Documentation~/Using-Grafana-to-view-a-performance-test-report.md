# Using Grafana to view a performance test report
You can use [Grafana](https://grafana.com/docs/grafana/latest/getting-started/what-is-grafana/) to display all of your performance metrics. Grafana is easier to customize than the [Observer CDS dashboard](https://observer.cds.internal.unity3d.com/project), and it displays your performance metrics more clearly, which is useful for complex tests. To display your performance metrics, you can write a query with Google BigQuery. If this is your first time using Grafana, see [Grafana Getting Started](https://grafana.com/docs/grafana/latest/guides/getting_started/). To learn what is possible with Grafana, investigate the [Grafana Playground](https://play.grafana.org/).

To access Unity’s internal Grafana:

1. Go to the [Unity Grafana](https://grafana.internal.unity3d.com/).
2. Sign in with your Okta details.
<a name="writing-a-query"></a>
## Writing a query
Grafana uses Google BigQuery to write queries. This means it includes specific functions native to the Google BigQuery database. To check the use case or existence of a function, check the [Google BigQuery SQL documentation page](https://cloud.google.com/bigquery/docs/reference/standard-sql/functions-and-operators). To check the scheme of Grafana’s performance database, see the [Grafana Database Scheme](#grafana-database-scheme).

To prepare to write your query, follow these steps: 

1. In the dashboard header, select **Add Panel** to create a new graph.
2. Click **Add Query** in the new panel.
3. Enter the panel’s edit mode. To do this, hover the cursor over the panel and press E on your keyboard.
4. In the **Query** field, which targets the source database, select the performance database `rd-perf-test-data-pdr BigQuery`.
5. The builder doesn’t work with Unity’s data structure. Click **Edit SQL** to hide the builder.

Some common functions do not work as expected in Grafana, so you need to use an alternative. For example: 

- Replace `run.StartTime` with `Endtime`.
  Replace `$_timeFilter` with the following line: `run.EndTime BETWEEN TIMESTAMP_MILLIS($__from) AND TIMESTAMP_MILLI($__to)`.

The following example query retrieves the frame timings in HDRP:

```#standardSQL
SELECT
    -- Select the average of the median of all tests.
	AVG(sampleGroup.Median) as median,
    -- Use Endtime here, because run.StartTime doesn’t work.
    run.EndTime as time,
    -- Extract the name of the sampler from the name of the SampleGroup.
    REGEXP_EXTRAC(sampleGroup.Definition.Name, 'Timing,\\w+,(.*)') as metric
FROM
perf_test_results.run,
    -- Use UNNEST for every Record type array. Refer to Grafana’s database scheme to learn which arrays are listed as Record. 
	UNNEST(Results) AS result,
	UNNEST(ProjectVersions) as pv,
	UNNEST(result.SampleGroups) AS sampleGroup
WHERE
    -- Filter using the project name you set in the utr command line.
    pv.ProjectName = 'HDRP'
    -- Filter by platform.
    AND BuildSettings.Platform = "PS4" ANDPlayerSystemInfo.DeviceModel = '$ps4_config'
    -- This filter only gets the data in the time window of Grafana, to avoid overloading the database.
    -- Use this line instead of $__timeFilter.
    AND run.EndTime BETWEEN TIMESTAMP_MILLIS($__from) AND TIMESTAMP_MILLI($__to)
    -- Filter by git branch name. The dashboard templating variable displays the results of this branch in the graph.
    AND pv.Branch = '$Git_branch_name'
    -- Filter by test name.
    AND result.TestName LIKE '%PerformanceTests.Counters%$HDRP_asset_config%'
    -- Filter by sampleGroup name.
    AND REGEXP_CONTAINS(sampleGroup.Definition.Name, 'Timing,CPU,{Selected_counter:regex}')
-- Group every data you select. This avoids a BigQuery error.
GROUP BY time, sampleGroup.Definition.Name
-- Discard medians that have 0 in value.
HAVING median <> 0
-- Sort by run date and metric name so that the metrics are always in the same order in the graph.
ORDER BY time, metric
```

```
<a name="naming-convention"></a>
```

## Naming convention
Before you can report your data in Grafana, you need to pack the information required into a SampleGroup name in Unity. Grafana only allows you to pair one [SampleGroup](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@1.0/api/Unity.PerformanceTesting.SampleGroup.html) name to one metric, so you need to have a  well-defined naming convention.

The following functions from the `PerformanceTestUtils` class format the data into an effective naming convention. This naming convention makes it easy for you to parse your SampleGroup in Grafana with regular expressions (regex):

| Function                | Description                                                  | Example                                                      |
| ----------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| `FormatTestName`        | This function formats the name of the test and packs the following:<br>- Data type<br>- Category<br>- Settings (generally the SRP asset alias)<br>- Settings category<br>- Test name | The format this function generates for a memory test:<br><br>`0001\_LitCube:Small,Deferred\_SRP:Default,RenderTexture`. |
| `FormatSampleGroupName` | This function formats the name of the SampleGroup you use to send a metric value. It contains the following:<br>- Metric name<br>- Category<br>- Data name | The format this function generates for a test that contains a counter test:<br>`Timing,GPU,Gbuffer`. |

When you filter your scenes in Grafana, you can use [REGEXP_CONTAINS](https://cloud.google.com/bigquery/docs/reference/standard-sql/functions-and-operators#regexp_contains) to match a certain format and [REGEXP_EXTRACT](https://cloud.google.com/bigquery/docs/reference/standard-sql/functions-and-operators#regexp_extract) to extract a certain part of the name. For example, the following query uses both of these functions to filter timings and display only the name of the counter:

```
#standardSQL
SELECT
    AVG(sampleGroup.Median) as median, run.EndTime as time,
    REGEXP_EXTRACT(sampleGroup.Definition.Name, 'Timing,\\w+,(.*)') as metric
FROM
perf_test_results.run,
    UNNEST(Results) AS result,
    UNNEST(ProjectVersions) as pv,
    UNNEST(result.SampleGroups) AS sampleGroup
WHERE
    pv.ProjectName = 'HDRP' AND BuildSettings.Platform = "PS4" AND PlayerSystemInfo.DeviceModel = '$ps4_config' --  Mandatory filters, ensure we use the right project, test suite and time window.
    AND run.EndTime BETWEEN TIMESTAMP_MILLIS($__from) AND TIMESTAMP_MILLIS($__to) -- Workaround for the $__timeFilter which doesn’t work for the Google BigQuery datasource.
    AND pv.Branch = '$Git_branch_name'
    AND result.TestName LIKE '%PerformanceTests.Counters%$HDRP_asset_config%'
    AND REGEXP_CONTAINS(sampleGroup.Definition.Name, 'Timing,CPU,${Selected_counter:regex}') -- Test suite filters.
GROUP BY time, sampleGroup.Definition.Name
HAVING median <> 0
ORDER BY time, metric
```
