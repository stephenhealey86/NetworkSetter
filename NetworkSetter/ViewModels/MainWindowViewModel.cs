using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using NetworkSetter.Events;
using NetworkSetter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NetworkSetter.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        #region Private Variables
        Window _window;
        private int CurrentTheme;
        private string errorMessage;
        private Snackbar DisplayError;
        private string Version
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var Major = assembly.GetName().Version.Major;
                var Minor = assembly.GetName().Version.Minor;
                var Revision = assembly.GetName().Version.Revision;
                return $"V{Major}.{Minor}.{Revision}";
            }
        }
        #endregion

        #region Public Variables
        public ObservableCollection<NetworkConfig> NetworkSettings { get; set; } = new ObservableCollection<NetworkConfig>();
        public int SelectedTab { get; set; } = 0;
        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                errorMessage = value;
                DisplayError.MessageQueue.Enqueue(ErrorMessage, "Ok", () => { errorMessage = null; });
            }
        }
        #endregion

        #region Commands
        /// <summary>
        /// The command to minimize the window
        /// </summary>
        public ICommand MinimizeCommand { get; set; }

        /// <summary>
        /// The command to maximize the window
        /// </summary>
        public ICommand MaximizeCommand { get; set; }

        /// <summary>
        /// The command to close the window
        /// </summary>
        public ICommand CloseCommand { get; set; }

        /// <summary>
        /// The command to show the system menu of the window
        /// </summary>
        public ICommand SystemMenuCommand { get; set; }

        /// <summary>
        /// The command to change theme
        /// </summary>
        public ICommand ThemeCommand { get; set; }

        /// <summary>
        /// The command for saving the Http server list as a file
        /// </summary>
        public ICommand SaveFileCommand { get; set; }

        /// <summary>
        /// The command for generating the Http server list from a file
        /// </summary>
        public ICommand OpenFileCommand { get; set; }

        /// <summary>
        /// The command to delete a HttpServer from the List
        /// </summary>
        public ICommand RemoveConfigCommand { get; set; }

        /// <summary>
        /// The command to delete a HttpServer from the List
        /// </summary>
        public ICommand NewConfigCommand { get; set; }

        /// <summary>
        /// The command to set adapter ip settings
        /// </summary>
        public ICommand ConfigureAdapterCommand { get; set; }

        /// <summary>
        /// The command for reseting the network adaptor to automatic
        /// </summary>
        public ICommand ObtainIPAutoCommand { get; set; }
        #endregion

        #region Constructor
        public MainWindowViewModel(Window window)
        {
            _window = window;
            Title = $"Network Settings - {Version}";
            SetCommands();
            GetUserSettings();
            SubscribeToEvents();
            DisplayError = _window.FindName("MyMessageQueue") as Snackbar;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Loop through all <see cref="HttpServer"/> in <seealso cref="ObservableCollection{T}"/> and subscribe to ServerErrorEvent
        /// </summary>
        private void SubscribeToEvents()
        {
            foreach (NetworkConfig config in NetworkSettings)
            {
                config.NetworkSettingsErrorEvent += NetworkErrorEvent;
                config.PropertyChanged += delegate { Refresh(); };
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NetworkErrorEvent(object sender, NetworkErrorEventArgs e)
        {
            ErrorMessage = e.ErrorMessage;
        }
        /// <summary>
        /// Set all <see cref="MainWindowViewModel"/> <seealso cref="ICommand"/>s
        /// </summary>
        private void SetCommands()
        {
            // Caption Buttons
            MinimizeCommand = new RelayCommand(() => _window.WindowState = WindowState.Minimized);
            MaximizeCommand = new RelayCommand(() => _window.WindowState ^= WindowState.Maximized);
            CloseCommand = new RelayCommand(() => CloseCommandAction());
            SystemMenuCommand = new RelayCommand(() => SystemMenuCommandAction());

            // Menu Buttons
            ThemeCommand = new RelayCommand(() => ThemeCommandAction());
            SaveFileCommand = new RelayCommand(async () => await SaveFileCommandAction());
            OpenFileCommand = new RelayCommand(async () => await OpenFileCommandAction());

            // Tab Commands
            RemoveConfigCommand = new RelayParamterCommand((param) => RemoveConfigCommandAction(param));
            NewConfigCommand = new RelayCommand(() => NewConfigCommandAction());
            ConfigureAdapterCommand = new RelayCommand(() => ConfigureAdapterCommandAction());
            ObtainIPAutoCommand = new RelayCommand(() => ObtainIPAutoCommandAction());
        }

        /// <summary>
        /// Refreshes the <see cref="MainWindow"/> <seealso cref="TabControl"/> <seealso cref="TabItem"/>s after changes made
        /// </summary>
        private void Refresh()
        {
            OnPropertyChanged(nameof(NetworkSettings));
            if (_window.FindName("MyTabControl") is TabControl tabs)
            {
                tabs.Items.Refresh();
            }
        }
        
        /// <summary>
        /// Serializes the <see cref="Object"/> into a JSON string and returns it
        /// </summary>
        /// <param name="obj">Object to turn in JSON string</param>
        /// <param name="subscribe">If true will resubscribe to object events</param>
        /// <returns></returns>
        private string ObjectToJsonString(object obj, bool subscribe)
        {
            foreach (NetworkConfig config in NetworkSettings)
            {
                // Remove event handler before serialization
                config.NetworkSettingsErrorEvent = null;
                //config.PropertyChanged = null;
            }
            var jsonObject = JsonConvert.SerializeObject(obj);
            JToken jo = JToken.Parse(jsonObject);
            jsonObject = jo.ToString(Newtonsoft.Json.Formatting.Indented);
            if (subscribe) { SubscribeToEvents(); }
            return jsonObject;
        }
        /// <summary>
        /// Gets the application user settings and updates the application with them
        /// </summary>
        private void GetUserSettings()
        {
            var jsonObject = (string)Properties.Settings.Default["Servers"];
            if (jsonObject != null)
            {
                try
                {
                    NetworkSettings = JsonConvert.DeserializeObject<ObservableCollection<NetworkConfig>>(jsonObject) ?? NetworkSettings;
                    if (NetworkSettings.Count == 0)
                    {
                        throw new Exception("Settings are empty.");
                    }
                }
                catch (Exception)
                {
                    NewConfigCommandAction();
                }
            }
            else
            {
                NewConfigCommandAction();
            }
            // Get theme
            var theme = (int)Properties.Settings.Default["Theme"];
            CurrentTheme = theme;
            Theme = ThemeTypes.GetTheme(CurrentTheme);
            OnPropertyChanged(nameof(Theme));

            // Get selected tab
            SelectedTab = (int)Properties.Settings.Default["SelectedTab"] >= 0 ? (int)Properties.Settings.Default["SelectedTab"] : 0;

            Refresh();
        }
        #endregion

        #region CommandActions
        /// <summary>
        /// Closes the window
        /// </summary>
        private void CloseCommandAction()
        {
            // Servers to remove before saving
            var list = new List<NetworkConfig>();
            // Loop through servers and stop and store unused for removing
            foreach (NetworkConfig config in NetworkSettings)
            {
                // Add to removal list
                if (config.Name == null || config.Name?.Length == 0)
                {
                    list.Add(config);
                }
            }
            // Remove blank servers
            foreach (NetworkConfig item in list)
            {
                NetworkSettings.Remove(item);
            }
            Refresh();
            // Store user settings
            var jsonObject = ObjectToJsonString(NetworkSettings, false);
            Properties.Settings.Default["Servers"] = jsonObject;
            Properties.Settings.Default["Theme"] = CurrentTheme;
            Properties.Settings.Default["SelectedTab"] = SelectedTab;
            Properties.Settings.Default.Save();
            _window.Close();
        }

        /// <summary>
        /// Populates the HttpServer list from json file
        /// </summary>
        /// <returns></returns>
        private async Task OpenFileCommandAction()
        {
            IsBusy = true;
            // Open windows save dialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            // Get the path name chosen by the user
            var fullPath = openFileDialog.FileName;
            // Get json string from file
            // Check user didn't just close window
            if (fullPath != null && fullPath.Length > 0)
            {
                try
                {
                    if (File.Exists(fullPath))
                    {
                        FileStream fileStream = File.Open(fullPath, FileMode.Open, FileAccess.Read);
                        byte[] buffer = new byte[1028];
                        await fileStream.ReadAsync(buffer, 0, 1028);
                        await fileStream.FlushAsync();
                        fileStream.Close();
                        var jsonObject = Encoding.ASCII.GetString(buffer);
                        NetworkSettings = JsonConvert.DeserializeObject<ObservableCollection<NetworkConfig>>(jsonObject);
                        Refresh();
                        ErrorMessage = $"Opened {Path.GetFileName(fullPath)}";
                    }
                }
                catch (Exception)
                {
                    ErrorMessage = "File not compatable";
                }
            }
            IsBusy = false;
        }

        /// <summary>
        /// Saves Http server list as a json file
        /// </summary>
        private async Task SaveFileCommandAction()
        {
            IsBusy = true;
            // Create json string from HttpServer and format
            var jsonObject = ObjectToJsonString(NetworkSettings, true);
            if (jsonObject.Length > 0)
            {
                // Open windows save dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.ShowDialog();
                // Get the path name chosen by the user
                var fullPath = saveFileDialog.FileName;
                // Check user didn't just close window
                if (fullPath != null && fullPath.Length > 0)
                {
                    // Ensure file extension is json
                    fullPath = Path.ChangeExtension(fullPath, null);
                    fullPath = Path.ChangeExtension(fullPath, "json");
                    byte[] buffer = Encoding.ASCII.GetBytes(jsonObject);
                    FileStream fileStream;
                    // Save file
                    if (File.Exists(fullPath))
                    {
                        fileStream = File.Open(fullPath, FileMode.Truncate, FileAccess.ReadWrite);
                        await fileStream.WriteAsync(buffer, 0, buffer.Length);
                        await fileStream.FlushAsync();
                        fileStream.Close();
                    }
                    else
                    {
                        fileStream = File.Create(fullPath);
                        await fileStream.WriteAsync(buffer, 0, buffer.Length);
                        await fileStream.FlushAsync();
                        fileStream.Close();
                    }
                    ErrorMessage = $"Current configuration saved as {Path.GetFileName(fullPath)}";
                }
            }
            IsBusy = false;
        }

        /// <summary>
        /// Adds a new blank HttpServer to the list
        /// </summary>
        private void NewConfigCommandAction()
        {
            if (NetworkSettings?.Count > 4)
            {
                return;
            }

            NetworkSettings.Add(new NetworkConfig());
            SelectedTab = NetworkSettings.Count - 1;
            NetworkSettings[SelectedTab].NetworkSettingsErrorEvent += NetworkErrorEvent;
            NetworkSettings[SelectedTab].PropertyChanged += delegate { Refresh(); };
            OnPropertyChanged(nameof(SelectedTab));
            Refresh();
        }

        /// <summary>
        /// Removes the first HttpServer from the list with a Request Uri thata matches the parameter
        /// </summary>
        /// <param name="param">HttpServer items Request property</param>
        private void RemoveConfigCommandAction(object param)
        {
            // Get string
            var str = (string)param;
            if (str != null && str.Length > 0)
            {
                // Remove first matching HttpServer from list and update UI
                var config = NetworkSettings.FirstOrDefault(x => x.Name == str);
                NetworkSettings.Remove(config);
                OnPropertyChanged(nameof(NetworkSettings));
            }
            else
            {
                // Remove all blank HttpServers from the list and update UI
                var list = new List<NetworkConfig>();
                foreach (NetworkConfig item in NetworkSettings)
                {
                    if (item.Name == null || item.Name.Length < 3)
                    {
                        list.Add(item);
                    }
                }
                foreach (NetworkConfig item in list)
                {
                    NetworkSettings.Remove(item);
                }
                OnPropertyChanged(nameof(NetworkSettings));
            }
            // If the list is empty add one new blank HttpServer
            if (NetworkSettings.Count == 0)
            {
                NewConfigCommandAction();
            }
        }

        /// <summary>
        /// Change UI theme
        /// </summary>
        private void ThemeCommandAction()
        {
            CurrentTheme++;
            CurrentTheme = CurrentTheme > 3 ? 0 : CurrentTheme;
            Theme = ThemeTypes.GetTheme(CurrentTheme);
            OnPropertyChanged(nameof(Theme));
        }

        /// <summary>
        /// Displays the system menu
        /// </summary>
        private void SystemMenuCommandAction()
        {
            var x = _window.Left;
            var y = _window.Top;
            if (_window.WindowState == WindowState.Maximized)
            {
                x = 0; y = -5;
            }
            SystemCommands.ShowSystemMenu(_window, new Point(x, y + 40));
        }

        /// <summary>
        /// Set the selected adpater settings
        /// </summary>
        private void ConfigureAdapterCommandAction()
        {
            NetworkSettings[SelectedTab].SetNetwork();
        }

        /// <summary>
        /// Set the selected adpater back to automatic IP
        /// </summary>
        private void ObtainIPAutoCommandAction()
        {
            NetworkSettings[SelectedTab].ObtainIPAuto();
        }
        #endregion
    }
}
