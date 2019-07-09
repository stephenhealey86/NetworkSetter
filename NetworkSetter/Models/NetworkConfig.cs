using NetworkSetter.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSetter.Models
{
    public class NetworkConfig
    {
        #region Private Variables
        private string name = "";
        #endregion

        #region Public Variables
        public string Name
        {
            get { return name; }
            set { name = value; OnNetworkSettingsRefreshEvent(new EventArgs()); }
        }

        private void OnNetworkSettingsRefreshEvent(EventArgs eventArgs)
        {
            NetworkSettingsRefreshEvent?.Invoke(this, eventArgs);
        }

        private void OnNetworkNetworkSettingsErrorEvent(NetworkErrorEventArgs eventArgs)
        {
            NetworkSettingsErrorEvent?.Invoke(this, eventArgs);
        }

        public List<string> NetworkAdapters => GetNetworkAdapters();

        public string SelectedNetworkAdapter { get; set; }

        public string IPAddress { get; set; }
        public string SubnetAddress { get; set; }
        public string GatewayAddress { get; set; }
        public EventHandler<NetworkErrorEventArgs> NetworkSettingsErrorEvent;
        public EventHandler<EventArgs> NetworkSettingsRefreshEvent;
        public bool ValidForm
        {
            get { return Name.Length < 3; }
        }
        public bool ValidConfig
        {
            get { return true; }
        }
        #endregion

        #region Constructor
        public NetworkConfig()
        {

        }
        #endregion

        #region Helpers
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
            return true;
        }
        #endregion
    }
}
