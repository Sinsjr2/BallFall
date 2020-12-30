using System;

public class DimensionsChangedNotification: UnityEngine.EventSystems.UIBehaviour {
    private Action handlers;

    override protected void OnRectTransformDimensionsChange()
    {
        if (handlers == null) {
            return;
        }
        handlers.Invoke();
    }

    #region "IHandler"
    public void AddHandler(Action handler)
    {
        handlers += handler;
    }

    public void RemoveHander(Action handler)
    {
        handler -= handler;
    }

    /// <summary>
    ///   ハンドラに登録してあったメソッドを消します。
    /// </summary>
    public void ClearHandler() {
        handlers = () => {};
    }
    #endregion
}
