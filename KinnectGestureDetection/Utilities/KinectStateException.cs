using System;
using System.Runtime.Serialization;

namespace KinectGestureDetection
{
    /// <summary>
    /// Exception class for kinect API.
    /// </summary>
    /// <remarks>Nothing special here, uses only functionality of base Exception class.</remarks>
    [Serializable]
    public class KinectStateException: Exception
    {
        public KinectStateException()
        { }

        public KinectStateException(string message)
            : base(message)
        { }

        public KinectStateException(string message, Exception innerException) 
            : base(message, innerException)
        { }

        public KinectStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
