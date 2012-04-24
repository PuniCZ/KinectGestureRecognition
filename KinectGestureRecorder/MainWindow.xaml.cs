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
using Microsoft.Kinect;
using KinectGestureDetection;
using KinectGestureDetection.Gestures;

namespace KinectGestureRecorder
{    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectGestureAPI kinect;
        //Timer timer = new Timer(200); 
        int counter;

        GenericOnePoint gesture;
        int gestureIndex;
        bool paused = false;

        public MainWindow()
        {
            InitializeComponent();

            foreach (var item in Enum.GetValues(typeof(JointType)))
            {
                cmbPoinType.Items.Add(item);
            }
            cmbPoinType.SelectedItem = JointType.HandRight;



            try
            {
                kinect = new KinectGestureAPI();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error on startup", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }            

            kinect.Video.ImageReady += new VideoImageReadyEventHandler(VideoImageReady);
            kinect.FrameReady += new FrameReadyEventHandler(SkeletonFrame_FrameReady);
            kinect.SequenceReady += new SequenceReadyEventHandler(SkeletonFrame_SequenceReady);
        }

        void SkeletonFrame_SequenceReady(object sender, EventArgs e)
        {
            if (gesture == null || paused)
                return;

            DirectionSequence sequence = ((SequenceEventArgs)e).Sequence;

            sequencesList.Items.Add(sequence);

            int sum = 0;
            foreach (DirectionSequence item in sequencesList.Items)
	        {
                sum += item.Length;
	        }

            if (sequencesList.Items.Count > 0)
                txtShowAproxLen.Text = (sum / sequencesList.Items.Count).ToString();
            else
                txtShowAproxLen.Text = "0";

        }

        void SkeletonFrame_FrameReady(object sender)
        {
            videoCanvas.Children.Clear();
            foreach (var item in kinect.Players.VisualElements)
            {
                videoCanvas.Children.Add(item);
            }
                       

            if (counter++ % 5 == 0)
            {
                if (paused || gesture == null)
                    return;

                gestureCanvas.Children.Clear();                

                foreach (var item in kinect.Players.GetGestureVisual(160, 120))
                {
                    gestureCanvas.Children.Add(item);
                }
            }

        }

        void VideoImageReady(object sender)
        {
            videoCanvas.Background = new ImageBrush(kinect.VideoSource);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            kinect.Dispose();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (gesture == null)
            {
                gesture = new GenericOnePoint(gestureIndex++);
                gesture.Initialize(txtGestureName.Text, (JointType)cmbPoinType.SelectedItem, Int32.Parse(txtNumOfStates.Text));
                kinect.RegisterGesture(gesture);
                button1.Content = "Stop capture";
            }
            else
            {
                //todo: realy unregister gesture from kinnect
                gesture = null;
                button1.Content = "Start new capture";
                sequencesList.Items.Clear();
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            paused = !paused;

            if (paused)
            {
                button2.Content = "Resume current capture";
            }
            else
            {
                button2.Content = "Pause current capture";
            }
        }

        private void sequencesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!paused || gesture == null)
                return;

            gestureCanvas.Children.Clear();

            if (sequencesList.SelectedItem == null)
                return;

            foreach (var item in ((DirectionSequence)sequencesList.SelectedItem).Visualize(160, 120))
            {
                gestureCanvas.Children.Add(item);
            }

            Dictionary<JointType, int[]> observations = new Dictionary<JointType, int[]>();
            observations.Add((JointType)cmbPoinType.SelectedItem, ((DirectionSequence)sequencesList.SelectedItem).ToArray);
            txtSeqProp.Text = gesture.Calculate(observations).ToString();

        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            while (sequencesList.SelectedIndex != -1)
                sequencesList.Items.RemoveAt(sequencesList.SelectedIndex);
            
            
            int sum = 0;
            foreach (DirectionSequence item in sequencesList.Items)
            {
                sum += item.Length;
            }
            if (sequencesList.Items.Count > 0)
                txtShowAproxLen.Text = (sum / sequencesList.Items.Count).ToString();
            else
                txtShowAproxLen.Text = "0";
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            
            foreach (var item in sequencesList.Items)
            {
                DirectionSequence seq = (DirectionSequence)item;
                Dictionary<JointType, int[]> observations = new Dictionary<JointType, int[]>();    
                observations.Add((JointType)cmbPoinType.SelectedItem, seq.ToArray);
                gesture.Learn(observations);
            }            

        }

        private void button5_Click_1(object sender, RoutedEventArgs e)
        {
            foreach (var item in sequencesList.SelectedItems)
            {
                DirectionSequence seq = (DirectionSequence)item;
                Dictionary<JointType, int[]> observations = new Dictionary<JointType, int[]>();
                observations.Add((JointType)cmbPoinType.SelectedItem, seq.ToArray);
                gesture.Learn(observations);
            }         
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".gdf"; // Default file extension
            dlg.Filter = "Gesture definition (.gdf)|*.gdf"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                gesture = new GenericOnePoint(gestureIndex++);
                gesture.LoadData(filename);
                kinect.RegisterGesture(gesture);
                txtPropThre.Text = gesture.ProbabilityThreshold.ToString();
                txtGestureName.Text = gesture.Name;
                txtGestureLength.Text = gesture.AproxLength.ToString();
                foreach (JointType item in Enum.GetValues(typeof(JointType)))
                {
                    if (gesture.UseJoint(item))
                    {
                        cmbPoinType.SelectedItem = item;
                        break;
                    }
                }                
                button1.Content = "Stop capture";
            }
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = gesture.Name; // Default file name
            dlg.DefaultExt = ".gdf"; // Default file extension
            dlg.Filter = "Gesture definition (.gdf)|*.gdf"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;
                gesture.ProbabilityThreshold = Double.Parse(txtPropThre.Text);
                gesture.AproxLength = Int32.Parse(txtGestureLength.Text);
                gesture.SaveTrainedData(filename);
            }
        }


    }
}
