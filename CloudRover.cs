using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Imaging;

namespace KiepRover
{
    class CloudRover
    {
        public interface CloudRoverListener
        {
            void BatteryLevel(int level);
            void ReceivedImage(BitmapImage image);
        }

        private const String UDP_IP = "192.168.1.1";
        private const int CONTROL_PORT = 5201;
        private const int VIDEO_PORT = 5207;
        private const String MESSAGE_INIT_VIDEO = "VIEW\x00\x00\x00\x00Q-F";
        private const String MESSAGE_KEEPALIVE = "KEEPALIVE";
        private const String MESSAGE_GO = "GO\x00\x00\x00\x00\x00\x00";
        private const String MESSAGE_BACK = "BACK\x00\x00\x00\x00";
        private const String MESSAGE_STOP = "STOP\x00\x00\x00\x00";
        private const String MESSAGE_LEFT = "LEFT\x00\x00\x00\x00";
        private const String MESSAGE_RIGHT = "RIGHT\x00\x00\x00\x00";
        private const String MESSAGE_CAMERA_UP = "CAMUP\x00\x00\x00\x00";
        private const String MESSAGE_CAMERA_DOWN = "CAMDOWN\x00\x00";
        private const String MESSAGE_SILENT_ON = "SILENTON\x00\x00\x00\x00";
        private const String MESSAGE_SILENT_OFF = "SILENTOFF\x00\x00\x00\x00";
        private const String MESSAGE_IR_LED_ON = "INFRALEDON\x00\x00\x00\x00";
        private const String MESSAGE_IR_LED_OFF = "INFRALEDOFF\x00\x00\x00\x00";
        private const int JPG_BUFFER_SIZE = 100000;

        private CloudRoverListener listener;

        private Timer sendMovementTimer = new Timer();
        private String activeMovement = "";

        private Timer keepaliveTimer = new Timer();
        private Stopwatch stopwatch = new Stopwatch();
        private UdpClient controlClient = new UdpClient(CONTROL_PORT);
        private UdpClient jpgClient = new UdpClient(VIDEO_PORT);
        private IPEndPoint controlRemoteIpEndPoint = new IPEndPoint(IPAddress.Any, CONTROL_PORT);
        private IPEndPoint jpgRemoteIpEndPoint = new IPEndPoint(IPAddress.Any, VIDEO_PORT);

        private byte[] jpgBuffer = new byte[JPG_BUFFER_SIZE];
        private int bytesInJpgBuffer = 0;

        public CloudRover()
        {
            sendMovementTimer.Elapsed += SendMovementTimer_Elapsed;
            sendMovementTimer.Interval = 80;
            keepaliveTimer.Elapsed += SendVideoKeepalive;
            keepaliveTimer.Interval = 1000;
        }

        private void SendMovementTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SendMessage(activeMovement);
        }

        public void MoveForward()
        {
            activeMovement = MESSAGE_GO;
            SendMessage(MESSAGE_GO);
            sendMovementTimer.Start();
        }

        public void MoveBackward()
        {
            activeMovement = MESSAGE_BACK;
            SendMessage(MESSAGE_BACK);
            sendMovementTimer.Start();
        }

        public void RotateLeft()
        {
            activeMovement = MESSAGE_LEFT;
            SendMessage(MESSAGE_LEFT);
            sendMovementTimer.Start();
        }

        public void RotateRight()
        {
            activeMovement = MESSAGE_RIGHT;
            SendMessage(MESSAGE_RIGHT);
            sendMovementTimer.Start();
        }

        public void Stop()
        {
            sendMovementTimer.Stop();
            SendMessage(MESSAGE_STOP);
        }

        public void CameraUp()
        {
            activeMovement = MESSAGE_CAMERA_UP;
            SendMessage(MESSAGE_CAMERA_UP);
            sendMovementTimer.Start();
        }

