<#
.SYNOPSIS
    将「星际商店」模组导出为可上传到 Steam 创意工坊的「发布副本」。
.DESCRIPTION
    在仓库父目录创建 星际商店_Release_<时间戳>\ 文件夹，
    只复制 Workshop 需要的目录与文件，跳过开发文件、缓存、压缩包、原图等。
    可选 -Zip 参数顺带打包成 zip 备份。
.PARAMETER OutputRoot
    输出根目录，默认是当前仓库的父目录。
.PARAMETER Zip
    打包成 zip 备份。
.PARAMETER KeepSource
    保留 Source\ 源代码（默认保留）。-KeepSource:$false 可排除。
.EXAMPLE
    .\export-workshop.ps1
    .\export-workshop.ps1 -Zip
    .\export-workshop.ps1 -OutputRoot D:\Temp -Zip -KeepSource:$false
#>
[CmdletBinding()]
param(
    [string]$OutputRoot,
    [switch]$Zip,
    [bool]$KeepSource = $true
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ModName  = Split-Path -Leaf $RepoRoot

if (-not $OutputRoot) { $OutputRoot = Split-Path -Parent $RepoRoot }

$Stamp     = Get-Date -Format 'yyyyMMdd_HHmm'
$ExportDir = Join-Path $OutputRoot ("{0}_Release_{1}" -f $ModName, $Stamp)
$TargetDir = Join-Path $ExportDir $ModName

Write-Host ""
Write-Host "==== 星际商店 - 发布副本导出 ====" -ForegroundColor Cyan
Write-Host "源目录 : $RepoRoot"
Write-Host "目标   : $TargetDir"
Write-Host ""

# ---- 1. 创建干净的目标目录 ----
if (Test-Path $TargetDir) {
    Write-Host "目标已存在，清空中..." -ForegroundColor Yellow
    Remove-Item -LiteralPath $TargetDir -Recurse -Force
}
New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

# ---- 2. 必须包含的顶层目录（白名单）----
$IncludeDirs = @('About','Assemblies','Defs','Languages','Patches','Textures','Docs')
if ($KeepSource) { $IncludeDirs += 'Source' }

# 根目录白名单文件
$IncludeFiles = @('LoadFolders.xml','LICENSE','LICENSE.txt','README.md')

# ---- 3. 排除规则 ----
$ExcludePatterns = @(
    '\\\.git\\',  '\\\.git$',
    '\\\.claude\\', '\\\.trae\\', '\\\.multi-agent\\',
    '\\\.uploads\\', '\\\.vs\\', '\\\.vscode\\', '\\\.idea\\',
    '\\plans\\', '\\开发测试\\', '\\教程\\',
    '\\bin\\', '\\obj\\'
)
$ExcludeExtensions = @('.rar','.zip','.7z','.pdb','.bak','.tmp','.log')
$ExcludeFileNames  = @(
    'starstore_mascot原图.jpg',
    '机娘原图.png',
    '猫娘原图.png',
    '猫娘(1).png',
    '内测说明文档.md',
    '反馈模板.txt',
    '群公告.txt',
    'Steam反馈收集方案.md',
    'Steam置顶讨论帖模板.md',
    '创意工坊介绍.txt',
    '创意工坊发布准备清单.md',
    '封面提示词.txt',
    '更新日志：1.1.4.txt',
    'build.bat',
    '.gitignore'
)

function Test-Exclude {
    param([string]$RelPath)
    foreach ($pat in $ExcludePatterns) {
        if ($RelPath -imatch $pat) { return $true }
    }
    $ext = [IO.Path]::GetExtension($RelPath).ToLower()
    if ($ExcludeExtensions -contains $ext) { return $true }
    $name = [IO.Path]::GetFileName($RelPath)
    if ($ExcludeFileNames -contains $name) { return $true }
    return $false
}

# ---- 4. 复制白名单目录 ----
foreach ($d in $IncludeDirs) {
    $src = Join-Path $RepoRoot $d
    if (-not (Test-Path $src)) {
        Write-Host "  跳过（不存在）: $d" -ForegroundColor DarkGray
        continue
    }
    Write-Host "复制目录: $d ..." -ForegroundColor Green
    $files = Get-ChildItem -LiteralPath $src -Recurse -File -Force
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($RepoRoot.Length).TrimStart('\')
        if (Test-Exclude $rel) { continue }
        $dst = Join-Path $TargetDir $rel
        $dstDir = Split-Path -Parent $dst
        if (-not (Test-Path $dstDir)) { New-Item -ItemType Directory -Path $dstDir -Force | Out-Null }
        Copy-Item -LiteralPath $f.FullName -Destination $dst -Force
    }
}

# ---- 5. 复制根目录白名单文件 ----
foreach ($f in $IncludeFiles) {
    $src = Join-Path $RepoRoot $f
    if (Test-Path $src) {
        if (Test-Exclude $f) { continue }
        Copy-Item -LiteralPath $src -Destination (Join-Path $TargetDir $f) -Force
        Write-Host "复制文件: $f" -ForegroundColor Green
    }
}

# ---- 6. 校验必备文件 ----
Write-Host ""
Write-Host "==== 校验 ====" -ForegroundColor Cyan
$required = @('About\About.xml','About\Preview.png')
$missing = @()
foreach ($r in $required) {
    $p = Join-Path $TargetDir $r
    if (Test-Path $p) { Write-Host "  [OK] $r" -ForegroundColor Green }
    else { Write-Host "  [MISSING] $r" -ForegroundColor Red; $missing += $r }
}

$dlls = Get-ChildItem -LiteralPath (Join-Path $TargetDir 'Assemblies') -Filter *.dll -ErrorAction SilentlyContinue
if ($dlls -and $dlls.Count -gt 0) {
    Write-Host "  [OK] Assemblies\*.dll ($($dlls.Count) 个)" -ForegroundColor Green
} else {
    Write-Host "  [MISSING] Assemblies\ 下没有 dll！请先 Release 编译" -ForegroundColor Red
    $missing += 'Assemblies\*.dll'
}

$pdbs = Get-ChildItem -LiteralPath $TargetDir -Recurse -Filter *.pdb -ErrorAction SilentlyContinue
if ($pdbs) {
    Write-Host "  [WARN] 检测到 .pdb 调试文件，建议手动删除：" -ForegroundColor Yellow
    $pdbs | ForEach-Object { Write-Host "      $($_.FullName)" }
}

$aboutXmlPath = Join-Path $TargetDir 'About\About.xml'
if (Test-Path $aboutXmlPath) {
    try {
        [xml]$xml = Get-Content -LiteralPath $aboutXmlPath -Encoding UTF8
        Write-Host ("  [i] 版本号: {0}" -f $xml.ModMetaData.modVersion) -ForegroundColor Cyan
        Write-Host ("  [i] 支持版本: {0}" -f ($xml.ModMetaData.supportedVersions.li -join ', ')) -ForegroundColor Cyan
    } catch {
        Write-Host "  [WARN] About.xml 解析失败" -ForegroundColor Yellow
    }
}

# Preview.png 体积检查（Workshop 限制 1MB）
$preview = Join-Path $TargetDir 'About\Preview.png'
if (Test-Path $preview) {
    $previewMB = [math]::Round((Get-Item $preview).Length / 1MB, 2)
    if ($previewMB -gt 1.0) {
        Write-Host "  [WARN] Preview.png 体积 ${previewMB} MB > 1 MB，请压缩" -ForegroundColor Yellow
    } else {
        Write-Host "  [OK] Preview.png 体积 ${previewMB} MB" -ForegroundColor Green
    }
}

# ---- 7. 体积统计 ----
$size = (Get-ChildItem -LiteralPath $TargetDir -Recurse -File | Measure-Object -Property Length -Sum).Sum
$sizeMB = [math]::Round($size / 1MB, 2)
Write-Host ""
Write-Host "总体积: $sizeMB MB" -ForegroundColor Cyan

# ---- 8. 可选打包 zip ----
if ($Zip) {
    $zipPath = "$ExportDir.zip"
    if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    Write-Host "打包 zip 中..." -ForegroundColor Cyan
    Compress-Archive -Path $TargetDir -DestinationPath $zipPath
    Write-Host "已生成: $zipPath" -ForegroundColor Green
}

Write-Host ""
if ($missing.Count -eq 0) {
    Write-Host "==== 完成 ====" -ForegroundColor Green
    Write-Host "下一步：用 RimWorld 主菜单 → 模组 → 上传到 Workshop，选择以下目录:" -ForegroundColor Green
    Write-Host "  $TargetDir" -ForegroundColor White
} else {
    Write-Host "==== 完成但有缺失项，请先处理上面 [MISSING] 标记 ====" -ForegroundColor Yellow
}
