# WindowsServiceInstaller Build Guide

C# tool for installing and managing Windows services using direct API calls (no sc.exe).

## Prerequisites

- .NET 8.0 SDK or later
- Windows OS

## Build Options

### 1. Quick Build (Development)

```powershell
dotnet build
```

**Output:** `bin\Debug\net8.0\WindowsServiceInstaller.exe`
- Fast build
- Requires .NET 8 runtime on target
- Includes debug symbols

### 2. Release Build

```powershell
dotnet build -c Release
```

**Output:** `bin\Release\net8.0\WindowsServiceInstaller.exe`
- Optimized
- Requires .NET 8 runtime on target
- Smaller than debug build

### 3. Self-contained Single File (Recommended)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**Output:** `bin\Release\net8.0\win-x64\publish\WindowsServiceInstaller.exe`

**Features:**
- Single executable file
- No .NET runtime required on target
- ~65 MB file size
- Easy deployment

### 4. Optimized Standalone (Lightest Self-contained)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=none `
  -p:DebugSymbols=false
```

**Output:** `bin\Release\net8.0\win-x64\publish\WindowsServiceInstaller.exe`

**Features:**
- Single executable file
- No .NET runtime required
- Smallest possible self-contained size (~30-40 MB)
- Stripped debug symbols
- Trimmed unused code
- Compressed

**Best for:**
- Red team operations
- Deployment to unknown systems
- Minimal footprint

### 5. Framework-dependent (Smallest)

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

**Output:** `bin\Release\net8.0\win-x64\publish\WindowsServiceInstaller.exe`

**Features:**
- Very small (~500 KB)
- Single file
- **Requires .NET 8 runtime** on target

**Best for:**
- Systems with .NET 8 already installed
- When file size is critical

## Build Comparison

| Build Type | Size | Runtime Needed? | Files | Use Case |
|------------|------|-----------------|-------|----------|
| Debug | ~200 KB | Yes | 1 + deps | Development |
| Release | ~200 KB | Yes | 1 + deps | Testing |
| Self-contained | ~65 MB | No | 1 | Deployment |
| **Optimized** | **~35 MB** | **No** | **1** | **Operations** |
| Framework-dep | ~500 KB | Yes | 1 | .NET installed |

## Target Platforms

### Windows x64 (Default)
```powershell
-r win-x64
```

### Windows x86 (32-bit)
```powershell
-r win-x86
```

### Windows ARM64
```powershell
-r win-arm64
```

## Usage After Build

```powershell
# Install service
.\WindowsServiceInstaller.exe install "C:\path\to\service.exe" ServiceName "Display Name" "Description"

# Start service
.\WindowsServiceInstaller.exe start ServiceName

# Stop service
.\WindowsServiceInstaller.exe stop ServiceName

# Check status
.\WindowsServiceInstaller.exe status ServiceName

# Uninstall service
.\WindowsServiceInstaller.exe uninstall ServiceName

# Help
.\WindowsServiceInstaller.exe help
```

## Recommended Builds

**For adversary emulation / operations:**
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true -p:DebugType=none
```

**For quick testing:**
```powershell
dotnet build -c Release
```

**For systems with .NET installed:**
```powershell
dotnet publish -c Release --self-contained false -p:PublishSingleFile=true
```

## Output Locations

After build/publish, find executables at:

- **Build:** `bin\{Configuration}\net8.0\`
- **Publish:** `bin\{Configuration}\net8.0\{runtime}\publish\`

Examples:
- `bin\Debug\net8.0\WindowsServiceInstaller.exe`
- `bin\Release\net8.0\win-x64\publish\WindowsServiceInstaller.exe`

## Clean Build

Remove all build artifacts:

```powershell
dotnet clean
```

Or manually delete:
```powershell
Remove-Item -Recurse -Force bin, obj
```

## Notes

- All builds require Administrator privileges to run (for service management)
- Self-contained builds are larger but more portable
- Trimming may break reflection (not an issue for this tool)
- Compression reduces file size without affecting functionality
