using System;
using System.Collections.Generic;

namespace JG.Modding
{
    /// <summary>
    /// Mutable service registry that the host game populates before mod entry points run.
    /// Implements <see cref="IModServiceProvider"/> so it can be passed directly to <see cref="IModContext"/>.
    /// </summary>
    public sealed class ModServiceRegistry : IModServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        /// <summary>Register a service instance. Replaces any previous registration for the same type.</summary>
        public void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _services[typeof(T)] = instance;
        }

        /// <summary>Remove a previously registered service. Returns true if it was present.</summary>
        public bool Unregister<T>() where T : class
            => _services.Remove(typeof(T));

        /// <summary>Remove all registrations.</summary>
        public void Clear() => _services.Clear();

        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj))
                return (T)obj;
            throw new InvalidOperationException(
                $"No service registered for type {typeof(T).Name}. " +
                "Ensure the host game registers this service in its mod bootstrap.");
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }
    }
}
