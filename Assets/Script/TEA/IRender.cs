namespace TEA {
    public interface IRender<Input, State, Message> {

        /// <summary>
        ///   stateの状態に従い描画を行います。
        ///   ディスパッチするとこのメソッドを呼び出します。
        /// </summary>
        void Render(State state);
    }
}
