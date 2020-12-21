using System;
using UnityEngine;
using UnityEngine.Assertions;

public class GameSceneRender : MonoBehaviour, IRender<Unit, GameSceneState, IGameSceneAction> {

    [SerializeField]
    BarRender barRender;

    [SerializeField]
    BallRender ballRender;

    [SerializeField]
    UIRender uiRender;

    /// <summary>
    ///   ボール生成位置
    /// </summary>
    [SerializeField]
    Vector2 ballInstantiatePos;

    public GameSceneState CreateState(Unit initial) {
        Assert.IsNotNull(barRender);
        Assert.IsNotNull(ballRender);
        Assert.IsNotNull(uiRender);
        var ballInitState = ballRender.CreateState(ballInstantiatePos);
        var barInitState = barRender.CreateState(Unit.Default);
        return new GameSceneState {
            ballInitState = ballInitState,
            ballState = ballRender.CreateState(ballInstantiatePos),
            barState = barInitState,
            barInitState = barInitState,
            uiState = uiRender.CreateState(Unit.Default),
        };
    }

    public void Setup(GameSceneState state, IDispacher<IGameSceneAction> dispacher) {
        ballRender.Setup(
            state.ballState,
            new ActionWrapper<IGameSceneAction, IBallAction>(
                dispacher, (d, act) => d.Dispach(new WrapBallAction {action = act})));

        barRender.Setup(
            state.barState,
            new ActionWrapper<IGameSceneAction, IBarAction>(
                dispacher, (d, act) => d.Dispach(new WrapBarAction {action = act})));

        uiRender.Setup(
            state.uiState,
            new ActionWrapper<IGameSceneAction, IUIAction>(
                dispacher, (d, act) => d.Dispach(new WrapUIAction {action = act})));
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

public struct GameSceneState : IEquatable<GameSceneState> {

    /// <summary>
    ///   ボールをインスタンス化するのに必要な初期状態
    /// </summary>
    public BallState ballInitState;
    public BallState ballState;

    /// <summary>
    ///   バーをインスタンス化するのに必要な初期設定
    /// </summary>
    public BarState barInitState;
    public BarState barState;

    public GameState gameState;
    public UIState uiState;

    public bool Equals(GameSceneState other) {
        return ballInitState.Equals(other.ballInitState) &&
            ballState.Equals(other.ballState) &&
            barInitState.Equals(other.barInitState) &&
            gameState == other.gameState &&
            barState.Equals(other.barState) &&
            uiState.Equals(other.uiState);
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
            default:
                throw new PatternMatchNotFoundException(msg);
        }
    }

    GameSceneState InitGame(GameSceneState state) {
        // 待機画面にする
        // バーとボールを初期状態にする
        state.uiState = uiUpdate.Update(state.uiState, Singleton<ToGameReadyUI>.Instance);
        state.barState = state.barInitState;
        state.ballState = state.ballInitState;
        state.gameState = GameState.Ready;
        return state;
    }

    /// <summary>
    ///   ゲームを実行状態にします。
    /// </summary>
    GameSceneState PlayGameUpdate(GameSceneState state) {
        state.uiState = uiUpdate.Update(state.uiState, Singleton<ToGamePlayUI>.Instance);
        state.barState.canMove = true;
        state.ballState.movesBall = true;
        state.gameState = GameState.Playing;
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
        state.ballState.movesBall = false;
        state.barState.canMove = false;
        state.uiState = uiUpdate.ToPauseUI(state.uiState);
        state.gameState = GameState.Pausing;
        return state;
    }

    GameSceneState Update(GameSceneState state, WrapBarAction act) {
        state.barState = barUpdate.Update(state.barState, act.action);
        return state;
    }

    GameSceneState Update(GameSceneState state, WrapBallAction msg) {
        switch (msg.action) {
            case OnOutOfArea _:
                // ボールが画面外に出たらゲームオーバー
                // このとき、ボールとバーを動かないようにする
                state.uiState = uiUpdate.Update(state.uiState, Singleton<ToGameOverUI>.Instance);
                state.ballState.movesBall = false;
                state.barState.canMove = false;
                state.gameState = GameState.GameOver;
                break;
            case OnCollisionBar colBar:
                // ボールを一番上に移動させる(x方向はランダム)
                // スコアをアップさせる
                var ballXPos = state.barState.movePos.GetPos(RandomEnum<BarPosition>.GetRandom()).x;
                var ballPos = new Vector2(ballXPos, state.ballInitState.position.y);
                state.ballState.position = ballPos;
                state.uiState = uiUpdate.Update(state.uiState, Singleton<IncScore>.Instance);
                break;
            default:
                // do nothing
                break;
        }
        state.ballState = ballUpdate.Update(state.ballState, msg.action);
        return state;
    }

    GameSceneState Update(GameSceneState state, WrapUIAction msg) {
        state.uiState = uiUpdate.Update(state.uiState, msg.action);
        return state;
    }
}
