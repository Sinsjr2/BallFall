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
        var buffer = new BufferDispatcher<IGameSceneMessage>();
        gameSceneRender.Setup(Unit.Default, buffer);
        var inputSubscription = new GameObject(nameof(InputSubscription)).AddComponent<InputSubscription>();
        var tea = new TEA<GameSceneState, IGameSceneMessage>(
            gameSceneRender.CreateState(),
            gameSceneRender);
        buffer.SetDispatcher(tea);

        inputSubscription.dispatcher = tea.Wrap((ChangedInput msg) => new OnInput{ state = msg.state });
    }
}
