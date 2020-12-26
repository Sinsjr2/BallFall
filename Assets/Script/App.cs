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
        var tea = new TEA<Unit, GameSceneState, IGameSceneAction>(
            Unit.Default,
            Singleton<InitGame>.Instance,
            gameSceneRender,
            gameSceneRender,
            new GameSceneUpdate());

        inputSubscription.dispacher = new ActionWrapper<IGameSceneAction, ChangedInput>(tea, (dispacher, act) => {
            dispacher.Dispach(new OnInput{ state = act.state });
        });
    }
}
