using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KiepRover
{
    /*
        TODO:
        - Log to file

        Ideas:
        - Screenshot / record video
        - Better JPG validation
        - Better wifi range scale
        - Mouse control
    */

    public partial class MainWindow : Window, CloudRover.CloudRoverListener, WifiManager.WifiManagerListener
    {
        private const string LOGFILE = "KiepRover.log";

        CloudRover cloudRover = new CloudRover();
        WifiManager wifiManager = new WifiManager();

        public MainWindow()
        {
            InitializeComponent();

#if !DEBUG
            this.Topmost = true;
            this.Focus();
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
#endif

            Log("Start");

            wifiManager.SetListener(this);
        }

        #region CloudRover UI updating

        private void ShowImage(BitmapImage image)
        {
            imgCam.Source = image;

            if (image != null)
            {
                tbLogging.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                tbLogging.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ShowBatteryLevel(int level)
        {
            Log("Battery level = " + level);
            batteryLevel.Value = level;

            // Disable glow animation on progress bar
            var glow = batteryLevel.Template.FindName("PART_GlowRect", batteryLevel) as FrameworkElement;
            if (glow != null) glow.Visibility = Visibility.Hidden;
        }

        #endregion

        #region WifiManager UI updating

        private void ShowConnectedToCloudRover()
        {
            Log("Connected to CloudRover");
            ShowLog("Connected to CloudRover wifi");
            signalStrength.IsIndeterminate = false;
            btnCameraUp.Visibility = System.Windows.Visibility.Visible;
            btnCameraDown.Visibility = System.Windows.Visibility.Visible;
            //tbLogging.Visibility = System.Windows.Visibility.Hidden;

            cloudRover.SetListener(this);
        }

        void ShowConnectedToDifferentNetwork(String ssid)
        {
            Log("Connected to " + ssid);
            ShowLog("Connected to wifi " + ssid);
            signalStrength.IsIndeterminate = true;
            signalStrength.Value = 0;
            batteryLevel.Value = 0;
            imgCam.Source = null;
            btnCameraUp.Visibility = System.Windows.Visibility.Hidden;
            btnCameraDown.Visibility = System.Windows.Visibility.Hidden;
            tbLogging.Visibility = System.Windows.Visibility.Visible;
        }

        private void ShowConnectingToCloudRover()
        {
            Log("Connecting to CloudRover");
            ShowLog("Connecting to CloudRover wifi...");
            signalStrength.IsIndeterminate = true;
            signalStrength.Value = 0;
            batteryLevel.Value = 0;
            imgCam.Source = null;
            btnCameraUp.Visibility = System.Windows.Visibility.Hidden;
            btnCameraDown.Visibility = System.Windows.Visibility.Hidden;
            tbLogging.Visibility = System.Windows.Visibility.Visible;
        }

        private void ShowDisconnected()
        {
            Log("Disconnected");
            ShowLog("Wifi disconnected");
            signalStrength.IsIndeterminate = true;
            signalStrength.Value = 0;
            batteryLevel.Value = 0;
            imgCam.Source = null;
            btnCameraUp.Visibility = System.Windows.Visibility.Hidden;
            btnCameraDown.Visibility = System.Windows.Visibility.Hidden;
            tbLogging.Visibility = System.Windows.Visibility.Visible;
        }

        private void ShowNoWifiAvailable()
        {
            Log("No wifi available");
            ShowLog("No wifi available");
            signalStrength.IsIndeterminate = false;
            signalStrength.Value = 0;
            batteryLevel.Value = 0;
            imgCam.Source = null;
            btnCameraUp.Visibility = System.Windows.Visibility.Hidden;
            btnCameraDown.Visibility = System.Windows.Visibility.Hidden;
            tbLogging.Visibility = System.Windows.Visibility.Visible;
        }

        private void ShowSignal(int strength)
        {
            Log("Signal strength = " + strength);
            signalStrength.Value = strength;

            // Disable glow animation on progress bar
            var glow = signalStrength.Template.FindName("PART_GlowRect", signalStrength) as FrameworkElement;
            if (glow != null) glow.Visibility = Visibility.Hidden;
        }

        void ShowCloudRoverNotFound()
        {
            Log("CloudRover wifi not found");
            ShowLog("CloudRover wifi not found...");
            signalStrength.IsIndeterminate = true;
            signalStrength.Value = 0;
            batteryLevel.Value = 0;
            imgCam.Source = null;
            btnCameraUp.Visibility = System.Windows.Visibility.Hidden;
            btnCameraDown.Visibility = System.Windows.Visibility.Hidden;
            tbLogging.Visibility = System.Windows.Visibility.Visible;

            // Enable glow animation for indeterminate progress bar
            var glow = signalStrength.Template.FindName("PART_GlowRect", signalStrength) as FrameworkElement;
            if (glow != null) glow.Visibility = Visibility.Visible;
        }

        private void ShowCloudRoverNoProfile()
        {
            Log("CloudRover no profile");
            ShowLog("First time, connect with Windows to CloudRover wifi with password \"12345\"");
            signalStrength.IsIndeterminate = false;
            signalStrength.Value = 0;
            batteryLevel.Value = 0;
            imgCam.Source = null;
            btnCameraUp.Visibility = System.Windows.Visibility.Hidden;
            btnCameraDown.Visibility = System.Windows.Visibility.Hidden;
            tbLogging.Visibility = System.Windows.Visibility.Visible;
        }

        void ShowLog(String text)
        {
            tbLogging.AppendText(text + "\n");
            tbLogging.ScrollToEnd();
        }

        #endregion

        #region UI event handling

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Escape:
                    Log("Key close");
                    this.Close();
                    break;
                case Key.Up:
                    Log("Key forward");
                    cloudRover.MoveForward();
                    break;
                case Key.Down:
                    Log("Key backward");
                    cloudRover.MoveBackward();
                    break;
                case Key.Left:
                    Log("Key left");
                    cloudRover.RotateLeft();
                    break;
                case Key.Right:
                    Log("Key right");
                    cloudRover.RotateRight();
                    break;
                case Key.PageUp:
                case Key.A:
                    Log("Key camera up");
                    cloudRover.CameraUp();
                    break;
                case Key.PageDown:
                case Key.Z:
                    Log("Key camera down");
                    cloudRover.CameraDown();
                    break;
                case Key.S:
                    Log("Key LED on");
                    cloudRover.InfraredLedOn();
                    break;
                case Key.X:
                    Log("Key LED off");
                    cloudRover.InfraredLedOff();
                    break;
                case Key.D:
                    Log("Key silent on");
                    cloudRover.SilentOn();
                    break;
                case Key.C:
                    Log("Key silent off");
                    cloudRover.SilentOff();
                    break;
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Up))
            {
                cloudRover.MoveForward();
            }
            else if (Keyboard.IsKeyDown(Key.Down))
            {
                cloudRover.MoveBackward();
            }
            else if (Keyboard.IsKeyDown(Key.Left))
            {
                cloudRover.RotateLeft();
            }
            else if (Keyboard.IsKeyDown(Key.Right))
            {
                cloudRover.RotateRight();
            }
            else
            {
                Log("Key stop");
                cloudRover.Stop();
            }
        }

        private void btnCameraUp_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Log("Mouse camera up");
            cloudRover.CameraUp();
        }

        private void btnCameraUp_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Log("Mouse camera stop");
            cloudRover.Stop();
        }

        private void btnCameraDown_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Log("Mouse camera down");
            cloudRover.CameraDown();
        }

        private void btnCameraDown_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Log("Mouse camera stop");
            cloudRover.Stop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cloudRover.SetListener(null);
            wifiManager.ConnectToOriginal();
        }

        #endregion

        #region CloudRover callbacks -> shift to UI thread

        void CloudRover.CloudRoverListener.ReceivedImage(BitmapImage image)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowImage(image);
            }));
        }

        void CloudRover.CloudRoverListener.BatteryLevel(int level)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowBatteryLevel(level);
            }));
        }

        #endregion

        #region WifiManager callbacks -> shift to UI thread

        void WifiManager.WifiManagerListener.ConnectedToCloudRover()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowConnectedToCloudRover();
            }));
        }

        void WifiManager.WifiManagerListener.ConnectedToDifferentNetwork(String ssid)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowConnectedToDifferentNetwork(ssid);
            }));
        }

        void WifiManager.WifiManagerListener.ConnectingToCloudRover()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowConnectingToCloudRover();
            }));
        }

        void WifiManager.WifiManagerListener.Disconnected()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowDisconnected();
            }));
        }

        void WifiManager.WifiManagerListener.NoWifiAvailable()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowNoWifiAvailable();
            }));
        }

        void WifiManager.WifiManagerListener.CloudRoverNotFound()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowCloudRoverNotFound();
            }));
        }

        void WifiManager.WifiManagerListener.Signal(int strength)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowSignal(strength);
            }));
        }

        void WifiManager.WifiManagerListener.CloudRoverNoProfile()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                ShowCloudRoverNoProfile();
            }));
        }

        #endregion

        private void Log(string text)
        {
            try
            {
                if (text != "")
                {
                    string baseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    StreamWriter cachefile = File.AppendText(baseDir + "\\" + LOGFILE);
                    cachefile.WriteLine(DateTime.Now + "\t" + text);
                    cachefile.Close();
                }
            }
            catch (Exception) { }
        }
    }
}
