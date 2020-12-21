using System;

public interface Resolver<State> {
    IUpdate<State, Message> GetInstance<Message>();
}

public interface IUpdate<State, Act> {
    State Update(State state, Act act);
}

public class Singleton<T> where T : new() {
    public static T Instance = new T();
}


public interface IRender<Input, State, Act> {
    /// <summary>
    ///   状態の作成を行います。
    ///   Unityであるとインスペクターから設定した値を初期値とする場合が多いのでこのようにしています。
    /// </summary>
    State CreateState(Input initial);

    /// <summary>
    ///   レンダの初期化を行います。
    /// </summary>
    void Setup(State state, IDispacher<Act> dispacher);

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

// TEA で処理する部分
/*
public class TEA<State, Message, Resolver, U>: IDispacher<Message> where Resolver : Resolver<State> {
    public State currentState => d.currentState;
    // Resolver updateResolver;
    IRender<State, Message, Unit> render;

    InnerDispacher<State, Message> d;

    /// <summary>
    ///   レンダーを呼び出し中かどうか
    /// </summary>
    bool isCallingRender = false;

    public TEA(Resolver r) {
        d = new InnerDispacher<State, Message> {updateResolver = r};
        // updateResolver = r;
    }


 /// <summary>
    ///   ジェネリックにしているのはボックス化を避けるため
    /// </summary>
    public void Dispach<T>(T msg) where T : struct, Message {
        d.Dispach(msg);
        // var prevState = currentState;
        // currentState = updateResolver.GetInstance<T>().Update(prevState, msg);
        // if (!isCallingRender) {
        //     isCallingRender = true;
        //     // render.Render(currentState, this);

        //     isCallingRender = false;
        // }
    }
}*/

public class TEA<Input, State, Act> : IDispacher<Act> where  State : IEquatable<State> {
    State currentState;

    IRender<Input, State, Act> render;
    IUpdate<State, Act> update;
    bool isCallingRender = false;

    /// <summary>
    ///   dispachが２回以上呼ばれたか
    /// </summary>
    bool isCalledMore2 = false;

    /// <summary>
    ///   レンダリングする上限回数。
    ///   1以上でないと一回でもレンダリングすると例外が発生します。
    ///   この回数以上レンダリングすると無限ループしている可能性があるので例外を発生させます。
    /// </summary>
    int maxRendering = 10;

    public TEA(Input initial,
               Act firstAct,
               IRender<Input, State, Act> render,
               IUpdate<State, Act> update) {

        this.render = render;
        this.update = update;

        currentState = this.render.CreateState(initial);
        this.render.Setup(currentState, this);
        Dispach(firstAct);
    }

    public void Dispach(Act msg) {
        var newState = update.Update(currentState, msg);
        if (isCallingRender) {
            currentState = newState;
            isCalledMore2 = true;
            return;
        }
        bool noRendering = newState.Equals(currentState);
        // レンダリングに使用しないだけで変更されているフィールドがあるかもしれないので更新する
        currentState = newState;
        if (noRendering) {
            return;
        }
        isCallingRender = true;

        try {
            // 無限ループを回避するため
            // レンダリングした回数
            for (int renderCount = 0; true; renderCount++) {
                if (maxRendering < renderCount) {
                    throw new InvalidOperationException($"レンダリングが指定された回数以上行われました。最大回数:{maxRendering}/n現在の状態:{currentState}");
                }
                render.Render(newState);
                // レンダー呼び出し中にStateが変更されたか
                bool isChanged = isCalledMore2 && !newState.Equals(currentState);
                if (!isChanged) {
                    break;
                }
                newState = currentState;
            }
        } finally {
            // 例外が発生したあとでもdispachが呼び出せるようにしておく
            isCallingRender = false;
            isCalledMore2 = false;
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

class InnerDispacher<State, Message> {

    public State currentState;
    public Resolver<State> updateResolver;
    bool isCallingRender = false;

    public void Dispach<T>(T msg) where T : struct, Message {
        var prevState = currentState;
        currentState = updateResolver.GetInstance<T>().Update(prevState, msg);
        if (!isCallingRender) {
            isCallingRender = true;
            // render.Render(currentState, this);

            isCallingRender = false;
        }
    }
}

interface DispachResolver {
    Dispacher<Message> GetInstance<Message>();
}

class Dispacher<Message> {

    DispachResolver resolver;

    public void Dispach<T>(T msg) where T : Message {
        resolver.GetInstance<Message>().Dispach(msg);;
    }
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

/*
public interface ActionWrapper<After> {

    AfterT Map<Before, AfterT>(Before action) where AfterT : struct, After;
}
*/


// ↑↑↑↑↑↑ TEA ↑↑↑↑↑↑↑

public interface IMyUnion {}

struct A : IMyUnion {
    public int hoge;
}

struct B : IMyUnion {
    public float value;
}
public class HogeD {

    public WrapMyUnion<T> foo<T>(T msg) where T : IMyUnion {
        return new WrapMyUnion<T> { inner = msg };
    }
}

public struct WrapMyUnion<Inner> : IMyUnion where Inner : IMyUnion {
    public Inner inner;
}

struct MyState {
    public int sum;
    public float floatSum;

    public string MyToString() {
        return $"int Sum: {sum}, float Sum {floatSum}";
    }
}


class MyUnionUpdate : Resolver<MyState>, IUpdate<MyState, A>, IUpdate<MyState, B>  {
    public IUpdate<MyState, Message> GetInstance<Message>() {
        return (IUpdate<MyState, Message>)this;
    }

    public MyState Update(MyState state, A msg) {
        state.sum += msg.hoge;
        return state;
    }

    public MyState Update(MyState state, B msg) {
        state.floatSum -= msg.value;
        return state;
    }
}

