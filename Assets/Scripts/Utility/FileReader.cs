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
}
