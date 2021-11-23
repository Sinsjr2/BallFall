using System;
using TEA;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
///   バーに対応するゲームオブジェクトにアタッチして使用します。
/// </summary>
public class BarRender : MonoBehaviour, ITEAComponent<BarState, IBarMessage> {

    /// <summary>
    ///   バーを左側に移動させたときの位置
    /// </summary>
    [SerializeField]
    Vector2 leftPos;

    /// <summary>
    ///   バーを右に移動させたときの位置
    /// </summary>
    [SerializeField]
    Vector2 rightPos;

    /// <summary>
    ///   バーを動かすためのtransform
    /// </summary>
    RectTransform barTransform;

    public BarState CreateState() {

        var centerPos = rightPos + (leftPos - rightPos) * 0.5f;
        return new BarState {
            barPosition = BarPosition.Center,
            canMove = false,
            movePos = new MovePos {
                leftPos = leftPos,
                centerPos = centerPos,
                rightPos = rightPos
            } };
    }

    public void Setup(IDispatcher<IBarMessage> dispatcher) {
        Assert.AreNotEqual(leftPos, Vector2.zero);
        Assert.AreNotEqual(rightPos, Vector2.zero);
        barTransform = (RectTransform)transform;
    }

    public void Render(BarState state) {
        barTransform.anchoredPosition = state.GetPosition();
    }
}

/// <summary>
///   バーやボール(x方向)が動く位置
/// </summary>
public struct MovePos : IEquatable<MovePos> {
    public Vector2 leftPos;
    public Vector2 centerPos;
    public Vector2 rightPos;

    public bool Equals(MovePos other) {
        return leftPos == other.leftPos &&
            centerPos == other.centerPos &&
            rightPos == other.rightPos;
    }

    public Vector2 GetPos(BarPosition pos) {
        switch(pos) {
            case BarPosition.Left:
                return leftPos;
            case BarPosition.Center:
                return centerPos;
            case BarPosition.Right:
                return rightPos;
            default:
                throw new PatternMatchNotFoundException(pos);
        }
    }
}

public struct BarState : IEquatable<BarState>, IUpdate<BarState, IBarMessage> {
    public MovePos movePos;
    public BarPosition barPosition;

    /// <summary>
    ///   バーが動かせるかどうか
    /// </summary>
    public bool canMove;

    /// <summary>
    ///   バーの座標を取得します。
    /// </summary>
    public Vector2 GetPosition() {
        return movePos.GetPos(barPosition);
    }

    public BarState Update(IBarMessage msg) {
        switch (msg) {
            case MoveBar moveBar: return Update(this, moveBar);
            default: throw new PatternMatchNotFoundException(msg);
        }
    }

    BarState Update(BarState state, MoveBar msg) {
        if (!state.canMove) {
            return state;
        }
        state.barPosition = msg.position;
        return state;
    }

    public bool Equals(BarState other) {
        return movePos.Equals(other.movePos) &&
            barPosition == other.barPosition &&
            canMove == other.canMove;
    }
}

public interface IBarMessage {}

public enum BarPosition : byte {
    Left,
    Center,
    Right,
}

public class MoveBar : IBarMessage {
    public readonly BarPosition position;

    public MoveBar(BarPosition pos) {
        this.position = pos;
    }
}

/// <summary>
///   バーの左右の位置を更新します。
/// </summary>
public struct UpdateInitialBarPos : IBarMessage {
    public MovePos movePos;
}
