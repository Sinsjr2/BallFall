using System;
using System.Collections.Generic;

public class TEA<Input, State, Message> : IDispatcher<Message> {
    State currentState;

    readonly IRender<Input, State, Message> render;
    readonly IUpdate<State, Message> update;
    bool isCallingRender = false;

    /// <summary>
    ///   実行するべきアクション
    /// </summary>
    readonly List<Message> messages = new List<Message>(16);

    /// <summary>
    ///   レンダリングする上限回数。
    ///   1以上でないと一回でもレンダリングすると例外が発生します。
    ///   この回数以上レンダリングすると無限ループしている可能性があるので例外を発生させます。
    /// </summary>
    int maxRendering = 10;

    public TEA(Input initial,
               Message firstMsg,
               IRender<Input, State, Message> render,
               StateInitializer<Input, State> initializer,
               IUpdate<State, Message> update) {

        this.render = render;
        this.update = update;

        currentState = initializer.CreateState(initial);
        this.render.Setup(initial, this);
        Dispatch(firstMsg);
    }

    public void Dispatch(Message msg) {
        if (isCallingRender) {
            messages.Add(msg);
            return;
        }
        isCallingRender = true;
        try {
            var newState = update.Update(currentState, msg);
            render.Render(newState);
            // 無限ループを回避するため
            // レンダリングした回数
            for (int renderCount = 0; true; renderCount++) {
                if (maxRendering < renderCount) {
                    throw new InvalidOperationException($"レンダリングが指定された回数以上行われました。最大回数:{maxRendering}/n現在の状態:{currentState}");
                }
                foreach (var a in messages) {
                    newState = update.Update(newState, a);
                }
                messages.Clear();
                render.Render(newState);
                // レンダー呼び出し中にdispatcherが呼ばれたかが変更されたか
                if (messages.Count <= 0) {
                    break;
                }
            }
            currentState = newState;
        } finally {
            // 例外が発生したあとでもdispatchが呼び出せるようにしておく
            isCallingRender = false;
            messages.Clear();
        }
    }
}
