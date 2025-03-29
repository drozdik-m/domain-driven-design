<#
MIT License

Copyright (c) 2025 Ing. Martin Drozdík

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
#>

<#
.SYNOPSIS
    Safely removes bin and obj folders from .NET projects.

.DESCRIPTION
    This script scans for .csproj files and removes only the bin and obj folders
    that exist at the same directory level as these project files. This prevents
    accidental deletion of unrelated bin folders (e.g., in node_modules).

    Folders node_modules and .git are excluded from the search.

.PARAMETER Path
    The root directory path to search for .NET projects. Defaults to the current directory.

.PARAMETER WhatIf
    Displays what would happen if the script runs without actually deleting folders.

.PARAMETER Verbose
    Displays detailed information about the script's execution.

.EXAMPLE
    .\Clean-DotNetProjects.ps1
    Cleans all .NET projects in the current directory and its subdirectories.

.EXAMPLE
    .\Clean-DotNetProjects.ps1 -Path "C:\Projects\MySolution" -Verbose
    Cleans all .NET projects in the specified path with verbose output.

.EXAMPLE
    .\Clean-DotNetProjects.ps1 -WhatIf
    Shows what folders would be deleted without actually removing them.

.NOTES
    Author: Martin Drozdík
    Version: 1.1
#>

# Define script parameters with validation and support for -WhatIf mode
[CmdletBinding(SupportsShouldProcess = $true)]
param (
    # Path parameter with validation to ensure the directory exists
    [Parameter(Position = 0)]
    [ValidateScript({
        if (-Not (Test-Path $_)) {
            throw "Path does not exist: $_"
        }
        return $true
    })]
    [string]$Path = (Get-Location).Path
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

#region Helper Functions

<#
.SYNOPSIS
    Outputs color-coded messages to the console for better readability.
.DESCRIPTION
    This function standardizes console output with color-coding based on message type.
    It adds a prefix to each message to indicate its severity or purpose.
.PARAMETER Message
    The text message to display.
.PARAMETER Type
    The type of message (Info, Success, Warning, Error) which determines its color.
#>
function Write-ColorOutput {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        
        [Parameter(Mandatory = $true)]
        [ValidateSet("Info", "Success", "Warning", "Error")]
        [string]$Type
    )

    # Define color and prefix mappings for different message types
    $colors = @{
        "Info" = "Cyan"
        "Success" = "Green"
        "Warning" = "Yellow"
        "Error" = "Red"
    }

    $prefixes = @{
        "Info" = "[INFO] "
        "Success" = "[SUCCESS] "
        "Warning" = "[WARNING] "
        "Error" = "[ERROR] "
    }

    # Output the message with appropriate color
    Write-Host ($prefixes[$Type] + $Message) -ForegroundColor $colors[$Type]
}

class ProjectFolderResult {
    [bool]$Success
    [int]$BinRemoved
    [int]$ObjRemoved
    [string]$ErrorMessage

    ProjectFolderResult([bool]$success, [string]$errorMessage) {
        $this.Success = $success
        $this.BinRemoved = 0
        $this.ObjRemoved = 0
        $this.ErrorMessage = $errorMessage
    }
}

<#
.SYNOPSIS
    Removes bin and obj folders for a specific .NET project.
.DESCRIPTION
    This function takes a project directory and removes its bin and obj folders.
    It provides proper error handling and logging for each operation.
.PARAMETER ProjectDir
    The directory containing the .NET project.
.PARAMETER ProjectName
    The name of the project file (for logging purposes).
.RETURNS
    An object containing the count of bin and obj folders removed and any errors encountered.
