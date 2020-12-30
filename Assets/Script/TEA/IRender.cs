public interface IRender<Input, State, Act> {

    /// <summary>
    ///   レンダの初期化を行います。
    /// </summary>
    void Setup(Input input, IDispacher<Act> dispacher);

    /// <summary>
    ///   stateの状態に従い描画を行います。
    ///   また、ボタンなどのコールバックが呼ばれた際のdispacherの設定も行います。
    ///   dispacherを呼び出すとこのメソッドを呼び出します。
    ///   よって、このメソッド内でdispacherを呼び出す際はstateの状態によってdispacherを呼び出さないようにする必要があります。
    /// </summary>
    void Render(State state);
}
