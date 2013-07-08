using System;
using System.IO;

namespace ActivitySpaces
{
    public enum LoggerType
    {
        ActivityManager,
        ActivityHome,
        ActivityCreated,
        ActivityButtonNameChanged,
        ActivityButtonIconChanged,
        ActivityButtonRenderStyleChanged,
        ActivityRemoved,
        ActivitySwitched,
        WindowClicked,
        WindowMoved,
        WindowActivated,
        WindowCreated,
        WindowDestroyed,
        WindowMinMax
    }

    public class DataLogger
    {
        private readonly StreamWriter _writer;
        public bool Enabled = true;
        public DataLogger(String filename)
        {
            if(Enabled)
                _writer = new StreamWriter(filename) {AutoFlush = true};
        }

        public void Log(LoggerType type, String content)
        {
            if (Enabled)
            {
                _writer.WriteLine(value: DateTime.Now + " " + type.ToString() + " " + content);
            }
        }

    }
}
