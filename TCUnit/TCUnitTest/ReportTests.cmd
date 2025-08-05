rem original
rem ".tools\reportgenerator.exe" "-reports:src\**\TestResults\**\coverage.cobertura.xml" -targetdir:reports -reporttypes:Html;MarkdownSummary -title:SampleReport

".tools\reportgenerator.exe" -reports:coverage.cobertura.xml -targetdir:reports-DotNet -reporttypes:Html;MarkdownSummary -title:SampleReport
".tools\reportgenerator.exe" -reports:TcUnit_xUnit_results.xml -targetdir:reports-cobertura -reporttypes:Cobertura -title:SampleReport
".tools\reportgenerator.exe" -reports:TestResultsDotnet.trx -targetdir:reports-TRX -reporttypes:Html;MarkdownSummary -title:SampleReport
rem ".tools\reportgenerator.exe" -reports:TcUnit_xUnit_results.xml -targetdir:reports -reporttypes:Html;MarkdownSummary -title:SampleReport