# Glob Pattern Reference for roslyn-diff

## Overview
The `--include` and `--exclude` options support glob patterns for filtering files in folder comparison mode.

## Pattern Syntax

### Wildcards

| Pattern | Description | Example | Matches | Doesn't Match |
|---------|-------------|---------|---------|---------------|
| `*` | Any characters except `/` | `*.cs` | `File.cs`, `Code.cs` | `src/File.cs` |
| `**` | Any directories (recursive) | `**/*.cs` | `File.cs`, `src/File.cs`, `src/sub/File.cs` | `File.txt` |
| `?` | Single character | `file?.cs` | `file1.cs`, `fileA.cs` | `file10.cs`, `file.cs` |

### Pattern Matching Rules

1. **Case Insensitive**: Patterns match case-insensitively
2. **Path Separators**: Both `/` and `\` are supported
3. **Relative Paths**: Patterns match against relative paths from the root folder

## Common Patterns

### Include Patterns

```bash
# Single file type
--include "*.cs"

# Multiple file types (repeatable)
--include "*.cs" --include "*.vb"

# Specific directory
--include "src/*.cs"

# Recursive in directory
--include "src/**/*.cs"

# All files recursively
--include "**/*"
```

### Exclude Patterns

```bash
# Exclude generated files
--exclude "*.g.cs"
--exclude "*.Designer.cs"

# Exclude build artifacts
--exclude "bin/**"
--exclude "obj/**"

# Exclude test files
--exclude "**/*Test.cs"
--exclude "**/*Tests.cs"

# Exclude specific directories
--exclude "node_modules/**"
--exclude ".git/**"
```

## Usage Examples

### Example 1: C# Source Files Only
```bash
roslyn-diff diff old/ new/ -r --include "*.cs"
```

### Example 2: Exclude Generated Code
```bash
roslyn-diff diff old/ new/ -r \
  --include "*.cs" \
  --exclude "*.g.cs" \
  --exclude "*.Designer.cs"
```

### Example 3: Specific Source Directory
```bash
roslyn-diff diff old/ new/ -r \
  --include "src/**/*.cs" \
  --exclude "src/**/bin/**" \
  --exclude "src/**/obj/**"
```

### Example 4: Multiple Languages
```bash
roslyn-diff diff old/ new/ -r \
  --include "*.cs" \
  --include "*.vb" \
  --exclude "**/*.g.cs" \
  --exclude "**/*.Designer.cs"
```

### Example 5: Exclude Test Projects
```bash
roslyn-diff diff old/ new/ -r \
  --include "**/*.cs" \
  --exclude "**/tests/**" \
  --exclude "**/*.Tests/**"
```

### Example 6: Only Test Files
```bash
roslyn-diff diff old/ new/ -r \
  --include "**/*Test.cs" \
  --include "**/*Tests.cs"
```

### Example 7: Documentation Files
```bash
roslyn-diff diff old/ new/ -r \
  --include "**/*.md" \
  --include "**/*.txt" \
  --exclude "**/node_modules/**"
```

### Example 8: Exclude Multiple Build Folders
```bash
roslyn-diff diff old/ new/ -r \
  --exclude "bin/**" \
  --exclude "obj/**" \
  --exclude "packages/**" \
  --exclude ".vs/**"
```

## Pattern Precedence

**Exclude patterns always take precedence over include patterns.**

Example:
```bash
roslyn-diff diff old/ new/ -r \
  --include "**/*.cs" \
  --exclude "**/*.g.cs"
