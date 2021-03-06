using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class GameSceneRender : MonoBehaviour, IRender<Unit, GameSceneState, IGameSceneAction>, StateInitializer<Unit, GameSceneState> {

    [SerializeField]
    RenderCache<BarRender, Unit, BarState, IBarAction> barRender;

    [SerializeField]
    MonoBehaviourRenderFactory<BallRender, Unit, BallState, IBallAction> ballRender;

    [SerializeField]
    RenderCache<UIRender, Unit, UIState, IUIAction> uiRender;

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

    IDispacher<IGameSceneAction> dispacher;

    public GameSceneState CreateState(Unit initial) {
        var ballInitState = ballRender.GetRender().CreateState(new Vector2(ballInstantiatePos.x, canvasRect.sizeDelta.y));
        var barInitState = barRender.GetRender().CreateState(Unit.Default);
        return new GameSceneState {
            ballInitState = ballInitState,
            ballState = new []{ ballInitState }.ToList(),
            ballGenerator = BallGenerator.RandomPos(100, 200),
            barState = barInitState,
            barInitState = barInitState,
            canvasSize = canvasRect.sizeDelta,
            uiState = uiRender.GetRender().CreateState(Unit.Default),
        };
    }

    public void Setup(Unit _, IDispacher<IGameSceneAction> dispacher) {
        Assert.IsNotNull(ballRenderParent);
        Assert.IsNotNull(canvasRect);
        this.dispacher = dispacher;
        notification = canvasRect.gameObject.AddComponent<DimensionsChangedNotification>();
        notification.AddHandler(
            () => dispacher.Dispach(new OnChangedCanvasSize {canvasSize = canvasRect.sizeDelta}));
        ballRender.Setup(
            (d, ballRender) => {
                ballRender.Setup(Unit.Default, d);
                ballRender.transform.SetParent(ballRenderParent, false);
                return ballRender;
            },
            dispacher.Wrap<IGameSceneAction, KeyValuePair<int, IBallAction>>(
                (d, indexAndAct) => d.Dispach(new WrapBallAction {
                        id = indexAndAct.Key, action = indexAndAct.Value})));

        barRender.Setup(
            Unit.Default,
            dispacher.Wrap<IGameSceneAction, IBarAction>(
                (d, act) => d.Dispach(new WrapBarAction {action = act})));

        uiRender.Setup(
            Unit.Default,
            dispacher.Wrap<IGameSceneAction, IUIAction>(
                (d, act) => d.Dispach(new WrapUIAction {action = act})));
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

public struct GameSceneState {

    /// <summary>
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

public interface IGameSceneAction {}

public class InitGame : IGameSceneAction {}

/// <summary>
///   ゲームを開始します。
/// </summary>
public class WrapBarAction : IGameSceneAction {
    public IBarAction action;
}

public class WrapBallAction : IGameSceneAction {
    public int id;
    public IBallAction action;
}

/// <summary>
///   キャンバスのサイズが変化したことを通知します。
/// </summary>
public class OnChangedCanvasSize : IGameSceneAction {
    public Vector2 canvasSize;
}

/// <summary>
///   キー入力があった時
/// </summary>
public class OnInput : IGameSceneAction {
    public InputState state;
}

public class WrapUIAction : IGameSceneAction {
    public IUIAction action;
}

public class GameSceneUpdate : IUpdate<GameSceneState, IGameSceneAction> {

    readonly BallUpdate ballUpdate = new BallUpdate();
    readonly BarUpdate barUpdate = new BarUpdate();
    readonly UIUpdate uiUpdate = new UIUpdate();

    public GameSceneState Update(GameSceneState state, IGameSceneAction msg) {
        switch (msg) {
            case InitGame initGame:
                return InitGame(state);
            case OnInput onInput:
                return Update(state, onInput);
            case WrapBarAction barAction:
                return Update(state, barAction);
            case WrapBallAction ballAction:
                return Update(state, ballAction);
            case WrapUIAction uiAction:
                return Update(state, uiAction);
            case OnChangedCanvasSize size:
                return Update(state, size);
            default:
                throw new PatternMatchNotFoundException(msg);
        }
    }

    GameSceneState InitGame(GameSceneState state) {
        // 待機画面にする
        // バーとボールを初期状態にする
        state.uiState = uiUpdate.Update(state.uiState, Singleton<ToGameReadyUI>.Instance);
        state.barState = state.barInitState;
        state.ballState = new [] {state.ballInitState}.ToList();
        state.gameState = GameState.Ready;
        return state;
    }

    /// <summary>
    ///   ゲームを実行状態にします。
    /// </summary>
    GameSceneState PlayGameUpdate(GameSceneState state) {
        state.uiState = uiUpdate.Update(state.uiState, Singleton<ToGamePlayUI>.Instance);
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
                state.barState = barUpdate.Update(state.barState, new MoveBar (pos : input.state.barPosition));
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
                    return InitGame(state);
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
        state.uiState = uiUpdate.ToPauseUI(state.uiState);
        state.gameState = GameState.Pausing;
        return state;
    }

    GameSceneState Update(GameSceneState state, WrapBarAction act) {
        state.barState = barUpdate.Update(state.barState, act.action);
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

    GameSceneState Update(GameSceneState state, WrapBallAction msg) {
        state.ballState[msg.id] = ballUpdate.Update(state.ballState[msg.id], msg.action);
        switch (msg.action) {
            case OnOutOfArea _:
                // ボールが画面外に出たらゲームオーバー
                // このとき、ボールとバーを動かないようにする
                state.uiState = uiUpdate.Update(state.uiState, Singleton<ToGameOverUI>.Instance);
                SetMovableToAllBall(state.ballState, false);
                state.barState.canMove = false;
                state.gameState = GameState.GameOver;
                break;
            case OnCollisionBar colBar:
                // ボールを削除する
                state.ballState.RemoveAt(msg.id);
                // スコアをアップさせる
                state.uiState = uiUpdate.Update(state.uiState, Singleton<IncScore>.Instance);
                // もしこれが最後のボールであれば、表示できなくてもボールを生成する。
                if (state.ballState.Count <= 0) {
                    state = MaybeRandomGenerateBall(state);
                }
                break;
            case NextFrame _:
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

    GameSceneState Update(GameSceneState state, WrapUIAction msg) {
        state.uiState = uiUpdate.Update(state.uiState, msg.action);
        return state;
    }
}
