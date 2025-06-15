# 最初に必要なディレクトリを作っておく
$destDirList=@("release\Aetherith_Data\StreamingAssets\LLM")
foreach ($destDir in $destDirList) {
    if (!(Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
}
# ベース定数の定義
$source = Resolve-Path "build"
$destination = Resolve-Path "release"
$excludePath = "Aetherith_Data\StreamingAssets"

# 例外的にコピーしたいファイルの相対パスを配列で指定（必要に応じて追加）
$includeExceptions = @(
    "Aetherith_Data\StreamingAssets\VRM\Default.vrm",
    "Aetherith_Data\StreamingAssets\undreamai-v1.2.5-llamacpp\"
    )

# 再帰的にファイルを取得
Get-ChildItem -Path $source -Recurse -File | ForEach-Object {
    $fullSourcePath = $_.FullName
    $relativePath = $fullSourcePath.Substring($source.Path.Length + 1)
    $destPath = Join-Path $destination $relativePath
    $destDir = Split-Path $destPath

    # 例外ファイルかどうかを判定（大文字小文字無視）
    $isException = $false
    foreach ($exception in $includeExceptions) {
        if ($relativePath -ieq $exception -or $relativePath -like "$exception*") {
            $isException = $true
            break
        }
    }
    # 除外対象であり、例外ファイルでもない → スキップ
    if (($relativePath -like "$excludePath*") -and -not $isException -or ($relativePath -like "*DoNotShip*")) {
        Write-Host "スキップ: $relativePath"
    } else {
        if (!(Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        Copy-Item -Path $_.FullName -Destination $destPath -Force
        Write-Host "コピー: $relativePath"
    }
}
