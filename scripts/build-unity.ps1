# 《时光之痕》Unity 原生包一键构建脚本
#
# 前置条件：Unity 许可证已激活（见「Unity 激活与出包说明.md」第一步，只有本人能做）。
# 用法：
#   pwsh -File scripts/build-unity.ps1            # 出包到 Build\Windows\时光之痕.exe
#   pwsh -File scripts/build-unity.ps1 -Run       # 出完直接跑起来
#   pwsh -File scripts/build-unity.ps1 -CheckOnly # 只让 Unity 编译一遍脚本，不出包
#
# 退出码：0 成功；1 构建失败；2 前置条件不满足（找不到编辑器 / 许可证未激活）。

[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$UnityExe = '',
    [switch]$Run,
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}

$ExeName = '时光之痕.exe'
$LicenseError = 'No valid Unity Editor license found'

function Find-UnityEditor {
    <# 按「显式参数 → 注册表卸载信息 → 常见安装目录」的顺序找编辑器，返回第一个可用的 Unity.exe。 #>
    param([string]$Explicit)

    if (-not [string]::IsNullOrWhiteSpace($Explicit)) {
        if (Test-Path -LiteralPath $Explicit) { return (Resolve-Path -LiteralPath $Explicit).Path }
        throw "指定的编辑器路径不存在：$Explicit"
    }

    $candidates = @()
    $roots = @("$env:ProgramFiles\Unity", "$env:ProgramFiles\Unity Hub", ${env:ProgramFiles(x86)} + '\Unity')
    foreach ($root in $roots) {
        if (Test-Path -LiteralPath $root) {
            $candidates += Get-ChildItem -LiteralPath $root -Recurse -Filter 'Unity.exe' -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '\\Editor\\Unity\.exe$' } |
                Select-Object -ExpandProperty FullName
        }
    }
    # 兼容本机这种「C:\Program Files\Unity 2021.3.45f2\Editor\Unity.exe」的非 Hub 布局
    $candidates += Get-ChildItem -LiteralPath $env:ProgramFiles -Directory -Filter 'Unity *' -ErrorAction SilentlyContinue |
        ForEach-Object { Join-Path $_.FullName 'Editor\Unity.exe' } |
        Where-Object { Test-Path -LiteralPath $_ }

    $found = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
    if (-not $found) {
        Write-Host '没找到 Unity 编辑器。装好后用 -UnityExe 指定，例如：' -ForegroundColor Yellow
        Write-Host '  pwsh -File scripts/build-unity.ps1 -UnityExe "C:\Program Files\Unity 2021.3.45f2\Editor\Unity.exe"'
        exit 2
    }
    return $found
}

function Get-VersionLine {
    <# 从 ProjectSettings/ProjectVersion.txt 取工程钉住的编辑器版本，用于提示版本不匹配。 #>
    param([string]$Project)
    $file = Join-Path $Project 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $file)) { return '' }
    $line = Select-String -Path $file -Pattern '^m_EditorVersion:\s*(.+)$' | Select-Object -First 1
    if ($line) { return $line.Matches[0].Groups[1].Value.Trim() }
    return ''
}

$editor = Find-UnityEditor -Explicit $UnityExe
$wantVersion = Get-VersionLine -Project $ProjectPath
Write-Host "编辑器： $editor"
Write-Host "工程：   $ProjectPath"
if ($wantVersion) {
    $actual = (Get-Item $editor).VersionInfo.ProductVersion
    if ($actual -notlike "*$($wantVersion.Replace('.',''))*" -and $actual -notmatch [regex]::Escape($wantVersion)) {
        Write-Host "注意：工程钉的是 $wantVersion，实际启动的是 $actual —— 版本不一致时 Unity 会升级工程，建议用 Hub 装对应版本。" -ForegroundColor Yellow
    }
}

$logDir = Join-Path $ProjectPath 'Logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$logPath = Join-Path $logDir "unity-build-$stamp.log"

$unityArgs = @('-batchmode', '-nographics', '-projectPath', $ProjectPath, '-logFile', $logPath)
if ($CheckOnly) {
    $unityArgs += @('-quit', '-buildTarget', 'Win64')
    Write-Host '模式：只编译脚本（-quit，不出包）'
} else {
    $unityArgs += @('-quit', '-executeMethod', 'BuildScript.BuildWindowsBatch')
    Write-Host '模式：构建 Windows 可执行文件'
}

Write-Host "日志： $logPath"
# Unity.exe 是 GUI 子系统程序：直接 & 调用不会等待，$LASTEXITCODE 会是空、日志也还没写完。
# 必须 Start-Process -Wait -PassThru 才能拿到真正的退出码。
$quoted = $unityArgs | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }
$proc = Start-Process -FilePath $editor -ArgumentList $quoted -Wait -PassThru
$code = if ($null -ne $proc.ExitCode) { $proc.ExitCode } else { -1 }

$logText = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw } else { '' }

if ($logText -match [regex]::Escape($LicenseError)) {
    Write-Host ''
    Write-Host '许可证未激活，构建没跑起来。请本人完成这一步：' -ForegroundColor Red
    Write-Host '  开始菜单 → Unity Hub → 右上角头像 Sign in → Licenses → Add → Get a free personal license'
    Write-Host '激活成功后重跑本脚本即可。'
    exit 2
}

if ($code -ne 0) {
    Write-Host ''
    Write-Host "Unity 退出码 $code，构建失败。日志里的报错行：" -ForegroundColor Red
    $hits = $logText -split "`r?`n" | Select-String -Pattern 'error CS\d+|Exception:|Build completed with a result of|Aborting' | Select-Object -Last 20
    if ($hits) { $hits | ForEach-Object { Write-Host "  $_" } } else { Write-Host "  （没抓到关键行，请直接看日志文件）" }
    exit 1
}

if ($CheckOnly) {
    Write-Host '脚本编译通过。' -ForegroundColor Green
    exit 0
}

$produced = Join-Path $ProjectPath "Build\Windows\$ExeName"
if (-not (Test-Path -LiteralPath $produced)) {
    Write-Host "Unity 报成功但找不到产物 $produced —— 请核对 BuildScript.cs 的输出目录。" -ForegroundColor Red
    exit 1
}

$sizeMb = [math]::Round((Get-Item -LiteralPath $produced).Length / 1MB, 2)
Write-Host ''
Write-Host "出包成功： $produced（$sizeMb MB）" -ForegroundColor Green

if ($Run) {
    Write-Host '正在启动……'
    Start-Process -FilePath $produced -WorkingDirectory (Split-Path -Parent $produced)
}
