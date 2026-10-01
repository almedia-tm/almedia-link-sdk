using System;

namespace AlmediaSDK
{
    // Raises multicast events one subscriber at a time: a throwing subscriber is
    // reported through AlmediaLog.Error and the remaining subscribers still run.
    internal static class EventDispatch
    {
        // OnLog dispatches through this class too, so a failure report that itself
        // throws must be dropped, never reported again.
        private static bool _reportingFailure;

        internal static void Raise(string eventName, Action handlers)
        {
            if (handlers == null) return;
            foreach (Action subscriber in handlers.GetInvocationList())
            {
                try { subscriber(); }
                catch (Exception e) { Report(eventName, subscriber, e); }
            }
        }

        internal static void Raise<T>(string eventName, Action<T> handlers, T arg)
        {
            if (handlers == null) return;
            foreach (Action<T> subscriber in handlers.GetInvocationList())
            {
                try { subscriber(arg); }
                catch (Exception e) { Report(eventName, subscriber, e); }
            }
        }

        internal static void Raise<T1, T2>(string eventName, Action<T1, T2> handlers, T1 arg1, T2 arg2)
        {
            if (handlers == null) return;
            foreach (Action<T1, T2> subscriber in handlers.GetInvocationList())
            {
                try { subscriber(arg1, arg2); }
                catch (Exception e) { Report(eventName, subscriber, e); }
            }
        }

        internal static void Report(string eventName, Delegate subscriber, Exception e)
        {
            if (_reportingFailure) return;
            _reportingFailure = true;
            try
            {
                var method = subscriber.Method;
                AlmediaLog.Error($"{eventName} handler {method.DeclaringType?.FullName}.{method.Name} threw: {e}");
            }
            catch
            {
                AlmediaLog.Error($"{eventName} handler threw, and the exception could not be described.");
            }
            finally
            {
                _reportingFailure = false;
            }
        }
    }
}
