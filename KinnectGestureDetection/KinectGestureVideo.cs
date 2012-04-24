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

namespace KinectGestureDetection
{
    /// <summary>
    /// Delegate for handling ImageReady event of KinectGestureVideo class.
    /// </summary>
    /// <param name="sender"></param>
    public delegate void VideoImageReadyEventHandler(object sender);

    /// <summary>
    /// Class representing kinec video output and containing basic video drawing mathods.
    /// </summary>
    public class KinectGestureVideo
    {

        #region Fields

        private KinectSensor kinect;
        private byte[] pixelData;
        private ColorImageFormat lastImageFormat = ColorImageFormat.Undefined;
        private WriteableBitmap outputImage;
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        #endregion

        #region Properties

        /// <summary>
        /// Represents video image output.
        /// </summary>
        public BitmapSource ImageSource
        {
            get { return outputImage; }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event emited when video image data is ready.
        /// </summary>
        public event VideoImageReadyEventHandler ImageReady;

        #endregion

        #region Public methods and consructor

        /// <summary>
        /// Creates new instance of video output class.
        /// </summary>
        /// <param name="kinect">Kinect device used for video capture.</param>
        public KinectGestureVideo(KinectSensor kinect)
        {
            this.kinect = kinect;
            kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(ColorImageReady);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Emit ImageReady event.
        /// </summary>
        private void OnImageReady()
        {
            if (ImageReady != null)
                ImageReady(this);
        }
        
        /// <summary>
        /// Callback for ImageReady event of kinect API. Contains basic image conversions and creates image data ready for WPF. Based on SDK.
        /// </summary>
        /// <param name="sender">Sende object.</param>
        /// <param name="e">Event arguments.</param>
        private void ColorImageReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame != null)
                {
                    // We need to detect if the format has changed.
                    bool haveNewFormat = this.lastImageFormat != imageFrame.Format;

                    if (haveNewFormat)
                    {
                        this.pixelData = new byte[imageFrame.PixelDataLength];
                    }

                    imageFrame.CopyPixelDataTo(this.pixelData);

                    // A WriteableBitmap is a WPF construct that enables resetting the Bits of the image.
                    // This is more efficient than creating a new Bitmap every frame.
                    if (haveNewFormat)
                    {
                        this.outputImage = new WriteableBitmap(
                            imageFrame.Width,
                            imageFrame.Height,
                            96,  // DpiX
                            96,  // DpiY
                            PixelFormats.Bgr32,
                            null);
                    }

                    this.outputImage.WritePixels(
                        new Int32Rect(0, 0, imageFrame.Width, imageFrame.Height),
                        this.pixelData,
                        imageFrame.Width * Bgr32BytesPerPixel,
                        0);

                    this.lastImageFormat = imageFrame.Format;
                    OnImageReady();
                }
            }
        }

        #endregion
    }
}