```

Result:
- ✅ `Program.cs` - Included
- ✅ `src/Code.cs` - Included
- ❌ `Program.g.cs` - Excluded (matches exclude)
- ❌ `src/Code.g.cs` - Excluded (matches exclude)

## Default Behavior

### No Patterns Specified
```bash
roslyn-diff diff old/ new/ -r
```
- **Includes**: All files
- **Excludes**: None

### Include Only
```bash
roslyn-diff diff old/ new/ -r --include "*.cs"
```
- **Includes**: Only files matching `*.cs`
- **Excludes**: All other files

### Exclude Only
```bash
roslyn-diff diff old/ new/ -r --exclude "*.g.cs"
```
- **Includes**: All files except those matching `*.g.cs`
- **Excludes**: Only `*.g.cs` files

## Pattern Testing

### Mental Model: Two-Step Process

1. **Step 1**: Apply include patterns
   - If no includes specified: Include all files
   - If includes specified: Include only matching files

2. **Step 2**: Apply exclude patterns
   - Remove any files matching exclude patterns
   - Exclude takes precedence

### Example Walkthrough

Files: `Code.cs`, `Code.g.cs`, `Data.json`

Pattern: `--include "*.cs" --exclude "*.g.cs"`

**Step 1 (Include)**:
- `Code.cs` ✅ (matches `*.cs`)
- `Code.g.cs` ✅ (matches `*.cs`)
- `Data.json` ❌ (doesn't match `*.cs`)

**Step 2 (Exclude)**:
- `Code.cs` ✅ (doesn't match `*.g.cs`)
- `Code.g.cs` ❌ (matches `*.g.cs`)
- `Data.json` ❌ (already excluded)

**Final Result**: Only `Code.cs`

## Advanced Patterns

### Negation (Not Supported Yet)
```bash
# Future feature
--include "!*.g.cs"  # NOT IMPLEMENTED
```

### Character Classes (Not Supported Yet)
```bash
# Future feature
--include "[Pp]rogram.cs"  # NOT IMPLEMENTED
--include "[a-z]*.cs"      # NOT IMPLEMENTED
```

### Brace Expansion (Not Supported Yet)
```bash
# Future feature
--include "{*.cs,*.vb}"  # NOT IMPLEMENTED
```

## Performance Considerations

### Optimize for Speed

1. **Use Specific Patterns**
   ```bash
   # Faster
   --include "src/**/*.cs"

   # Slower
   --include "**/*.cs"
   ```

2. **Exclude Large Directories Early**
   ```bash
   --exclude "node_modules/**" \
   --exclude "packages/**"
   ```

3. **Use Top-Level Exclusions**
   ```bash
   # Faster
   --exclude "bin/**"

   # Same result but more checks
   --exclude "**/bin/**"
   ```

## Troubleshooting

### Pattern Doesn't Match

1. **Check case sensitivity**
   - Patterns are case-insensitive
   - `*.CS` matches `file.cs`

2. **Check path separators**
   - Use `/` in patterns
   - Windows `\` is converted internally

3. **Check relative paths**
   - Patterns match from folder root
   - `src/file.cs` not `C:/project/src/file.cs`

### Too Many Files

1. **Add exclude patterns**
   ```bash
   --exclude "bin/**" --exclude "obj/**"
   ```

2. **Use more specific includes**
   ```bash
   --include "src/**/*.cs"  # Instead of --include "**/*.cs"
   ```

### No Files Found

1. **Check recursive flag**
   ```bash
   roslyn-diff diff old/ new/ -r  # Don't forget -r!
   ```

2. **Check pattern syntax**
   ```bash
   # Wrong
   --include "*.cs/"

   # Right
   --include "*.cs"
   ```

3. **Verify files exist**
   ```bash
   ls -R old/
   ls -R new/
   ```

## Pattern Conversion (Internal)

The implementation converts glob patterns to regular expressions:

| Glob | Regex | Description |
|------|-------|-------------|
| `*.cs` | `^[^/]*\.cs$` | Any `.cs` file (no path) |
| `**/*.cs` | `^(?:.*/)?[^/]*\.cs$` | Any `.cs` file (any path) |
| `file?.cs` | `^file[^/]\.cs$` | `file` + one char + `.cs` |
| `src/**/*.cs` | `^src/(?:.*/)?[^/]*\.cs$` | `.cs` files under `src/` |

## See Also

- [Multi-File Diff Documentation](MULTI_FILE_DIFF.md)
- [Phase 2 Implementation Summary](../PHASE2_IMPLEMENTATION_SUMMARY.md)
- [CLI Usage Examples](CLI_EXAMPLES.md)
