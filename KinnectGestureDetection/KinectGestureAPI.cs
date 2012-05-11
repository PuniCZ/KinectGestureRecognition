using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using KinectGestureDetection.Gestures;
using Microsoft.Kinect;

namespace KinectGestureDetection
{
    public class KinectGestureAPI: IDisposable
    {

        #region Fields

        private KinectSensor kinect;
        private readonly Dictionary<KinectSensor, bool> sensorIsInitialized = new Dictionary<KinectSensor, bool>();

        private Dictionary<IGesture, bool> gestureStore;

        private KinectGestureVideo video;
        private KinectGesturePlayerCollection players;
        private bool disposed = false;
        private Skeleton[] skeletonData;
        private bool usedLowResolution = false;

        #endregion

        #region Events

        /// <summary>
        /// Emited when frame procesing is completed and visualization data is ready.
        /// </summary>
        /// <remarks>Dont have any special event arguments, only sending object (KinectGesturePlayerCollection class instance).</remarks>
        public event FrameReadyEventHandler FrameReady;

        /// <summary>
        /// Emited when complete not empty and not still sequence of directions is finished.
        /// </summary>
        /// <remarks>Event arguments (SequenceEventArgs) contains recorded sequence. This sequence is clone of the recorded one.</remarks>
        /// <seealso cref="SequenceEventArgs"/>
        public event SequenceReadyEventHandler SequenceReady;

