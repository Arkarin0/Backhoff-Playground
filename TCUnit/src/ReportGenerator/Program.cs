using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Globalization;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: JUnitReporter <input-junit.xml> <output-file> [--format html|markdown]");
            return 2;
        }

        var inputPath = args[0];
        var outputPath = args[1];
        var format = "markdown";
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i].StartsWith("--format", StringComparison.OrdinalIgnoreCase))
            {
                var parts = args[i].Split(new[] { '=', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) format = parts[1].ToLowerInvariant();
                else if (i + 1 < args.Length) { format = args[++i].ToLowerInvariant(); }
            }
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input file not found: {inputPath}");
            return 3;
        }

        try
        {
            var doc = XDocument.Load(inputPath);
            var parser = new JUnitParser();
            var root = parser.Parse(doc);

            string output;
            if (format == "html")
                output = ReportGenerator.GenerateHtml(root);
            else
                output = ReportGenerator.GenerateMarkdown(root);

            File.WriteAllText(outputPath, output, Encoding.UTF8);
            Console.WriteLine($"Report written to {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            return 1;
        }
    }
}

#region Data Models
public class TestSuiteSummary
{
    public string Name { get; set; }
    public int Tests { get; set; }
    public int Failures { get; set; }
    public int Errors { get; set; }
    public int Skipped { get; set; }
    public double TimeSeconds { get; set; }
    public List<TestCaseResult> TestCases { get; } = new List<TestCaseResult>();
    public List<TestSuiteSummary> ChildSuites { get; } = new List<TestSuiteSummary>();
}

public class TestCaseResult
{
    public string ClassName { get; set; }
    public string Name { get; set; }
    public string Status { get; set; } // passed, failed, error, skipped
    public double TimeSeconds { get; set; }
    public string Message { get; set; }
    public string Details { get; set; }
}
#endregion

#region Parser
public class JUnitParser
{
    public TestSuiteSummary Parse(XDocument doc)
    {
        var root = doc.Root ?? throw new InvalidDataException("Empty XML document");

        if (root.Name.LocalName.Equals("testsuites", StringComparison.OrdinalIgnoreCase))
        {
            var rootSummary = new TestSuiteSummary { Name = "All TestSuites" };
            foreach (var ts in root.Elements().Where(e => e.Name.LocalName.Equals("testsuite", StringComparison.OrdinalIgnoreCase)))
            {
                var parsed = ParseTestSuite(ts);
                rootSummary.ChildSuites.Add(parsed);
                Accumulate(rootSummary, parsed);
            }
            return rootSummary;
        }
        else if (root.Name.LocalName.Equals("testsuite", StringComparison.OrdinalIgnoreCase))
        {
            return ParseTestSuite(root);
        }
        else
        {
            throw new InvalidDataException("Unexpected root element: " + root.Name);
        }
    }

    TestSuiteSummary ParseTestSuite(XElement ts)
    {
        var summary = new TestSuiteSummary
        {
            Name = (string)ts.Attribute("name") ?? "Unnamed",
            Tests = TryParseIntAttr(ts, "tests"),
            Failures = TryParseIntAttr(ts, "failures"),
            Errors = TryParseIntAttr(ts, "errors"),
            Skipped = TryParseIntAttr(ts, "skipped"),
            TimeSeconds = TryParseDoubleAttr(ts, "time")
        };

        var tcElements = ts.Elements().Where(e => e.Name.LocalName.Equals("testcase", StringComparison.OrdinalIgnoreCase));
        foreach (var tce in tcElements)
        {
            var tcr = new TestCaseResult
            {
                Name = (string)tce.Attribute("name") ?? "UnnamedTest",
                ClassName = (string)tce.Attribute("classname") ?? "",
                TimeSeconds = TryParseDoubleAttr(tce, "time"),
            };

            var statusAttr = ((string)tce.Attribute("status"))?.ToUpperInvariant();
            switch (statusAttr)
            {
                case "PASS": tcr.Status = "passed"; break;
                case "FAIL": tcr.Status = "failed"; break;
                case "ERROR": tcr.Status = "error"; break;
                case "SKIPPED":
                case "IGNORE": tcr.Status = "skipped"; break;
                default: tcr.Status = "passed"; break;
            }

            summary.TestCases.Add(tcr);
        }

        return summary;
    }

    int TryParseIntAttr(XElement el, string attr)
    {
        var a = (string)el.Attribute(attr);
        return int.TryParse(a, out var v) ? v : 0;
    }

    double TryParseDoubleAttr(XElement el, string attr)
    {
        var a = (string)el.Attribute(attr);
        return double.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0;
    }

    void Accumulate(TestSuiteSummary target, TestSuiteSummary child)
    {
        target.Tests += child.Tests;
        target.Failures += child.Failures;
        target.Errors += child.Errors;
        target.Skipped += child.Skipped;
        target.TimeSeconds += child.TimeSeconds;
    }
}
#endregion

