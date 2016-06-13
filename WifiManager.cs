using SimpleWifi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace KiepRover
{
    class WifiManager
    {
        public interface WifiManagerListener
        {
            void NoWifiAvailable();
            void Disconnected();
            void ConnectedToDifferentNetwork(String ssid);
            void ConnectingToCloudRover();
            void CloudRoverNoProfile();
            void CloudRoverNotFound();
            void ConnectedToCloudRover();
            void Signal(int strength);
        }

        private const String CLOUD_ROVER_SSID = "iCloudRover_879";

        private Wifi wifi = new Wifi();
        Timer signalStrengthTimer = new Timer();
        Timer cloudRoverInRangeTimer = new Timer();
        Timer stillConnectedTimer = new Timer();

        private WifiManagerListener listener;
        private AccessPoint originalAccessPoint;


        public WifiManager()
        {
            signalStrengthTimer.Elapsed += SignalStrengthTimer_Elapsed;
            signalStrengthTimer.Interval = 1000;
            cloudRoverInRangeTimer.Elapsed += cloudRoverInRangeTimer_Elapsed;
            cloudRoverInRangeTimer.Interval = 2000;
            stillConnectedTimer.Elapsed += stillConnectedTimer_Elapsed;
            stillConnectedTimer.Interval = 2000;
        }

        public void SetListener(WifiManagerListener listener)
        {
            this.listener = listener;

            if (wifi.NoWifiAvailable)
            {
                listener.NoWifiAvailable();
            }
            else
            {
                try
                {
                    CheckNetwork();
                }
                catch (Exception)
                {
                    listener.NoWifiAvailable();
                }
            }
        }

        private void CheckNetwork()
        {
            if (wifi.ConnectionStatus == WifiStatus.Connected)
            {
                AccessPoint connectedAccessPoint = GetConnectedAccessPoint();
                if (connectedAccessPoint == null)
                {
                    listener.Disconnected();
                    ConnectToCloudRover();
                }
                else
                {
                    if (connectedAccessPoint.Name.Equals(CLOUD_ROVER_SSID))
                    {
                        listener.ConnectedToCloudRover();
                        signalStrengthTimer.Start();
                        stillConnectedTimer.Start();
                    }
                    else
                    {
                        listener.ConnectedToDifferentNetwork(connectedAccessPoint.Name);
                        ConnectToCloudRover();
                    }
                }
            }
            else
            {
                listener.Disconnected();
                ConnectToCloudRover();
            }
        }

        private AccessPoint GetConnectedAccessPoint()
        {
            try
            {
                foreach (AccessPoint accessPoint in wifi.GetAccessPoints())
                {
                    if (accessPoint.IsConnected)
                    {
                        return accessPoint;
                    }
                }
            }
            catch (Exception) {
                listener.NoWifiAvailable();
            }
            return null;
        }

        private void ConnectToCloudRover()
        {
            AccessPoint connectedAccessPoint = GetConnectedAccessPoint();
            if (connectedAccessPoint != null && connectedAccessPoint.Name.Equals(CLOUD_ROVER_SSID))
            {
                listener.ConnectedToCloudRover();
                signalStrengthTimer.Start();
                stillConnectedTimer.Start();
                return;
            }

            AccessPoint cloudRoverAccessPoint = GetCloudRoverAccessPoint();
            if (cloudRoverAccessPoint != null)
            {
                originalAccessPoint = connectedAccessPoint;
                ConnectToCloudRover(cloudRoverAccessPoint);
            }
            else
            {
                listener.CloudRoverNotFound();
                cloudRoverInRangeTimer.Start();
            }
        }

        void cloudRoverInRangeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            cloudRoverInRangeTimer.Stop();
            ConnectToCloudRover();
        }

        private void ConnectToCloudRover(AccessPoint cloudRoverAccessPoint)
        {
            if (!cloudRoverAccessPoint.HasProfile)
            {
                listener.CloudRoverNoProfile();
                return;
            }

            listener.ConnectingToCloudRover();
            AuthRequest authRequest = new AuthRequest(cloudRoverAccessPoint);
            cloudRoverAccessPoint.ConnectAsync(authRequest, false, OnConnectedComplete);
        }

        private void OnConnectedComplete(bool success)
        {
            CheckNetwork();
        }

        private AccessPoint GetCloudRoverAccessPoint()
        {
            try
            {
                foreach (AccessPoint accessPoint in wifi.GetAccessPoints())
                {
                    if (accessPoint.Name.Equals(CLOUD_ROVER_SSID))
                    {
                        return accessPoint;
                    }
                }
            }
            catch (Exception) {
                listener.NoWifiAvailable();
            }
            return null;
        }

        public void ConnectToOriginal()
        {
            signalStrengthTimer.Stop();
            stillConnectedTimer.Stop();

            if (originalAccessPoint == null)
            {
                return;
            }

            try {
                AccessPoint connectedAccessPoint = GetConnectedAccessPoint();
                if (connectedAccessPoint != null && connectedAccessPoint.Name.Equals(originalAccessPoint.Name))
                {
                    listener.ConnectedToDifferentNetwork(connectedAccessPoint.Name);
                    return;
                }

                AuthRequest authRequest = new AuthRequest(originalAccessPoint);
                originalAccessPoint.ConnectAsync(authRequest);
            } 
            catch (Exception)
            {
                listener.NoWifiAvailable();
            }
        }

        private void SignalStrengthTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            AccessPoint accessPoint = GetConnectedAccessPoint();
            if (accessPoint != null) 
            { 
                int signalStrength = Convert.ToInt32(accessPoint.SignalStrength);
                listener.Signal(signalStrength);
            }
        }

        void stillConnectedTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            AccessPoint connectedAccessPoint = GetConnectedAccessPoint();
            if (connectedAccessPoint == null || !connectedAccessPoint.Name.Equals(CLOUD_ROVER_SSID))
            {
                stillConnectedTimer.Stop();
                CheckNetwork();
            }
        }
    }
}
