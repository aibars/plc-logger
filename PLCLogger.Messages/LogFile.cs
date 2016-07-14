using System;
using System.IO;

namespace PLCLogger.Messages
{
    public class LogFile
    {
        public Log MessageLog;

        private string NombreArchivo { get; set; }

        public LogFile()
        {
            MessageLog = new Log();
            this.NombreArchivo = "logfile.log";
        }

        /// <summary>
        /// Escribe un log de mensajes completo en un archivo
        /// </summary>
        /// <param name="messageLog"></param>
        public bool write(Log _MessageLog)
        {
            var retVal = true;

            for (var i = 0; i < _MessageLog.Logs.Count && retVal; i++)
            {
                if (write(_MessageLog.Logs[i]))
                {
                    _MessageLog.Logs.RemoveAt(i);
                }
                else
                {
                    retVal = false;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Escribe una línea de log en un archivo
        /// </summary>
        /// <param name="logLine"></param>
        public bool write(LogLine logLine)
        {
            FileStream fs = null;
            var retVal = true;

            lock (this)
            {
                try
                {
                    fs = new FileStream(NombreArchivo, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                    if (fs != null)
                    {
                        StreamWriter sw = null;
                        sw = new StreamWriter(fs);

                        fs.Seek(0, SeekOrigin.End);
                        sw.WriteLine(logLine.ToString());
                        sw.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageLog.Add(ex.Message);
                    retVal = false;
                }

                if (fs != null) fs.Close();
            }

            return retVal;
        }

        /// <summary>
        /// Escribe un mensaje de texto en un archivo
        /// </summary>
        /// <param name="mensaje"></param>
        public bool write(string mensaje)
        {
            return write(new LogLine(mensaje));
        }
    }
}