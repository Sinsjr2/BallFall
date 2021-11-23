using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace TEA.Unity {
    /// <summary>
    ///   複数値があるオブジェクトをrenderに渡す際に使用します。
    ///   このとき、レンダーの作成は一度だけ呼び出します。
    ///   Renderの前に毎回呼び出すわけではないので注意してください。
    /// </summary>
    [Serializable]
    public class MonoBehaviourRenderFactory<T, State, Message> : ITEAComponent<IEnumerable<State>, KeyValuePair<int, Message>>, IDisposable
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

        Func<IDispatcher<Message>, GameObjectActivateRender> createRender;
        public Func<IDispatcher<Message>, T, T> Initializer {
            set => createRender = d => new GameObjectActivateRender(value(d, render));
        }

        IDispatcher<KeyValuePair<int, Message>> dispatcher;

        public T GetRender() {
            Assert.IsNotNull(render);
            return render;
        }

        public void Setup(IDispatcher<KeyValuePair<int, Message>> dispatcher) {
            Assert.IsNotNull(render);
            // １度しか呼び出していないか確認
            Assert.IsNull(cachedRender);
            cachedRender = new List<GameObjectActivateRender>();
            this.dispatcher = dispatcher;
        }

        /// <summary>
        ///   引数の配列は先頭から順番にrenderに渡していきます。
        /// </summary>
        public void Render(IEnumerable<State> state) {
            // 先にSetupを呼ぶ必要がある
            Assert.IsNotNull(cachedRender);
            var nextRenderIndex = cachedRender.ApplyToRender(dispatcher, createRender, state);
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
        ///   所持しているゲームオブジェクトを全て削除します。
        /// </summary>
        public void Dispose() {
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
