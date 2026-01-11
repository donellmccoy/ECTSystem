#!/usr/bin/env dotnet script
// Bisection script to find which test creates unwanted files/state
// Usage: dotnet script FindPolluter.cs <file_or_dir_to_check> <test_pattern>
// Example: dotnet script FindPolluter.cs '.git' 'AF.ECT.Tests/**/*.cs'
// Or compile and run: dotnet FindPolluter.csproj <file_or_dir_to_check> <test_pattern>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class FindPolluter
{
    static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet FindPolluter.cs <file_to_check> <test_pattern>");
            Console.WriteLine("Example: dotnet FindPolluter.cs '.git' 'AF.ECT.Tests/**/*.cs'");
            Environment.Exit(1);
        }

        string pollutionCheck = args[0];
        string testPattern = args[1];
        string workingDir = args.Length > 2 ? args[2] : Environment.CurrentDirectory;

        Console.WriteLine($"🔍 Searching for test that creates: {pollutionCheck}");
        Console.WriteLine($"Test pattern: {testPattern}");
        Console.WriteLine();

        // Get list of test files matching pattern
        var testFiles = GetTestFiles(workingDir, testPattern);
        if (!testFiles.Any())
        {
            Console.WriteLine("❌ No test files found matching pattern");
            Environment.Exit(1);
        }

        Console.WriteLine($"Found {testFiles.Count} test files");
        Console.WriteLine();

        // Bisection search
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
        var regex = new Regex(pattern.Replace("**", ".*").Replace("*", "[^/]*"));
        var files = Directory.GetFiles(baseDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => regex.IsMatch(f))
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

            // Run tests from left to mid
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
            // Run xUnit tests for the given test files
            var testClassNames = testFiles
                .SelectMany(f => ExtractTestClasses(f))
                .Distinct()
                .ToList();

            if (!testClassNames.Any())
            {
                return false;
            }

            // Run tests with dotnet test
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

            // Check if pollution exists
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
}
