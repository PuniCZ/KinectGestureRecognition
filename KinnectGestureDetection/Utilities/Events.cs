using System;

namespace KinectGestureDetection
{
    /// <summary>
    /// Delegate for FrameReady event.
    /// </summary>
    /// <param name="sender">Sender object.</param>
    /// <seealso cref="FrameReady"/>
    public delegate void FrameReadyEventHandler(object sender);

    /// <summary>
    /// Delegate for SequenceReady event.
    /// </summary>
    /// <param name="sender">Sender object.</param>
    /// <param name="e">Event arguments.</param>
    public delegate void SequenceReadyEventHandler(object sender, SequenceEventArgs e);

    /// <summary>
    /// Delegate for GestureRecognized event.
    /// </summary>
    /// <param name="sender">Sender object.</param>
    /// <param name="e">Event arguments.</param>
    public delegate void GestureRecognizedEventHandler(object sender, GestureEventArgs e);

    /// <summary>
    /// Represents event arguments for SequenceReady event.
    /// </summary>
    /// <seealso cref="SequenceReady"/>
    public class SequenceEventArgs : EventArgs
    {
        private DirectionSequence sequence;

        /// <summary>
        /// Recorded sequence data.
        /// </summary>
        /// <remarks>can be safely used, because its a copy of them.</remarks>
        public DirectionSequence Sequence
        {
            get { return sequence; }
            set { sequence = value; }
        }

        /// <summary>
        /// Creates new instance by passing sequence data.
        /// </summary>
        /// <param name="seq">Should be copy of recorded sequence data (original data can be erased after this call).</param>
        public SequenceEventArgs(DirectionSequence seq)
        {
            Sequence = seq;
        }
    }

    /// <summary>
    /// Represents event arguments for GestureRecognized event.
    /// </summary>
    /// <seealso cref="GestureRecognized"/>
    public class GestureEventArgs : EventArgs
    {
        private KinectGesturePlayer sourcePlayer;
        private Gestures.IGesture gesture;
        private double probability;
        private bool forced;
        private int length;
        private bool valid;

        /// <summary>
        /// Played which performed gesture.
        /// </summary>
        public KinectGesturePlayer SourcePlayer
        {
            get { return sourcePlayer; }
            set { sourcePlayer = value; }
        }

        /// <summary>
        /// Instance of recognized gesture.
        /// </summary>
        /// <remarks>Here we can use Name or Id property of gesture to get more information abou it.</remarks>
        public Gestures.IGesture Gesture
        {
            get { return gesture; }
            set { gesture = value; }
        }

        /// <summary>
        /// Probability log likehood of succefully gesture recognition.
        /// </summary>
        /// <remarks>This value should be in range of zero and -few hundred.</remarks>
        public double Probability
        {
            get { return probability; }
            set { probability = value; }
        }

        /// <summary>
        /// Set to true, if gesture is recognized as forced (not axpecialy as gesture with biggest likehood).
        /// </summary>
        /// <seealso cref="KinectGestureAPI.RegisterGesture"/>
        public bool IsForced
        {
            get { return forced; }
            set { forced = value; }
        }

        /// <summary>
        /// Set to true, if gesture is recognized as valid (have his probability greather than ProbabilityThreshold).
        /// </summary>
        public bool IsValid
        {
            get { return valid; }
            set { valid = value; }
        }

        /// <summary>
        /// Legth of recognized gesture (for his primary joint).
        /// </summary>
        public int Length
        {
            get { return length; }
            set { length = value; }
        }

        /// <summary>
        /// Create new instance by passing all needed data.
        /// </summary>
        /// <param name="player">Player performed gesture.</param>
        /// <param name="gesture">Recognized gesture.</param>
        /// <param name="probability">Probability of recognition.</param>
        /// <param name="length">Length of recognized gesture.</param>
        /// <param name="forced">True if gesture was forced.</param>
        public GestureEventArgs(KinectGesturePlayer player, Gestures.IGesture gesture, double probability, int length, bool valid, bool forced = false)
        {
            SourcePlayer = player;
            Gesture = gesture;
            Probability = probability;
            IsForced = forced;
            IsValid = valid;
            Length = length;
        }

    }

}