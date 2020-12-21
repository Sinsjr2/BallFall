using System;
using UnityEngine;

/// <summary>
///   キーボードの入力をディスパッチします。
///   キーボードの状態が変わったときのみ通知します。
/// </summary>
public class InputSubscription : MonoBehaviour {

    InputState prevState;

    public IDispacher<ChangedInput> dispacher { set; private get; }

    void Update() {
        // 矢印キーの左右が押されていなければ、中央にする
        // 両方押されていば場合は、前のバーの状態を維持する
        BarPosition barPos = BarPosition.Center;
        if (Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.RightArrow))
            barPos = prevState.barPosition;
        else if (Input.GetKey(KeyCode.LeftArrow)) {
            barPos = BarPosition.Left;
        } else if (Input.GetKey(KeyCode.RightArrow)) {
            barPos = BarPosition.Right;
        }

        var newInput = new InputState {
            pushedEnterButton = Input.GetKeyDown(KeyCode.Return),
            pushedEscapeButton = Input.GetKeyDown(KeyCode.Escape),
            barPosition = barPos
        };

        bool isChanged = !prevState.Equals(newInput);
        prevState = newInput;
        if (isChanged) {
            dispacher?.Dispach(new ChangedInput { state = newInput });
        }
    }
}

public struct InputState : IEquatable<InputState> {
    public BarPosition barPosition;

    /// <summary>
    ///   決定ボタンが押されたか
    /// </summary>
    public bool pushedEnterButton;

    /// <summary>
    ///   中断・終了ボタンが押されたかどうか
    /// </summary>
    public bool pushedEscapeButton;

    public bool Equals(InputState other) {
        return barPosition == other.barPosition &&
            pushedEnterButton == other.pushedEnterButton &&
            pushedEscapeButton == other.pushedEscapeButton;
    }
}

/// <summary>
///   キーボーの状態が変化しました。
/// </summary>
public struct ChangedInput {
    public InputState state;
}
