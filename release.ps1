$source = Resolve-Path "build"
$destination = Resolve-Path "release"
$excludePath = "Aetherith_Data\StreamingAssets"

# 再帰的にファイルを取得
Get-ChildItem -Path $source -Recurse -File | ForEach-Object {
    # コピー元ファイルの相対パス
    $fullSourcePath = $_.FullName
    $relativePath = $fullSourcePath.Substring($source.Path.Length + 1)
    # コピー先パス
    $destPath = Join-Path $destination $relativePath
    # コピー先の親フォルダーを作成
    $destDir = Split-Path $destPath
    
    # コピー先パスに「Aetherith_Data\StreamingAssets」が含まれていたらスキップ
    if ($destPath -like "*$excludePath*" -or $destPath -like "*DoNotShip*") {
        Write-Host "スキップ: $destPath"
    } else {
        if (!(Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        Copy-Item -Path $_.FullName -Destination $destPath -Force
        Write-Host "コピー: $destPath"
    }
}