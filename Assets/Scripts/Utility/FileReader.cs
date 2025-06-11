using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SafeFileReader
{
    public static string ReadOrCreateTextFile(string path, Encoding encoding = null, string defaultContent = "")
    {
        encoding ??= Encoding.UTF8;

        try
        {
            using (FileStream fs = new FileStream(
                path,
                FileMode.OpenOrCreate,  // ファイルがあれば開く、なければ作る  
                FileAccess.ReadWrite,   // 読み書き両方を許可  
                FileShare.None          // 他のプロセスのアクセスをブロック（必要に応じてReadに）  
            ))
            {
                if (fs.Length == 0 && !string.IsNullOrEmpty(defaultContent))
                {
                    // 新規作成されたので初期内容を書き込む  
                    using (var writer = new StreamWriter(fs, encoding, 4096, leaveOpen: true))
                    {
                        writer.Write(defaultContent);
                        writer.Flush();
                    }

                    fs.Seek(0, SeekOrigin.Begin); // 読み直しのため先頭へ戻す  
                }

                using (var reader = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: true))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        catch (IOException ex)
        {
            Debug.LogError($"ファイル読み書きエラー: {ex.Message}");
            return null;
        }
    }
    /// <summary>
    /// string path = PathVerifier("C:\BasePath\","C:\BasePath\calc.exe");
    /// if (path == "C:\BasePath\calc.exe") { /*OK*/ }
    /// PathVerifier("C:\BasePath\","../../Windows/System32/calc.exe");
    /// /// if (path == "C:\BasePath\calc.exe") { /*OK*/ }
    /// </summary>

    public static string PathVerifier(string basePath, string targetPath) {
        // パスを結合し正規化する
        string fullPath = Path.GetFullPath(Path.Combine(targetPath));
        // ベースパスとターゲットパスも正規化する
        // これにより、相対パスやシンボリックリンクの影響を受けない。また区切り文字の違いも吸収される
        basePath = Path.GetFullPath(basePath);
        targetPath = Path.GetFullPath(targetPath);

        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(targetPath))
        {
            Debug.LogError("ベースパスまたはターゲットパスが空です。");
            return fullPath;
        }
        
        // ベースパスよりもさかのぼっている場合はベースパス+ファイル名を返す
        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"正規化されたパスは `{fullPath}` です。ターゲットパス {targetPath} はベースパス {basePath} の外にあります。");
            return Path.Combine(basePath, Path.GetFileName(targetPath));
        }
        // ディレクトリを遡っていないので問題なし
        return targetPath;
    }
}
