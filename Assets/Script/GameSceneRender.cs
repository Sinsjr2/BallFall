using System.Collections.Generic;
using System.Linq;
using TEA;
using TEA.Unity;
using UnityEngine;
using UnityEngine.Assertions;

public class GameSceneRender : MonoBehaviour, IRender<Unit, GameSceneState, IGameSceneMessage> {

    [SerializeField]
    RenderCache<BarRender, Unit, BarState, IBarMessage> barRender;

    [SerializeField]
    MonoBehaviourRenderFactory<BallRender, Unit, BallState, IBallMessage> ballRender;

    [SerializeField]
    RenderCache<UIRender, Unit, UIState, IUIMessage> uiRender;

    /// <summary>
    ///   ボールの生成するかの判定に使用します。
    /// </summary>
    [SerializeField]
    RectTransform canvasRect;

    /// <summary>
    ///   ボールの親として設定するオブジェクト
    /// </summary>
    [SerializeField]
    Transform ballRenderParent;

    /// <summary>
    ///   ボール生成位置
    /// </summary>
    [SerializeField]
    Vector2 ballInstantiatePos;

    DimensionsChangedNotification notification;

    public GameSceneState CreateState() {
        var ballInitState = ballRender.GetRender().CreateState(new Vector2(ballInstantiatePos.x, canvasRect.sizeDelta.y));
        var barInitState = barRender.GetRender().CreateState();
        return new GameSceneState {
            ballInitState = ballInitState,
            ballState = new []{ ballInitState }.ToList(),
            ballGenerator = BallGenerator.RandomPos(100, 200),
            barState = barInitState,
            barInitState = barInitState,
            canvasSize = canvasRect.sizeDelta,
            uiState = uiRender.GetRender().CreateState(),
        };
    }

    public void Setup(Unit _, IDispatcher<IGameSceneMessage> dispatcher) {
        Assert.IsNotNull(ballRenderParent);
        Assert.IsNotNull(canvasRect);
        notification = canvasRect.gameObject.AddComponent<DimensionsChangedNotification>();
        notification.AddHandler(
            () => dispatcher.Dispatch(new OnChangedCanvasSize {canvasSize = canvasRect.sizeDelta}));
        ballRender.Setup(
            (d, ballRender) => {
                ballRender.Setup(Unit.Default, d);
                ballRender.transform.SetParent(ballRenderParent, false);
                return ballRender;
            },
            dispatcher.Wrap((KeyValuePair<int, IBallMessage> indexAndMsg) =>
                              new WrapBallMessage { id = indexAndMsg.Key, message = indexAndMsg.Value}));

        barRender.GetRender().Setup(
            Unit.Default,
            dispatcher.Wrap((IBarMessage msg) => new WrapBarMessage {message = msg}));

        uiRender.GetRender().Setup(
            Unit.Default,
            dispatcher.Wrap((IUIMessage msg) => new WrapUIMessage {message = msg}));
    }

    void OnDestroy() {
        ballRender.Clear();
        notification.ClearHandler();
    }

    public void Render(GameSceneState state) {
        ballRender.Render(state.ballState);
        barRender.Render(state.barState);
        uiRender.Render(state.uiState);
    }
}

/// <summary>
///   現在の状態を表します。
/// </summary>
public enum GameState : byte {
    Ready,
    Pausing,
    Playing,
    GameOver,
}

public struct GameSceneState : IUpdate<GameSceneState, IGameSceneMessage> {

    /// <summary>x
    ///   ボールをインスタンス化するのに必要な初期状態
    /// </summary>
    public BallState ballInitState;

    /// <summary>
    ///   表示しているボール
    ///   一番最後の要素が最新の生成したボールです。
    /// </summary>
    public List<BallState> ballState;

    /// <summary>
    ///   次に表示するボタンを作ります。
    /// </summary>
    public BallGenerator? ballGenerator;

    /// <summary>
    ///   バーをインスタンス化するのに必要な初期設定
    /// </summary>
    public BarState barInitState;
    public BarState barState;

    public GameState gameState;
    public UIState uiState;

    /// <summary>
    ///   ボールの生成をするかどうかを判定するために使用するキャンバスの現在の大きさ
    /// </summary>
    public Vector2 canvasSize;

