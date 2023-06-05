using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestWebView2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isInitialized = false, _initializeInProgress = false;
        private const string WebView2RuntimeSubFolder = "WebView2";
        private string _ignoreUrl = "https://skylight.local"; // default value
        

        private ProxyInfo _proxyInfo = new ProxyInfo
        {
            ProxyType = ProxyType.NoProxy
        };

        public MainWindow()
        {
            Log.LogEvent += Log_LogEvent;
            InitializeComponent();
            txtIgnoreUrl.Text = _ignoreUrl;
        }

        private void Log_LogEvent(object? sender, string e)
        {
            txtLogs.Text += e;
        }

        private async Task EnsureInitialized()
        {
            if (!_isInitialized)
            {
                await InitializeWebView2();
            }
        }

        private Microsoft.Web.WebView2.Wpf.WebView2 webViewControl = null;
        private async Task InitializeWebView2() {
            if (_initializeInProgress)
            {
                Log.Debug("WebView2 Initialization Already InProgress!");
                return;
            }

            try
            {   
                _initializeInProgress = true;
                btnApply.IsEnabled = false;
                btnNavigate.IsEnabled = false;

                if (webViewControl != null)
                {
                    webViewGrid.Children.Remove(webViewControl);
                    webViewControl.Dispose();
                    webViewControl = null;
                }

                var currentProcessFolder = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                var browserExecutableFolder = System.IO.Path.Combine(currentProcessFolder, WebView2RuntimeSubFolder);

                string additionalBrowserArguments = null;
                if (_proxyInfo != null)
                {
                    switch(_proxyInfo.ProxyType)
                    {
                        case ProxyType.NoProxy:
                            additionalBrowserArguments = $"--proxy-server=\"direct://\"";
                            break;
                        case ProxyType.CustomProxy:
                            additionalBrowserArguments = $"--proxy-server={_proxyInfo.Host}:{_proxyInfo?.Port}";
                            break;
                        case ProxyType.SystemProxy:
                        default:
                            break;
                    }
                    Log.Debug($"Set WebView2 additionalBrowserArguments = {additionalBrowserArguments ?? "(null)"}");
                }
                var env = await CoreWebView2Environment.CreateAsync(browserExecutableFolder, null, new CoreWebView2EnvironmentOptions(additionalBrowserArguments))
                                                       .ConfigureAwait(true);
                Log.Debug($"Initializing WebView2 (browserExecFodler: {browserExecutableFolder}, proxy: {_proxyInfo.ProxyType}, proxyHost: {_proxyInfo.Host}, proxyPort: {_proxyInfo.Port})");
                webViewControl = new Microsoft.Web.WebView2.Wpf.WebView2();
                Grid.SetRow(webViewControl, 1);
                Grid.SetColumn(webViewControl, 0);
                Grid.SetColumnSpan(webViewControl, 4);
                webViewGrid.Children.Add(webViewControl);

                await webViewControl.EnsureCoreWebView2Async(env).ConfigureAwait(true);
               
                Log.Debug("WebView2 Initialized Successfully");
                webViewControl.NavigationStarting += WebViewControl_NavigationStarting;

                _isInitialized = true;
            }
            finally
            {
                _initializeInProgress = false;
                btnApply.IsEnabled = true;
                btnNavigate.IsEnabled = true;
            }
        }

        private void WebViewControl_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Log.Debug($"WebView2 NavigationStarting called for {e.Uri}");
            var url = e.Uri.ToString().Trim().TrimEnd('/');
            var ignoreUrl = _ignoreUrl.Trim().TrimEnd('/');
            if (url.Equals(ignoreUrl.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                e.Cancel = true;
                Log.Debug($"WebView2 Navigation CANCELLED for {e.Uri}");
            }
        }

        private async void OnSettingsApplyPressed(object sender, RoutedEventArgs e)
        {
            if (txtIgnoreUrl.Text.Trim() != null)
            {
                try 
                {
                    _ignoreUrl = txtIgnoreUrl.Text.Trim();

                    var proxyInfo = new ProxyInfo();
                    proxyInfo.ProxyType = ProxyType.NoProxy;
                    proxyInfo.Host = "";
                    proxyInfo.Port = 0;
                    if ((bool)radioCustomProxy.IsChecked)
                    {
                        proxyInfo.ProxyType = ProxyType.CustomProxy;
                        proxyInfo.Host = txtProxyHost.Text;
                        int port = 0;
                        int.TryParse(txtProxyPort.Text, out port);
                        proxyInfo.Port = port;
                    }
                    else if ((bool)radioSystemProxy.IsChecked)
                    {
                        proxyInfo.ProxyType = ProxyType.SystemProxy;
                    }
                    _proxyInfo = proxyInfo;

                    var settingsLog = $"IgnoreUrl: {_ignoreUrl}" + Environment.NewLine;
                    settingsLog += $"ProxyType: {_proxyInfo.ProxyType}" + Environment.NewLine;
                    settingsLog += $"ProxyHost: {_proxyInfo.Host}" + Environment.NewLine;
                    settingsLog += $"ProxyPort: {_proxyInfo.Port}" + Environment.NewLine;

                    await InitializeWebView2();
                    Log.Debug($"WebView2 Settings applied:{Environment.NewLine}{settingsLog}");
                }
                catch (Exception ex)
                {
                    Log.Error("Unexpected error on navigation", ex);
                    MessageBox.Show(ex.ToString(), "Unexpected error on navigation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            }
        }

        private async void OnNavigatePressed(object sender, RoutedEventArgs e)
        {
            await EnsureInitialized();

            if (!string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                try
                {
                    webViewControl.CoreWebView2?.Navigate(txtUrl.Text);
                }
                catch(Exception ex)
                {
                    Log.Error("Unexpected error on navigation", ex);
                    MessageBox.Show(ex.ToString(), "Unexpected error on navigation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            }
        }

        private void OnClearLogsPressed(object sender, RoutedEventArgs e)
        {
            txtLogs.Text = "";
        }
    }

    public enum ProxyType
    {
        NoProxy,
        SystemProxy,
        CustomProxy
    }

    public class ProxyInfo
    {
        public ProxyType ProxyType { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
}
