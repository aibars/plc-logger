using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Globalization;

namespace PLCLogger
{
    public partial class FPrincipal : Form
    {
        public static PLC_Interface plc;
        static int PLCPlanta_Period1, PLCPlanta_Period2;
        public static Database DB;
        public static Config config;
        public static Log MessageLog;
        public static LogFile LogFile;

        bool b_init_ok, b_terminate;
        int WD_terminate;

        int PLCPlanta_PeriodTimer1;

        //------------------------------------------------------------------------------
        public FPrincipal()
        {
            InitializeComponent();
            try
            {
                plc = new PLC_Interface();
                config = new Config();
                DB = new Database();
                LogFile = new LogFile();
                MessageLog = new Log("FPrincipal");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                MessageLog.Add(e.Message);
            }
            b_init_ok = false;
            b_terminate = false;


            if (!config.Load("config.json"))
            {
                MessageBox.Show("Error al abrir archivo de configuración.");
                MessageLog.Add(config.MessageLog);
                //cerrar la aplicación
            }
            else
            {

            //Le asigna la configuración al plc
            plc.Protocol = config.Protocol;
            switch (plc.Protocol)
            {
                case "MODBUS/TCP":
                    {
                        plc.IPAddress = config.IPAddrPLC;
                        plc.Port = config.Port;
                        plc.UnitID = config.UnitID;
                        break;
                    }
                case "MODBUS/RTU":
                    {
                        plc.SerialPort = config.SerialPort;
                        plc.BaudRate = config.BaudRate;
                        plc.Parity = config.Parity;
                        plc.StopBits = config.StopBits;
                        plc.Type = config.Type;
                        plc.UnitID = config.UnitID;
                        break;
                    }
             }

                PLCPlanta_Period1 = config.DataReloadPeriod1;
                PLCPlanta_Period2 = config.DataReloadPeriod2;

                timer_UpdateUI.Start();
                BackgoundWorker_PLCSync.RunWorkerAsync();
                MessageLog.Add("Inicio ejecución programa.");

                b_init_ok = true;
            }
        }

        //--------------------------------------------------------------------------------

