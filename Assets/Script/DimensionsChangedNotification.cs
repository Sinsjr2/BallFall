using TEA;
using UnityEngine.EventSystems;

#nullable enable

public class DimensionsChangedNotification: UIBehaviour, ISetup<Unit> {
    IDispatcher<Unit>? dispatcher;

    protected override void OnRectTransformDimensionsChange() {
        dispatcher?.Dispatch(Unit.Default);
    }

    public void Setup(IDispatcher<Unit> dispatcher) {
        this.dispatcher = dispatcher;
    }
}
