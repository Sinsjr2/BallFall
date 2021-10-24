using System;

namespace TEA {

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
    }
}
