using System;
using AlmediaSDK;

namespace AlmediaLink
{
    internal abstract class EventBridgeBase<TOldHandler, TNewHandler>
        where TOldHandler : Delegate
        where TNewHandler : Delegate
    {
        private readonly object _lock = new object();
        private readonly Action<TNewHandler> _subscribe;
        private readonly Action<TNewHandler> _unsubscribe;
        private TNewHandler _forward;
        private TOldHandler _handlers;

        protected readonly string EventName;

        protected EventBridgeBase(string eventName, Action<TNewHandler> subscribe, Action<TNewHandler> unsubscribe)
        {
            EventName = eventName;
            _subscribe = subscribe;
            _unsubscribe = unsubscribe;
        }

        protected void SetForward(TNewHandler forward) => _forward = forward;

        protected TOldHandler Handlers => _handlers;

        public void Add(TOldHandler handler)
        {
            if (handler == null) return;
            lock (_lock)
            {
                var first = _handlers == null;
                _handlers = (TOldHandler)Delegate.Combine(_handlers, handler);
                if (first) _subscribe(_forward);
            }
        }

        public void Remove(TOldHandler handler)
        {
            if (handler == null) return;
            lock (_lock)
            {
                _handlers = (TOldHandler)Delegate.Remove(_handlers, handler);
                if (_handlers == null) _unsubscribe(_forward);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _handlers = null;
                _unsubscribe(_forward);
            }
        }
    }

    internal sealed class EventBridge<TNew, TOld> : EventBridgeBase<Action<TOld>, Action<TNew>>
    {
        private readonly Func<TNew, TOld> _convert;

        public EventBridge(string eventName, Func<TNew, TOld> convert,
            Action<Action<TNew>> subscribe, Action<Action<TNew>> unsubscribe)
            : base(eventName, subscribe, unsubscribe)
        {
            _convert = convert;
            SetForward(Forward);
        }

        private void Forward(TNew value)
        {
            var handlers = Handlers;
            if (handlers == null) return;
            EventDispatch.Raise(EventName, handlers, _convert(value));
        }
    }

    internal sealed class EventBridge<TNew1, TNew2, TOld1, TOld2>
        : EventBridgeBase<Action<TOld1, TOld2>, Action<TNew1, TNew2>>
    {
        private readonly Func<TNew1, TOld1> _convert1;
        private readonly Func<TNew2, TOld2> _convert2;

        public EventBridge(string eventName, Func<TNew1, TOld1> convert1, Func<TNew2, TOld2> convert2,
            Action<Action<TNew1, TNew2>> subscribe, Action<Action<TNew1, TNew2>> unsubscribe)
            : base(eventName, subscribe, unsubscribe)
        {
            _convert1 = convert1;
            _convert2 = convert2;
            SetForward(Forward);
        }

        private void Forward(TNew1 value1, TNew2 value2)
        {
            var handlers = Handlers;
            if (handlers == null) return;
            EventDispatch.Raise(EventName, handlers, _convert1(value1), _convert2(value2));
        }
    }
}
