using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using KinectGestureDetection.Gestures;

namespace KinectGestureDetection
{
    /// <summary>
    /// Represents collection of 6 KinectGesturePlayer and their common interfaces 
    /// </summary>
    public class KinectGesturePlayerCollection
    {
        #region Fields

        private KinectSensor kinect;
        private List<UIElement> visualElements;
        private KinectGesturePlayer[] players = new KinectGesturePlayer[6];
        private DateTime lastUpdate;
        private bool visualUsed = false;
        private KinectGestureAPI api;

        #endregion

        #region Properties and public static fields

        /// <summary>
        /// List of Visual Elements representing visualization data (skeletons and points) for all players.
        /// </summary>
        public List<UIElement> VisualElements
        {
            get 
            {
                visualUsed = true;
                return visualElements; 
            }
            private set { visualElements = value; }
        }
        
        /// <summary>
        /// Represents set of colors for players.
        /// </summary>
        public static Color[] DefaultPlayerColors = { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Magenta, Colors.Cyan };

        #endregion

        #region Public methods and constructors

        /// <summary>
        /// Construct and initialize each KinectGesturePlayer instance and register their events.
        /// </summary>
        /// <param name="api">Instance of api using this collection.</param>
        /// <param name="playerColors">Array of 6 colors user for players visualization. If not set, uses dafaults.</param>
        public KinectGesturePlayerCollection(KinectGestureAPI api, Color[] playerColors = null)
        {
            this.api = api;
            this.kinect = api.Kinect;

            if (playerColors == null)
                playerColors = DefaultPlayerColors;

            for (int i = 0; i <= 5; i++)
            {
                players[i] = new KinectGesturePlayer(kinect, i, playerColors[i % 6]);
                players[i].SequenceReady += new SequenceReadyEventHandler(KinectGesturePlayerCollection_SequenceReady);
                players[i].GestureRecognized += new GestureRecognizedEventHandler(KinectGesturePlayerCollection_GestureRecognized);
            }

            visualElements = new List<UIElement>();
            lastUpdate = DateTime.MinValue;
        }        

        /// <summary>
        /// Update internal data states based on captured skeletons and depth frame. Should be called for every ready frame.
        /// </summary>
        /// <param name="skeletons">Array of skeleton data from kinect API.</param>
        /// <param name="depthFrame">Depth frame from kinect API.</param>
        public void Update(Skeleton[] skeletons, DepthImageFrame depthFrame)
        {
            VisualElements.Clear();

            for (int i = 0; i < Math.Min(skeletons.Length, 6); i++)
            {
                //pass data to every player
                players[i].Update(skeletons[i]);

                //visualize player data
                if (visualUsed)
                    VisualElements.AddRange(players[i].GetSkeletonVisual(depthFrame));
            }
            lastUpdate = DateTime.Now;

            //emit event and propagete it into API class
            api.OnFrameReady();
        }

        /// <summary>
        /// Register gesture for each player.
        /// </summary>
        /// <param name="gesture">Gesture to register.</param>
        /// <param name="forcedGestureRecognition">Force gesture to be recognized for each valid direction sequence 
        /// (event is emmited for this gesture every time, not just if it has max probability)</param>
        public void RegisterGestureForAll(IGesture gesture, bool forcedGestureRecognition = false)
        {            
            for (int i = 0; i <= 5; i++)
            {
                players[i].RegisterGesture(gesture, forcedGestureRecognition);             
            }
        }

        /// <summary>
        /// Creates gusture visualization for every player.
        /// </summary>
        /// <param name="centerX">X coordination on canvas where visualization begins.</param>
        /// <param name="centerY">Y coordination on canvas where visualization begins.</param>
        /// <returns>List of UIElements representing shape of gesture (current direction sequence).</returns>
        public List<UIElement> GetGestureVisual(double centerX, double centerY)
        {
            List<UIElement> visual = new List<UIElement>();
            for (int i = 0; i <= 5; i++)
            {
                visual.AddRange(players[i].GetGestureVisual(centerX, centerY));
            }
            return visual;
        }

        #endregion

        #region Private event handling

        private void KinectGesturePlayerCollection_GestureRecognized(object sender, GestureEventArgs e)
        {
            //propagete event into API class
            api.OnGestureRecognized(e);
        }

        private void KinectGesturePlayerCollection_SequenceReady(object sender, SequenceEventArgs e)
        {
            //propagete event into API class
            api.OnSequenceReady(e);
        }

        #endregion
    }
}
