using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        string command = args[0].ToLower();
        string[] commandArgs = args.Skip(1).ToArray();

        switch (command)
        {
            case "find-polluter":
                await FindPolluter(commandArgs);
                break;
            case "render-graphs":
                await RenderGraphs(commandArgs);
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                PrintUsage();
                break;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("ECTSystem Skills Utilities");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- <command> [arguments]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine();
        Console.WriteLine("  find-polluter <file_to_check> <test_pattern> [working_dir]");
        Console.WriteLine("    Find which test creates unwanted files/state");
        Console.WriteLine("    Example: dotnet run -- find-polluter '.git' 'AF.ECT.Tests' .");
        Console.WriteLine();
        Console.WriteLine("  render-graphs <skill-directory> [--combine]");
        Console.WriteLine("    Render graphviz diagrams from SKILL.md to SVG");
        Console.WriteLine("    Example: dotnet run -- render-graphs ./brainstorming --combine");
        Console.WriteLine();
    }

    static async Task FindPolluter(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: find-polluter <file_to_check> <test_pattern> [working_dir]");
            Console.WriteLine("Example: find-polluter '.git' 'AF.ECT.Tests' .");
            return;
        }

        string pollutionCheck = args[0];
        string testPattern = args[1];
        string workingDir = args.Length > 2 ? args[2] : Environment.CurrentDirectory;

        Console.WriteLine($"🔍 Searching for test that creates: {pollutionCheck}");
        Console.WriteLine($"Test pattern: {testPattern}");
        Console.WriteLine();

        var testFiles = GetTestFiles(workingDir, testPattern);
        if (!testFiles.Any())
        {
            Console.WriteLine("❌ No test files found matching pattern");
            return;
        }

        Console.WriteLine($"Found {testFiles.Count} test files");
        Console.WriteLine();

        var polluter = await BisectTests(testFiles, pollutionCheck, workingDir);

        if (polluter != null)
        {
            Console.WriteLine();
            Console.WriteLine($"✅ FOUND POLLUTER: {polluter}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine($"1. Open: {polluter}");
            Console.WriteLine("2. Look for setup/teardown that doesn't clean up");
            Console.WriteLine("3. Add proper cleanup in test fixture or [TearDown] method");
        }
        else
        {
            Console.WriteLine("❌ Could not identify single polluter");
            Console.WriteLine("Pollution may be cumulative from multiple tests");
        }
    }

    static List<string> GetTestFiles(string baseDir, string pattern)
    {
        var searchPattern = pattern.Replace("AF.ECT.Tests", "*Tests.cs");
        var files = Directory.GetFiles(baseDir, searchPattern, SearchOption.AllDirectories)
            .Where(f => f.Contains("Tests"))
            .ToList();
        return files;
    }

    static async Task<string> BisectTests(List<string> testFiles, string pollutionCheck, string workingDir)
    {
        Console.WriteLine("Running bisection search...");
        Console.WriteLine();

        int left = 0;
        int right = testFiles.Count - 1;

        while (left < right)
        {
            int mid = (left + right) / 2;

            var testSubset = testFiles.Skip(left).Take(mid - left + 1).ToList();
            Console.WriteLine($"Testing subset [{left}-{mid}] ({testSubset.Count} files)...");

            bool polluted = await RunTestsAndCheck(testSubset, pollutionCheck, workingDir);

            if (polluted)
            {
                right = mid;
                Console.WriteLine($"✓ Pollution found in this subset, narrowing...");
            }
            else
            {
                left = mid + 1;
                Console.WriteLine($"✓ No pollution in this subset, searching right half...");
            }

            Console.WriteLine();
        }

        if (left == right)
        {
            return testFiles[left];
        }

        return null;
    }

    static async Task<bool> RunTestsAndCheck(List<string> testFiles, string pollutionCheck, string workingDir)
    {
        try
        {
            var testClassNames = testFiles
                .SelectMany(f => ExtractTestClasses(f))
                .Distinct()
                .ToList();

            if (!testClassNames.Any())
            {
                return false;
            }

            var filter = string.Join("|", testClassNames.Select(c => $"FullyQualifiedName~{c}"));

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test --filter \"{filter}\" --no-build --logger \"console;verbosity=quiet\"",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            await process.WaitForExitAsync();

            return FileOrDirectoryExists(Path.Combine(workingDir, pollutionCheck));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running tests: {ex.Message}");
            return false;
        }
    }

    static List<string> ExtractTestClasses(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var classMatches = Regex.Matches(content, @"public\s+class\s+(\w+)\s*(?:\:|where|{)");
            return classMatches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    static bool FileOrDirectoryExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    static async Task RenderGraphs(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: render-graphs <skill-directory> [--combine]");
            Console.WriteLine("Example: render-graphs ./brainstorming --combine");
            return;
        }

        string skillDir = args[0];
        bool combine = args.Contains("--combine");

        if (!Directory.Exists(skillDir))
        {
            Console.WriteLine($"❌ Directory not found: {skillDir}");
            return;
        }

        string skillMdPath = Path.Combine(skillDir, "SKILL.md");
        if (!File.Exists(skillMdPath))
        {
            Console.WriteLine($"❌ SKILL.md not found in {skillDir}");
            return;
        }

        var dotBlocks = ExtractDotBlocks(skillMdPath);
        if (!dotBlocks.Any())
        {
            Console.WriteLine("⚠️  No dot blocks found in SKILL.md");
            return;
        }

        Console.WriteLine($"Found {dotBlocks.Count} dot diagram(s)");

        if (combine)
        {
            await RenderCombined(dotBlocks, skillDir);
        }
        else
        {
            await RenderSeparate(dotBlocks, skillDir);
        }
    }

    static List<string> ExtractDotBlocks(string mdFilePath)
    {
        var blocks = new List<string>();
        var content = File.ReadAllText(mdFilePath);

        var matches = Regex.Matches(content, @"```dot\s*\n([\s\S]*?)\n```");
        foreach (Match match in matches)
        {
            blocks.Add(match.Groups[1].Value);
        }

        return blocks;
    }

    static async Task RenderSeparate(List<string> dotBlocks, string skillDir)
    {
        for (int i = 0; i < dotBlocks.Count; i++)
        {
            string outputFile = Path.Combine(skillDir, $"diagram-{i + 1}.svg");
            await RenderDotToSvg(dotBlocks[i], outputFile);
            Console.WriteLine($"✓ Rendered: {outputFile}");
        }
    }

    static async Task RenderCombined(List<string> dotBlocks, string skillDir)
    {
        var combined = new System.Text.StringBuilder();
        combined.AppendLine("digraph combined {");
        combined.AppendLine("  graph [rankdir=TB];");

        int diagramIndex = 1;
        foreach (var block in dotBlocks)
        {
            var content = Regex.Replace(block, @"digraph\s+\w+\s*\{", "");
            content = Regex.Replace(content, @"^\}", "");

            combined.AppendLine($"  subgraph cluster_{diagramIndex} {{");
            combined.AppendLine($"    label=\"Diagram {diagramIndex}\";");
            combined.AppendLine("    " + string.Join("\n    ", content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l))));
            combined.AppendLine("  }");
            diagramIndex++;
        }

        combined.AppendLine("}");

        string outputFile = Path.Combine(skillDir, "diagram-combined.svg");
        await RenderDotToSvg(combined.ToString(), outputFile);
        Console.WriteLine($"✓ Rendered combined: {outputFile}");
    }

    static async Task RenderDotToSvg(string dotContent, string outputFilePath)
    {
        try
        {
            string tempDotFile = Path.GetTempFileName();
            File.WriteAllText(tempDotFile, dotContent);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "dot",
                    Arguments = $"-Tsvg \"{tempDotFile}\" -o \"{outputFilePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"❌ Failed to render {outputFilePath}");
                    var error = process.StandardError.ReadToEnd();
                    Console.WriteLine($"   Error: {error}");
                }
            }
            finally
            {
                File.Delete(tempDotFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error rendering diagram: {ex.Message}");
            Console.WriteLine("   Make sure graphviz is installed");
        }
    }
}