        private void FPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!b_terminate)
            {
                e.Cancel = true;
                WD_terminate = Environment.TickCount;
                BackgoundWorker_PLCSync.CancelAsync();
            }
            b_terminate = true;
        }

        //--------------------------------------------------------------------------------

        private void FPrincipal_Shown(object sender, EventArgs e)
        {
            FSplash Splash = new FSplash();
            Splash.ShowDialog();
        }

        //--------------------------------------------------------------------------------

        private void bw_PLCSync_DoWork(object sender, DoWorkEventArgs e)
        {
            MessageLog.Add("Inicio tarea de manejo de comunicaciones.");

            while (!BackgoundWorker_PLCSync.CancellationPending)
            {
                if(plc.Protocol== "MODBUS/TCP")  this.plc_tcp(); 
                if (plc.Protocol=="MODBUS/RTU") this.plc_rtu(); 
                
            }

            MessageLog.Add("Fin tarea de manejo de comunicaciones.");

            e.Cancel = true;
        }

        /// <summary>
        /// Actualiza la interfaz con el usuario
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_UpdateUI_Tick(object sender, EventArgs e)
        {
            bool b_terminate_ok = false;
            CultureInfo esAR = new CultureInfo("es-AR");

            // Maneja el cierre de la aplicación
            if (b_init_ok && b_terminate)
            {
                if (BackgoundWorker_PLCSync.IsBusy)
                {
                    if (Environment.TickCount - WD_terminate >= 10000)
                    {
                        MessageLog.Add("Error al terminar tarea de manejo de comunicaciones.");
                        b_terminate_ok = true;
                    }
                }
                else
                {
                    b_terminate_ok = true;
                }

                if (b_terminate_ok)
                {
                    if (!config.Save("config.xml"))
                    {
                        MessageBox.Show("Error al guardar archivo de configuración \"config.json\".");
                        MessageLog.Add(config.MessageLog);
                    }
                    MessageLog.Add("Fin ejecución programa.");
                    Close();
                }
            }

            for (int i = 0; i < MessageLog.Logs.Count; i++)
                listBox1.Items.Insert(0, MessageLog.Logs[i].ToString());
            while (listBox1.Items.Count > 100) listBox1.Items.RemoveAt(100);

            // Escribe el log de mensajes en el archivo de Log
            LogFile.write(MessageLog);

            // Actualiza el progreso de la comunicación con el PLC
            int progress = plc.progress;
            label5.Text = progress.ToString() + " %";
            progressBar1.Value = progress;

            if (BackgoundWorker_PLCSync.CancellationPending) label14.Text = "Terminando comunicaciones...";
            else if (plc.connected || plc.connected_rtu) label14.Text = "Conectado con PLC";
            else label14.Text = "Sin conexión con PLC";
        }

        private void plc_tcp()
        {
            //Comunicación con el PLC de la planta a través de Ethernet
            try
            {

                if (!plc.connected)
                {
                    if (plc.conectar())
                    {
                        plc.MessageLog.Add("Conectado con PLC en dirección IP:" + plc.IPAddress);
                    }
                    else
                    {
                        // Si no está conectado, espera 1000 ms antes de reintentar
                        System.Threading.Thread.Sleep(1000);
                    }
                    PLCPlanta_PeriodTimer1 = Environment.TickCount;
                }
                else
                {
                    // Lectura de los datos en modo 0
                    if (Environment.TickCount - PLCPlanta_PeriodTimer1 >= PLCPlanta_Period1)
                    {
                        PLCPlanta_PeriodTimer1 = Environment.TickCount;
                        bool result = plc.Sync_ReadMemory();
                        if (!result)
                        {
                            plc.desconectar();
                        }
                        else
                        {
                            DB.Sync(plc, Database.Modos.Guardar);
                            MessageLog.Add(DB.MessageLog);
                            DB.Sync(plc, Database.Modos.LeerEscrituras);
                            foreach (Variable var in plc.Variables_Escritura)
                            {
                                if (plc.Sync_WriteMemory(var))
                                {
                                    MessageLog.Add("Escrito " + var.valor + " en " + var.direccion);
                                }
                                else
                                {
                                    MessageLog.Add("Error al escribir en " + var.direccion);
                                }

                            }
                        }
                    }
                    else
                    {
                        // Si no llegó el momento de sincronizar, espera 100 ms antes de reintentar
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception err)
            {
                MessageLog.Add(err.Message + "\r\n" + err.StackTrace + "\r\n" + err.TargetSite + "\r\n");
            }
            MessageLog.Add(plc.MessageLog);
            MessageLog.Add(DB.MessageLog);
        }
        private void plc_rtu()
        {
            try
            {
                if (!plc.connected_rtu)
                {
                    
                    if (plc.conectar())
                    {
                        plc.MessageLog.Add("Conectado con PLC en puerto: " + plc.SerialPort);
                    }
                    else
                    {
                        // Si no está conectado, espera 1000 ms antes de reintentar
                        System.Threading.Thread.Sleep(1000);
                    }
                    PLCPlanta_PeriodTimer1 = Environment.TickCount;
                }
                else
                {
                    bool result;

                    // Lectura de los datos en modo 0
                    if (Environment.TickCount - PLCPlanta_PeriodTimer1 >= PLCPlanta_Period1)
                    {
                        PLCPlanta_PeriodTimer1 = Environment.TickCount;
                        result = plc.Sync_ReadMemory(); 
                        if (!result)
                        {
                            plc.desconectar();
                        }
                        else
                        {
                            DB.Sync(plc, Database.Modos.Guardar);
                            MessageLog.Add(DB.MessageLog);
                            DB.Sync(plc, Database.Modos.LeerEscrituras);
                            foreach (Variable var in plc.Variables_Escritura)
                            {
                                if (plc.Sync_WriteMemory(var)) 
                                {
                                    MessageLog.Add("Escrito " + var.valor_escritura + " en " + var.direccion);
                                }
                                else
                                {
                                    MessageLog.Add("Error al escribir en " + var.direccion);
                                }

                            }
                        }
                    }
                    else
                    {
                        // Si no llegó el momento de sincronizar, espera 100 ms antes de reintentar
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception err)
            {
                
                MessageLog.Add(err.Message + "\r\n" + err.StackTrace + "\r\n" + err.TargetSite + "\r\n");
            }
            MessageLog.Add(plc.MessageLog);
            MessageLog.Add(DB.MessageLog);
        }

    }
}
