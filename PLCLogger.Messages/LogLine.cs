using System;
using System.Globalization;

namespace PLCLogger.Messages
{
    public class LogLine
    {
        public DateTime time;
        public string message;
        public string parent;

        public LogLine(string _message, string _parent = null)
        {
            time = DateTime.Now;
            message = _message;
            parent = _parent;
        }

        public override string ToString()
        {
            var esAR = new CultureInfo("es-AR");
            string linea = null;

            linea = time.ToString(esAR) + ":";
            linea += "\t[" + parent + "]\t" + message;

            return linea;
        }
    }
}