#region ReportGenerator
public static class ReportGenerator
{
    static string StatusBadge(string status) => status switch
    {
        "passed" => "✔️ Passed",
        "failed" => "❌ Failed",
        "error" => "⚠️ Error",
        "skipped" => "➖ Skipped",
        _ => status
    };

    public static string GenerateMarkdown(TestSuiteSummary root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Test Report: {root.Name}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Tests: **{root.Tests}**");
        sb.AppendLine($"- {StatusBadge("passed")}: **{Math.Max(0, root.Tests - root.Failures - root.Errors - root.Skipped)}**");
        sb.AppendLine($"- {StatusBadge("failed")}: **{root.Failures}**");
        sb.AppendLine($"- {StatusBadge("error")}: **{root.Errors}**");
        sb.AppendLine($"- {StatusBadge("skipped")}: **{root.Skipped}**");
        sb.AppendLine($"- Total time: **{root.TimeSeconds:0.###}s**");
        sb.AppendLine();

        foreach (var suite in root.ChildSuites)
            AppendSuiteMd(sb, suite, 2);

        return sb.ToString();
    }

    static void AppendSuiteMd(StringBuilder sb, TestSuiteSummary suite, int level)
    {
        var prefix = new string('#', level);
        sb.AppendLine($"{prefix} Suite: {suite.Name}");
        sb.AppendLine();
        sb.AppendLine($"- Tests: {suite.Tests}  ");
        sb.AppendLine($"- {StatusBadge("failed")}: {suite.Failures}  ");
        sb.AppendLine($"- {StatusBadge("error")}: {suite.Errors}  ");
        sb.AppendLine($"- {StatusBadge("skipped")}: {suite.Skipped}  ");
        sb.AppendLine($"- Time: {suite.TimeSeconds:0.###}s  ");
        sb.AppendLine();

        if (suite.TestCases.Any())
        {
            var grouped = suite.TestCases.GroupBy(tc => tc.ClassName).OrderBy(g => g.Key);
            foreach (var group in grouped)
            {
                sb.AppendLine($"#### Class: {group.Key}");
                sb.AppendLine();
                sb.AppendLine("| Test | Status | Time (s) |");
                sb.AppendLine("|---|---|---:|");
                foreach (var t in group)
                {
                    sb.AppendLine($"| {t.Name} | {StatusBadge(t.Status)} | {t.TimeSeconds:0.###} |");
                }
                sb.AppendLine();
            }
        }
    }

    public static string GenerateHtml(TestSuiteSummary root)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\"><title>Test Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Segoe UI,Arial;margin:20px} table{border-collapse:collapse;width:100%} th,td{border:1px solid #ddd;padding:6px} th{background:#f4f4f4}");
        sb.AppendLine(".passed{color:green}.failed{color:red}.error{color:#b00020}.skipped{color:orange}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>Test Report: {root.Name}</h1>");
        sb.AppendLine("<h2>Summary</h2>");
        sb.AppendLine($"<p>Tests: {root.Tests}<br/>");
        sb.AppendLine($"✔️ Passed: {Math.Max(0, root.Tests - root.Failures - root.Errors - root.Skipped)}<br/>");
        sb.AppendLine($"❌ Failed: {root.Failures}<br/>");
        sb.AppendLine($"⚠️ Errors: {root.Errors}<br/>");
        sb.AppendLine($"➖ Skipped: {root.Skipped}<br/>");
        sb.AppendLine($"Total time: {root.TimeSeconds:0.###}s</p>");

        foreach (var suite in root.ChildSuites)
            AppendSuiteHtml(sb, suite, 2);

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    static void AppendSuiteHtml(StringBuilder sb, TestSuiteSummary suite, int level)
{
    string indent = new string(' ', level * 4); // 4 spaces per level

    sb.AppendLine($"{indent}<details open>");
    sb.AppendLine($"{indent}  <summary><strong>Suite:</strong> {suite.Name} " +
                  $"(Tests: {suite.Tests}, ❌ {suite.Failures} failed, " +
                  $"⚠️ {suite.Errors} errors, ➖ {suite.Skipped} skipped, " +
                  $"Time: {suite.TimeSeconds:0.###}s)</summary>");

    // Group testcases by class
    var grouped = suite.TestCases.GroupBy(tc => tc.ClassName).OrderBy(g => g.Key);
    foreach (var group in grouped)
    {
        sb.AppendLine($"{indent}  <details>");
        sb.AppendLine($"{indent}    <summary><strong>Class:</strong> {group.Key}</summary>");
        sb.AppendLine($"{indent}    <ul>");
        foreach (var t in group)
        {
            var css = t.Status;
            sb.AppendLine($"{indent}      <li class='{css}'>{t.Name} — {StatusBadge(t.Status)} ({t.TimeSeconds:0.###}s)</li>");
        }
        sb.AppendLine($"{indent}    </ul>");
        sb.AppendLine($"{indent}  </details>");
    }

    // Handle nested suites if present
    foreach (var child in suite.ChildSuites)
        AppendSuiteHtml(sb, child, level + 1);

    sb.AppendLine($"{indent}</details>");
}


}
#endregion
