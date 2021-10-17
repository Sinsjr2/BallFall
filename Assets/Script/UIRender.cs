using System;
using TEA;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class UIRender : MonoBehaviour, IRender<Unit, UIState, IUIMessage> {

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

    public UIState CreateState() {
        Assert.IsNotNull(statusText);
        Assert.IsNotNull(scoreText);
        return new UIState {
            score = 0,
            statusMessage = "",
            showMessage = false,
        };
    }

    public void Setup(Unit _, IDispatcher<IUIMessage> dispatcher) {
    }

    public void Render(UIState state) {
        statusText.enabled = state.showMessage;
        statusText.text = state.statusMessage;
        scoreText.text =  state.score.ToString();
    }

}

public struct UIState : IEquatable<UIState>, IUpdate<UIState, IUIMessage> {
    public int score;

    /// <summary>
    ///   開始の文字列やゲームオーバーの文字列
    /// </summary>
    public string statusMessage;

    /// <summary>
    ///   メッセージを表示するか
    /// </summary>
    public bool showMessage;

    public UIState Update(IUIMessage msg) {
        switch (msg) {
            case IncScore incScore: return Update(this, incScore);
            case ToGameReadyUI toGameReadyUI: return Update(this, toGameReadyUI);
            case ToGamePlayUI toGamePlayUI: return Update(this, toGamePlayUI);
            case ToGameOverUI toGameOverUI: return Update(this, toGameOverUI);
            default: throw new PatternMatchNotFoundException(msg);
        }
    }

    UIState Update(UIState state, IncScore msg) {
        state.score++;
        return state;
    }

    /// <summary>
    ///   一時中断中のUIに変更します。
    /// </summary>
    public UIState ToPauseUI() {
        var state = this;
        state.statusMessage = "Pausing";
        state.showMessage = true;
        return state;
    }

    UIState Update(UIState state, ToGameReadyUI msg) {
        state.score = 0;
        state.statusMessage = "Readly?";
        state.showMessage = true;
        return state;
    }

    UIState Update(UIState state, ToGamePlayUI msg) {
        state.statusMessage = "";
        state.showMessage = false;
        return state;
    }

    UIState Update(UIState state, ToGameOverUI msg) {
        state.statusMessage = "Game Over";
        state.showMessage = true;
        return state;
    }

    public bool Equals(UIState other) {
        return statusMessage == other.statusMessage &&
            showMessage == other.showMessage &&
            score == other.score;
    }
}

public interface IUIMessage {}

/// <summary>
///   得点を１増加させます。
/// </summary>
public class IncScore : IUIMessage {
}

/// <summary>
///   ゲームを開始するUIにします。
///   この時スコアを0にリセットします。
/// </summary>
public class ToGameReadyUI : IUIMessage {}

/// <summary>
///   ゲームプレイ中のUIにします。
/// </summary>
public class ToGamePlayUI : IUIMessage {}

/// <summary>
///   ゲームオーバー画面にします。
/// </summary>
public class ToGameOverUI : IUIMessage {}
