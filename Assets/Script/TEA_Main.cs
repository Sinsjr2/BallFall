using System;
using UnityEngine.Events;

public class Box<T> {
    public T value;

    public Box(T value) {
        this.value = value;
    }
}

public class UnityEventRender<Action> : IDisposable {
    object value = null;
    UnityAction action = null;
    IDispatcher<Action> dispatch;

    readonly UnityEvent unityEvent;

    public UnityEventRender(UnityEvent unityEvent) {
        this.unityEvent = unityEvent;
    }

    public void Dispose() {
        if (!(action is null)) {
            unityEvent.RemoveListener(action);
        }
    }

    void Listner<T>() where T : struct, Action {
        var dispatch = this.dispatch;
        dispatch.Dispatch(((Box<T>)value).value);
    }

    public void Render<T>(IDispatcher<Action> dispatch, T msg) where T : struct, Action {
        this.dispatch = dispatch;
        var v = value as Box<T>;
        // アロケーションを避けるために同じオブジェクトを使いまわす
        if (v is null) {
            v = new Box<T>(msg);
        } else {
            v.value = msg;
        }
        // このクラス内でしか、コールバックの登録を追加したり、外したりすることを想定していない。
        // よって、一度だけ登録するようにする
        if (action is null) {
            action = Listner<T>;
            unityEvent.AddListener(action);
        }
    }
}
