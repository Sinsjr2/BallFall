using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace TEA.Unity {
    /// <summary>
    ///   複数値があるオブジェクトをrenderに渡す際に使用します。
    ///   このとき、Setupはインスタンス化した時に一度だけ呼び出します。
    ///   Renderの前に毎回呼び出すわけではないので注意してください。
    /// </summary>
    [Serializable]
    public class MonoBehaviourRenderFactory<T, Input, State, Message> :
        IRender<Func<IDispatcher<Message>, T, T>, List<State>, KeyValuePair<int, Message>>
        where T : MonoBehaviour, IRender<Input, State, Message> {

        struct DispatcherAndRender {
            public T render;
            public MessageWrapper<int, KeyValuePair<int, Message>, Message> dispatcher;

            public DispatcherAndRender(T render, MessageWrapper<int, KeyValuePair<int, Message>, Message> dispatcher) {
                this.render = render;
                this.dispatcher = dispatcher;
            }
        }

        /// <summary>
        ///   必ず設定する必要があります。
        /// </summary>
        [SerializeField]
        [Tooltip("複数生成するレンダー")]
        T render;

        /// <summary>
        ///   今までに作成されたrenderのキャッシュ
        /// </summary>
        List<DispatcherAndRender> cachedRender;

        IDispatcher<KeyValuePair<int, Message>> dispatcher;

        Func<IDispatcher<Message>, T, T> initializer;

        public T GetRender() {
            Assert.IsNotNull(render);
            return render;
        }

        public void Setup(Func<IDispatcher<Message>, T, T> initializer, IDispatcher<KeyValuePair<int, Message>> dispatcher) {
            Assert.IsNotNull(render);
            this.initializer = initializer;
            // 以前に初期化しているかもしれないのでリセットする
            Clear();
            cachedRender = new List<DispatcherAndRender>();
            this.dispatcher = dispatcher;
        }

        /// <summary>
        ///   引数の配列は先頭から順番にrenderに渡していきます。
        /// </summary>
        public void Render(List<State> state) {
            // 先にSetupを呼ぶ必要がある
            Assert.IsNotNull(cachedRender);
            using (var e = state.GetEnumerator()) {
                int index = 0;
                // キャッシュからrenderを呼び出す
                foreach (var r in cachedRender) {
                    // ステートがあるうちはrenderに渡す
                    if (e.MoveNext()) {
                        r.render.gameObject.SetActive(true);
                        r.dispatcher.value = index;
                        r.render.Render(e.Current);
                    }
                    else {
                        // 不要な分はgameobjectをNonActiveにすることで持っておく
                        // １つでもdisableなオブジェクトを見つけるとあとはすべてdisableになっていると仮定する
                        var go = r.render.gameObject;
                        if (!go.activeSelf) {
                            break;
                        }
                        go.SetActive(false);
                    }
                    index++;
                }
                // 足りない分をインスタンス化する
                for (; e.MoveNext(); index++) {
                    var go = GameObject.Instantiate(render);
                    var pair = new DispatcherAndRender(
                        go,
                        dispatcher.Wrap<int, KeyValuePair<int, Message>, Message>(
                            (d, i, msg) => d.Dispatch(new KeyValuePair<int, Message>(i, msg))));
                    pair.dispatcher.value = index;
                    cachedRender.Add(pair);
                    go = initializer(pair.dispatcher, go);
                    go.Render(e.Current);
                }
            }
        }

        /// <summary>
        ///   キャッシュを消します。
        /// </summary>
        public void Clear() {
            if (cachedRender is null) {
                return;
            }
            foreach (var r in cachedRender) {
                // オブジェクトが破棄されていないときのみ処理する
                if (r.render) {
                    GameObject.Destroy(r.render.gameObject);
                }
            }
            cachedRender.Clear();
        }
    }
}
