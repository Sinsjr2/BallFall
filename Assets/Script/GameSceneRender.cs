using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public struct Hoge<T> {
    public int hoge;
    public GameObject obj;
    public T value;
}

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

    public void Setup(Input input, IDispacher<Act> dispacher) {
        Assert.IsNotNull(render);
        render.Setup(input, dispacher);
    }

    public void Render(State state) {
        if (prevState.Equals(state)) {
            return;
        }
        prevState = state;
        render.Render(state);
    }
}

/// <summary>
///   複数値があるオブジェクトをrenderに渡す際に使用します。
///   このとき、Setupはインスタンス化した時に一度だけ呼び出します。
///   Renderの前に毎回呼び出すわけではないので注意してください。
/// </summary>
[Serializable]
public struct MonoBehaviourRenderFactory<T, Input, State, Act>
    where T : MonoBehaviour, IRender<Input, State, Act> {

    /// <summary>
    ///   必ず設定する必要があります。
    /// </summary>
    [SerializeField]
    [Tooltip("複数生成するレンダー")]
    T render;

    /// <summary>
    ///   今までに作成されたrenderのキャッシュ
    /// </summary>
    List<T> cachedRender;

    IDispacher<Act> dispacher;

    Func<IDispacher<Act>, T, T> initializer;

    public T GetRender() {
        Assert.IsNotNull(render);
        return render;
    }

    public void Setup(Func<IDispacher<Act>, T, T> initializer, IDispacher<Act> dispacher) {
        Assert.IsNotNull(render);
        this.initializer = initializer;
        cachedRender = new List<T>();
        this.dispacher = dispacher;
    }

    /// <summary>
    ///   引数の配列は先頭から順番にrenderに渡していきます。
    /// </summary>
    public void Render(List<State> state) {
        using (var e = state.GetEnumerator()) {
            // キャッシュからrenderを呼び出す
            foreach (var r in cachedRender) {
                // ステートがあるうちはrenderに渡す
                if (e.MoveNext()) {
                    r.gameObject.SetActive(true);
                    r.Render(e.Current);
                } else {
                    // 不要な分はgameobjectをNonActiveにすることで持っておく
                    // １つでもdisableなオブジェクトを見つけるとあとはすべてdisableになっていると仮定する
                    var go = r.gameObject;
                    if (!go.activeSelf) {
                        break;
                    }
                    go.SetActive(false);
                }
            }
            // 足りない分をインスタンス化する
            while (e.MoveNext()) {
                var go = GameObject.Instantiate(render);
                cachedRender.Add(go);
                go = initializer(dispacher, go);
                go.Render(e.Current);
            }
        }
    }

    /// <summary>
    ///   キャッシュを消します。
    /// </summary>
    public void Clear() {
        foreach(var r in cachedRender) {
            GameObject.Destroy(r.gameObject);
        }
        cachedRender.Clear();
    }
}

public class GameSceneRender : MonoBehaviour, IRender<Unit, GameSceneState, IGameSceneAction>, StateInitializer<Unit, GameSceneState> {

    [SerializeField]
    RenderCache<BarRender, Unit, BarState, IBarAction> barRender;

    [SerializeField]
    MonoBehaviourRenderFactory<BallRender, Unit, BallState, IBallAction> ballRender;

    [SerializeField]
    RenderCache<UIRender, Unit, UIState, IUIAction> uiRender;

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

    public GameSceneState CreateState(Unit initial) {
        var ballInitState = ballRender.GetRender().CreateState(ballInstantiatePos);
        var barInitState = barRender.GetRender().CreateState(Unit.Default);
        return new GameSceneState {
            ballInitState = ballInitState,
            ballState = new []{ ballRender.GetRender().CreateState(ballInstantiatePos) }.ToList(),
            barState = barInitState,
            barInitState = barInitState,
            uiState = uiRender.GetRender().CreateState(Unit.Default),
        };
    }

    public void Setup(Unit _, IDispacher<IGameSceneAction> dispacher) {
        Assert.IsNotNull(ballRenderParent);
        ballRender.Setup(
            (d, ballRender) => {
                ballRender.Setup(Unit.Default, d);
                ballRender.transform.SetParent(ballRenderParent, false);
                return ballRender;
            },
            new ActionWrapper<IGameSceneAction, IBallAction>(
                dispacher, (d, act) => d.Dispach(new WrapBallAction {action = act})));

        barRender.Setup(
            Unit.Default,
            new ActionWrapper<IGameSceneAction, IBarAction>(
                dispacher, (d, act) => d.Dispach(new WrapBarAction {action = act})));

        uiRender.Setup(
            Unit.Default,
            new ActionWrapper<IGameSceneAction, IUIAction>(
                dispacher, (d, act) => d.Dispach(new WrapUIAction {action = act})));
    }

    void Destory() {
        ballRender.Clear();
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
    public List<BallState> ballState;

    /// <summary>
    ///   バーをインスタンス化するのに必要な初期設定
    /// </summary>
    public BarState barInitState;
    public BarState barState;

    public GameState gameState;
    public UIState uiState;
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
        var ballST = state.ballState[0];
        ballST.movesBall = true;
        state.ballState[0] = ballST;

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
        var ballST = state.ballState[0];
        ballST.movesBall = false;
        state.ballState[0] = ballST;

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
                {
                    var ballST = state.ballState[0];
                    ballST.movesBall = false;
                    state.ballState[0] = ballST;
                }
                state.barState.canMove = false;
                state.gameState = GameState.GameOver;
                break;
            case OnCollisionBar colBar:
                // ボールを一番上に移動させる(x方向はランダム)
                // スコアをアップさせる
                var ballXPos = state.barState.movePos.GetPos(RandomEnum<BarPosition>.GetRandom()).x;
                var ballPos = new Vector2(ballXPos, state.ballInitState.position.y);
                {
                    var ballST = state.ballState[0];
                    ballST.position = ballPos;
                    state.ballState[0] = ballST;
                }
                state.uiState = uiUpdate.Update(state.uiState, Singleton<IncScore>.Instance);
                break;
            default:
                // do nothing
                break;
        }
        state.ballState[0] = ballUpdate.Update(state.ballState[0], msg.action);
        return state;
    }

    GameSceneState Update(GameSceneState state, WrapUIAction msg) {
        state.uiState = uiUpdate.Update(state.uiState, msg.action);
        return state;
    }
}
