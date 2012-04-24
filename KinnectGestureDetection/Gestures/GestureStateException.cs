using System;
using System.Runtime.Serialization;

namespace KinectGestureDetection.Gestures
{
    /// <summary>
    /// Exception class for gestures.
    /// </summary>
    /// <remarks>Nothing special here, uses only functionality of base Exception class.</remarks>
    [Serializable]
    public class GestureStateException: Exception
    {
        public GestureStateException() { }

        public GestureStateException(string message)
            : base(message)
        { }

        public GestureStateException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        public GestureStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
