public interface StateInitializer <Input, State> {
    /// <summary>
    ///   状態の作成を行います。
    ///   Unityであるとインスペクターから設定した値を初期値とする場合が多いのでこのようにしています。
    /// </summary>
    State CreateState(Input initial);
}
