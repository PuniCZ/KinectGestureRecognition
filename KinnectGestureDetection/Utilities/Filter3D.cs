using System;
using System.Windows.Media.Media3D;
using Microsoft.Kinect;

namespace KinectGestureDetection
{
    /// <summary>
    /// Class representing filter for one SkeletonPoint.
    /// </summary>
    class Filter3D
    {

        #region Fields

        private SkeletonPoint[] history;
        private int size;
        private float threshold;
        private int actPos;
        private SkeletonPoint outputValue = new SkeletonPoint();
        private SkeletonPoint lastValue = new SkeletonPoint();

        #endregion

        #region Properties

        /// <summary>
        /// Output filtered value as SkeletonPoint.
        /// </summary>
        public SkeletonPoint Value { get { return outputValue; } }

        /// <summary>
        /// Output filtered value as Vector3D.
        /// </summary>
        public Vector3D ValueVector3D { get { return new Vector3D(outputValue.X, outputValue.Y, outputValue.Z); } }

        /// <summary>
        /// Used to store last value as SkeletonPoint.
        /// </summary>
        /// <remarks>Value is equal with LastVector3D.</remarks>
        public SkeletonPoint Last { get { return lastValue; } private set { lastValue = value; } }

        /// <summary>
        /// Used to store last value as Vector3D.
        /// </summary>
        /// <remarks>Value is equal with Last.</remarks>
        public Vector3D LastVector3D { get { return new Vector3D(lastValue.X, lastValue.Y, lastValue.Z); } }

        #endregion

        #region Public methods and constructors

        /// <summary>
        /// Create new instance of filter.
        /// </summary>
        /// <param name="size">Number of items in history.</param>
        /// <param name="threshold">Minimal range which change output value.</param>
        /// <remarks>
        /// Greather size means bigger stability, but longer reaction time.
        /// Greather range means bigger stability, but longer reaction time and bigger change reaction distance.
        /// </remarks>
        public Filter3D(int size, float threshold = 1f)
        {
            this.size = size;
            this.threshold = threshold;
            actPos = 0;
            history = new SkeletonPoint[size];
        }

        /// <summary>
        /// Add new point to filter.
        /// </summary>
        /// <param name="point">Point to be added.</param>
        public void AddPoint(SkeletonPoint point)
        {
            history[actPos] = point;
            ComputeOutput();
            actPos = (actPos + 1) % size;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Provides internal fiter functionality, like handling history and thresholds.
        /// </summary>
        private void ComputeOutput()
        {
            float sumX = 0f, sumY = 0f, sumZ = 0f;
            int pointCount = size;
            //handle history
            for (int i = 0; i < size; i++)
            {
                if (history[i] != null)
                {
                    int multiple = 1;
                    if ((actPos + i) % size < 2)
                    {
                        multiple = 5;
                        pointCount += 4;
                    }
                    else if ((actPos + i) % size < 4)
                    {
                        multiple = 3;
                        pointCount += 2;
                    }
                    sumX += history[i].X * multiple;
                    sumY += history[i].Y * multiple;
                    sumZ += history[i].Z * multiple;
                }
                else
                    pointCount--;
            }

            //dont use threshold for last point
            Last = outputValue; 

            //handle threshold
            float tmpVal;
            tmpVal = sumX / pointCount;
            if (Math.Abs(outputValue.X - tmpVal) > threshold)
                outputValue.X = tmpVal;
            tmpVal = sumY / pointCount;
            if (Math.Abs(outputValue.Y - tmpVal) > threshold)
                outputValue.Y = tmpVal;
            tmpVal = sumZ / pointCount;
            if (Math.Abs(outputValue.Z - tmpVal) > threshold)
                outputValue.Z = tmpVal;
        }

        #endregion       

    }
}
