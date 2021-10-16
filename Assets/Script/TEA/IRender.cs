namespace TEA {
    public interface IRender<Input, State, Message> {

        /// <summary>
        ///   レンダの初期化を行います。
        /// </summary>
        void Setup(Input input, IDispatcher<Message> dispatcher);

        /// <summary>
        ///   stateの状態に従い描画を行います。
        ///   また、ボタンなどのコールバックが呼ばれた際のdispatcherの設定も行います。
        ///   dispatcherを呼び出すとこのメソッドを呼び出します。
        ///   よって、このメソッド内でdispatcherを呼び出す際はstateの状態によってdispatcherを呼び出さないようにする必要があります。
        /// </summary>
        void Render(State state);
    }
}
