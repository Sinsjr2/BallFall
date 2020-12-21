using System;
using UnityEngine;
using UnityEngine.Events;


public class TEA_Main : MonoBehaviour {
    class MyUnionButtonRender : ButtonRender<IMyUnion> {}
    public void TestButton(){

        // new AOTProblemExample().Error();;
        // return;

        // Resolver<MyState> resolver = new MyUnionUpdate();
        // Debug.Log(resolver.GetInstance<A>());

        // GenericInterface a = new Imple1Interface();
        // Debug.Log(a.GetInstance<int>().value);
        // a.DoSomething("あいうえお");
        // return;

        // Profiler.BeginSample("AllocCheck: Creation");
        // var component = new TEA<MyState, IMyUnion, MyUnionUpdate>(new MyUnionUpdate());
        // Profiler.EndSample();

        // var go = new GameObject().AddComponent<MyUnionButtonRender>();

        // Profiler.BeginSample("AllocCheck: Dispach");
        // for(int i = 0; i < 1000; i++) {
        //     IDispacher<IMyUnion> o = component;
        //     var b = new B {value = 100.0f};
        //     go.OnClick(o, b);
        //     Profiler.EndSample();
        //     IDispacher<IMyUnion> c = component;
        //     c.Dispach(new A { hoge = 9 });
        //     c.Dispach(new B { value = 4.7f });
        // }

        // Debug.Log(component.currentState.MyToString());
    }

    void Dummy() {
        // new MyUnionUpdate().GetInstance<A>();
        // new MyUnionUpdate().GetInstance<B>();
    //             var component = new TEA<MyState, IMyUnion, MyUnionUpdate>(new MyUnionUpdate());
    //             component.Dispach(new A());
    }
}


public class Box<T> {
    public T value;

    public Box(T value) {
        this.value = value;
    }
}

public class ColliderRender : MonoBehaviour {

    object dispach = null;

    public void OnCollisionEnterAction<Msg, Initial, T>(IDispacher<Msg> dispach,
                                                        Initial initial,
                                                        Func<Initial, Collider, T> createMsg) where T : Msg {
        this.dispach = dispach;
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
