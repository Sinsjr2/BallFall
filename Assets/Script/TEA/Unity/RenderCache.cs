using System;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
///   前回書き込んだ状態と比較して、異なっていればRenderを呼び出すようにします。
/// </summary>
[Serializable]
public struct RenderCache<T, Input, State, Act> : IRender<Input, State, Act>
    where T : class, IRender<Input, State, Act>
    where State : IEquatable<State> {

    [SerializeField]
    T render;

    /// <summary>
    ///   前回Renderに渡した値
    /// </summary>
    State prevState;

    public T GetRender() {
        Assert.IsNotNull(render);
        return render;
    }

    public void Setup(Input input, IDispatcher<Act> dispatcher) {
        Assert.IsNotNull(render);
        render.Setup(input, dispatcher);
    }

    public void Render(State state) {
        if (prevState.Equals(state)) {
            return;
        }
        prevState = state;
        render.Render(state);
    }
}
