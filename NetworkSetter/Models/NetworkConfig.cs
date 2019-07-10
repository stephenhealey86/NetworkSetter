using NetworkSetter.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetworkSetter.Models
{
    public class NetworkConfig : INotifyPropertyChanged
    {
        #region Private Variables
        private string name = "";
        private string ipAddress = "0.0.0.0";
        private string subnetAddress = "0.0.0.0";
        private string gatewayAddress = "0.0.0.0";
        private string selectedNetworkAdapter = null;
        #endregion

        #region Public Variables
        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged(); }
        }

        public List<string> NetworkAdapters => GetNetworkAdapters();

        public string SelectedNetworkAdapter
        {
            get { return selectedNetworkAdapter; }
            set { selectedNetworkAdapter = value; OnPropertyChanged(); }
        }

        public string IPAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; OnPropertyChanged(); }
        }
        public string SubnetAddress
        {
            get { return subnetAddress; }
            set { subnetAddress = value; OnPropertyChanged(); }
        }
        public string GatewayAddress
        {
            get { return gatewayAddress; }
            set { gatewayAddress = value; OnPropertyChanged(); }
        }
        public EventHandler<NetworkErrorEventArgs> NetworkSettingsErrorEvent;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ValidForm
        {
            get { return Name.Length < 3; }
        }
        public bool ValidConfig
        {
            get { return ValidIPAddress(); }
        }
        #endregion

        #region Constructor
        public NetworkConfig()
        {

        }
        #endregion

        #region Helpers
        private void OnNetworkNetworkSettingsErrorEvent(NetworkErrorEventArgs eventArgs)
        {
            NetworkSettingsErrorEvent?.Invoke(this, eventArgs);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private List<string> GetNetworkAdapters()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            var list = new List<string>() { null };
            list.AddRange((from adapter in adapters
             select adapter.Description).ToList());
            return list;
        }

        public void SetNetworkAsync()
        {
            try
            {
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

                var mos = from ManagementObject mo in moc
                                where mo["Description"].ToString() == SelectedNetworkAdapter
                                select mo;
                var adapter = mos.First();
                if ((bool)adapter["IPEnabled"] && ValidIPAddress())
                {
                    ManagementBaseObject newIP = adapter.GetMethodParameters("EnableStatic");
                    ManagementBaseObject newGateway = adapter.GetMethodParameters("SetGateways");

                    newIP["IPAddress"] = new string[] { IPAddress };
                    newIP["SubnetMask"] = new string[] { SubnetAddress };

                    newGateway["DefaultIPGateway"] = new string[] { GatewayAddress };

                    ManagementBaseObject setIP = adapter.InvokeMethod("EnableStatic", newIP, null);
                    ManagementBaseObject setGateway = adapter.InvokeMethod("SetGateways", newGateway, null);

                    OnNetworkNetworkSettingsErrorEvent(new NetworkErrorEventArgs("IP settings updated"));
                }
                else
                {
                    OnNetworkNetworkSettingsErrorEvent(new NetworkErrorEventArgs("Failed to set adapter"));
                }
            }
            catch (Exception e)
            {
                OnNetworkNetworkSettingsErrorEvent(new NetworkErrorEventArgs(e.Message));
            }
        }

        private bool ValidIPAddress()
        {
            return Regex.IsMatch(IPAddress, @"\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b")
                    && Regex.IsMatch(SubnetAddress, @"\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b")
                    && Regex.IsMatch(GatewayAddress, @"\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b")
                    && SelectedNetworkAdapter != null;
        }
        #endregion
    }
}
