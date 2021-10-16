using System;

namespace TEA {
    public class MessageWrapper<Input, Before, After> : IDispatcher<After> {

        readonly IDispatcher<Before> dispatcher;
        readonly Action<IDispatcher<Before>, Input, After> dispatch;
        public Input value { set; private get; }

        public MessageWrapper(IDispatcher<Before> dispatcher, Action<IDispatcher<Before>, Input, After> dispatch) {
            this.dispatcher = dispatcher;
            this.dispatch = dispatch;
        }

        public void Dispatch(After msg) {
            dispatch(dispatcher, value, msg);
        }
    }

    public class MessageWrapper<Before, After> : IDispatcher<After> {

        readonly IDispatcher<Before> dispatcher;
        readonly Action<IDispatcher<Before>, After> dispatch;

        public MessageWrapper(IDispatcher<Before> dispatcher, Action<IDispatcher<Before>, After> dispatch) {
            this.dispatcher = dispatcher;
            this.dispatch = dispatch;
        }

        public void Dispatch(After msg) {
            dispatch(dispatcher, msg);
        }
    }

    public static class MessageWrapper {
        public static MessageWrapper<Before, After> Wrap<Before, After>(
            this IDispatcher<Before> dispatcher,
            Action<IDispatcher<Before>, After> dispatch) {
            return new MessageWrapper<Before, After>(dispatcher, dispatch);
        }

        public static MessageWrapper<Input, Before, After> Wrap<Input, Before, After>(
            this IDispatcher<Before> dispatcher,
            Action<IDispatcher<Before>, Input, After> dispatch) {
            return new MessageWrapper<Input, Before, After>(dispatcher, dispatch);
        }
    }
}
