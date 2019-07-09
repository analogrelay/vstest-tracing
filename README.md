# vstest tracing and diagnostics

Data Collectors for `dotnet vstest` that collect EventPipe traces, etc.

## Traces Sample:

1. `cd samples/TracesSample`
1. `dotnet build`
1. `dotnet vstest .\bin\Debug\netcoreapp3.0\TracesSample.dll --logger:trx --settings:TracesSample.runsettings`

### Output

```
Microsoft (R) Test Execution Command Line Tool Version 16.0.1
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
Results File: C:\Code\anurse\vstest-tracing\samples\TracesSample\bin\TestResults\anurse_DESKTOP-R3AEM4H_2019-07-08_17_17_13_255.trx

Attachments:
  C:\Code\anurse\vstest-tracing\samples\TracesSample\bin\TestResults\ced1bb29-a849-4f0c-8767-af831c046c23\dotnet.10604.netperf
  C:\Code\anurse\vstest-tracing\samples\TracesSample\bin\TestResults\ced1bb29-a849-4f0c-8767-af831c046c23\testhost.22364.netperf

Total tests: 2. Passed: 2. Failed: 0. Skipped: 0.
Test Run Successful.
Test execution time: 2.0309 Seconds
```

Test Results include `netperf` trace files for the whole run **and** all .NET Core processes spawned by the run.

### Configuration

Configured via a `.runsettings` file:

**TracesSample.runsettings**:

```xml
<RunSettings>
  <!-- Configurations that affect the Test Framework -->
  <RunConfiguration>
    <!-- Paths relative to directory that contains .runsettings file-->
    <ResultsDirectory>.\bin\TestResults</ResultsDirectory>
    <TestAdaptersPaths>.\bin\Debug\netcoreapp3.0</TestAdaptersPaths>
  </RunConfiguration>
    <DataCollectionRunSettings>
        <DataCollectors>
            <DataCollector friendlyName="EventPipe">
                <Configuration>
                    <Providers>
                        <!-- Profiles, could (one day) borrow these from dotnet-trace config files -->
                        <Profile name="Default" />

                        <!-- Individual EventPipe providers -->
                        <Provider name="Custom-EventSource" />
                    </Providers>
                </Configuration>
            </DataCollector>
        </DataCollectors>
    </DataCollectionRunSettings>
</RunSettings>
```