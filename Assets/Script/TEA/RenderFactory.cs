using System;
using System.Collections.Generic;

namespace TEA {

    public class RenderFactory<TRender, State, Message> where TRender : IRender<State> {
        readonly IDispatcher<KeyValuePair<int, Message>> dispatcher;
        readonly Func<IDispatcher<Message>, TRender> createRender;

        public RenderFactory(IDispatcher<KeyValuePair<int, Message>> dispatcher, Func<IDispatcher<Message>, TRender> createRender) {
            this.dispatcher = dispatcher;
            this.createRender = createRender;
        }

        /// <summary>
        ///  キャッシュに対して、あれば描画し、そうでなければ
        ///  新しくオブジェクトを作ります。
        ///  戻り値で処理したキャッシュの最後のインデックスを返します。
        /// </summary>
        public int ApplyToRender(List<TRender> cachedRender,
                                 IEnumerable<State> state) {
            using var e = state.GetEnumerator();
            int i = 0;
            // キャッシュからrenderを呼び出す
            foreach (var r in cachedRender) {
                if (!e.MoveNext()) {
                    return i;
                }
                // ステートがあるうちはrenderに渡す
                r.Render(e.Current);
                i++;
            }
            // 足りない分をインスタンス化する
            for (; e.MoveNext(); i++) {
                var index = i;
                var render = createRender(dispatcher.Wrap((Message msg) => new KeyValuePair<int, Message>(index, msg)));
                cachedRender.Add(render);
                render.Render(e.Current);
            }
            return i;
        }
    }
}
