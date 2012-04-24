using System.Collections.Generic;
using Microsoft.Kinect;

namespace KinectGestureDetection.Gestures
{
    /// <summary>
    /// Interface for every gesture, wich can work with this library.
    /// </summary>
    /// <remarks></remarks>
    public interface IGesture
    {
        /// <summary>
        /// Represent name of the gesture. Name is used only for external use, dont have any internal or side effect.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Stores Id of this gesture. This ID needs to be uniqeue within all gestures used by this library. Should be set in constructor.
        /// </summary>
        /// <remarks>Needs to be set before registering gesture.</remarks>
        int Id { get; }

        /// <summary>
        /// Value representing threshold between succefully and unsuccefully recognized gesture.
        /// </summary>
        /// <remarks>This value dont have any library-internal functionality, can be used by internal gesture logic or most likely by application itself.</remarks>
        /// <example>Its logarithm of propability likehood, so propper values should be between -70 and -300.</example>
        double ProbabilityThreshold { get; }

        /// <summary>
        /// Average lenght of this gesture. This value can be used in 
        /// </summary>
        /// <remarks>This value dont have any library-internal functionality, but should be used by internal gesture logic (eg. in Calculation method) or application itself.</remarks>
        /// <seealso cref="Calculate"/>
        int AproxLength { get; }

        /// <summary>
        /// Represents joint primary used by this gesture.
        /// </summary>
        /// <remarks>This value is currently used only for GestureRecognized event as index of joint, which legth will be part of event arguments of this event.</remarks>
        /// <seealso cref="GestureRecognized"/>
        JointType PrimaryJoint { get; }

        /// <summary>
        /// This method will be called when gesture itself will be loaded, expecialy in RegisterGesture mathod of this library.
        /// </summary>
        /// <param name="inputFile">Path to file, which store gesture related data, that needs to be loaded.</param>
        /// <seealso cref="RegisterGesture"/>
        /// <seealso cref="SaveTrainedData"/>
        void LoadData(string inputFile);

        /// <summary>
        /// This method saves current gesture state. It should be equivalent of LoadData method, but can be blank.
        /// </summary>
        /// <remarks>This method is'n curently used inside this library, but should be used by programmer when gesture is created.</remarks>
        /// <param name="outputFile">Path to file, that should store gesture data.</param>
        /// <seealso cref="LoadData"/>
        void SaveTrainedData(string outputFile);

        /// <summary>
        /// This method should be used for training gesture state with passed DirectionSequence (converted to array of int).
        /// </summary>
        /// <param name="observations">Dictionary containing sequence which should be learned for one or more joints. Sequence needs to be converted to array of int.</param>
        /// <returns>Computed log probability for this sequence.</returns>
        /// <example>
        /// The folowing example shows learning every sequence obtained from SequenceReady event of KinectGesturePlayer class.
        /// <code>
        /// void SequenceReady(object sender, EventArgs e)
        /// {
        ///     DirectionSequence sequence = ((SequenceEventArgs)e).Sequence;
        ///     Dictionary<JointType, int[]> observations = new Dictionary<JointType, int[]>();
        ///     observations.Add(gesture.PrimaryJoint, sequence.ToArray);
        ///     gesture.Learn(observations);
        /// }
        /// </code>
        /// </example>
        double Learn(Dictionary<JointType, int[]> observations);

        /// <summary>
        /// Method used by library to get probability for this gesture and passed observation data.
        /// </summary>
        /// <remarks>
        /// Should contain computing of probability on direction sequence data and comparation of gesture AproxLength and length of observed sequence.
        /// </remarks>
        /// <param name="observations">Dictionary containing observerd sequences which should be evaluated for one or more joints. Sequence needs to be converted to array of int.</param>
        /// <returns>Computed log probability for this sequence.</returns>
        double Calculate(Dictionary<JointType, int[]> observations);

        /// <summary>
        /// Inside this mehod should be registered all joint, that gesture needs for evaluation and propper function.
        /// </summary>
        /// <param name="player">Reference on player to register this gesture.</param>
        /// <seealso cref="KinectGesturePlayer.RegisterJoint"/>
        /// <example>
        /// Following code shows example of this method used for registering this gesture for receive sequence data from HandRight joint.
        /// <code>
        /// public void RegisterJointListeners(KinectGesturePlayer player)
        /// {
        ///     player.RegisterJoint(JointType.HandRight, this);
        /// }
        /// </code>
        /// </example>
        void RegisterJointListeners(KinectGesturePlayer player);

        /// <summary>
        /// Tests, if this gesture use passed JointType for recognition.
        /// </summary>
        /// <param name="type">JointType to test.</param>
        /// <returns>True if JointType is used, else otherwise.</returns>
        bool UseJoint(JointType type);   
    }
}
