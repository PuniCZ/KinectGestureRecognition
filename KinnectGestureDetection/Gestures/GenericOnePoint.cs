using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Accord.Statistics.Models.Markov;
using Microsoft.Kinect;

namespace KinectGestureDetection.Gestures
{
    /// <summary>
    /// Generic class for simple gestures, that use one hmm (tracking one joint) and dont have any aditional special functionality. 
    /// </summary>
    public class GenericOnePoint: IGesture
    {
        #region Fields

        private int gestureId;
        private HiddenMarkovModel hmm;
        private JointType trackedJoint;
        private string name;
        private bool registered = false;
        private double probabilityThreshold;
        private int aproxLength;

        #endregion
        
        #region Constructs

        /// <summary>
        /// Create "undefined" generic gesture with specified ID.
        /// </summary>
        /// <param name="id">Custom gesture ID (can be used for gesture identification).</param>
        public GenericOnePoint(int id)
        {
            gestureId = id;
            hmm = null;
            AproxLength = 0;
        }

        /// <summary>
        /// Initialize new generic gesture tracking specified joint and using specified number of HMM states.
        /// </summary>
        /// <param name="name">Name of gesture. Should not be empty.</param>
        /// <param name="trackedJoint">Point, witch this gesture will track.</param>
        /// <param name="numberOfHmmStates">Number of HMM states, withc this gesture will use.</param>
        public void Initialize(string name, JointType trackedJoint, int numberOfHmmStates)
        {
            this.name = name;
            this.trackedJoint = trackedJoint;
            hmm = new HiddenMarkovModel(DirectionRecognition.DirectionsCount, numberOfHmmStates);
        }

        #endregion

        #region IGesture Members

        /// <summary>
        /// Name of this gesture.
        /// </summary>
        /// <exception cref="GestureStateException">Throw if used on uninitialized or unloaded gesture.</exception>
        public string Name
        {
            get
            {
                if (name == "")
                    throw new GestureStateException("Trying to get name of unloaded generic gesture. Load it first.");
                else
                    return name;
            }
        }

        /// <summary>
        /// Id of this gesture.
        /// </summary>
        public int Id
        {
            get { return gestureId; }
        }

        /// <summary>
        /// Threshold, that determines when the gesture was recognized succefuly (if calculated gesture probability is greather, recognition shold be positive)
        /// </summary>
        public double ProbabilityThreshold { 
            get { return probabilityThreshold; }
            set { probabilityThreshold = value; }
        }

