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
    public class MonoBehaviourRenderFactory<T, State, Message> :
        IRender<IEnumerable<State>>
        where T : MonoBehaviour, IRender<State> {

        /// <summary>
        ///  描画をする時にゲームオブジェクトをアクティブにします。
        /// </summary>
        class GameObjectActivateRender : IRender<State> {
            readonly T obj;
            public readonly GameObject GO;

            public GameObjectActivateRender(T obj) {
                this.obj = obj;
                GO = obj.gameObject;
            }

            public void Render(State state) {
                GO.SetActive(true);
                obj.Render(state);
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
        List<GameObjectActivateRender> cachedRender;

        RenderFactory<GameObjectActivateRender, State, Message> factory;

        public T GetRender() {
            Assert.IsNotNull(render);
            return render;
        }

        public void Setup(Func<IDispatcher<Message>, T, T> initializer, IDispatcher<KeyValuePair<int, Message>> dispatcher) {
            Assert.IsNotNull(render);
            factory = new(dispatcher, d => new GameObjectActivateRender(initializer(d, render)));
            // 以前に初期化しているかもしれないのでリセットする
            Clear();
            cachedRender = new List<GameObjectActivateRender>();
        }

        /// <summary>
        ///   引数の配列は先頭から順番にrenderに渡していきます。
        /// </summary>
        public void Render(IEnumerable<State> state) {
            // 先にSetupを呼ぶ必要がある
            Assert.IsNotNull(cachedRender);
            var nextRenderIndex = factory.ApplyToRender(cachedRender, state);
            // 不要な分はgameobjectをNonActiveにすることで持っておく
            // １つでもdisableなオブジェクトを見つけるとあとはすべてdisableになっていると仮定する
            for (int i = nextRenderIndex; i < cachedRender.Count; i++) {
                var go = cachedRender[i].GO;
                if (!go.activeSelf) {
                    break;
                }
                go.SetActive(false);
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
                if (r.GO) {
                    UnityEngine.Object.Destroy(r.GO);
                }
            }
            cachedRender.Clear();
        }
    }
}
