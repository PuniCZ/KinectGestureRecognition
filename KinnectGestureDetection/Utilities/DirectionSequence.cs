using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;


namespace KinectGestureDetection
{
    /// <summary>
    /// Class representing sequence of direction data and basic operations on them.
    /// </summary>
    public class DirectionSequence
    {
        #region Fields

        private List<DirectionRecognition.Directions> sequence;
        private JointType type;

        #endregion

        #region Properties

        /// <summary>
        /// Gets this sequence converted to array of int.
        /// </summary>
        public int[] ToArray
        {
            get { return Array.ConvertAll<DirectionRecognition.Directions, int>(sequence.ToArray(), value => DirectionRecognition.ToInt(value)); }
        }

        /// <summary>
        /// Gets lenght of this sequence.
        /// </summary>
        public int Length
        {
            get { return sequence.Count; }
        }

        #endregion

        #region Public methods and constructor

        /// <summary>
        /// Creates new instance of DirectionSequence.
        /// </summary>
        /// <param name="sequenceType">Allow add only directions related to this type.</param>
        public DirectionSequence(JointType sequenceType)
        {
            sequence = new List<DirectionRecognition.Directions>(20);
            type = sequenceType;
        }

        /// <summary>
        /// Add specified direction to sequence.
        /// </summary>
        /// <remarks>Allows add only directions with (in constructor) specified type. If types did not match, direction would not be added.</remarks>
        /// <param name="type">Type of direction related point.</param>
        /// <param name="direction">Direction that should be added to this sequence.</param>
        public void AddDirection(JointType type, DirectionRecognition.Directions direction)
        {
            if (type == this.type)
                sequence.Add(direction);
        }

        public void Clear()
        {
            sequence.Clear();
        }

        /// <summary>
        /// Test last "numberOfTested" directions, that less than "threshold" percent occurs has other type than direction than None.
        /// </summary>
        /// <param name="numberOfTested">Number of tested values from the end of sequence.</param>
        /// <param name="threshold">Rate of allowed invalid moves (other than None) - 1f means 100%</param>
        /// <returns></returns>
        public bool IsStill(int numberOfTested, float threshold)
        {
            if (sequence.Count < numberOfTested)
                numberOfTested = sequence.Count;

            DirectionRecognition.Directions[] tested = new DirectionRecognition.Directions[numberOfTested];
            sequence.CopyTo(sequence.Count - numberOfTested, tested, 0, numberOfTested);

            if (tested.Count(d => d != DirectionRecognition.Directions.None) / (double)numberOfTested < threshold)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Test sequence if it's mostly still (have no direction in most than 80% cases).
        /// </summary>
        /// <returns>True if it's still, false otherwise.</returns>
        public bool IsMostlyStill()
        {
            if ((sequence.Count(value => value == DirectionRecognition.Directions.None) / (double)sequence.Count) > 0.8)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Trim sequence for still part on begining and end (basicly detect begin and end of gesture itself).
        /// </summary>
        public void Trim()
        {
            if (IsMostlyStill())
            {
                sequence.Clear();
                return;
            }
            int range = -1;
            int countOther = 0;

            //trim beggining
            for (int i = 0; i < sequence.Count; i++)
            {
                if (sequence[i] != DirectionRecognition.Directions.None)
                {
                    countOther++;
                    range++;
                }
                else
                    range = -1;

                if (i >= range && (range > 2 || (i > 3 && countOther / (double)i > 0.3)))
                {
                    sequence.RemoveRange(0, i - range);
                    break;
                }
            }

            range = -1;
            countOther = 0;

            //trim end
            for (int i = sequence.Count - 1; i >= 0; i--)
            {
                if (sequence[i] != DirectionRecognition.Directions.None)
                {
                    countOther++;
                    range++;
                }
                else
                    range = -1;

                if ((sequence.Count - i) >= range && (range > 2 || ((sequence.Count - i) > 3 && countOther / (double)(sequence.Count - i) > 0.3)))
                {
                    sequence.RemoveRange(i + range, sequence.Count - (i + range));
                    break;
                }
            }
        }

        /// <summary>
        /// Convert sequence to string.
        /// </summary>
        /// <returns>String representing the sequence.</returns>
        public override string ToString()
        {
            StringBuilder str = new StringBuilder(type.ToString() + " ");

            foreach (var item in sequence)
            {
                str.Append(item.ToString());
                str.Append(' ');
            }

            return str.ToString();
        }

        /// <summary>
        /// Load sequence from string.
        /// </summary>
        /// <param name="data">Sequence string.</param>
        /// <returns>True if loaded sucessfully, else otherwise.</returns>
        public bool FromString(string data)
        {
            sequence.Clear();
            string[] records = data.Split(' ');

            if (!Enum.TryParse<JointType>(records[0], out type))
                return false;

            for (int i = 1; i < records.Length; i++)
            {
                DirectionRecognition.Directions dir;
                if (Enum.TryParse<DirectionRecognition.Directions>(records[i], out dir))
                    sequence.Add(dir);
                else
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Create deep copy of this sequence.
        /// </summary>
        /// <returns>Copy of sequence.</returns>
        public DirectionSequence Clone()
        {
            DirectionSequence newSeq = new DirectionSequence(this.type);
            newSeq.sequence = new List<DirectionRecognition.Directions>(this.sequence);
            return newSeq;
        }

        /// <summary>
        /// Visualize the sequence.
        /// </summary>
        /// <param name="centerX">X coordination on canvas where visualization begins.</param>
        /// <param name="centerY">Y coordination on canvas where visualization begins.</param>
        /// <param name="color">Color of visual elements.</param>
        /// <returns>List of UIElements representing sequence.</returns>
        public List<UIElement> Visualize(double centerX, double centerY, SolidColorBrush color = null)
        {
            List<UIElement> uiElements = new List<UIElement>();

            double currX = centerX;
            double currY = centerY;
                      
            foreach (var direction in sequence)
            {
                if (direction == DirectionRecognition.Directions.None)
                    continue;
                                
                Line line = new Line();

                double x = 0;
                double y = 0;

                if ((direction & DirectionRecognition.Directions.Right) != 0)
                    x += 5;
                if ((direction & DirectionRecognition.Directions.Left) != 0)
                    x -= 5;
                if ((direction & DirectionRecognition.Directions.Top) != 0)
                    y -= 5;
                if ((direction & DirectionRecognition.Directions.Bottom) != 0)
                    y += 5;
                
                line.X1 = currX;
                line.X2 = currX + x;
                line.Y1 = currY;
                line.Y2 = currY + y;

                if (color == null)
                    line.Stroke = Brushes.LightGreen;
                else
                    line.Stroke = color;

                line.StrokeThickness = 3;

                currX += x;
                currY += y;
                uiElements.Add(line);
            }

            return uiElements;
        }
                
        

        #endregion
    }
}
