using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Ports;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PLCLogger
{
    public class Config : IDisposable
    {
        int[] _IPAddrPLC;
       

        public Log MessageLog;
        
        public string IPAddrPLC
        {
            get
            {
                return (_IPAddrPLC[0].ToString() + "." +
                    _IPAddrPLC[1].ToString() + "." +
                    _IPAddrPLC[2].ToString() + "." +
                    _IPAddrPLC[3].ToString());
            }
            set
            {
                string[] valores;
                int[] ip;

                valores = value.Trim().Split(new char[] { '.' }, 4);
                if (valores.Length == 4)
                {
                    ip = new int[4];
                    if (int.TryParse(valores[0], out ip[0]))
                    {
                        if (int.TryParse(valores[1], out ip[1]))
                        {
                            if (int.TryParse(valores[2], out ip[2]))
                            {
                                if (int.TryParse(valores[3], out ip[3]))
                                {
                                    _IPAddrPLC[0] = ip[0];
                                    _IPAddrPLC[1] = ip[1];
                                    _IPAddrPLC[2] = ip[2];
                                    _IPAddrPLC[3] = ip[3];
                                }
                            }
                        }
                    }
                }
            }
        }
        public string Protocol;
        public string SerialPort;
        public int Port;
        public int BaudRate;
        public Parity Parity;
        public string Type;
        public byte UnitID;
        public StopBits StopBits;
        public int DataReloadPeriod1, DataReloadPeriod2;
        public bool config_ok;

        public Config()
        {
            MessageLog = new Log("Config");
            _IPAddrPLC = new int[4];
            for (int i = 0; i < 4; i++)
                _IPAddrPLC[i] = 0;
            Port = 0;
            BaudRate = 0;
            SerialPort = null;
            Protocol = null;
            UnitID = 0;
            DataReloadPeriod1 = 1000;
            DataReloadPeriod2 = 60000;
        }

        public void Dispose()
        {
            
        }

        /// <summary>Lee la configuración desde un archivo</summary>
        /// <param name="path">Dirección del archivo de configuración</param>
        public bool Load(string path)
        {
            MessageLog.Clear();
            config_ok = false;

            // Intenta abrir el archivo de configuración
            try
            {
            string json = String.Empty;            
            StringBuilder file = new StringBuilder();
            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    file.AppendLine(line);
                }
            }
            json = file.ToString();
            JObject objeto = JObject.Parse(json);
            Protocol = objeto["nodes"]["protocol"].ToString();
            switch (Protocol)
            {
                case "MODBUS/TCP":
                    {
                        IPAddrPLC = objeto["nodes"]["address"].ToString();
                        Port = Convert.ToInt16(objeto["nodes"]["port"].ToString());
                        break;
                    }
                case "MODBUS/RTU":
                    {
                        SerialPort = objeto["nodes"]["port"].ToString();
                        BaudRate = Convert.ToInt16(objeto["nodes"]["baudrate"].ToString());
                        switch (objeto["nodes"]["parity"].ToString())
                        {
                            case "none":
                                Parity = Parity.None; break;
                            case "odd":
                                Parity = Parity.Odd; break;
                        }
                        switch (objeto["nodes"]["stopbits"].ToString())
                        {
                            case "1":
                                StopBits = StopBits.One; break;
                            case "0":
                                StopBits = StopBits.None; break;
                            case "1.5":
                                StopBits = StopBits.OnePointFive; break;
                            case "2":
                                StopBits = StopBits.Two; break;
                        }
                        UnitID = (byte)objeto["nodes"]["id"];
                        Type = objeto["nodes"]["type"].ToString();
                        break;
                    }
                default: 
                    {
                        MessageLog.Add("No se reconoce protocolo");
                        break;
                    }
            }
            
            
            return true;   
                }
            catch(Exception ex)
            {
                MessageLog.Add(ex.Message);
                return false;
            }

          
        }
        
        /// <summary>Convierte una posición de memoria en la forma %MWX a X</summary>
        /// <param name="address">Cadena conteniend la dirección de memoria</param>
        /// <param name="grupo">Grupo resultante de parsing de posición. 1 para tipo de memoria, 2 para posición, 3 para bit</param>
        public static ushort convertAdrress(string address, int grupo)
        {
            ushort dir;
            Match match = Regex.Match(address, @"%M([FWD])?(\d+)\.?(\d+)?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return dir = Convert.ToUInt16(match.Groups[grupo].Value);

            }
            else
            {
                return 0;
            }

        }
        /// <summary>Convierte las posiciones de memoria en la forma %MWX a X como entero de la lista entera de variables</summary>
        /// <param name="variables">Variables definidas en archivo de configuración</param>
        public void convertAdrress(ref List<Variable> variables)
        {
            foreach (Variable v in variables)
            {
                Match match = Regex.Match(v.Direccion, @"%M([FWD])?(\d+)\.?(\d+)?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    v.Address = Convert.ToUInt16(match.Groups[2].Value);
                    if (v.Type.Equals("bit"))
                    {
                        v.Subaddress = Convert.ToUInt16(match.Groups[3].Value);
                    }
                }
                else { MessageLog.Add("Posición de memoria no válida"); }
            }
        }
        /// <summary>Trae una lista de variables desde JSON</summary>
        public List<Variable> leerVariablesJSON()
        {
            string json = String.Empty;
            StringBuilder file = new StringBuilder();
            try
            {
            using (StreamReader sr = new StreamReader("config.json", System.Text.Encoding.UTF8))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    file.AppendLine(line);
                }
            }
            json = file.ToString();
            JObject objeto = JObject.Parse(json);
            string json_var = objeto["variables"].ToString();
           
                List<Variable> variables = JsonConvert.DeserializeObject<List<Variable>>(json_var);
                convertAdrress(ref variables);
                return variables;
            }
            catch (Exception ex)
            {
                MessageLog.Add(ex.Message);
                return null;
            }
        }
        /// <summary>Guarda la configuración a un archivo</summary>
        /// <param name="NombreArchivo">Nombre del archivo de configuración. Redefinir para JSON</param>
        public bool Save(string NombreArchivo)
        {
            bool retval = true;
            FileStream fs = null;

            // Intenta abrir el archivo de configuración
            try
            {
                

                // Si el archivo ya existe, crea un backup
                if (File.Exists(NombreArchivo))
                {
                    string file1, file2;

                    for (int i = 10; i >= 2; i--)
                    {
                        file1 = NombreArchivo + ".bk" + (i - 1).ToString();
                        file2 = NombreArchivo + ".bk" + i.ToString();
                        if (File.Exists(file1))
                        {
                            if (File.Exists(file2))
                                File.Delete(file2);
                            File.Move(file1, file2);
                        }
                    }

                    file1 = NombreArchivo + ".bk1";
                    if (File.Exists(file1))
                        File.Delete(file1);
                    File.Move(NombreArchivo, file1);
                }

                fs = new FileStream(NombreArchivo, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                fs.SetLength(0);

                
            }
            catch 
            {
            
                retval = false;
            }

            if (fs != null)
            {
                fs.Close();
            }

            return retval;
        }
    }
}
