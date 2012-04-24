using System;
using System.Collections.Generic;
using System.Linq;
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

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                kinect = new KinectGestureAPI(0, false);

                int gestureID = 0;

                kinect.RegisterGesture(new GenericOnePoint(gestureID++), "Gestures/RightHand/Circle.gdf");
                kinect.RegisterGesture(new GenericOnePoint(gestureID++), "Gestures/RightHand/Wave.gdf");
                kinect.RegisterGesture(new GenericOnePoint(gestureID++), "Gestures/RightHand/WaveInFront.gdf");
                kinect.RegisterGesture(new GenericOnePoint(gestureID++), "Gestures/RightHand/Take.gdf");

                timer.Tick += new EventHandler(timer_Tick);
                timer.Interval = new TimeSpan(0,0,3);

            }
            catch (GestureStateException ex)
            {
                MessageBox.Show(ex.Message, "Error on gesture loading", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            catch (KinectStateException ex)
            {
                MessageBox.Show(ex.Message, "Error on kinect initailizazion", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            catch (Exception ex){
                MessageBox.Show(ex.Message, "Error on startup", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            

            kinect.Video.ImageReady += new VideoImageReadyEventHandler(VideoImageReady);
            kinect.FrameReady += new FrameReadyEventHandler(SkeletonFrame_FrameReady);
            kinect.GestureRecognized += new GestureRecognizedEventHandler(Players_GestureRecognized);
 
        }

        void timer_Tick(object sender, EventArgs e)
        {
            gestureInfo.Content = "";
            gestureProbability.Content = "";
        }


        void Players_GestureRecognized(object sender, GestureEventArgs e)
        {
            timer.Stop();
            gestureInfo.Content = e.Gesture.Name;
            gestureProbability.Content = String.Format("Probability: {0} \nPlayer: {1} \nLength: {2} \nValid: {3}", e.Probability, e.SourcePlayer.Index, e.Length, e.IsValid.ToString());
            gestureHistoryListBox.Items.Insert(0, e);
            timer.Start();
        }

        
        void SkeletonFrame_FrameReady(object sender)
        {
            video.Children.Clear();
            foreach (var item in kinect.Players.VisualElements)
            {
                video.Children.Add(item);
            }

            if (counter++ % 5 == 0)
            {
                gestureCanvas.Children.Clear();
                foreach (var item in kinect.Players.GetGestureVisual(160, 120))
                {
                    gestureCanvas.Children.Add(item);
                }
            }
        }

        void VideoImageReady(object sender)
        {
            video.Background = new ImageBrush(kinect.VideoSource);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            kinect.Dispose();
        }

    }
}
