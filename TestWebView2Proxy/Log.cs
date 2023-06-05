using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWebView2
{
    public static class Log
    {
        private readonly static object _sender = new object();
        public static event EventHandler<string> LogEvent;

        public static void Debug(string text)
        { 
            if (LogEvent != null) {
                var info = $"DEBUG: {text}";
                info += Environment.NewLine;
                LogEvent(_sender, info);
            }
        }

        public static void Error(string text, Exception ex = null)
        {
            if (LogEvent != null)
            {
                var error = $"ERROR: {text}";
                if (ex != null)
                {
                    error += Environment.NewLine + ex.ToString();
                }
                error += Environment.NewLine;
                LogEvent(_sender, error);
            }
        }
    }
}