    public GameSceneState Update(IGameSceneMessage msg) {
        switch (msg) {
            case InitGame: return InitialGame(this);
            case OnInput onInput: return Update(this, onInput);
            case WrapBarMessage barMessage: return Update(this, barMessage);
            case WrapBallMessage ballMessage: return Update(this, ballMessage);
            case WrapUIMessage uiMessage: return Update(this, uiMessage);
            case OnChangedCanvasSize size: return Update(this, size);
            default: throw new PatternMatchNotFoundException(msg);
        }
    }

    GameSceneState InitialGame(GameSceneState state) {
        // 待機画面にする
        // バーとボールを初期状態にする
        state.uiState = state.uiState.Update(Singleton<ToGameReadyUI>.Instance);
        state.barState = state.barInitState;
        state.ballState = new [] {state.ballInitState}.ToList();
        state.gameState = GameState.Ready;
        return state;
    }

    /// <summary>
    ///   ゲームを実行状態にします。
    /// </summary>
    GameSceneState PlayGameUpdate(GameSceneState state) {
        state.uiState = state.uiState.Update(Singleton<ToGamePlayUI>.Instance);
        state.barState.canMove = true;
        SetMovableToAllBall(state.ballState, true);

        state.gameState = GameState.Playing;
        return state;
    }

    GameSceneState Update(GameSceneState state, OnChangedCanvasSize size) {
        Debug.Log(state.canvasSize);
        state.canvasSize = size.canvasSize;
        Debug.Log(size.canvasSize);
        return state;
    }

    public GameSceneState Update(GameSceneState state, OnInput input) {
        switch (state.gameState) {
            case GameState.Ready:
                return input.state.pushedEnterButton
                    ? PlayGameUpdate(state)
                    : state;
            case GameState.Playing:
                // 中断ボタンの方を優先する
                if (input.state.pushedEscapeButton) {
                    return PauseGame(state);
                }
                state.barState = state.barState.Update(new MoveBar (pos : input.state.barPosition));
                return state;
            case GameState.Pausing:
                // エンターで再開する
                // エスケープで終了する。
                // 終了を優先する
                if (input.state.pushedEscapeButton) {
                    StopGame();
                    // エディタモードであると１フレーム後に終了する
                    return state;
                } else if (input.state.pushedEnterButton) {
                    return PlayGameUpdate(state);
                }
                return state;
            case GameState.GameOver:
                // ゲームを終了する場合はEscキー
                // 再開する場合はEnterキー
                // 同時押しされた場合は、終了を優先する
                if (input.state.pushedEscapeButton) {
                    StopGame();
                    // エディタモードであると１フレーム後に終了する
                    return state;
                } else if (input.state.pushedEnterButton) {
                    return InitialGame(state);
                }
                return state;
            default:
                throw new PatternMatchNotFoundException(state.gameState);
        }
    }

    /// <summary>
    ///   ゲームを終了させ、アプリを停止させます。
    /// </summary>
    static void StopGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    ///   ゲームを一時停止します。
    /// </summary>
    GameSceneState PauseGame(GameSceneState state) {
        SetMovableToAllBall(state.ballState, false);
        state.barState.canMove = false;
        state.uiState = state.uiState.ToPauseUI();
        state.gameState = GameState.Pausing;
        return state;
    }

    GameSceneState Update(GameSceneState state, WrapBarMessage msg) {
        state.barState = state.barState.Update(msg.message);
        return state;
    }

    /// <summary>
    ///   最後の要素を取得します。要素がなければ例外が発生します。
    /// </summary>
    static T GetLast<T>(List<T> xs) {
        return xs[xs.Count - 1];
    }

    /// <summary>
    ///   すべてのボールに対してボールが動くことができるかどうかを設定します。
    /// </summary>
    static void SetMovableToAllBall(List<BallState> balls, bool canMove) {
        for (int i = 0; i < balls.Count; i++) {
            var ballST = balls[i];
            ballST.movesBall = canMove;
            balls[i] = ballST;
        }
    }