        /// <summary>
        /// Load generic gesture from specified file.
        /// </summary>
        /// <param name="inputFile">Path to file with gesture data.</param>
        /// <exception cref="GestureStateException">Throw on some gesture related error.</exception>
        /// <exception cref="SerializationException">If data in source file is not serializeable.</exception>
        public void LoadData(string inputFile)
        {
            if (registered)
                throw new GestureStateException("Cannot load data for registered gesture. Load it before registering any listener.");


            hmm = null;

            using (FileStream fs = new FileStream(inputFile, FileMode.Open))
            {
                try
                {
                    StreamReader stream = new StreamReader(fs);
                    string line = stream.ReadLine();
                    string[] metadata = line.Split(':');

                    if (metadata.Length == 4 && metadata[0] == "GestureGenericOnePoint")
                        AproxLength = 0;
                    else if (metadata.Length == 5 && metadata[0] == "GestureGenericOnePoint")
                    {
                        if (!Int32.TryParse(metadata[4], out aproxLength))
                            throw new GestureStateException("Failed to parse gesture length.");
                    }
                    else
                        throw new GestureStateException("Invalid input file format.");

                    name = metadata[1];

                    if (!Enum.TryParse<JointType>(metadata[2], out trackedJoint))
                        throw new GestureStateException("Unknown joint type.");

                    if (!Double.TryParse(metadata[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out probabilityThreshold))
                        throw new GestureStateException("Failed to parse probability.");

                    fs.Position = line.Length + Environment.NewLine.Length;
                    BinaryFormatter formatter = new BinaryFormatter();
                    hmm = (HiddenMarkovModel)formatter.Deserialize(fs);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to load. Reason: " + e.Message);
                    throw;
                }
            }

        }

        /// <summary>
        /// Save actual gesture state to specified file after learning. Most of this data are HMM configuration.
        /// </summary>
        /// <param name="outputFile">Path to file to store gesture data.</param>
        /// <exception cref="GestureStateException">Throw if used on uninitialized or unloaded gesture.</exception>
        public void SaveTrainedData(string outputFile)
        {
            if (hmm == null)
                throw new GestureStateException("Trying to save data of undefined generic gesture. Load or create it first.");

            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(outputFile, FileMode.Create);
                StreamWriter writer = new StreamWriter(stream);
                StringBuilder metadata = new StringBuilder("GestureGenericOnePoint:");
                metadata.Append(Name);
                metadata.Append(':');
                metadata.Append(trackedJoint.ToString());
                metadata.Append(':');
                metadata.Append(ProbabilityThreshold);
                metadata.Append(':');
                metadata.Append(AproxLength);
                writer.WriteLine(metadata.ToString());
                writer.Flush();
                formatter.Serialize(stream, hmm);
                stream.Close();
            }
            catch (Exception ex)
            {
                throw new GestureStateException("Problem on saving gesture data: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Update gesture HMM state based on passed data (obsarvations). Uses only gesture specified joint. Can take a while.
        /// </summary>
        /// <param name="observations">Observation data, that needs to be converted from enum to int array.</param>
        /// <returns>Logarithm likehood for passed data after training.</returns>
        /// <exception cref="GestureStateException">Throw if used on uninitialized or unloaded gesture.</exception>
        public double Learn(Dictionary<JointType, int[]> observations)
        {
            if (hmm == null)
                throw new GestureStateException("Trying to use undefined generic gesture. Load or create it first.");
            
            return hmm.Learn(observations[trackedJoint], 0.0001);
        }

        /// <summary>
        /// Calculate logarithm likehood for passed data.
        /// </summary>
        /// <param name="observations">Observation data, that needs to be converted from enum to int array.</param>
        /// <returns>Logarithm likehood for passed data.</returns>
        /// <exception cref="GestureStateException">If called when gesture is uninitialized.</exception>
        public double Calculate(Dictionary<JointType, int[]> observations)
        {
            if (hmm == null)
                throw new GestureStateException("Trying to use undefined generic gesture. Load or create it first.");

            if (observations[trackedJoint].Length == 0)
                return 0;
            else
                if (AproxLength == 0)
                    return hmm.Evaluate(observations[trackedJoint], true);
                else
                    return hmm.Evaluate(observations[trackedJoint], true) - Math.Pow(Math.Abs(observations[trackedJoint].Length - AproxLength), 1.75);
        }

        /// <summary>
        /// Register gesture joint to specified point tracker. Needs to be used after load or initialize.
        /// </summary>
        /// <param name="player">Point tracked that supplies related data for this gesture.</param>
        public void RegisterJointListeners(KinectGesturePlayer player)
        {
            player.RegisterJoint(trackedJoint, this);
            registered = true;
        }

        /// <summary>
        /// Checks, if passed joint type is used in this gesture.
        /// </summary>
        /// <param name="type">Tested joint type.</param>
        /// <returns>True if passed joint is tracked or false if not.</returns>
        public bool UseJoint(JointType type)
        {
            return type == trackedJoint;
        }

        /// <summary>
        /// Aproximetly gesture length (this value is used to optimize propability output dependig on gesture length)
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If value is lower than zero.</exception>
        public int AproxLength
        {
            get { return aproxLength; }
            set 
            {
                if (value >= 0)
                    aproxLength = value;
                else
                    throw new ArgumentOutOfRangeException("AproxLength", "This property must be greather or equal zero.");
            }
        }

        /// <summary>
        /// Returns gesture primary tracked joint.
        /// </summary>
        public JointType PrimaryJoint 
        {
            get { return trackedJoint; }       
        }

        #endregion
    }
}
