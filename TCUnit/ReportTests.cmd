mkdir results
dotnet run --project "src\ReportGenerator\ReportGenerator.csproj" "TcUnit_xUnit_results.xml" "results\Twincat2.html" --format html
dotnet run --project "src\ReportGenerator\ReportGenerator.csproj" "TcUnit_xUnit_results.xml" "results\Twincat2.md" --format markdown