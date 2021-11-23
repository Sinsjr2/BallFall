using System;
using System.Collections.Generic;

namespace TEA {

    public static class RenderFactory {
        /// <summary>
        ///  キャッシュに対して、あれば描画し、そうでなければ
        ///  新しくオブジェクトを作ります。
        ///  戻り値で処理したキャッシュの最後のインデックスを返します。
        /// </summary>
        public static int ApplyToRender<TRender, State, Message>(
            this List<TRender> cachedRender,
            IDispatcher<KeyValuePair<int, Message>> dispatcher,
            Func<IDispatcher<Message>, TRender> createRender,
            IEnumerable<State> state) where TRender : IRender<State> {
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
