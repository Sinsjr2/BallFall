using System;

public class ActionWrapper<Input, Before, After> : IDispatcher<After> {

    readonly IDispatcher<Before> dispatcher;
    readonly Action<IDispatcher<Before>, Input, After> dispatch;
    public Input value {set; private get;}

    public ActionWrapper(IDispatcher<Before> dispatcher, Action<IDispatcher<Before>, Input, After> dispatch) {
        this.dispatcher = dispatcher;
        this.dispatch = dispatch;
    }

    public void Dispatch(After act) {
        dispatch(dispatcher, value, act);
    }
}

public class ActionWrapper<Before, After> : IDispatcher<After> {

    readonly IDispatcher<Before> dispatcher;
    readonly Action<IDispatcher<Before>, After> dispatch;

    public ActionWrapper(IDispatcher<Before> dispatcher, Action<IDispatcher<Before>, After> dispatch) {
        this.dispatcher = dispatcher;
        this.dispatch = dispatch;
    }

    public void Dispatch(After act) {
        dispatch(dispatcher, act);
    }
}

public static class ActionWrapper {
    public static ActionWrapper<Before, After> Wrap<Before, After>(
        this IDispatcher<Before> dispatcher,
        Action<IDispatcher<Before>, After> dispatch) {
        return new ActionWrapper<Before, After>(dispatcher, dispatch);
    }

    public static ActionWrapper<Input, Before, After> Wrap<Input, Before, After>(
        this IDispatcher<Before> dispatcher,
        Action<IDispatcher<Before>, Input, After> dispatch) {
        return new ActionWrapper<Input, Before, After>(dispatcher, dispatch);
    }
}
