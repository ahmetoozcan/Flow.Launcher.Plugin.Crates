dotnet publish Flow.Launcher.Plugin.Crates -c Debug -r win-x64 --no-self-contained

$AppDataFolder = [Environment]::GetFolderPath("ApplicationData")
$flowLauncherExe = "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"

if (Test-Path $flowLauncherExe) {
    Stop-Process -Name "Flow.Launcher" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    if (Test-Path "$AppDataFolder\FlowLauncher\Plugins\Crates") {
        Remove-Item -Recurse -Force "$AppDataFolder\FlowLauncher\Plugins\Crates"
    }

    Copy-Item "Flow.Launcher.Plugin.Crates\bin\Debug\win-x64\publish" "$AppDataFolder\FlowLauncher\Plugins\" -Recurse -Force
    Rename-Item -Path "$AppDataFolder\FlowLauncher\Plugins\publish" -NewName "Crates"
    Copy-Item "Flow.Launcher.Plugin.Crates\Images" "$AppDataFolder\FlowLauncher\Plugins\Crates" -Recurse -Force

    Start-Sleep -Seconds 2
    Start-Process $flowLauncherExe
} else {
    Write-Host "Flow.Launcher.exe not found. Please install Flow Launcher first"
}