        /// <summary>
        /// Emited, when gesture is recognized.
        /// </summary>
        /// <remarks>Event arguments (GestureEventArgs) contains important gesture related information like source player, gesture itsels, probability 
        /// of succefully recognition, gesture length (for actualy recognized gesture of course) and if gesture was emited as forced one.</remarks>
        /// <seealso cref="GestureEventArgs"/>
        public event GestureRecognizedEventHandler GestureRecognized;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the kinect senzor device. When setting this property, checks, is there is initialized kinect device, uninitialize it and
        /// prepare and initialize the new one por purpouses of this library.
        /// </summary>
        /// <example>All you need to do is just use the folowing code: Kinect = KinectSensor.KinectSensors[newKinectID];</example>
        /// <exception cref="KinectStateException">On uncessfully initialize kinect device.</exception>
        public KinectSensor Kinect
        {
            get
            {
                return kinect;
            }

            set
            {
                //uninitailize if there was one
                if (kinect != null)
                {
                    bool wasInitialized;
                    sensorIsInitialized.TryGetValue(kinect, out wasInitialized);
                    if (wasInitialized)
                    {
                        UninitializeKinectServices(kinect);
                        sensorIsInitialized[kinect] = false;
                    }
                }

                //initialize new one
                kinect = value;
                if (kinect != null)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        kinect = InitializeKinectServices(kinect);
                        if (kinect != null)
                        {
                            kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(KinectAllFramesReady);
                            sensorIsInitialized[kinect] = true;
                            //create players collection for this kinect device
                            players = new KinectGesturePlayerCollection(this);
                            ReregisterAllGesturesForNewKinectDevice();
                        }
                    }                    
                    else
                        throw new KinectStateException("Unable to succefully initialize current kinect device.");
                }
            }
        }

        
        /// <summary>
        /// Gets instance of class representing video stream. If there is no one, creates it.
        /// </summary>
        public KinectGestureVideo Video
        {
            get
            {
                if (video == null)
                    video = new KinectGestureVideo(kinect);
                return video;
            }
        }

        /// <summary>
        /// Gets instance of class representing collection of all players binded to current active kinnect device.
        /// </summary>
        public KinectGesturePlayerCollection Players
        {
            get
            {
                return players;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of kinect gesture API.
        /// </summary>
        /// <param name="kinectID">ID of kinect device connected to this computer. 0 means first, 1 second...</param>
        /// <param name="useLowDepthResolution">Set to true on older and slower computers. Should lower CPU process time to analyze image (but not so much).</param>
        /// <exception cref="KinectStateException">If there is no kinect device connected or kinectID didn't match any connected kinect device.</exception>
        public KinectGestureAPI(int kinectID = 0, bool useLowDepthResolution = false)
        {
            gestureStore = new Dictionary<IGesture, bool>();

            if (KinectSensor.KinectSensors.Count == 0)
                throw new KinectStateException("There is no kinect device connected to this computer.");

            if (KinectSensor.KinectSensors.Count < kinectID)
                throw new KinectStateException(String.Format("There is no kinect device with id {0} connected to this computer.", kinectID));

            usedLowResolution = useLowDepthResolution; //needs to be set befor first kinect initialization

            try
            {
                Kinect = KinectSensor.KinectSensors[kinectID]; //also initialize it in setter
            }
            catch
            {                
                throw;
            }
            
        }

        /// <summary>
        /// Destructor (uninitialize kinect device)
        /// </summary>
        ~KinectGestureAPI()
        {
            Dispose(false);
        }

        #endregion

        #region Public methods
        
        /// <summary>
        /// Register passed gesture instance to be recognized by this API. Gesture can be any type implementing interface IGesture. 
        /// This function should used for gestures, that don't need external datafile or for creating new generic gestures.
        /// </summary>
        /// <param name="gesture">Instance of gesture. Must impement IGesture interface.</param>
        /// <param name="forcedGestureRecognition">If true, thist gesture will be evaluated for every valid sequence and will be emited event for it. 
        /// This means that event wil be emited for thist gesture every possible time not depending on his current likehood or if this event have maximal likehooh.</param>
        /// <returns>Returns passed gesture.</returns>
        /// <seealso cref="IGesture"/>
        /// <exception cref="KinectStateException">Gesture with this id already registered.</exception>
        public IGesture RegisterGesture(IGesture gesture, bool forcedGestureRecognition = false)
        {
            if (gestureStore.Select(gest => gest.Key.Id == gesture.Id).Contains(true))
                throw new KinectStateException("Cannot register two gestures with same ID.");

            gestureStore.Add(gesture, forcedGestureRecognition);
            players.RegisterGestureForAll(gesture, forcedGestureRecognition);
            return gesture;
        }

        /// <summary>
        /// Register passed gesture instance to be recognized by this API. Gesture can be any type implementing interface IGesture. 
        /// This function should used for gestures, that require external datafile to load his configuration.
        /// </summary>
        /// <param name="gesture">Instance of gesture. Must impement IGesture interface.</param>
        /// <param name="gestureDataFile">Path to file containing gesture definition or configuration data.</param>
        /// <param name="forcedGestureRecognition">If true, thist gesture will be evaluated for every valid sequence and will be emited event for it. 
        /// This means that event wil be emited for thist gesture every possible time not depending on his current likehood or if this event have maximal likehooh.</param>
        /// <returns>Returns passed gesture.</returns>
        /// <seealso cref="IGesture"/>
        /// <exception cref="KinectStateException">Gesture with this id already registered.</exception>
        /// <exception cref="GestureStateException">On some error in gesture initialization or loading.</exception>
        public IGesture RegisterGesture(IGesture gesture, string gestureDataFile, bool forcedGestureRecognition = false)
        {
            if (gestureStore.Select(gest => gest.Key.Id == gesture.Id).Contains(true))
                throw new KinectStateException("Cannot register two gestures with same ID.");

            gestureStore.Add(gesture, forcedGestureRecognition);
            gesture.LoadData(gestureDataFile);
            players.RegisterGestureForAll(gesture, forcedGestureRecognition);
            return gesture;
        }

        #endregion

        #region Private and internal methods

        /// <summary>
        /// Reregister formerly registered gestures again for new kinect device.
        /// </summary>
        private void ReregisterAllGesturesForNewKinectDevice()
        {
            if (players == null)
                return;

            foreach (var gesture in gestureStore)
            {
                players.RegisterGestureForAll(gesture.Key, gesture.Value);
            }
        }

        /// <summary>
        /// Called when frame is ready.
        /// </summary>
        internal void OnFrameReady()
        {
            if (FrameReady != null)
                FrameReady(this);
        }

        /// <summary>
        /// Called when sequence is ready.
        /// </summary>
        internal void OnSequenceReady(SequenceEventArgs e)
        {
            if (SequenceReady != null)
                SequenceReady(this, e);
        }

        /// <summary>
        /// Called when gesture is recognized.
        /// </summary>
        internal void OnGestureRecognized(GestureEventArgs e)
        {
            if (GestureRecognized != null)
                GestureRecognized(this, e);
        }

        #endregion


        #region Internal kinect methods (mostly based on API reference solutions)

        /// <summary>
        /// Initialize kinect device. This method is based on kinect SDK reference code.
        /// </summary>
        /// <param name="sensor">Instance of kinect device to initialize.</param>
        /// <returns>Initialized kinect device or null if initialization failed.</returns>
        private KinectSensor InitializeKinectServices(KinectSensor sensor)
        {
            // Centralized control of the formats for Color/Depth
            if (usedLowResolution)
            {                
                sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            }
            else
            {
                sensor.DepthStream.Enable(DepthImageFormat.Resolution80x60Fps30);
            }

            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.SkeletonStream.Enable();
            // Start streaming
            try
            {
                sensor.Start();
            }
            catch (IOException)
            {
                return null;
            }

            return sensor;
        }

        /// <summary>
        /// Uninitialize kinect device. This method is based on kinect SDK reference code.
        /// </summary>
        /// <param name="sensor">Devide to be uninitialized</param>
        private void UninitializeKinectServices(KinectSensor sensor)
        {
            // Stop streaming
            sensor.Stop();

            // Disable skeletonengine, as only one Kinect can have it enabled at a time.
            if (sensor.SkeletonStream != null)
            {
                sensor.SkeletonStream.Disable();
            }
        }

        /// <summary>
        /// Callback for processing data from kinect device, when they are ready.
        /// </summary>
        /// <param name="sender">Kinect device, which has data ready.</param>
        /// <param name="e">Event arguments.</param>
        private void KinectAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // Have we already been "shut down" by the user of this viewer, 
            // or has the SkeletonStream been disabled since this event was posted?
            if ((this.Kinect == null) || !(((KinectSensor)sender).SkeletonStream.IsEnabled || ((KinectSensor)sender).ColorStream.IsEnabled))
                return;

            bool haveSkeletonData = false;

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    if ((this.skeletonData == null) || (this.skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        //number of tracked skeletons changed
                        this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    //copy current data to local variable (needs to be done to prevent canging them during processing)
                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);

                    haveSkeletonData = true;
                }
            }

            //if there is skeleton data
            if (haveSkeletonData)
            {
                using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
                {
                    //and depth frame
                    if (depthImageFrame != null)
                    {
                        //pass received data to all instances of players
                        players.Update(this.skeletonData, depthImageFrame);
                    }
                }
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose (destruct) this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose (destruct) this object. Method based on MSDN source.
        /// </summary>
        /// <param name="disposing">True if dispose called manualy, not from GC.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    //component.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                if (Kinect != null)
                    Kinect = null;

                // Note disposing has been done.
                disposed = true;

            }
        }

        #endregion
    }
}
