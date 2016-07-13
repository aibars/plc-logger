using System.Collections.Generic;

namespace PLCLogger.Messages
{
    public class Log
    {
        public List<LogLine> Logs;

        public string parent;

        public Log(string _parent = null)
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
        public void Add(Log _Log, bool move = true)
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
}