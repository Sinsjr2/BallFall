using System;
using System.Linq;

/// <summary>
///   Enumの乱数を作ります。
/// </summary>
public static class RandomEnum<T> where T : System.Enum {

    /// <summary>
    ///   数値からEnumに変換するためのテーブル
    /// </summary>
    static readonly T[] enums = Enum.GetValues(typeof(T)).Cast<T>().ToArray();

    /// <summary>
    ///   ランダムな値を取得します。
    /// </summary>
    public static T GetRandom() {
        return enums[UnityEngine.Random.Range(0, enums.Length)];
    }
}