#>
function Remove-ProjectFolders {
    [OutputType([ProjectFolderResult])]
    [CmdletBinding(SupportsShouldProcess = $true)]
    param (
        [Parameter(Mandatory = $true)]
        [string]$ProjectDir,
        
        [Parameter(Mandatory = $true)]
        [string]$ProjectName
    )
    
    # Initialize result object with default values
    $result = [ProjectFolderResult]::new($true, "")
    
    # Create an array of directory objects to process
    $dirsToProcess = @(
        [PSCustomObject]@{ Path = Join-Path -Path $ProjectDir -ChildPath "bin"; Type = "bin" }
        [PSCustomObject]@{ Path = Join-Path -Path $ProjectDir -ChildPath "obj"; Type = "obj" }
    )
    
    try {
        # Process each directory (bin and obj)
        foreach ($dir in $dirsToProcess) {
            
            # Skip directories that don't exist
            if (-not (Test-Path -Path $dir.Path)) {
                Write-Verbose "No $($dir.Type) folder found for project: $ProjectName"
                continue
            }
            
            # Process directory if it exists
            if ($PSCmdlet.ShouldProcess($dir.Path, "Remove")) {
                [System.IO.Directory]::Delete($dir.Path, $true)
                Write-ColorOutput -Message "Removed $($dir.Type) folder from project: $ProjectName" -Type "Success"
            } else {
                # WhatIf mode - just report what would happen
                Write-ColorOutput -Message "Would remove $($dir.Type) folder from project: $ProjectName" -Type "Info"
            }
            
            # Update the appropriate counter - only once per folder type
            if ($dir.Type -eq "bin") {
                $result.BinRemoved += 1
            } else {
                $result.ObjRemoved += 1
            }
        }
        
        return $result
    }
    catch {
        # Handle any exceptions
        Write-ColorOutput -Message "Failed to remove folders for project $ProjectName. Error: $($_.Exception.Message)" -Type "Error"
        return [ProjectFolderResult]::new($false, $_.Exception.Message)
    }
}

class CleanupStats {
    [int]$ProjectsFound
    [int]$BinFoldersRemoved
    [int]$ObjFoldersRemoved
    [int]$Errors
    [string[]]$ErrorMessages

    CleanupStats() {
        $this.ProjectsFound = 0
        $this.BinFoldersRemoved = 0
        $this.ObjFoldersRemoved = 0
        $this.Errors = 0
        $this.ErrorMessages = @()
    }

    # Method to add error message
    [void]AddError([string]$message) {
        $this.Errors++
        $this.ErrorMessages += $message
    }

    # Method to add results from ProjectFolderResult
    [void]AddResult([ProjectFolderResult]$result) {
        if ($result.Success) {
            $this.BinFoldersRemoved += $result.BinRemoved
            $this.ObjFoldersRemoved += $result.ObjRemoved
        } else {
            $this.AddError($result.ErrorMessage)
        }
    }
}

<#
.SYNOPSIS
    Finds .NET projects and cleans their bin and obj folders.
.DESCRIPTION
    This function recursively searches for .csproj files from a root path,
    identifies their parent directories, and removes the bin and obj folders
    at the same level as these project files.
.PARAMETER RootPath
    The starting directory to search for .NET projects.
.RETURNS
    An object containing statistics about the cleanup operation.
#>
function Find-AndCleanProjects {
    [OutputType([CleanupStats])]
    [CmdletBinding(SupportsShouldProcess = $true)]
    param (
        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )
    
    # Initialize stats with our new class
    $stats = [CleanupStats]::new()
    
    try {
        # Start the search process
        Write-ColorOutput -Message "Searching for .NET projects in: $RootPath" -Type "Info"
        
        # Find all .csproj files and ensure we get full paths
        $projectFiles = Get-ChildItem -Path $RootPath -Filter "*.csproj" -Recurse -File -Exclude "node_modules", ".git" | 
            Select-Object -ExpandProperty FullName
        
        # Early return if no projects found
        if ($projectFiles.Count -eq 0) {
            Write-ColorOutput -Message "No .NET projects found in the specified path." -Type "Info"
            return $stats
        }
        
        # Update statistics
        $stats.ProjectsFound = $projectFiles.Count
        Write-ColorOutput -Message "Found $($stats.ProjectsFound) .NET project(s)" -Type "Info"
        
        # Process each project file
        foreach ($projectFile in $projectFiles) {
            # Extract project directory and name
            $projectDir = Split-Path -Path $projectFile -Parent
            $projectName = Split-Path -Path $projectFile -Leaf
            
            Write-Verbose "Processing project: $projectName"
            
            # Call the refactored Remove-ProjectFolders function
            $result = Remove-ProjectFolders -ProjectDir $projectDir -ProjectName $projectName
            
            # Use the class method to update statistics
            $stats.AddResult($result)
        }
    }
    catch {
        $stats.AddError("An error occurred while searching for projects: $($_.Exception.Message)")
        Write-ColorOutput -Message "An error occurred while searching for projects: $($_.Exception.Message)" -Type "Error"
    }
    
    # Return the collected statistics
    return $stats
}

