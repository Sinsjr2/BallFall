using System;

public class Singleton<T> where T : new() {
    public static T Instance = new T();
}

public interface StateInitializer <Input, State> {
    /// <summary>
    ///   状態の作成を行います。
    ///   Unityであるとインスペクターから設定した値を初期値とする場合が多いのでこのようにしています。
    /// </summary>
    State CreateState(Input initial);
}

/// <summary>
///   何も行いません。
/// </summary>
public class NoneUpdate<State, Msg> : IUpdate<State, Msg> {
    public static readonly NoneUpdate<State, Msg> Instance = new NoneUpdate<State, Msg>();
    private NoneUpdate() {}
    public State Update(State state, Msg msg) {
        return state;
    }
}

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
