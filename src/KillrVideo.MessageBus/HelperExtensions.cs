using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace KillrVideo.MessageBus
{
    public static class HelperExtensions
    {
        /// <summary>
        /// Returns true if the given Type is the IHandleMessage&lt;T&gt; interface.
        /// </summary>
        public static bool IsMessageHandlerInterface(this Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof (IHandleMessage<>);
        }

        /// <summary>
        /// Returns any IHandleMessage&lt;T&gt; interfaces implemented by the given Type.
        /// </summary>
        public static IEnumerable<Type> GetMessageHandlerInterfaces(this Type t)
        {
            while (t != null && t != typeof (object))
            {
                IEnumerable<Type> interfaces = t.GetInterfaces().Where(ifc => ifc.IsMessageHandlerInterface());
                foreach (Type ifc in interfaces)
                {
                    yield return ifc;
                }

                // Go up inheritance chain
                t = t.BaseType;
            }
        }

        
    }
}
