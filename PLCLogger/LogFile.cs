using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace PLCLogger
{
    /// <summary>
    /// Representa una línea de mensaje de un log de mensajes
    /// </summary>
    public class LogLine
    {
        public DateTime time;
        public string message;
        public string parent;

        public LogLine( string _message, string _parent=null )
        {
            time = DateTime.Now;
            message = _message;
            parent = _parent;
        }

        public override string ToString()
        {
            CultureInfo esAR = new CultureInfo("es-AR");
            string linea = null;

            linea = time.ToString(esAR) + ":";
            linea += "\t[" + parent + "]\t" + message;

            return linea;
        }
    }

    /// <summary>
    /// Almacena un log de mensajes incluyendo el tiempo de ocurrencia del evento
    /// </summary>
    public class Log
    {
        public List<LogLine> Logs;
        public string parent;

        public Log( string _parent = null )
        {
            Logs = new List<LogLine>();
            parent = _parent;
        }

        public void Add(string message)
        {
            Logs.Add(new LogLine(message, parent));
        }

        /// <summary>
        /// Agrega un Log de mensajes completo
        /// </summary>
        /// <param name="_Log">Log de mensajes a agregar</param>
        /// <param name="clear">Indica si borra los mensajes del Log de origen</param>
        public void Add(Log _Log, bool move=true)
        {
            Logs.AddRange(_Log.Logs);
            if (move) _Log.Clear();
        }

        public void Add(LogLine _LogLine)
        {
            Logs.Add(_LogLine);
        }

        public void Clear()
        {
            Logs.Clear();
        }
    }

    public class LogFile
    {
        string _NombreArchivo;
        public Log MessageLog;

        string NombreArchivo
        {
            get
            {
                return (_NombreArchivo);
            }
            set
            {
                _NombreArchivo = value;
            }
        }

        public LogFile()
        {
            MessageLog = new Log();
            _NombreArchivo = "logfile.log";
        }

        /// <summary>
        /// Escribe un log de mensajes completo en un archivo
        /// </summary>
        /// <param name="messageLog"></param>
        public bool write(Log _MessageLog)
        {
            bool retVal = true;

            for (int i = 0; i < _MessageLog.Logs.Count && retVal; i++)
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
            bool retVal = true;

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

                if (fs != null)
                    fs.Close();
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
