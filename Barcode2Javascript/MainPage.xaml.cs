using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Browser;
using System.Threading;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;

namespace Barcode2Javascript
{
    //Register whole type as accessible from script
    [ScriptableType]
    public partial class MainPage : UserControl
    {
        //Register the custom event to be as accessible from script
        [ScriptableMember]
        public event EventHandler<CustomEventHandler> BarcodeRead;

        public CaptureSource captureSource;

        // brush for the video feed
        public VideoBrush webcamBrush;

        public MainPage()
        {
            InitializeComponent();

            //Register this instance to be accessible from script
            HtmlPage.RegisterScriptableObject("Barcode2Javascript", this);

            // Create the CaptureSource.
            captureSource = new CaptureSource();
            captureSource.CaptureImageCompleted += new EventHandler<CaptureImageCompletedEventArgs>(captureSource_CaptureImageCompleted);

            // Prevent displays from peeking through the expanded webcam display
            capturedBarcodes.SetValue(Canvas.ZIndexProperty, -1);
            capturedImage.SetValue(Canvas.ZIndexProperty, -1);  
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (captureSource.State != CaptureState.Started)
            {
                // Set the video capture source the WebCam.
                captureSource.VideoCaptureDevice =
                   CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice();

                // set the source on the VideoBrush used to display the video
                webcamBrush = new VideoBrush();
                webcamBrush.SetSource(captureSource);

                // Set the Fill property of the Rectangle to the VideoBrush.
                webcamDisplay.Fill = webcamBrush;

                // Request access to device and verify the VideoCaptureDevice is not null.
                if (CaptureDeviceConfiguration.RequestDeviceAccess() && captureSource.VideoCaptureDevice != null)
                {
                    try
                    {
                        captureSource.Start();
                        captureSource.CaptureImageAsync();
                        StartButton.Content = "Stop Camera";
                    }
                    catch (Exception)
                    {
                        // Notify user that the webcam could not be started.
                        MessageBox.Show("There was a problem starting the webcam \n" +
                                        "If using a Mac, verify default device settings.\n" +
                                        "Right click app to access the Configuration settings.");
                    }
                }
                else
                {
                    MessageBox.Show("Could not start Webcam. Verify device is connected " +
                                    "and privacy permission allow access to device.");
                }
            }
            else if (captureSource.State == CaptureState.Started)
            {
                webcamDisplay.Fill = new SolidColorBrush();
                captureSource.Stop();
                StartButton.Content = "Start Camera";
            }
        }

        void captureSource_CaptureImageCompleted(object sender, CaptureImageCompletedEventArgs e)
        {
            com.google.zxing.qrcode.QRCodeReader qrRead = new com.google.zxing.qrcode.QRCodeReader();
            //This is like a platform neutral way of identifying colors in an image
            RGBLuminanceSource luminiance = new RGBLuminanceSource(e.Result, e.Result.PixelWidth, e.Result.PixelHeight);
            //The next 2 things are used to change color to black and white to be read by the reader
            com.google.zxing.common.HybridBinarizer binarizer = new com.google.zxing.common.HybridBinarizer(luminiance);
            com.google.zxing.BinaryBitmap binBitmap = new com.google.zxing.BinaryBitmap(binarizer);
            com.google.zxing.Result results = default(com.google.zxing.Result);
            try
            {
                //barcode found
                results = qrRead.decode(binBitmap);

                capturedBarcodes.Items.Insert(0, new ScannedImage(results.Text, e.Result));
                capturedBarcodes.SelectedIndex = 0;
                mediaElement1.Stop();
                mediaElement1.Play();

                ImageBrush brush = new ImageBrush();
                brush.ImageSource = e.Result;
                capturedImage.Fill = brush;
            }
            catch (com.google.zxing.ReaderException)
            {
                //no barcode found
                if (captureSource.State == CaptureState.Started)
                {
                    captureSource.CaptureImageAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                StartButton_Click(this, new RoutedEventArgs());
            }

            try
            {
                BarcodeRead(this, new CustomEventHandler() { Barcode = results.Text });
            }
            catch (Exception)
            {
                //no javascript event attached
            }
        }

        private void capturedBarcodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageBrush brush = new ImageBrush();
            brush.ImageSource = ((sender as ListBox).SelectedItem as ScannedImage).Image;
            capturedImage.Fill = brush;
        }

        private void TestAudio_Click(object sender, RoutedEventArgs e)
        {
            mediaElement1.Stop();
            mediaElement1.Play();
        }

        private void mediaElement1_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (captureSource.State == CaptureState.Started)
            {
                captureSource.CaptureImageAsync();
            }
        }

        private void webcamDisplay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Rectangle r = (sender as Rectangle);
            if (r.Width == 200)
            {
                r.Width = userControl.ActualWidth;
                r.Height = userControl.ActualHeight;
                Grid.SetColumnSpan(r, 2);
                Grid.SetRowSpan(r, 2);
                Grid.SetColumn(r, 0);
                Grid.SetRow(r, 0);
                customIcon.Source = new BitmapImage(new Uri("Images/zoom_out.png", UriKind.Relative));
            }
            else
            {
                r.Width = 200;
                r.Height = 200;
                Grid.SetColumnSpan(r, 1);
                Grid.SetRowSpan(r, 1);
                Grid.SetColumn(r, 1);
                Grid.SetRow(r, 0);
                customIcon.Source = new BitmapImage(new Uri("Images/zoom_in.png", UriKind.Relative));
            }
        }

        private void webcamDisplay_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point pt = e.GetPosition(canvas1);
            customIcon.SetValue(Canvas.LeftProperty, pt.X);
            customIcon.SetValue(Canvas.TopProperty, pt.Y);
        }

        private void webcamDisplay_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            customIcon.Visibility = System.Windows.Visibility.Visible;
        }

        private void webcamDisplay_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            customIcon.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    [ScriptableType]
    public class CustomEventHandler : EventArgs
    {
        public string Barcode { get; set; }
    }

    public class ScannedImage : Object
    {
        public ScannedImage(string barcode, ImageSource img) { Barcode = barcode; Image = img; Timestamp = DateTime.Now; }
        public DateTime Timestamp { get; set; } 
        public string Barcode { get; set; }
        public ImageSource Image { get; set; }
        public override string ToString() { return Timestamp.ToString("HH:mm:ss") + " - " + Barcode; }
    }
}

