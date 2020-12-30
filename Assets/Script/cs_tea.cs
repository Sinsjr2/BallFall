using System;
using System.Collections.Generic;

public interface Resolver<State> {
    IUpdate<State, Message> GetInstance<Message>();
}

public interface IUpdate<State, Act> {
    State Update(State state, Act act);
}

public class Singleton<T> where T : new() {
    public static T Instance = new T();
}

public interface StateInitializer <Input, State> {
    /// <summary>
    ///   状態の作成を行います。
    ///   Unityであるとインスペクターから設定した値を初期値とする場合が多いのでこのようにしています。
    /// </summary>
    State CreateState(Input initial);
}

public interface IRender<Input, State, Act> {

    /// <summary>
    ///   レンダの初期化を行います。
    /// </summary>
    void Setup(Input input, IDispacher<Act> dispacher);

    /// <summary>
    ///   stateの状態に従い描画を行います。
    ///   また、ボタンなどのコールバックが呼ばれた際のdispacherの設定も行います。
    ///   dispacherを呼び出すとこのメソッドを呼び出します。
    ///   よって、このメソッド内でdispacherを呼び出す際はstateの状態によってdispacherを呼び出さないようにする必要があります。
    /// </summary>
    void Render(State state);
}


/// <summary>
///   何も行いません。
/// </summary>
public class NoneUpdate<State, Msg> : IUpdate<State, Msg> {
    public static readonly NoneUpdate<State, Msg> Instance = new NoneUpdate<State, Msg>();
    private NoneUpdate() {}
    public State Update(State state, Msg msg) {
        return state;
    }
}

public enum Unit {
    Default
}

public class TEA<Input, State, Act> : IDispacher<Act> {
    State currentState;

    readonly IRender<Input, State, Act> render;
    readonly IUpdate<State, Act> update;
    bool isCallingRender = false;

    /// <summary>
    ///   実行するべきアクション
    /// </summary>
    readonly List<Act> actions = new List<Act>(16);

    /// <summary>
    ///   レンダリングする上限回数。
    ///   1以上でないと一回でもレンダリングすると例外が発生します。
    ///   この回数以上レンダリングすると無限ループしている可能性があるので例外を発生させます。
    /// </summary>
    int maxRendering = 10;

    public TEA(Input initial,
               Act firstAct,
               IRender<Input, State, Act> render,
               StateInitializer<Input, State> initializer,
               IUpdate<State, Act> update) {

        this.render = render;
        this.update = update;

        currentState = initializer.CreateState(initial);
        this.render.Setup(initial, this);
        Dispach(firstAct);
    }

    public void Dispach(Act msg) {
        if (isCallingRender) {
            actions.Add(msg);
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
                foreach (var a in actions) {
                    newState = update.Update(newState, a);
                }
                actions.Clear();
                render.Render(newState);
                // レンダー呼び出し中にdispacherが呼ばれたかが変更されたか
                if (actions.Count <= 0) {
                    break;
                }
            }
            currentState = newState;
        } finally {
            // 例外が発生したあとでもdispachが呼び出せるようにしておく
            isCallingRender = false;
            actions.Clear();
        }
    }
}


/// <summary>
///   Switch case で見つからなかった場合
/// </summary>
public class PatternMatchNotFoundException : Exception {

    public PatternMatchNotFoundException(object notFoundValue)
        : base ($"{notFoundValue}はcaseに存在しません"){}
}


public interface IDispacher<Act> {
    void Dispach(Act act);
}

public class ActionWrapper<Input, Before, After> : IDispacher<After> {

    readonly IDispacher<Before> dispacher;
    readonly Action<IDispacher<Before>, Input, After> dispach;
    public Input value {set; private get;}

    public ActionWrapper(IDispacher<Before> dispacher, Action<IDispacher<Before>, Input, After> dispach) {
        this.dispacher = dispacher;
        this.dispach = dispach;
    }

    public void Dispach(After act) {
        dispach(dispacher, value, act);
    }
}

public class ActionWrapper<Before, After> : IDispacher<After> {

    readonly IDispacher<Before> dispacher;
    readonly Action<IDispacher<Before>, After> dispach;

    public ActionWrapper(IDispacher<Before> dispacher, Action<IDispacher<Before>, After> dispach) {
        this.dispacher = dispacher;
        this.dispach = dispach;
    }

    public void Dispach(After act) {
        dispach(dispacher, act);
    }
}

public static class ActionWrapper {
    public static ActionWrapper<Before, After> Create<Before, After>(
        IDispacher<Before> dispacher,
        Action<IDispacher<Before>, After> dispach) {
        return new ActionWrapper<Before, After>(dispacher, dispach);
    }

    public static ActionWrapper<Input, Before, After> Create<Input, Before, After>(
        IDispacher<Before> dispacher,
        Action<IDispacher<Before>, Input, After> dispach) {
        return new ActionWrapper<Input, Before, After>(dispacher, dispach);
    }

}