        public void CameraDown()
        {
            activeMovement = MESSAGE_CAMERA_DOWN;
            SendMessage(MESSAGE_CAMERA_DOWN);
            sendMovementTimer.Start();
        }

        public void InfraredLedOn()
        {
            SendMessage(MESSAGE_IR_LED_ON);
        }

        public void InfraredLedOff()
        {
            SendMessage(MESSAGE_IR_LED_OFF);
        }

        public void SilentOn()
        {
            SendMessage(MESSAGE_SILENT_ON);
        }

        public void SilentOff()
        {
            SendMessage(MESSAGE_SILENT_OFF);
        }

        public void SetListener(CloudRoverListener listener)
        {
            if (listener == null)
            {
                keepaliveTimer.Stop();
                this.listener = null;
                return;
            }

            if (this.listener == null)
            {
                controlClient.BeginReceive(new AsyncCallback(ControlReceiveCallback), null);
                jpgClient.BeginReceive(new AsyncCallback(JpgReceiveCallback), null);
                keepaliveTimer.Start();
            }

            this.listener = listener;
            stopwatch.Start();
            SendMessage(MESSAGE_INIT_VIDEO);
        }

        private void SendVideoKeepalive(object sender, ElapsedEventArgs e)
        {
            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                if (listener != null)
                {
                    listener.ReceivedImage(null);
                }

                stopwatch.Restart();
                SendMessage(MESSAGE_INIT_VIDEO);
            }

            SendMessage(MESSAGE_KEEPALIVE);
        }

        private void SendMessage(String message)
        {
            byte[] packet = Encoding.ASCII.GetBytes(message);
            controlClient.Send(packet, packet.Length, UDP_IP, CONTROL_PORT);
        }

        private void ControlReceiveCallback(IAsyncResult result)
        {
            byte[] received = controlClient.EndReceive(result, ref controlRemoteIpEndPoint);
            ProcessControlMessage(received);

            if (listener != null)
            {
                controlClient.BeginReceive(new AsyncCallback(ControlReceiveCallback), null);
            }
        }

        private void ProcessControlMessage(byte[] data)
        {
            try
            {
                String message = Encoding.ASCII.GetString(data);
                var regex = new Regex(@"BATLEVEL=(\d)");
                Match match = regex.Match(message);
                if (match.Success)
                {
                    int level = Convert.ToInt32(match.Groups[1].Value);
                    int percentage = (7 - level) * 100 / 7;

                    if (listener != null)
                    {
                        listener.BatteryLevel(percentage);
                    }
                }
            }
            catch (Exception) { }
        }
        
        private void JpgReceiveCallback(IAsyncResult result)
        {
            byte[] received = jpgClient.EndReceive(result, ref jpgRemoteIpEndPoint);
            ProcessJpgData(received);
            if (listener != null)
            {
                jpgClient.BeginReceive(new AsyncCallback(JpgReceiveCallback), null);
            }
        }

        private void ProcessJpgData(byte[] data)
        {
            // While length == 403 -> append to buffer (excluding first 3 bytes)

            if (data.Length <= 3)
            {
                return;
            }

            if (bytesInJpgBuffer + data.Length - 3 < JPG_BUFFER_SIZE)
            {
                System.Buffer.BlockCopy(data, 3, jpgBuffer, bytesInJpgBuffer, data.Length - 3);
                bytesInJpgBuffer += data.Length - 3;
            }
            else
            {
                Console.WriteLine("Buffer full");
            }

            if (data.Length != 403)
            {
                stopwatch.Restart();

                BitmapImage image = ToImage(jpgBuffer, bytesInJpgBuffer);
                if (listener != null && image != null)
                {
                    listener.ReceivedImage(image);
                }

                jpgBuffer = new byte[100000];
                bytesInJpgBuffer = 0;
            }
        }

        private BitmapImage ToImage(byte[] array, int size)
        {
            using (var ms = new System.IO.MemoryStream(array, 0, size))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
