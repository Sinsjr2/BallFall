using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
///   移動させるボールのオブジェクトにアタッチして使用します
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class BallRender : MonoBehaviour , IRender<Unit, BallState, IBallMessage> {

    IDispatcher<IBallMessage> dispatch;

    /// <summary>
    ///   ボールを動かすためのTransform
    /// </summary>
    RectTransform ballTransform;

    [SerializeField]
    [Tooltip("落下速度")]
    Vector2 speed;

    public BallState CreateState(Vector2 instancePos) {
        Assert.AreNotEqual(speed, Vector2.zero);
        return new BallState { position = instancePos, speed = speed, movesBall = false };
    }

    public void Setup(Unit _, IDispatcher<IBallMessage> dispatcher) {
        this.dispatch = dispatcher;
        ballTransform = (RectTransform)transform;
    }

    public void Render(BallState state) {
        ballTransform.anchoredPosition = state.position;
        // 自身のコンポーネントをdisableにすることでUpdateを停止させる
        enabled = state.movesBall;
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (!(collision.gameObject.GetComponent<BallAreaOutCollider>() is null)) {
            dispatch.Dispatch(Singleton<OnOutOfArea>.Instance);
        }
        if (!(collision.gameObject.GetComponent<BarRender>() is null)) {
            dispatch.Dispatch(Singleton<OnCollisionBar>.Instance);
        }
    }

    void Update() {
        dispatch?.Dispatch(Singleton<NextFrame>.Instance);
    }
}

public interface IBallMessage {}

/// <summary>
///   バーに衝突しました。
/// </summary>
public class OnCollisionBar : IBallMessage {
};

/// <summary>
///   次のフレームのボールの位置を計算します。
/// </summary>
public class NextFrame : IBallMessage {
}

/// <summary>
///   ボールがエリア外に抜けた時
/// </summary>
public class OnOutOfArea : IBallMessage {
}

/// <summary>
///   ボール１つの状態
/// </summary>
public struct BallState : System.IEquatable<BallState> {

    /// <summary>
    ///   ボールの位置
    /// </summary>
    public Vector2 position;

    /// <summary>
    ///   進む速度
    /// </summary>
    public Vector2 speed;

    /// <summary>
    ///   ボールを動かすかどうか
    /// </summary>
    public bool movesBall;

    public bool Equals(BallState other) {
        return position == other.position &&
            speed == other.speed &&
            movesBall == other.movesBall;
    }
}

public class BallUpdate : IUpdate<BallState, IBallMessage> {

    public BallState Update(BallState state, IBallMessage msg) {
        switch (msg) {
            case NextFrame nextFrame:
                return Update(state, nextFrame);
            case OnOutOfArea _:
                return state;
            case OnCollisionBar _:
                return state;
            default:
                throw new PatternMatchNotFoundException(msg);
        }
    }

    public BallState Update(BallState state, NextFrame msg) {
        state.position += state.speed * Time.deltaTime;
        return state;
    }
}
