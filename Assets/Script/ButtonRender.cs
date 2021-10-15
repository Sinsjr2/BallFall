using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class ButtonRender<Action> : MonoBehaviour {

    Button btn;
    UnityEventRender<Action> onClick;

    void Awake() {
        btn = GetComponent<Button>();
        onClick = new UnityEventRender<Action>(btn.onClick);
    }

    void OnDestroy() {
        onClick.Dispose();
    }

    public void OnClick<T>(IDispatcher<Action> dispatch, T msg) where T : struct, Action {
        onClick.Render(dispatch, msg);
    }
}