<#
.SYNOPSIS
    Displays a summary of the cleanup operation.
.DESCRIPTION
    Prints statistics about the cleanup operation,
    including the number of projects found, folders removed, and errors encountered.
#>
function Show-Summary {
    param(
        [Parameter(Mandatory = $true)]
        [CleanupStats]$Stats
    )
    
    # Format and display the summary
    Write-Host "`n============== Summary ==============" -ForegroundColor Cyan
    Write-Host "Projects found: $($Stats.ProjectsFound)" -ForegroundColor White
    
    # Adjust messaging based on WhatIf mode
    if ($WhatIfPreference) {
        Write-Host "Bin folders that would be removed: $($Stats.BinFoldersRemoved)" -ForegroundColor White
        Write-Host "Obj folders that would be removed: $($Stats.ObjFoldersRemoved)" -ForegroundColor White
        Write-Host "Total folders that would be removed: $($Stats.BinFoldersRemoved + $Stats.ObjFoldersRemoved)" -ForegroundColor White
    }
    else {
        Write-Host "Bin folders removed: $($Stats.BinFoldersRemoved)" -ForegroundColor White
        Write-Host "Obj folders removed: $($Stats.ObjFoldersRemoved)" -ForegroundColor White
        Write-Host "Total folders removed: $($Stats.BinFoldersRemoved + $Stats.ObjFoldersRemoved)" -ForegroundColor White
    }
    
    # Show errors if any occurred
    $errorColor = if ($Stats.Errors -gt 0) { "Red" } else { "White" }
    Write-Host "Errors encountered: $($Stats.Errors)" -ForegroundColor $errorColor
    
    # Display error messages if any
    if ($Stats.Errors -gt 0) {
        Write-Host "Error details:" -ForegroundColor Red
        foreach ($errorMsg in $Stats.ErrorMessages) {
            Write-Host " - $errorMsg" -ForegroundColor Red
        }
    }
    
    # Final status message
    if ($Stats.Errors -eq 0) {
        $actionWord = if ($WhatIfPreference) { "would complete" } else { "completed" }
        Write-Host "Cleanup $actionWord successfully!" -ForegroundColor Green
    } else {
        Write-Host "Cleanup completed with $($Stats.Errors) error(s)" -ForegroundColor Red
    }
    
    Write-Host "======================================`n" -ForegroundColor Cyan
}
#endregion

#region Main Script Execution
try {
    $resolvedPath = Resolve-Path -Path $Path
    
    # Display startup messages
    Write-ColorOutput -Message "Starting .NET project cleanup..." -Type "Info"
    if ($WhatIfPreference) {
        Write-ColorOutput -Message "Running in WhatIf mode - no folders will be deleted" -Type "Info"
    }
    
    # Find and clean projects, collect statistics
    $stats = Find-AndCleanProjects -RootPath $resolvedPath
    
    # Display operation summary
    Show-Summary -Stats $stats
}
catch {
    Write-ColorOutput -Message "A critical error occurred: $($_.Exception.Message)" -Type "Error"
    exit 1
}
#endregion