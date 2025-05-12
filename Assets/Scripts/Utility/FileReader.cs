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
                FileMode.OpenOrCreate,  // �t�@�C��������ΊJ���A�Ȃ���΍��  
                FileAccess.ReadWrite,   // �ǂݏ�������������  
                FileShare.None          // ���̃v���Z�X�̃A�N�Z�X���u���b�N�i�K�v�ɉ�����Read�Ɂj  
            ))
            {
                if (fs.Length == 0 && !string.IsNullOrEmpty(defaultContent))
                {
                    // �V�K�쐬���ꂽ�̂ŏ������e����������  
                    using (var writer = new StreamWriter(fs, encoding, 4096, leaveOpen: true))
                    {
                        writer.Write(defaultContent);
                        writer.Flush();
                    }

                    fs.Seek(0, SeekOrigin.Begin); // �ǂݒ����̂��ߐ擪�֖߂�  
                }

                using (var reader = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: true))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        catch (IOException ex)
        {
            Debug.LogError($"�t�@�C���ǂݏ����G���[: {ex.Message}");
            return null;
        }
    }
}
