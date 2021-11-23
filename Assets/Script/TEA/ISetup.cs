namespace TEA {

    /// <summary>
    ///  通知先の設定を行うためのインターフェース
    ///  ディスパッチ時のメッセージの型を型推論させるために使用します。
    /// </summary>
    public interface ISetup<Message> {

        /// <summary>
        ///  通知先を設定します。
        /// </summary>
        public void Setup(IDispatcher<Message> dispatcher);
    }
}
