# TCUNit

This Example demonstrates how to use TcUnit.

## The prerequirements

- ensure you have `dotnet` installed. if not run `.\scripts\dotnet-install.ps1`.
- download and install the [TcUnit Runner](https://github.com/tcunit/TcUnit-Runner/releases/tag/0.9.3.0)
- ensure that you can run a local twincat runtime instance.
- Check that the used AMSNetID in the `Run-Unittests` script matches with your ID.

## Run the Tests

Run the tests by executing the `Run-Unittests.cmd` script.

A `TcUnit_xUnit_results.xml` file will be generated.

## Build the Results

Run the `Run-ReportTests.cmd` script to build the raports.

A `Twincat2.html` and `Twincat2.md` file will be generated in the `results` directory.
