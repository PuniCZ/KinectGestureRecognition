using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectGestureDetection
{
    /// <summary>
    /// Represents one player (user) in gesture recognition API.
    /// </summary>
    public class KinectGesturePlayer
    {
        #region Static fields

        /// <summary>
        /// Dictionary for joint colors, based on SKD data.
        /// </summary>
        private static Dictionary<JointType, Brush> jointColors = new Dictionary<JointType, Brush>() { 
            {JointType.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointType.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointType.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
            {JointType.Head, new SolidColorBrush(Color.FromRgb(200, 0,   0))},
            {JointType.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79,  84,  33))},
            {JointType.ElbowLeft, new SolidColorBrush(Color.FromRgb(84,  33,  42))},
            {JointType.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointType.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
            {JointType.ShoulderRight, new SolidColorBrush(Color.FromRgb(33,  79,  84))},
            {JointType.ElbowRight, new SolidColorBrush(Color.FromRgb(33,  33,  84))},
            {JointType.WristRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointType.HandRight, new SolidColorBrush(Color.FromRgb(37,   69, 243))},
            {JointType.HipLeft, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointType.KneeLeft, new SolidColorBrush(Color.FromRgb(69,  33,  84))},
            {JointType.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
            {JointType.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointType.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
            {JointType.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222,  76))},
            {JointType.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
            {JointType.FootRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))}
        };

        #endregion

        #region Fields

        private Color color;
        private int index;
        private JointCollection joints;
        private SolidColorBrush brush;
        private KinectSensor kinect;
        private bool tracking = false;
        private int frameCounter;
        private Dictionary<JointType, Filter3D> trackedPoints;
        private Dictionary<JointType, DirectionSequence> directionSequences;
        private List<Gestures.IGesture> gestureListeners;
        private List<Gestures.IGesture> forcedRecognition;
                
        #endregion
                
        #region Properties

        /// <summary>
        /// Player color used for visualization.
        /// </summary>
        public Color Color {
            get { return color; }
            set { color = value; } 
        }

        /// <summary>
        /// Player index used for player identifikation.
        /// </summary>
        public int Index { 
            get { return index; }
            private set { index = value; }
        }

        /// <summary>
        /// Provides access to all player tracked joints.
        /// </summary>
        public JointCollection Joints
        {
            get { return joints; }
            private set { joints = value; }
        }

        #endregion

        #region Internal events

        internal event SequenceReadyEventHandler SequenceReady;
        internal event GestureRecognizedEventHandler GestureRecognized;

        #endregion

        #region Constructor and public methods

        /// <summary>
        /// Creates instance of player.
        /// </summary>
        /// <param name="kinect">Instance of kinect senzor used for tracking.</param>
        /// <param name="index">Player index.</param>
        /// <param name="color">Player color.</param>
        public KinectGesturePlayer(KinectSensor kinect,  int index, Color color)
        {
            this.Color = color;
            this.Index = index;
            this.kinect = kinect;
            this.brush = new SolidColorBrush(color);

            trackedPoints = new Dictionary<JointType, Filter3D>();
            directionSequences = new Dictionary<JointType, DirectionSequence>();
            gestureListeners = new List<Gestures.IGesture>();
            forcedRecognition = new List<Gestures.IGesture>();
        }

        /// <summary>
        /// Register gesture for this player.
        /// </summary>
        /// <param name="gesture">Gesture to register.</param>
        /// <param name="forcedGestureRecognition">If true, thist gesture will be evaluated for every valid sequence and will be emited event for it. 
        /// This means that event wil be emited for thist gesture every possible time not depending on his current likehood or if this event have maximal likehooh.</param>
        public void RegisterGesture(Gestures.IGesture gesture, bool forcedGestureRecognition = false)
        {
            gesture.RegisterJointListeners(this);
            if (forcedGestureRecognition)
                forcedRecognition.Add(gesture);
        }

        /// <summary>
        /// Register player joint as tracked by specified gesture.
        /// </summary>
        /// <remarks>If joint in not used yet, creates also filter and sequence tracker for it.</remarks>
        /// <param name="type">Type of joint which should be tracked.</param>
        /// <param name="gesture">Gesture for which is joint registered.</param>
        public void RegisterJoint(JointType type, Gestures.IGesture gesture)
        {
            if (!trackedPoints.ContainsKey(type))
            {
                trackedPoints.Add(type, new Filter3D(3, 0.005f));
                directionSequences.Add(type, new DirectionSequence(type));
            }
            gestureListeners.Add(gesture);
        }
        
        /// <summary>
        /// Update player state based on passed skeleton data.
        /// </summary>
        /// <remarks>This method should be called for every prepared skeleton data frame. Also handles all recognition freatures and emits propper events.</remarks>
        /// <param name="skeletonData">Skeleton data for current player and frame.</param>
        public void Update(Skeleton skeletonData)
        {
            if (SkeletonTrackingState.Tracked == skeletonData.TrackingState)
            {
                //store joits
                Joints = skeletonData.Joints;
                tracking = true;
                //call function for joint updates
                this.UpdateJoints(Joints);                
            }
            else
                tracking = false;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Emits SequenceReady event.
        /// </summary>
        /// <remarks>Create copy of passed sequence, so it can be changed/cleared after call.</remarks>
        /// <param name="sequence">Sequence to be added into event arguments.</param>
        private void OnSequenceReady(DirectionSequence sequence)
        {
            //create copy of sequence (sequence is recycling in normal case) only if there if any listener
            if (SequenceReady != null)
                SequenceReady(this, new SequenceEventArgs(sequence.Clone()));
        }

        /// <summary>
        /// Emits GestureRecognized event.
        /// </summary>
        /// <param name="gesture">Recognized gesture.</param>
        /// <param name="probability">Gesture probability.</param>
        /// <param name="length">Gesture length.</param>
        /// <param name="forced">Was gesture forced?</param>
        private void OnGestureRecognized(Gestures.IGesture gesture, double probability, int length, bool forced = false)
        {
            //create copy of sequence (sequence is recycling in normal case) only if there if any listener
            if (GestureRecognized != null)
                GestureRecognized(this, new GestureEventArgs(this, gesture, probability, length, (probability >= gesture.ProbabilityThreshold), forced));
        }

        /// <summary>
        /// Internal method for joint update processing.
        /// </summary>
        /// <remarks>Needs to be finished to allow mutliple point gesture detection.</remarks>
        /// <param name="joints">Processed joints.</param>
        private void UpdateJoints(JointCollection joints)
        {
            //bool recognizeGesture = false; //TODO: Use in future

            //check all tracked joints
            foreach (var point in trackedPoints)
            {
                //add value to filter
                point.Value.AddPoint(joints[point.Key].Position);
                //relolve direction
                DirectionRecognition.Directions direction = DirectionRecognition.GetDirection(point.Value.ValueVector3D, point.Value.LastVector3D);
                //and add it to sequence
                directionSequences[point.Key].AddDirection(point.Key, direction);

                //every 10 frames (about half second) check sequnce state
                frameCounter++;
                if (frameCounter % 10 == 0)
                {
                    //if is last 1 second still
                    if (directionSequences[point.Key].IsStill(20, 0.10f)) 
                    {                        
                        // and try recognize gesture
                        // TODO: recognizeGesture = true; //add better multipoint gesture-finished detection mechanism 
                        // meanwhile is used single point tracking mechanism
                        RecognizeGestureForSingleSequences(point.Key, directionSequences[point.Key]);                        
                    }
                }
            }

            // TODO: Detect gesture end better for multiple tracked point in same time.
            // Currently is recognition performed for every still tracked point independently, so in set are only one sequence and its imposible to make multipe tracking joint,
            // however other parts on library are ready for it. 
            // So it needs to be finished and becouse of that are following lines commented out and replaced with sigle point tracking machanism in cycle above.

            //if (recognizeGesture)
            //    RecognizeGestureForMultipleSequences();
        }

        /// <summary>
        /// Try recognize multiple points gestures, meanwhile unused (check-out comments in UpdateJoints method). 
        /// </summary>
        private void RecognizeGestureForMultipleSequences()
        {
            Dictionary<JointType, int[]> observations = new Dictionary<JointType, int[]>();
            //for every sequnce
            foreach (var seq in directionSequences)
            {
                //is not sequence "empty"
                if (!seq.Value.IsMostlyStill())
                {
                    //cleanup sequence
                    seq.Value.Trim();

                    //emit evet on sequence ready
                    OnSequenceReady(seq.Value);

                    //fill up observation field
                    observations.Add(seq.Key, seq.Value.ToArray);
                }  

                //clear sequence
                seq.Value.Clear();
            }

            //TODO: Handle multiple point gestures eg when is gesture ready for recognition 
            //and how to handle one is part of another (eg is recognized some gesture inside another)
            if (observations.Count > 0)
                RecognizeGesture(observations);
        }

        /// <summary>
        /// Try to recognize single poitn gesture. Used instead of RecognizeGestureForMultipleSequences, unless propper gesture end detection with multiple points isnt finished.
        /// </summary>
        /// <param name="joint">Type of tracked joint.</param>
        /// <param name="seq">Recognized sequence.</param>
        private void RecognizeGestureForSingleSequences(JointType joint, DirectionSequence seq)
        {
            Dictionary<JointType, int[]> observations = new Dictionary<JointType, int[]>();

            //is not sequence "empty"
            if (!seq.IsMostlyStill())
            {
                //cleanup sequence
                seq.Trim();

                //emit evet on sequence ready
                OnSequenceReady(seq);

                //fill up observation field
                observations.Add(joint, seq.ToArray);

                //recognize
                if (observations.Count > 0)
                    RecognizeGesture(observations);
            }            

            //clear sequence
            seq.Clear();            
        }

        /// <summary>
        /// Recognize gesture for specified obsarvations data.
        /// </summary>
        /// <param name="observations">Observation (sequence) data.</param>
        private void RecognizeGesture(Dictionary<JointType, int[]> observations)
        {
            Gestures.IGesture recognizedGesture = null;

            //prepare probability check
            double maxProbability = Double.MinValue;
            //dictionary used to store probabilities and prevent multiple gesture.Calculation call
            Dictionary<Gestures.IGesture, double> gestureProbabilities = new Dictionary<Gestures.IGesture, double>(gestureListeners.Count);

            //check all gesture and find out maximal
            foreach (var gesture in gestureListeners)
            {
                //calculate and store result
                gestureProbabilities.Add(gesture, gesture.Calculate(observations));
                if (gestureProbabilities[gesture] > maxProbability)
                {
                    //new maximum
                    recognizedGesture = gesture;
                    maxProbability = gestureProbabilities[gesture];
                }
            }

            //have something recognized -> emit event
            if (recognizedGesture != null)
                OnGestureRecognized(recognizedGesture, gestureProbabilities[recognizedGesture], observations[recognizedGesture.PrimaryJoint].Length);

            //emit forced enets based on precached data
            foreach (var gesture in forcedRecognition)
            {
                //event is emited anyway, so dont emit its twice
                if (gesture != recognizedGesture)
                {
                    OnGestureRecognized(gesture, gestureProbabilities[gesture], observations[gesture.PrimaryJoint].Length, true);
                }
            }
        }

        #endregion

        #region Visualization methods

        /// <summary>
        /// Visualize player skeleton with his color.
        /// </summary>
        /// <param name="depthFrame">Depth frame data used for real world to 2D video conversion</param>
        /// <returns>List of UIElemets representing player skeleton.</returns>
        public List<UIElement> GetSkeletonVisual(DepthImageFrame depthFrame)
        {
            List<UIElement> uiElements = new List<UIElement>();

            if (joints == null || kinect == null || !tracking)
                return uiElements;

            //draw bones
            uiElements.Add(GetBodySegment(depthFrame, joints, brush, JointType.HipCenter, JointType.Spine, JointType.ShoulderCenter, JointType.Head));
            uiElements.Add(GetBodySegment(depthFrame, joints, brush, JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft));
            uiElements.Add(GetBodySegment(depthFrame, joints, brush, JointType.ShoulderCenter, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight));
            uiElements.Add(GetBodySegment(depthFrame, joints, brush, JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
            uiElements.Add(GetBodySegment(depthFrame, joints, brush, JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight));

            // Draw joints
            foreach (Joint joint in joints)
            {
                Point jointPos = GetPosition2DLocation(depthFrame, joint.Position);
                Line jointLine = new Line();
                jointLine.X1 = jointPos.X - 3;
                jointLine.X2 = jointLine.X1 + 6;
                jointLine.Y1 = jointLine.Y2 = jointPos.Y;
                jointLine.Stroke = jointColors[joint.JointType];
                jointLine.StrokeThickness = 6;
                uiElements.Add(jointLine);                
            }

            //draw tracked and filtered points as squeres
            foreach (var item in trackedPoints)
            {
                uiElements.Add(GetPointVisual(depthFrame, item.Value.Value));
            }

            return uiElements;
        }

        /// <summary>
        /// Visualise playr currently performed gesture (sequence of tracked points).
        /// </summary>
        /// <remarks>Multiple points uses same starting point and color, so should be used only if we track only one joint or needs to be rewrited.</remarks>
        /// <param name="centerX">X coordination on canvas where visualization begins.</param>
        /// <param name="centerY">Y coordination on canvas where visualization begins.</param> 
        /// <returns>List of UI elemets representing gesture.</returns>        
        public List<UIElement> GetGestureVisual(double centerX, double centerY)
        {
            List<UIElement> uiElements = new List<UIElement>();
            //only if player is tracked
            if (tracking)
            {
                foreach (var sequence in directionSequences)
                {
                    uiElements.AddRange(sequence.Value.Visualize(centerX, centerY, brush));
                }
            }
            return uiElements;
        }

        /// <summary>
        /// Visualize point as squere.
        /// </summary>
        /// <param name="depthFrame">Depth frame data.</param>
        /// <param name="point">Visualized point.</param>
        /// <returns>List of UIElemets representing point.</returns>
        private UIElement GetPointVisual(DepthImageFrame depthFrame, SkeletonPoint point)
        {
            Polyline polyline = new Polyline();

            Point jointPos = GetPosition2DLocation(depthFrame, point);

            polyline.Points.Add(new Point(jointPos.X - 6, jointPos.Y - 6));
            polyline.Points.Add(new Point(jointPos.X - 6, jointPos.Y + 6));
            polyline.Points.Add(new Point(jointPos.X + 6, jointPos.Y + 6));
            polyline.Points.Add(new Point(jointPos.X + 6, jointPos.Y - 6));
            polyline.Points.Add(new Point(jointPos.X - 6, jointPos.Y - 6));

            polyline.Stroke = Brushes.Red;
            polyline.StrokeThickness = 2;

            return polyline;
        }

        /// <summary>
        /// Convert real-world positions to 2D image coords.
        /// </summary>
        /// <param name="depthFrame">Datth frame used to conversion.</param>
        /// <param name="skeletonPosition">Point which position should be converted.</param>
        /// <returns>Point in 2D image.</returns>
        private Point GetPosition2DLocation(DepthImageFrame depthFrame, SkeletonPoint skeletonPosition)
        {
            DepthImagePoint depthPoint = depthFrame.MapFromSkeletonPoint(skeletonPosition);
            ColorImagePoint colorPoint = depthFrame.MapToColorImagePoint(depthPoint.X, depthPoint.Y, kinect.ColorStream.Format);

            // map back to skeleton.Width & skeleton.Height
            return new Point(colorPoint.X, colorPoint.Y);                
        }
        
        /// <summary>
        /// Create body segment. Based on SDK codes.
        /// </summary>
        /// <param name="depthFrame">Depth frame.</param>
        /// <param name="joints">All skeleton joints.</param>
        /// <param name="brush">Color.</param>
        /// <param name="ids">Joints to be drawned.</param>
        /// <returns>Polyline representing body segment.</returns>
        private Polyline GetBodySegment(DepthImageFrame depthFrame, JointCollection joints, Brush brush, params JointType[] ids)
        {
            PointCollection points = new PointCollection(ids.Length);
            for (int i = 0; i < ids.Length; ++i)
            {
                points.Add(GetPosition2DLocation(depthFrame, joints[ids[i]].Position));
            }

            Polyline polyline = new Polyline();
            polyline.Points = points;
            polyline.Stroke = brush;
            polyline.StrokeThickness = 5;
            return polyline;
        }

        #endregion

    }
}
