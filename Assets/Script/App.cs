using TEA;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
///   メイン関数的な役割を果たします。
/// </summary>
public class App : MonoBehaviour {

    [SerializeField]
    GameSceneRender gameSceneRender;

    void Start() {
        Assert.IsNotNull(gameSceneRender);
        var inputSubscription = new GameObject(nameof(InputSubscription)).AddComponent<InputSubscription>();
        var tea = new TEA<Unit, GameSceneState, IGameSceneMessage>(
            Unit.Default,
            Singleton<InitGame>.Instance,
            gameSceneRender,
            gameSceneRender);

        inputSubscription.dispatcher = tea.Wrap<IGameSceneMessage, ChangedInput>((dispatcher, msg) => {
            dispatcher.Dispatch(new OnInput{ state = msg.state });
        });
    }
}
