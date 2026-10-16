# 《时光之痕》正式美术「逐组」出图驱动。
# 按推荐顺序一组一组跑：场景底图 -> 每个角色立绘（基准图先出，变体自动带参考图）-> CG。
# 每组出完立刻生成一张带 id 标注的审图表，人工看过再替换 Assets/Resources/Art 里的占位图。
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\art-batch.ps1 -DryRun   # 不联网，验证分组与命令
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\art-batch.ps1            # 全部组
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\art-batch.ps1 -Groups bg,linxiao
#
# 退出码：0 全部成功 / 2 前置条件不满足（缺 key 等）/ 3 有分组失败

[CmdletBinding()]
param(
    [string[]]$Groups = @(),
    [string]$Out = 'art/out',
    [switch]$DryRun,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$OutputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

$GenTool = 'story/tools/gen-art.js'
$ReviewTool = 'story/tools/art-review.py'
$Manifest = 'art/manifest.json'

# 分组顺序：kind 直接用 bg/cg；立绘按角色，引用多的先出
$DefaultGroups = @('bg', 'linxiao', 'alice', 'raine', 'xya', 'lord', 'zhou', 'vera', 'cg')
# 立绘文件名前缀 = 角色名；场景/CG 的产物落在 Bg / Cg 子目录
$KindSubdir = @{ bg = 'Bg'; cg = 'Cg'; portrait = 'Portrait' }

function Write-Step([string]$text) { Write-Host "==> $text" -ForegroundColor Cyan }
function Write-Warn([string]$text) { Write-Host "    $text" -ForegroundColor Yellow }

function Test-Preflight {
    $problems = @()
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) { $problems += '找不到 node，无法运行 gen-art.js' }
    if (-not (Test-Path $Manifest)) { $problems += "缺少 $Manifest" }
    if (-not (Test-Path $GenTool)) { $problems += "缺少 $GenTool" }
    if (-not (Test-Path $ReviewTool)) { $problems += "缺少 $ReviewTool" }

    $provider = $env:DSH_IMAGE_PROVIDER
    $keyName = switch ($provider) { 'google' { 'GOOGLE_GEMINI_API_KEY' } 'openai' { 'OPENAI_API_KEY' } default { '' } }
    $haveKey = $false
    if ($keyName) {
        $keyName2 = $keyName
        $val = (Get-Item "Env:$keyName2" -ErrorAction SilentlyContinue).Value
        $haveKey = -not [string]::IsNullOrWhiteSpace($val)
    }
    if (-not $DryRun) {
        if (-not $provider) {
            $problems += '未设置 DSH_IMAGE_PROVIDER（google 或 openai）'
        } elseif (-not $haveKey) {
            $problems += "DSH_IMAGE_PROVIDER=$provider 但环境变量 $keyName 为空"
        }
    }
    return $problems
}

function Show-KeyHelp {
    Write-Host ''
    Write-Host '真实出图需要一个图像生成 key（本脚本只从环境变量读，不写进仓库、不打印值）：' -ForegroundColor Yellow
    Write-Host '  $env:DSH_IMAGE_PROVIDER = "google"   # 或 "openai"'
    Write-Host '  $env:GOOGLE_GEMINI_API_KEY = "<你的 key>"   # provider=google 时'
    Write-Host '  $env:OPENAI_API_KEY = "<你的 key>"          # provider=openai 时'
    Write-Host '可选：$env:DSH_IMAGE_MODEL 覆盖默认模型（google 默认 gemini-2.0-flash-preview-image-generation，openai 默认 gpt-image-1）'
    Write-Host '不想用 API：把 art/manifest.json 的 prompt/size 交给外部画师，交付 <id>.png 后仍可用本脚本的审图表环节验收。'
    Write-Host ''
}

function Invoke-Group([string]$group) {
    $mode = if ($DryRun) { '--dry-run' } else { '--generate' }
    $genArgs = @($GenTool, $mode, '--used-only', '--only', $group, '--out', $Out)
    if ($Force) { $genArgs += '--force' }
    Write-Step "分组 $group 出图（$mode）"
    # 必须 Out-Host：否则子进程 stdout 会混进函数返回值，$code 变成字符串数组导致误判失败
    & node @genArgs | Out-Host
    $code = $LASTEXITCODE
    if ($code -ne 0) {
        Write-Warn "分组 $group 出图退出码 $code"
        return $code
    }
    if ($DryRun) { return 0 }
    Update-ReviewSheet $group
    return 0
}

function Update-ReviewSheet([string]$group) {
    $dir = if ($KindSubdir.ContainsKey($group)) { Join-Path $Out $KindSubdir[$group] } else { Join-Path $Out 'Portrait' }
    if (-not (Test-Path $dir)) { Write-Warn "没有产物目录 $dir，跳过审图表"; return }
    $sheet = Join-Path $Out ("_review/{0}.png" -f $group)
    $pyArgs = @($ReviewTool, '--dir', $dir, '--out', $sheet, '--title', "分组 $group")
    if (-not $KindSubdir.ContainsKey($group)) { $pyArgs += @('--filter', "$group`_*", '--cols', '3') }
    Write-Step "分组 $group 审图表"
    & python @pyArgs | Out-Host
    if ($LASTEXITCODE -ne 0) { Write-Warn "审图表生成失败（退出码 $LASTEXITCODE），出图本身已成功" }
}

function Main {
    if ($Groups.Count -eq 0) { $Groups = $DefaultGroups }
    Write-Host "仓库：$RepoRoot"
    Write-Host "输出：$Out    模式：$(if ($DryRun) { 'dry-run（不联网）' } else { 'generate（真实出图）' })"
    Write-Host "分组：$($Groups -join ' -> ')"

    $problems = Test-Preflight
    if ($problems.Count -gt 0) {
        $problems | ForEach-Object { Write-Host "前置条件不满足：$_" -ForegroundColor Red }
        Show-KeyHelp
        exit 2
    }

    $failed = @()
    foreach ($g in $Groups) {
        $code = Invoke-Group $g
        if ($code -ne 0) { $failed += $g }
    }

    Write-Host ''
    Write-Step '汇总'
    foreach ($g in $Groups) {
        $state = if ($failed -contains $g) { '失败' } else { 'OK' }
        Write-Host ("  {0,-10} {1}" -f $g, $state)
    }
    if (-not $DryRun) { Write-Host "  审图表目录：$(Join-Path $Out '_review')" }
    if ($failed.Count -gt 0) {
        Write-Host "有 $($failed.Count) 个分组失败：$($failed -join ', ')" -ForegroundColor Red
        exit 3
    }
    if (-not $DryRun) {
        Write-Host ''
        Write-Host '下一步：逐张看 art/out/_review/*.png，满意的组再替换占位图（文件名保持不变）：'
        Write-Host "  node story/tools/gen-art.js --generate --used-only --only <组> --out Assets/Resources/Art --force"
        Write-Host '  然后重建：build-preview.js --standalone / build-preview.js / smoke-preview.js / build-exe.js'
    }
    exit 0
}

Main
