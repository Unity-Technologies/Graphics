# Graphics performance testing package

The Graphics performance testing package allows you to measure and report performance markers for your graphics package. It is built on top of the [Performance Testing Extension for Unity Test Runner](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@2.0/manual/index.html) and contains utility functions to measure the following:

- Frame timings
- Memory usage
- Build performance
- Static Shader Analysis data such as VGPR, SGPR, and Occupancy

When you use the performance testing package in a project, it sends measured performance data to Unity’s Google BigQuery database. You can use Grafana to visualise this data. For more information, see [Using Grafana to view a performance test report](#using-grafana-to-view-a-performance-test-report).

You can use Yamato to run your tests automatically. For more information see [Using Yamato to automate your tests](#using-yamato-to-automate-your-tests).

![img](Images/Grafana-HDRP.png)
Performance data visualised in Grafana.