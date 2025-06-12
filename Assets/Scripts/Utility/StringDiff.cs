using System;
using UnityEngine;
public class StringDiff
{
    /// <summary>
    /// 文字列の差分を計算し、変更された部分を返します。
    /// </summary>
    /// <param name="oldText">古い文字列</param>
    /// <param name="newText">新しい文字列</param>
    /// <returns>変更された部分の文字列</returns>
    public static string GetDiff(string current, string previous)
    {
        int commonLength = 0;
        int minLength = Math.Min(previous.Length, current.Length);

        // 先頭から一致している文字数をカウント
        for (int i = 0; i < minLength; i++)
        {
            if (previous[i] != current[i])
                break;
            commonLength++;
        }

        string diff = current.Substring(commonLength);
        return diff;
    }
}