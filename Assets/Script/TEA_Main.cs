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
    IDispacher<Action> dispach;

    readonly UnityEvent unityEvent;

    public UnityEventRender(UnityEvent unityEvent) {
        this.unityEvent = unityEvent;
    }

    public void Dispose() {
        if (!ReferenceEquals(action, null)) {
            unityEvent.RemoveListener(action);
        }
    }

    void Listner<T>() where T : struct, Action {
        var dispach = this.dispach;
        dispach.Dispach(((Box<T>)value).value);
    }

    public void Render<T>(IDispacher<Action> dispach, T msg) where T : struct, Action {
        this.dispach = dispach;
        var v = value as Box<T>;
        // アロケーションを避けるために同じオブジェクトを使いまわす
        if (ReferenceEquals(v, null)) {
            v = new Box<T>(msg);
        } else {
            v.value = msg;
        }
        // このクラス内でしか、コールバックの登録を追加したり、外したりすることを想定していない。
        // よって、一度だけ登録するようにする
        if (ReferenceEquals(action, null)) {
            action = Listner<T>;
            unityEvent.AddListener(action);
        }
    }
}
