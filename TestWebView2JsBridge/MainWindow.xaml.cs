using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;

namespace TestWebView2JsBridge
{
    public interface IJavaScriptCallback
    {
        void CallbackFromJavascript(string message);
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IJavaScriptCallback
    {
        private bool _isInitialized = false, _initializeInProgress = false;
        private const string WebView2RuntimeSubFolder = "WebView2";

        private static string GetSetupJSBridgeScript()
        {
            return @"
function invokeNative(msg) {
    window.chrome.webview.hostObjects.JsCallbackObject.PostMessage(msg); 
}
console.log('invokeNative injected successfully');
";
        }

        public MainWindow()
        {
            InitializeComponent();

            txtJsText.Text = @"
function jsFunctionFromCsharp()
{ 
    // alert('Hello from Csharp!');   
    var result = document.getElementById('txt-send').value;
    return result;
}
jsFunctionFromCsharp();
";
        }

        private async void OnNavigatePressed(object sender, RoutedEventArgs e)
        {
            await EnsureWebView2Initialized();
            if (!string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                try
                {
                    webViewControl.CoreWebView2?.Navigate(txtUrl.Text);
                }
                catch (Exception ex)
                {   
                    MessageBox.Show(ex.ToString(), "Unexpected error on navigation", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            }
        }

        private async Task EnsureWebView2Initialized()
        {
            if (_isInitialized || _initializeInProgress)
            {   
                return;
            }

            try
            {   
                _initializeInProgress = true;
                btnNavigate.IsEnabled = false;

                var currentProcessFolder = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                var browserExecutableFolder = System.IO.Path.Combine(currentProcessFolder, WebView2RuntimeSubFolder);
                var env = await CoreWebView2Environment.CreateAsync(browserExecutableFolder, null, null).ConfigureAwait(true);
                await webViewControl.EnsureCoreWebView2Async(env).ConfigureAwait(true);

                webViewControl.NavigationStarting += WebViewControl_NavigationStarting;
                webViewControl.NavigationCompleted += WebViewControl_NavigationCompleted;

                webViewControl.CoreWebView2.AddHostObjectToScript("JsCallbackObject", new JavaScriptCallbackObject(this));
                
                _isInitialized = true;
            }
            finally
            {
                _initializeInProgress = false;
                btnNavigate.IsEnabled = true;
            }
        }

        private async void WebViewControl_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // JsBridge Setup
            if (e.IsSuccess)
            {
                var jsBrdigeScript = GetSetupJSBridgeScript();
                var jsResult = await webViewControl.ExecuteScriptAsync(jsBrdigeScript);
                Console.WriteLine(jsResult);
            }
        }

        private async void OnExecuteJsPressed(object sender, RoutedEventArgs e)
        {
            try 
            {
                var jsScript = txtJsText.Text.Trim();
                if (!string.IsNullOrWhiteSpace(jsScript))
                {
                    var result = await webViewControl.ExecuteScriptAsync(jsScript);
                    MessageBox.Show($"Result: {result} returned from Javascript.");       
                    result = null;
                    GC.Collect();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Unexpected error on executing javascript", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void WebViewControl_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Console.WriteLine($"WebView2 NavigationStarting called for {e.Uri}");
        }

        public void CallbackFromJavascript(string message)
        {  
            txtReceivedFromBrowser.Text = HttpUtility.UrlDecode(message);
        }

        [ComVisible(true)]
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public class JavaScriptCallbackObject
        {
            private IJavaScriptCallback _callbackHandler;

            public JavaScriptCallbackObject(IJavaScriptCallback callbackHandler)
            {
                _callbackHandler = callbackHandler;
            }

            public void PostMessage(string msg)
            {
                _callbackHandler?.CallbackFromJavascript(msg);
            }
        }
    }
}
