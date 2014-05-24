using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MiniSlate
{
    class Logger
    {
        private static Logger instance = new Logger();
        private bool isEnabled = false;
        private System.IO.StreamWriter logfile;

        public enum MessageType{
            FATAL,
            ERROR,
            WARNING,
            INFORMATION
        };
        public Logger()
        {
            logfile = new System.IO.StreamWriter("log.txt", true, System.Text.Encoding.GetEncoding("Shift-JIS"));
        }
        
        static public void Log(MessageType type, string message)
        {
            instance.LogImpl(type, message);
        }

        static public void Enable(bool enabled)
        {
            instance.isEnabled = enabled;
        }

        private void LogImpl(MessageType type, string message)
        {
            if (type == MessageType.FATAL || type == MessageType.ERROR)
            {
                MessageBox.Show(message, type.ToString(), MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            if (isEnabled && logfile != null)
            {
                DateTime time = DateTime.Now;
                logfile.WriteLine(time.ToString("[yyyy/MM/dd HH:mm:ss]") + ":" + type.ToString() + ":" + message);
            }
        }
    }
}
