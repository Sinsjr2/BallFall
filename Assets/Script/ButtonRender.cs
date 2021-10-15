using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class ButtonRender<Message> : MonoBehaviour {

    Button btn;
    UnityEventRender<Message> onClick;

    void Awake() {
        btn = GetComponent<Button>();
        onClick = new UnityEventRender<Message>(btn.onClick);
    }

    void OnDestroy() {
        onClick.Dispose();
    }

    public void OnClick<T>(IDispatcher<Message> dispatch, T msg) where T : struct, Message {
        onClick.Render(dispatch, msg);
    }
}

