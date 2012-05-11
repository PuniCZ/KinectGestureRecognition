using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinectGestureDetection;
using KinectGestureDetection.Gestures;
using System.Windows.Threading;

namespace KinnectGestureDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectGestureAPI kinect;
        DispatcherTimer timer = new DispatcherTimer(); 
        int counter;

        /// <summary>
        /// Window constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                kinect = new KinectGestureAPI(0, false);

                //gesture counter
                int gestureID = 0;

                //add all gestures in gesture folder
                string[] files = Directory.GetFiles("Gestures", "*.gdf", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    kinect.RegisterGesture(new GenericOnePoint(gestureID++), file);
                }
            }
            catch (GestureStateException ex)
            {
                MessageBox.Show(ex.Message, "Error on gesture loading", MessageBoxButton.OK, MessageBoxImage.Error);

                if (kinect != null)
                    kinect.Dispose();

                Environment.Exit(1);
            }
            catch (KinectStateException ex)
            {
                MessageBox.Show(ex.Message, "Error on kinect initailizazion", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error on startup", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            //register events
            kinect.Video.ImageReady += new VideoImageReadyEventHandler(VideoImageReady);
            kinect.FrameReady += new FrameReadyEventHandler(SkeletonFrame_FrameReady);
            kinect.GestureRecognized += new GestureRecognizedEventHandler(Players_GestureRecognized);

            //timer for gesture info hide
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 3);
        }
        
        /// <summary>
        /// Timer event for cleanup gesture info.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void timer_Tick(object sender, EventArgs e)
        {
            gestureInfo.Content = "";
            gestureProbability.Content = "";
        }

        /// <summary>
        /// Event handler for gesture recognition.
        /// </summary>
        /// <param name="sender">Source kinect gesture recognition api instance.</param>
        /// <param name="recognitionInfo">Info about recognition.</param>
        private void Players_GestureRecognized(object sender, GestureEventArgs recognitionInfo)
        {
            timer.Stop();
            gestureInfo.Content = recognitionInfo.Gesture.Name;
            if (recognitionInfo.IsValid)
                gestureInfo.Foreground = Brushes.Green;
            else
                gestureInfo.Foreground = Brushes.Red;

            gestureProbability.Content = String.Format(
                "Probability: {0:0.0} \nPlayer: {1} \nLength: {2} \nValid: {3}", 
                recognitionInfo.Probability, 
                recognitionInfo.SourcePlayer.Index, 
                recognitionInfo.Length, 
                recognitionInfo.IsValid.ToString()
            );
            gestureHistoryListBox.Items.Insert(0, recognitionInfo);
            timer.Start();
        }

        /// <summary>
        /// Event handler for skeleton frame ready.
        /// </summary>
        /// <param name="sender">Source kinect gesture recognition api instance.</param>
        private void SkeletonFrame_FrameReady(object sender)
        {
            video.Children.Clear();
            foreach (var item in kinect.Players.VisualElements)
            {
                video.Children.Add(item);
            }

            //visualize gesture only every 5th frame
            if (counter++ % 5 == 0)
            {
                gestureCanvas.Children.Clear();
                foreach (var item in kinect.Players.GetGestureVisual(160, 120))
                {
                    gestureCanvas.Children.Add(item);
                }
            }
        }

        /// <summary>
        /// Event handler for video frame ready.
        /// </summary>
        /// <param name="sender">Source kinect gesture recognition api instance.</param>
        private void VideoImageReady(object sender)
        {
            video.Background = new ImageBrush(kinect.Video.ImageSource);
        }

        /// <summary>
        /// Event handler on window close.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Window_Closed(object sender, EventArgs e)
        {
            kinect.Dispose();
        }

    }
}
