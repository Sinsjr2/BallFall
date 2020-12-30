using System;

public class ActionWrapper<Input, Before, After> : IDispacher<After> {

    readonly IDispacher<Before> dispacher;
    readonly Action<IDispacher<Before>, Input, After> dispach;
    public Input value {set; private get;}

    public ActionWrapper(IDispacher<Before> dispacher, Action<IDispacher<Before>, Input, After> dispach) {
        this.dispacher = dispacher;
        this.dispach = dispach;
    }

    public void Dispach(After act) {
        dispach(dispacher, value, act);
    }
}

public class ActionWrapper<Before, After> : IDispacher<After> {

    readonly IDispacher<Before> dispacher;
    readonly Action<IDispacher<Before>, After> dispach;

    public ActionWrapper(IDispacher<Before> dispacher, Action<IDispacher<Before>, After> dispach) {
        this.dispacher = dispacher;
        this.dispach = dispach;
    }

    public void Dispach(After act) {
        dispach(dispacher, act);
    }
}

public static class ActionWrapper {
    public static ActionWrapper<Before, After> Wrap<Before, After>(
        this IDispacher<Before> dispacher,
        Action<IDispacher<Before>, After> dispach) {
        return new ActionWrapper<Before, After>(dispacher, dispach);
    }

    public static ActionWrapper<Input, Before, After> Wrap<Input, Before, After>(
        this IDispacher<Before> dispacher,
        Action<IDispacher<Before>, Input, After> dispach) {
        return new ActionWrapper<Input, Before, After>(dispacher, dispach);
    }
}