    GameSceneState Update(GameSceneState state, WrapBallMessage msg) {
        state.ballState[msg.id] = state.ballState[msg.id].Update(msg.message);
        switch (msg.message) {
            case OnOutOfArea:
                // ボールが画面外に出たらゲームオーバー
                // このとき、ボールとバーを動かないようにする
                state.uiState = state.uiState.Update(Singleton<ToGameOverUI>.Instance);
                SetMovableToAllBall(state.ballState, false);
                state.barState.canMove = false;
                state.gameState = GameState.GameOver;
                break;
            case OnCollisionBar:
                // ボールを削除する
                state.ballState.RemoveAt(msg.id);
                // スコアをアップさせる
                state.uiState = state.uiState.Update(Singleton<IncScore>.Instance);
                // もしこれが最後のボールであれば、表示できなくてもボールを生成する。
                if (state.ballState.Count <= 0) {
                    state = MaybeRandomGenerateBall(state);
                }
                break;
            case NextFrame:
                if (state.ballState.Count <= 0) {
                    break;
                }
                state = MaybeRandomGenerateBall(state);
                break;
            default:
                // do nothing
                break;
        }
        return state;
    }

    /// <summary>
    ///   ボールの間隔、x座標に関してランダムにボールを生成します。これは、ゲームが実行中のみ機能します。
    /// </summary>
    static GameSceneState MaybeRandomGenerateBall(GameSceneState state) {
        if (state.gameState != GameState.Playing) {
            return state;
        }

        var latestBall = GetLast(state.ballState);
        var nextPos = state.ballGenerator?.MaybeGenerate(state.canvasSize, latestBall);
        if (nextPos.HasValue) {
            state.ballGenerator = BallGenerator.RandomPos(50, 300);
            var ballXPos = state.barState.movePos.GetPos(RandomEnum<BarPosition>.GetRandom()).x;
            // ボールを生成する(yの相対位置、x方向はランダム)
            var ballPos = new Vector2(ballXPos, latestBall.position.y + nextPos.Value);
            var newBall = state.ballInitState;
            newBall.movesBall = true;
            newBall.position = ballPos;

            state.ballState.Add(newBall);
        }
        return state;
    }

    GameSceneState Update(GameSceneState state, WrapUIMessage msg) {
        state.uiState = state.uiState.Update(msg.message);
        return state;
    }

}

/// <summary>
///   ボールが見えるようになると生成します。
/// </summary>
public struct BallGenerator {

    /// <summary>
    ///   次に生成するボール(前のボールからの相対的な距離)
    /// </summary>
    public readonly float nextBallrelativeYPos;

    /// <summary>
    ///   生成予定座標(前ボールとの相対座標)からオブジェクトを生成します。
    /// </summary>
    public BallGenerator(float ballPos) {
        nextBallrelativeYPos = ballPos;
    }

    /// <summary>
    ///   画面に入る位置であれば、ボールのy座標を生成します。
    /// </summary>
    public float? MaybeGenerate(Vector2 canvasSize, BallState ballState) {
        if (ballState.position.y + nextBallrelativeYPos <= canvasSize.y) {
            // 表示範囲に入った時
            return nextBallrelativeYPos;
        }
        return null;
    }

    /// <summary>
    ///   ランダムなy位置でボールを生成します。
    /// </summary>
    public static BallGenerator RandomPos(float min, float max) {
        Assert.IsTrue(min <= max);
        return new BallGenerator(UnityEngine.Random.Range(min, max));
    }
}

public interface IGameSceneMessage {}

public class InitGame : IGameSceneMessage {}

/// <summary>
///   ゲームを開始します。
/// </summary>
public class WrapBarMessage : IGameSceneMessage {
    public IBarMessage message;
}

public class WrapBallMessage : IGameSceneMessage {
    public int id;
    public IBallMessage message;
}

/// <summary>
///   キャンバスのサイズが変化したことを通知します。
/// </summary>
public class OnChangedCanvasSize : IGameSceneMessage {
    public Vector2 canvasSize;
}

/// <summary>
///   キー入力があった時
/// </summary>
public class OnInput : IGameSceneMessage {
    public InputState state;
}

public class WrapUIMessage : IGameSceneMessage {
    public IUIMessage message;
}
