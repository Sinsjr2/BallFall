using System;
using System.Collections.Generic;

namespace TEA {
    public class TEA<Input, State, Message> : IDispatcher<Message>
        where State : IUpdate<State, Message> {
        State currentState;

        readonly IRender<Input, State, Message> render;
        bool isCallingRender = false;

        /// <summary>
        ///   実行するべきメッセージ
        /// </summary>
        readonly List<Message> messages = new List<Message>(16);

        /// <summary>
        ///   レンダリングする上限回数。
        ///   1以上でないと一回でもレンダリングすると例外が発生します。
        ///   この回数以上レンダリングすると無限ループしている可能性があるので例外を発生させます。
        /// </summary>
        int maxRendering = 10;

        public TEA(Input input,
                   Message firstMsg,
                   IRender<Input, State, Message> render,
                   State initialState) {

            this.render = render;

            currentState = initialState;
            this.render.Setup(input, this);
            Dispatch(firstMsg);
        }

        public void Dispatch(Message msg) {
            if (isCallingRender) {
                messages.Add(msg);
                return;
            }
            isCallingRender = true;
            try {
                var newState = currentState.Update(msg);
                render.Render(newState);
                // 無限ループを回避するため
                // レンダリングした回数
                for (int renderCount = 0; true; renderCount++) {
                    if (maxRendering < renderCount) {
                        throw new InvalidOperationException($"レンダリングが指定された回数以上行われました。最大回数:{maxRendering}/n現在の状態:{currentState}");
                    }
                    foreach (var a in messages) {
                        newState = newState.Update(a);
                    }
                    messages.Clear();
                    render.Render(newState);
                    // レンダー呼び出し中にdispatcherが呼ばれたかが変更されたか
                    if (messages.Count <= 0) {
                        break;
                    }
                }
                currentState = newState;
            }
            finally {
                // 例外が発生したあとでもdispatchが呼び出せるようにしておく
                isCallingRender = false;
                messages.Clear();
            }
        }
    }
}
