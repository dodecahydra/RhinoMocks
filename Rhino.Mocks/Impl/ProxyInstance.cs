using System;
using Rhino.Mocks.Interfaces;

namespace Rhino.Mocks.Impl
{
    using System.Collections;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// This is a dummy type that is used merely to give DynamicProxy the proxy instance that
    /// it needs to create IProxy's types.
    /// </summary>
    public class ProxyInstance : MarshalByRefObject, IMockedObject
    {
        private MockRepository repository;
        private int hashCode;
        private IList originalMethodsToCall;
        private IList propertiesToSimulate;
        private IDictionary propertiesValues;
        private IDictionary eventsSubscribers;

        /// <summary>
        /// Create a new instance of <see cref="ProxyInstance"/>
        /// </summary>
        public ProxyInstance(MockRepository repository)
        {
            this.repository = repository;
            hashCode = MockedObjectsEquality.NextHashCode;
        }

        /// <summary>
        /// The unique hash code of this proxy, which is not related
        /// to the value of the GetHashCode() call on the object.
        /// </summary>
        public int ProxyHash
        {
            get { return hashCode; }
        }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        public MockRepository Repository
        {
            get { return repository; }
        }

        /// <summary>
        /// Return true if it should call the original method on the object
        /// instead of pass it to the message chain.
        /// </summary>
        /// <param name="method">The method to call</param>
        public bool ShouldCallOriginal(MethodInfo method)
        {
            if (originalMethodsToCall == null)
                return false;
            return originalMethodsToCall.Contains(method);

        }

        /// <summary>
        /// Register a method to be called on the object directly
        /// </summary>
        public void RegisterMethodForCallingOriginal(MethodInfo method)
        {
            if (originalMethodsToCall == null)
                originalMethodsToCall = new ArrayList();
            originalMethodsToCall.Add(method);
        }

        /// <summary>
        /// Register a property on the object that will behave as a simple property
        /// </summary>
        public void RegisterPropertyBehaviorFor(PropertyInfo prop)
        {
            if (propertiesToSimulate == null)
                propertiesToSimulate = new ArrayList();
            propertiesToSimulate.Add(prop.GetGetMethod());
            propertiesToSimulate.Add(prop.GetSetMethod());
        }

        /// <summary>
        /// Check if the method was registered as a property method.
        /// </summary>
        public bool IsPropertyMethod(MethodInfo method)
        {
            if (propertiesToSimulate == null)
                return false;
            return propertiesToSimulate.Contains(method);
        }

        /// <summary>
        /// Do get/set on the property, according to need.
        /// </summary>
        public object HandleProperty(MethodInfo method, object[] args)
        {
            if (propertiesValues == null)
                propertiesValues = new Hashtable();

            if (method.Name.StartsWith("get_"))
            {
                string key = GenerateKey(method, args);
                if (propertiesValues.Contains(key) == false && method.ReturnType.IsValueType)
                {
                    throw new InvalidOperationException(
                        string.Format("Can't return a value for property {0} because no value was set and the Property return a value type.", method.Name.Substring(4)));
                }
                return propertiesValues[key];
            }

            object value = args[args.Length - 1];
            propertiesValues[GenerateKey(method, args)] = value;
            return null;
        }


        /// <summary>
        /// Do add/remove on the event
        /// </summary>
        public void HandleEvent(MethodInfo method, object[] args)
        {
            if (eventsSubscribers == null)
                eventsSubscribers = new Hashtable();

            Delegate subscriber = (Delegate )args[0];
            if(method.Name.StartsWith("add_"))
            {
                AddEvent(method, subscriber);
            }
            else
            {
                RemoveEvent(method, subscriber);
            }
        }

        /// <summary>
        /// Get the subscribers of a spesific event
        /// </summary>
        public Delegate GetEventSubscribers(string eventName)
        {
            if (eventsSubscribers == null)
                return null;
            return (Delegate)eventsSubscribers[eventName];
        }

        private static string GenerateKey(MethodInfo method, object[] args)
        {
            if ((method.Name.StartsWith("get_") && args.Length == 0) ||
                (method.Name.StartsWith("set_") && args.Length == 1))
                return method.Name.Substring(4);
            StringBuilder sb = new StringBuilder();
            sb.Append(method.Name.Substring(4));
            int len = args.Length;
            if (method.Name.StartsWith("set_"))
                len--;
            for (int i = 0; i < len; i++)
            {
                sb.Append(args[i].GetHashCode());
            }
            return sb.ToString();
        }

        private void RemoveEvent(MethodInfo method, Delegate subscriber)
        {
            string eventName = method.Name.Substring(7);
            Delegate existing = (MulticastDelegate)eventsSubscribers[eventName];
            existing = MulticastDelegate.Remove(existing, subscriber);
            eventsSubscribers[eventName] = existing;
        }

        private void AddEvent(MethodInfo method, Delegate subscriber)
        {
            string eventName = method.Name.Substring(4);
            Delegate existing = (MulticastDelegate)eventsSubscribers[eventName];
            existing = MulticastDelegate.Combine(existing, subscriber);
            eventsSubscribers[eventName] = existing;
        }
    }
}