using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class UIRender : MonoBehaviour, IRender<Unit, UIState, IUIAction> {

    /// <summary>
    ///   開始メッセージやゲームオーバー
    /// </summary>
    [SerializeField]
    Text statusText;

    /// <summary>
    ///   スコアの数値を書き込みます。
    /// </summary>
    [SerializeField]
    Text scoreText;

    public UIState CreateState(Unit initial) {
        Assert.IsNotNull(statusText);
        Assert.IsNotNull(scoreText);
        return new UIState {
            score = 0,
            statusMessage = "",
            showMessage = false,
        };
    }

    public void Setup(Unit _, IDispacher<IUIAction> dispacher) {
    }

    public void Render(UIState state) {
        statusText.enabled = state.showMessage;
        statusText.text = state.statusMessage;
        scoreText.text =  state.score.ToString();
    }

}

public struct UIState : IEquatable<UIState> {
    public int score;

    /// <summary>
    ///   開始の文字列やゲームオーバーの文字列
    /// </summary>
    public string statusMessage;

    /// <summary>
    ///   メッセージを表示するか
    /// </summary>
    public bool showMessage;

    public bool Equals(UIState other) {
        return statusMessage == other.statusMessage &&
            showMessage == other.showMessage &&
            score == other.score;
    }
}

public interface IUIAction {}

/// <summary>
///   得点を１増加させます。
/// </summary>
public class IncScore : IUIAction {
}

/// <summary>
///   ゲームを開始するUIにします。
///   この時スコアを0にリセットします。
/// </summary>
public class ToGameReadyUI : IUIAction {}

/// <summary>
///   ゲームプレイ中のUIにします。
/// </summary>
public class ToGamePlayUI : IUIAction {}

/// <summary>
///   ゲームオーバー画面にします。
/// </summary>
public class ToGameOverUI : IUIAction {}

public class UIUpdate : IUpdate<UIState, IUIAction> {

    public UIState Update(UIState state, IUIAction msg) {
        switch (msg) {
            case IncScore incScore:
                return Update(state, incScore);
            case ToGameReadyUI toGameReadyUI:
                return Update(state, toGameReadyUI);
            case ToGamePlayUI toGamePlayUI:
                return Update(state, toGamePlayUI);
            case ToGameOverUI toGameOverUI:
                return Update(state, toGameOverUI);
            default:
                throw new PatternMatchNotFoundException(msg);
        }
    }

    public UIState Update(UIState state, IncScore msg) {
        state.score++;
        return state;
    }

    /// <summary>
    ///   一時中断中のUIに変更します。
    /// </summary>
    public UIState ToPauseUI(UIState state) {
        state.statusMessage = "Pausing";
        state.showMessage = true;
        return state;
    }

    public UIState Update(UIState state, ToGameReadyUI msg) {
        state.score = 0;
        state.statusMessage = "Readly?";
        state.showMessage = true;
        return state;
    }

    public UIState Update(UIState state, ToGamePlayUI msg) {
        state.statusMessage = "";
        state.showMessage = false;
        return state;
    }

    public UIState Update(UIState state, ToGameOverUI msg) {
        state.statusMessage = "Game Over";
        state.showMessage = true;
        return state;
    }
}
