using System;
using System.IO.Ports;
using Modbus;
using ModbusTCP;
using PLCLogger.Messages;

namespace PLCLogger.Logic
{
    public class PLC : Master 
    {
        // Declara los eventos
        public Log MessageLog;
        int[] _IPAddress;

        // ------------------------------------------------------------------------

        public string IPAddress
        {
            get
            {
                return (_IPAddress[0].ToString() + "." +
                    _IPAddress[1].ToString() + "." +
                    _IPAddress[2].ToString() + "." +
                    _IPAddress[3].ToString());
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
                                    _IPAddress[0] = ip[0];
                                    _IPAddress[1] = ip[1];
                                    _IPAddress[2] = ip[2];
                                    _IPAddress[3] = ip[3];
                                }
                            }
                        }
                    }
                }
            }
        }
        protected bool bDesconectar, bCommError;
        public int WordsRead, WordsReadMax;
        public string Protocol;
        public string SerialPort;
        public int Port;
        public int BaudRate;
        public Parity Parity;
        public string Type;
        public byte UnitID;
        public StopBits StopBits;
        public ModbusSlaveSerial ms;
        public ModbusMasterSerial mm;
        public bool connected_rtu;
        
        // ------------------------------------------------------------------------
        public int progress
        {
            get
            {
                if (WordsReadMax == 0)
                    return (0);
                else
                {
                    int aux = (WordsRead * 100) / WordsReadMax;
                    if (aux < 0) return (0);
                    if (aux > 100) return (100);
                    else return (aux);
                }
            }
        }

        // ------------------------------------------------------------------------

        public PLC()
        {
            _IPAddress = new int[4];
            for (int i = 0; i < 4; i++)
                _IPAddress[i] = 0;

            MessageLog = new Log( "PLC" );
            bCommError = false;
            connected_rtu = false;
            this.OnException += new ModbusTCP.Master.ExceptionData(ProcesarError);
        }

        // ------------------------------------------------------------------------

        ~PLC()
        {
            desconectar();
        }

        // ------------------------------------------------------------------------

        /// <summary>Se conecta al PLC</summary>
        public bool conectar()
        {
            
            switch(Protocol)
            {
                case "MODBUS/TCP":
                    {
                        if (!connected)
                        {
                            try
                            {
                                connect(IPAddress, Port);
                            }
                            catch (Exception ex)
                            {
                                MessageLog.Add(ex.Message);
                            }
                        }
                        bCommError = false;
                        return (connected);

                    }
                case "MODBUS/RTU":
                    {
                        
                            if (!connected_rtu)
                            {

                                try
                                {
                                    if (Type == "slave")
                                    {
                                        mm = new ModbusMasterSerial(ModbusSerialType.RTU, SerialPort, BaudRate, 8, Parity, StopBits, Handshake.None);
                                        mm.Connect();
                                        connected_rtu = true;
                                    }
                                    else { connected_rtu = false; throw new Exception("No se reconoce tipo"); }
                                }
                                catch (Exception err)
                                {
                                    MessageLog.Add(err.Message + "\r\n" + err.StackTrace + "\r\n" + err.TargetSite + "\r\n");
                                    connected_rtu = false;
                                }
                            }
                            bCommError = false;
                            return (connected_rtu);
                        
                        
                    }
                default: return (!connected);
                }
        }

        // ------------------------------------------------------------------------

        /// <summary>Se desconecta del PLC</summary>
        public bool desconectar()
        {
           
            switch(Protocol)
            {
                case "MODBUS/TCP":
                    {
                        if (connected)
                        {
                            disconnect();
                        }

                        bCommError = false;
                        return (!connected);
                    }
                case "MODBUS/RTU":
                    {
                        if (connected)
                        {
                           //desconectar serial
                        }

                        bCommError = false;
                        return (!connected);
                    }
                default: return (!connected);
            }
        }
       
        protected void SetWord(int posicion, Byte[] valores, int dato)
        {
            if (valores.Length < posicion * 2 + 2) Array.Resize(ref valores, posicion * 2 + 2);
            valores[posicion * 2 + 1] = (byte)(dato & 0xFF);
            valores[posicion * 2] = (byte)((dato >> 8) & 0xFF);
        }
        protected void SetUInt(int posicion, UInt16[] valores, string valor)
        {
            UInt16 num;
            UInt16.TryParse(valor, out num);
            valores[posicion] = num;
        }
        protected void SetUDInt(int posicion, UInt16[] valores, string valor)
        {
            UInt32 num;
            UInt32.TryParse(valor, out num);
            byte[] bytes = new byte[4];
            bytes = BitConverter.GetBytes(num);
            valores[posicion] = (UInt16)(bytes[1] << 8 | bytes[0]);
            valores[posicion+1] = (UInt16)(bytes[3] << 8 | bytes[2]);
            
        }
        protected void SetInt(int posicion, UInt16[] valores, string valor)
        {
            UInt16 num;
            UInt16.TryParse(valor, out num);
            valores[posicion] = num;
        }
        protected void SetByte(int posicion, UInt16[] valores, string valor)
        {
            UInt16 num;
            int n = int.Parse(valor, System.Globalization.NumberStyles.HexNumber);
            UInt16.TryParse(n.ToString(), out num);
            valores[posicion]= num;
        }
        protected void SetDInt(int posicion, UInt16[] valores, string valor)
        {
            int result;
            Int32.TryParse(valor, out result);
            byte[] bytes = new byte[4];
            bytes = BitConverter.GetBytes(result);
            valores[posicion] = (UInt16)(bytes[1] << 8 | bytes[0]);
            valores[posicion + 1] = (UInt16)(bytes[3] << 8 | bytes[2]);
        }
        protected void SetFloat(int posicion, UInt16[] valores, string valor)
        {
            float num;
            float.TryParse(valor, out num);
            byte[] bytes =  BitConverter.GetBytes(num);
            valores[posicion] = (UInt16)(bytes[1] << 8 | bytes[0]);
            valores[posicion + 1] = (UInt16)(bytes[3] << 8 | bytes[2]);

        }
        protected void SetDate(int posicion, UInt16[] valores, string valor)
        {
            //Ver como viene desde la interfáz: YYYYMMDD, DD/MM/YYYY etc. Asumir YYYYMMDD
            //o bien se puede parsear
            string year = valor.Substring(0, 4);
            string date = valor.Substring(4, 4);
            valores[posicion+1] = Convert.ToUInt16(year, 16);
            valores[posicion] = Convert.ToUInt16(date, 16);

        }
        protected void SetTime(int posicion, UInt16[] valores, string valor)
        {
            byte[] n = new byte[4];
            n = BitConverter.GetBytes(Convert.ToInt32(valor));  
            valores[posicion] = (UInt16)(n[1] << 8 | n[0]);
            valores[posicion+1] = (UInt16)(n[3] << 8 | n[2]);

        }
        protected void SetTimeOfDay(int posicion, UInt16[] valores, string valor)
        {
            byte[] n = new byte[4];
            n = BitConverter.GetBytes(Convert.ToInt32(valor));
            valores[posicion] = (UInt16)(n[1] << 8 | n[0]);
            valores[posicion + 1] = (UInt16)(n[3] << 8 | n[2]);
        }
        protected void SetString(int posicion, UInt16[] valores, string valor)
        {
            byte[][] byteArray = new byte[32][];
            for (int i = 0; i < valor.Length; i++)
            {
                byteArray[i] = new byte[2];
                byteArray[i] = BitConverter.GetBytes(valor[i]);
            }
            for (int i = valor.Length; i < 32; i++)
            {
                byteArray[i] = new byte[1];
                byteArray[i][0] = 0; 
            }
            for (int i = 0, j=0; i < byteArray.Length; i+=2,j++)
            {
                valores[posicion+j] = (UInt16)(byteArray[i+1][0] << 8 | byteArray[i][0]);
            }
        }
        protected void SetBit(int posicion, bool lh, UInt16[] valores, string valor)
        {
            if (lh)
            {
                if (valor.ToLower().Equals("true"))
                    valores[posicion] = 1;
                else valores[posicion] = 0;
            }
            else 
            {
                if (valor.ToLower().Equals("true"))
                     valores[posicion] = 256;
                else valores[posicion] = 0;
            }
        }
        // ------------------------------------------------------------------------
        protected void SetBit(int posicion, int subaddress, UInt16[] valores, int vactual,string valor)
        {
            int num = 0;
            int[] bits = new int[16];
            for (int i = 0; i < 16; i++)
            {
                if (vactual % 2 == 0) bits[i] = 0;
                else bits[i] = 1;
                vactual = vactual / 2;
            } 
            if (valor.ToLower().Equals("true"))
            {
                bits[subaddress] = 1;
            }
            else
            {
                bits[subaddress] = 0;
            }
            for (int i = 0; i < 16; i++)
            {
                if (bits[i]==1) num += (int)Math.Pow(2, i);
            }
            valores[posicion] = (UInt16)num;
        }
        // ------------------------------------------------------------------------
        protected void SetDWord(int posicion, UInt16[] valores, UInt32 dato)
        {
            if (valores.Length < posicion + 2) Array.Resize(ref valores, posicion + 2);
            valores[posicion] = (UInt16)(dato & 0xFFFF);
            valores[posicion + 1] = (UInt16)((dato >> 16) & 0xFFFF);
        }
        // ------------------------------------------------------------------------
        /// <summary>
        /// Extrae una palabra del vector de valores
        /// </summary>
        /// <param name="posicion">Posición de la cual extaer el valor</param>
        /// <param name="valores">Vector de datos</param>
        /// <returns></returns>
        protected UInt16 GetWord(int posicion, Byte[] valores)
        {
            if (valores.Length < posicion * 2 + 2) return 0;
            else return ((UInt16)((valores[posicion * 2] << 8 | valores[posicion * 2 + 1]) & 0xFFFF));
        }
        // ------------------------------------------------------------------------
        /// <summary>
        /// Extrae una palabra doble del vector de valores
        /// </summary>
        /// <param name="posicion">Posición de la cual extraer el valor</param>
        /// <param name="valores">Vector de datos.</param>
        /// <returns></returns>
        protected UInt32 GetDWord(int posicion, Byte[] valores)
        {
            if (valores.Length < posicion * 2 + 4) return 0;
            else return ((UInt32)(valores[posicion * 2 + 2] << 24 | valores[posicion * 2 + 3] << 16 | valores[posicion * 2] << 8 | valores[posicion * 2 + 1]));
        }
        // ------------------------------------------------------------------------
        /// <summary>
        /// Extrae una palabra doble del vector de valores
        /// </summary>
        /// <param name="posicion">Posición de la cual extraer el valor</param>
        /// <param name="valores">Vector de datos.</param>
        /// <returns></returns>
        protected UInt32 GetDWord(int posicion, UInt16[] valores)
        {
            if (valores.Length < posicion + 2) return 0;
            else return ((UInt32)(valores[posicion + 1] << 16 | valores[posicion]));
        }
        // ------------------------------------------------------------------------
        /// <summary>
        /// Extrae una palabra doble del vector de valores
        /// </summary>
        /// <param name="posicion">Posición de la cual extraer el valor</param>
        /// <param name="valores">Vector de datos.</param>
        /// <returns></returns>
        protected Int32 GetDInt(int posicion, UInt16[] valores)
        {
            if (valores.Length < posicion + 2) return 0;
            else return ((Int32)(valores[posicion + 1] << 16 | valores[posicion]));
        }
        // ----------------------------------------------------------------------
        protected UInt32 GetUDInt(int posicion, UInt16[] valores)
        {
            if (valores.Length < posicion + 2) return 0;
            else return ((UInt32)(valores[posicion + 1] << 16 | valores[posicion]));
        }
        // ------------------------------------------------------------------------
        protected float GetFloat(int posicion, UInt16[] valores)
        {
            if (valores.Length < posicion + 2) return 0;
            else
            {
                return BitConverter.ToSingle(new byte[] { (byte)((valores[posicion]) & 0xFF), (byte)((valores[posicion] >> 8) & 0xFF), (byte)((valores[posicion + 1]) & 0xFF), (byte)((valores[posicion + 1] >> 8) & 0xFF) }, 0);
            }
        }
        // ------------------------------------------------------------------------
        protected bool GetBit(int posicion, bool lh, UInt16[] valores)
        {
            if (valores.Length < posicion + 1) return (false);
            else
            {
                if (lh)
                    return ((valores[posicion] & 0x0100) != 0);
                else
                    return ((valores[posicion] & 0x0001) != 0);
            }
        }
        // ------------------------------------------------------------------------
        protected bool GetBit(int posicion, int subaddress, UInt16[] valores)
        {
            if (((valores[posicion] >> subaddress) & 0x0001) != 0) return true;
            else return false;
        }
        // ------------------------------------------------------------------------
        // Muestra en decimal el primer o último byte, según lh
        protected byte GetByte(int posicion, bool lh, UInt16[] valores)
        {
            if (valores.Length < posicion + 1)
                return (0);
            else
            {
                if (lh)
                    return ((byte)((valores[posicion] & 0xFF00) >> 8));
                else
                    return ((byte)(valores[posicion] & 0x00FF));
            }
        }
        // ------------------------------------------------------------------------
        protected string GetString(int posicion, int max, UInt16[] valores)
        {
            int i;
            byte[] bytearray = new byte[max];

            for (i = 0; i < max; i++)
            {
                bytearray[i] = (byte)(valores[posicion + i / 2] >> ((i & 1) != 0 ? 8 : 0) & 0xFF);
                if (bytearray[i] == 0) break;
            }
            return (System.Text.ASCIIEncoding.ASCII.GetString(bytearray, 0, i));
        }
        // ------------------------------------------------------------------------
        protected string GetDate(int posicion, UInt16[] valores)
        {
            string fecha;
            if (valores.Length < posicion + 2) return "";
            else if (valores[posicion] == 0) return "";
            else
            {
                fecha = ((valores[posicion + 1] << 16 | valores[posicion]).ToString("X"));
                return fecha.Substring(6, 2) + "/" + fecha.Substring(4, 2) + "/" + fecha.Substring(0, 4);
            }
        }
        // ------------------------------------------------------------------------
        protected string GetTime(int posicion, UInt16[] valores)
        {
            if (valores.Length < posicion + 2) return "";
            else return ((valores[posicion + 1] << 16 | valores[posicion]).ToString());
        }
        // ------------------------------------------------------------------------
        protected string GetTimeOfDay(int posicion, UInt16[] valores)
        {
            if (valores.Length < posicion + 2) return "";
            else return ((valores[posicion + 1] << 16 | valores[posicion]).ToString("X"));
        }
        // ------------------------------------------------------------------------
        protected Int16 GetInt(int posicion, UInt16[] valores)
        {
            return (Int16)valores[posicion];
        }

        // ------------------------------------------------------------------------
        /// <summary>
        /// Lee palabras desde la memoria del PLC
        /// </summary>
        /// <param name="addr">Dirección del inicio del bloque de memoria</param>
        /// <param name="length">Cantidad de palabras a leer</param>
        /// <param name="Buffer">Buffer que recibirá los datos leidos</param>
        /// <returns></returns>
        protected bool ReadHoldingRegisterEx(int addr, int length, UInt16[] Buffer, int offset=0)
        {
            int max_length, i, j;
            byte[] valores = new byte[512];

            try
            {

                for (i = 0; i < length; i += 120)
                {
                    max_length = i + 120 < length ? 120 : length - i;

                    ReadHoldingRegister(0, addr + i, (byte)max_length, ref valores);
                    if (valores == null || valores.Length == 0)
                    {
                        bCommError = true;
                        break;
                    }

                    for (j = 0; j < max_length; j++)
                        Buffer[i + j + offset] = GetWord(j, valores);

                    //WordsRead += max_length;
                }
            }
            catch (Exception ex)
            {
                MessageLog.Add(ex.Message);
                bCommError = true;
            }

            return (!bCommError);
        }

        // ------------------------------------------------------------------------
        /// <summary>
        /// Escribe palabras a la memoria del PLC
        /// </summary>
        /// <param name="addr">Dirección del inicio del bloque de memoria</param>
        /// <param name="length">Cantidad de palabras a escribir</param>
        /// <param name="Buffer">Buffer que contiene los datos que se escribiran en el PLC</param>
        /// <returns></returns>
        protected bool WriteHoldingRegisterEx(int addr, int length, UInt16[] Buffer, int offset=0)
        {
            int max_length, i, j;
            byte[] valores;

            try
            {
                if (length == 1)
                {
                    valores = new byte[2];
                    SetWord(0, valores, Buffer[0 + offset]);
                    WriteSingleRegister(0, addr, valores);
                    WordsRead++;
                }
                else
                {
                    for (i = 0; i < length; i += 110)
                    {
                        max_length = i + 110 < length ? 110 : length - i;
                        valores = new byte[max_length * 2];

                        for (j = 0; j < max_length; j++)
                            SetWord(j, valores, Buffer[i + j + offset]);

                        WriteMultipleRegister(0, addr + i, max_length, valores);
                        if (bCommError)
                            break;

                        WordsRead += max_length;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageLog.Add(ex.Message);
                bCommError = true;
            }

            return (!bCommError);
        }

        // ------------------------------------------------------------------------
        void ProcesarError(int ID, Byte function, Byte exception)
        {
            String exc = "Error de MODBUS: ";
            switch (exception)
            {
                case excIllegalFunction: exc += "¡Función Inválida!"; break;
                case excIllegalDataAdr: exc += "¡Direccion de datos inválida!"; break;
                case excIllegalDataVal: exc += "¡Valor de datos inválido!"; break;
                case excSlaveDeviceFailure: exc += "¡Falla del dispositivo esclavo!"; break;
                case excAck: exc += "¡Acknowledge!"; break;
                case excMemParityErr: exc += "¡Error de paridad de memoria!"; break;
                case excGatePathUnavailable: exc += "¡Ruta al Gateway no disponible!"; break;
                case excExceptionTimeout: exc += "¡Time-out del esclavo!"; break;
                case excExceptionConnectionLost: exc += "¡Se perdió la conexión!"; break;
                case excExceptionNotConnected: exc += "¡Sin conexión!"; break;
            }

            MessageLog.Add(exc);
            bCommError = true;
        }

        public virtual bool Sync()
        {
            return (true);
        }

        public virtual bool Sync(int modo)
        {
            return (true);
        }
    }
}
