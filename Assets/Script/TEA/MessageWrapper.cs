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

    public class MessageWrapper<TSource, TResult> : IDispatcher<TSource> {

        readonly IDispatcher<TResult> dispatcher;
        readonly Func<TSource, TResult> selector;

        public MessageWrapper(IDispatcher<TResult> dispatcher, Func<TSource, TResult> selector) {
            this.dispatcher = dispatcher;
            this.selector = selector;
        }

        public void Dispatch(TSource msg) {
            dispatcher.Dispatch(selector(msg));
        }
    }

    public static class MessageWrapper {
        public static IDispatcher<TSource> Wrap<TSource, TResult>(
            this IDispatcher<TResult> dispatcher,
            Func<TSource, TResult> selector) {
            return new MessageWrapper<TSource, TResult>(dispatcher, selector);
        }

        public static MessageWrapper<Input, Before, After> Wrap<Input, Before, After>(
            this IDispatcher<Before> dispatcher,
            Action<IDispatcher<Before>, Input, After> dispatch) {
            return new MessageWrapper<Input, Before, After>(dispatcher, dispatch);
        }
    }
}
