using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace KinectGestureDetection
{
    /// <summary>
    /// Class used for vector based values to discreete combination of directions in threee planes.
    /// </summary>
    public static class DirectionRecognition
    {
        /// <summary>
        /// Enum representing individual directions and also ints combinations.
        /// </summary>
        /// <remarks>For combinating values is used bitfields and logical sum (or) so they can be used as bit flags. Contains only static members.</remarks>
        public enum Directions
        {
            None         = 0,
            
            Top          = 0x100,
            Bottom       = 0x200,
            Left         = 0x010,
            Right        = 0x020,
            Front        = 0x001,
            Back         = 0x002,

            TopLeft      = Top | Left,
            TopRight     = Top | Right,
            TopFront     = Top | Front,
            TopBack      = Top | Back,
            BottomLeft   = Bottom | Left,
            BottomRight  = Bottom | Right,
            BottomFront  = Bottom | Front,
            BottomBack   = Bottom | Back,
            FrontLeft    = Front | Left,
            FrontRight   = Front | Right,
            BackLeft     = Back | Left,
            BackRight    = Back | Right,

            TopLeftFront     = Top | Left | Front,
            TopLeftBack      = Top | Left | Back,
            TopRightFront    = Top | Right | Front,
            TopRightBack     = Top | Right | Back,
            BottomLeftFront  = Bottom | Left | Front,
            BottomLeftBack   = Bottom | Left | Back,
            BottomRightFront = Bottom | Right | Front,
            BottomRightBack  = Bottom | Right | Back,
        }

        /// <summary>
        /// Represent count of direction sequences.
        /// </summary>
        public static int DirectionsCount { get { return Enum.GetValues(typeof(DirectionRecognition.Directions)).Length; } }
        
        /// <summary>
        /// Used for Direction to int conversion.
        /// </summary>
        private static Dictionary<Directions, int> enumTranslations = new Dictionary<Directions, int>();

        /// <summary>
        /// Static constructor.
        /// </summary>
        /// <remarks>Prepare data for Direction to int conversion.</remarks>
        static DirectionRecognition()
        {
            int index = 0;
            foreach (var item in Enum.GetValues(typeof(DirectionRecognition.Directions)))
            {
                enumTranslations.Add((DirectionRecognition.Directions)item, index++);
            }
        }

        /// <summary>
        /// Convert direction to his int value. 
        /// </summary>
        /// <param name="dir">Direction to convert.</param>
        /// <returns>Direction coverted to int.</returns>
        public static int ToInt(Directions dir)
        {
            return enumTranslations[dir];
        }

        /// <summary>
        /// Returns substraction of two points as direction. 
        /// </summary>
        /// <remarks>
        /// Result can be combination of one to three directions, depengind oh they relation. 
        /// As first and major direction is chose the one, which has then most difference size. 
        /// The second and third are adden only if they have their diff size bigger than hals of the first (the bigger one). If not, this direction is eliminated. 
        /// </remarks>
        /// <param name="currentPoint">Currently received point.</param>
        /// <param name="lastPoint">Recently received point.</param>
        /// <returns>Decoded direction.</returns>
        public static Directions GetDirection(Vector3D currentPoint, Vector3D lastPoint)
        {
            return GetDirection(Vector3D.Subtract(currentPoint, lastPoint));
        }

        /// <summary>
        /// Method used to recognize direction from vector.
        /// </summary>
        /// <param name="dirVector">Vector representing position change to transform.</param>
        /// <returns>Decoded direction.</returns>
        private static Directions GetDirection(Vector3D dirVector)
        {
            if (dirVector.Length == 0)
                return Directions.None;

            Directions direction = Directions.None;
            
            Dictionary<Directions, double> directionOrder = new Dictionary<Directions, double>();

            //convert diff to direction
            if (dirVector.X > 0)
                directionOrder.Add(Directions.Right, dirVector.X);
            else
                directionOrder.Add(Directions.Left, dirVector.X);

            if (dirVector.Y > 0)
                directionOrder.Add(Directions.Top, dirVector.Y);
            else
                directionOrder.Add(Directions.Bottom, dirVector.Y);

            if (dirVector.Z > 0)
                directionOrder.Add(Directions.Back, dirVector.Z);
            else
                directionOrder.Add(Directions.Front, dirVector.Z);
                        
            KeyValuePair<Directions, double> firstDirection = new KeyValuePair<Directions, double>(Directions.None, 0);
            KeyValuePair<Directions, double> secondDirection = new KeyValuePair<Directions, double>(Directions.None, 0);
            KeyValuePair<Directions, double> thirdDirection = new KeyValuePair<Directions, double>(Directions.None, 0);
            
            //get there most used directions
            foreach (var item in directionOrder)
            {
                if (Math.Abs(item.Value) > Math.Abs(firstDirection.Value))
                {
                    thirdDirection = secondDirection;
                    secondDirection = firstDirection;
                    firstDirection = item;
                }
                else if (Math.Abs(item.Value) > Math.Abs(secondDirection.Value))
                {
                    thirdDirection = secondDirection;
                    secondDirection = item;
                }
                else if (Math.Abs(item.Value) > Math.Abs(thirdDirection.Value))
                {
                    thirdDirection = item;
                }
            }

            //combine them
            direction |= firstDirection.Key;

            if (Math.Abs(firstDirection.Value) < Math.Abs(secondDirection.Value * 2))
                direction |= secondDirection.Key;

            if (Math.Abs(firstDirection.Value) < Math.Abs(thirdDirection.Value * 2))
                direction |= thirdDirection.Key;

            return direction;
        }

    }
}
