using System;

public enum Unit {
    Default
}

/// <summary>
///   Switch case で見つからなかった場合
/// </summary>
public class PatternMatchNotFoundException : Exception {

    public PatternMatchNotFoundException(object notFoundValue)
        : base ($"{notFoundValue}はcaseに存在しません"){}
}
