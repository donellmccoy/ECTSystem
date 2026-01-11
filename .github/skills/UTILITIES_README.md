# ECTSystem Skills Utilities

C# utilities for ECTSystem skill development and debugging.

## Project Structure

```
.github/skills/
├── ECTSystem.Skills/                    # Console app project
│   ├── Program.cs                       # Both find-polluter and render-graphs commands
│   ├── ECTSystem.Skills.csproj          # .NET 9.0 console app
│   └── bin/Debug/net9.0/
│       └── ECTSystem.Skills.dll         # Compiled assembly
├── FindPolluter.ps1                     # PowerShell alternative (Windows)
├── UTILITIES_README.md                  # This file
```

## Quick Start

```bash
# Navigate to the utilities folder
cd .github/skills/ECTSystem.Skills

# Build the project
dotnet build

# Run utilities
dotnet run -- find-polluter '.git' 'AF.ECT.Tests' ../../..
dotnet run -- render-graphs ../brainstorming --combine
```

### 1. FindPolluter - Test Pollution Detection

Identifies which test creates unwanted files or state (test pollution).

**Available in:**
- **C#**: `FindPolluter.cs` - Standalone console app
- **PowerShell**: `FindPolluter.ps1` - Windows-friendly script
- **CLI**: `dotnet run -- find-polluter ...`

**Usage:**

```bash
# Using CLI
dotnet run -- find-polluter <file_to_check> <test_pattern> [working_dir]

# Examples
dotnet run -- find-polluter '.git' 'AF.ECT.Tests' .
dotnet run -- find-polluter 'bin/Debug' 'AF.ECT.WebClient.Tests' .

# Using PowerShell
.\FindPolluter.ps1 -FileToCheck '.git' -TestPattern 'AF.ECT.Tests'
```

**What it does:**
1. Identifies all test files matching the pattern
2. Performs binary search (bisection) on test files
3. Runs subsets of tests to isolate which test creates pollution
4. Reports the offending test file
5. Suggests cleanup strategies

**When to use:**
- Tests are leaving behind `.git` directories
- Tests are creating temporary files not cleaned up
- Tests are modifying shared state
- Tests are polluting the workspace

---

### 2. RenderGraphs - GraphViz Diagram Renderer

Extracts and renders GraphViz diagrams from SKILL.md files to SVG.

**Available in:**
- **C#**: `RenderGraphs.cs` - Standalone console app
- **CLI**: `dotnet run -- render-graphs ...`

**Usage:**

```bash
# Render each diagram separately
dotnet run -- render-graphs ./brainstorming

# Combine all diagrams into one
dotnet run -- render-graphs ./brainstorming --combine

# Render from specific directory
dotnet run -- render-graphs ../../.github/skills/test-driven-development
```

**What it does:**
1. Reads SKILL.md from the specified directory
2. Extracts all ` ```dot ... ``` ` code blocks
3. Renders each to individual SVG files
4. Optionally combines all into one diagram
5. Saves SVG files in the skill directory for visualization

**When to use:**
- Need to visualize process flows from skills
- Creating documentation with diagrams
- Debugging complex decision trees
- Sharing skill workflows with team

**Requirements:**
- GraphViz installed (`dot` command available)
  - macOS: `brew install graphviz`
  - Linux: `apt-get install graphviz`
  - Windows: https://graphviz.org/download/

---

## Building the Project

```bash
# Build from the utilities folder
cd .github/skills/ECTSystem.Skills
dotnet build

# Run directly from source
dotnet run -- find-polluter '.git' 'AF.ECT.Tests' ../../..
dotnet run -- render-graphs ../brainstorming

# Build release version
dotnet build -c Release

# Run release binary
./bin/Release/net9.0/ECTSystem.Skills.exe find-polluter '.git' 'AF.ECT.Tests' ../../..

# Or from root, using dotnet
cd ../..  # back to root
dotnet .github/skills/ECTSystem.Skills/bin/Release/net9.0/ECTSystem.Skills.dll find-polluter '.git' 'AF.ECT.Tests'
```

---

## Equivalent Legacy Tools

These C# utilities replace the original scripts:

| Old Tool | New Tool | Language | Notes |
|----------|----------|----------|-------|
| `find-polluter.sh` | `ECTSystem.Skills` (find-polluter command) | C# | Bash → C# for cross-platform |
| `find-polluter.sh` | `FindPolluter.ps1` | PowerShell | Bash → PowerShell for Windows |
| `render-graphs.js` | `ECTSystem.Skills` (render-graphs command) | C# | Node.js → C# for consistency |

## Architecture

**Program.cs**: Single-file application dispatching to sub-commands
- Parses command-line arguments
- Routes to appropriate function (find-polluter or render-graphs)
- Provides unified CLI interface
- ~400 lines of well-documented C#

**FindPolluter function**: Test pollution detection
- Binary search (bisection) algorithm
- Test class extraction via regex
- Uses `dotnet test` CLI for running subsets
- Checks for file/directory existence
- Returns offending test file or null if not found

**RenderGraphs function**: GraphViz rendering
- Markdown parsing for ` ```dot ... ``` ` blocks
- SVG generation via external `dot` command
- Diagram combination logic
- Proper error handling with user-friendly messages

---

## Integration with ECTSystem

These utilities are part of the ECTSystem skills framework and can be:

1. **Run from command line** during development
2. **Integrated into CI/CD** pipelines for test validation
3. **Embedded in IDE tasks** (VS Code `tasks.json`)
4. **Called from other scripts** or automation

Example VS Code task:

```json
{
  "label": "Find test polluters",
  "type": "shell",
  "command": "dotnet",
  "args": [
    "run",
    "--",
    "find-polluter",
    ".git",
    "AF.ECT.Tests"
  ],
  "group": "test",
  "presentation": {
    "reveal": "always",
    "clear": true
  }
}
```

---

## Development

All utilities follow ECTSystem conventions:
- ✅ File-scoped namespaces
- ✅ XML documentation comments
- ✅ Async/await patterns
- ✅ .NET 9.0 target
- ✅ Proper error handling
- ✅ No external dependencies (uses System.* only)

---

## Troubleshooting

**"dot: command not found"**
- Install GraphViz on your system
- Verify `dot` is in PATH: `which dot` (macOS/Linux) or `where dot` (Windows)

**"No test files found"**
- Verify test pattern matches actual test files
- Check working directory is correct
- Ensure test files match `*Tests.cs` convention

**"Pollution check failed"**
- Verify the file/directory path is correct
- Check working directory permissions
- Ensure test framework is properly installed

---

## See Also

- `.github/skills/` - Complete skills framework
- `AF.ECT.Tests/` - Test project structure
- ECTSystem development documentation
