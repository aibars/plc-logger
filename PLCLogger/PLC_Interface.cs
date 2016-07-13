using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace PLCLogger
{
    public class PLC_Interface : PLC
    {
        
        public List<Variable> Variables;
        public List<Variable> Variables_Escritura;
        public UInt16[] MemoriaPLC;
        public UInt16[] MemoriaPLC_Escritura; //WriteHoldingRegisterEx requiere una estructura del mismo tipo que la memoria del plc
        public Config config;
        public struct Intervalo
        {
            public int i;
            public List<Variable> variables;
        }
        public List<Intervalo> intervalos;
        public PLC_Interface() : base()
        {
            MemoriaPLC = new UInt16[32000];
            MemoriaPLC_Escritura = new UInt16[32000];
            Variables = new List<Variable>();
            config = new Config();
        }
       
        public bool Sync_ReadMemory()
        {
         Variables =  config.leerVariablesJSON();
         WordsRead = 0;   
         WordsReadMax = Variables.Count;
            //intervalos = this.armarIntervalos();
            
            switch(base.Protocol)
            {
                case "MODBUS/TCP":
                   
                    bool retval = ReadHoldingRegisterEx(Variables[0].address, Variables[Variables.Count-1].address, MemoriaPLC, Variables[0].address);
                   
                   
                    if (!retval) return (false);
                  
                break;
                case "MODBUS/RTU":
                            foreach (Intervalo intervalo in intervalos)
                            {
                                if (intervalo.variables.Count == 1)
                                {
                                    if (intervalo.variables[0].cant_elem == 1)
                                    {
                                        //leer una variable de un elemento
                                        ushort address = (ushort)intervalo.variables[0].address;
                                        MemoriaPLC[intervalo.variables[0].address] = mm.ReadHoldingRegisters(1, address, 1).First();
                                    }
                                    else
                                    {
                                        //leer la cant_elem de una variable
                                        for (int i = intervalo.variables[0].address; i < intervalo.variables[0].address+intervalo.variables[0].cant_elem; i++)
                                            MemoriaPLC[i] = mm.ReadHoldingRegisters(1, (ushort)i, 1).First();
                                    }
                                }
                                else
                                {
                                    if (intervalo.variables[intervalo.variables.Count - 1].cant_elem == 1)
                                    {
                                        //leer desde el comienzo del intervalo hasta la posición final
                                        for (int i = intervalo.variables[0].address, j = 0; i < intervalo.variables.Count + intervalo.variables[0].address; i++, j++)
                                            MemoriaPLC[i] = mm.ReadHoldingRegisters(1, (ushort)i, (ushort)intervalo.variables[j].cant_elem).First();
                                    }
                                    else
                                    {
                                        //leer desde el comienzo del intervalo hasta la posición final más cant_elem de la última variable; 
                                        int max = intervalo.variables.Count;
                                        for (int i = intervalo.variables[0].address; i < intervalo.variables[0].address + max + intervalo.variables[max].cant_elem; i++)
                                            MemoriaPLC[i] = mm.ReadHoldingRegisters(1, (ushort)i, 1).First();
                                    }
                                }
                            }
                break;
        }

         
            foreach (Variable var in Variables)
               {
                   
                   switch (var.type)
                   {
                       case "bool":
                               var.valor = GetBit(var.address, var.lh, MemoriaPLC).ToString();
                               break;
                       case "int":
                               var.valor = GetInt(var.address, MemoriaPLC).ToString();
                               break;
                       case "byte": 
                               var.valor = GetByte(var.address, var.lh, MemoriaPLC).ToString();
                               break;
                       case "dint":
                               var.valor = GetDInt(var.address, MemoriaPLC).ToString();
                               break;
                       case "uint":
                               var.valor = MemoriaPLC[var.address].ToString();
                               break;
                       case "udint":
                               var.valor = GetUDInt(var.address, MemoriaPLC).ToString();
                               break;
                       case "real":
                               var.valor = GetFloat(var.address, MemoriaPLC).ToString();
                               break;
                       case "string":
                               var.valor = GetString(var.address, 50, MemoriaPLC);
                               break;
                       case "date":
                               var.valor = GetDate(var.address, MemoriaPLC);
                               break;
                       case "time":
                               var.valor = GetTime(var.address, MemoriaPLC);
                               break;
                       case "timeofday":
                               var.valor = GetTimeOfDay(var.address, MemoriaPLC);
                               break;
                       case "bit": 
                               var.valor = GetBit(var.address, var.subaddress, MemoriaPLC).ToString();
                               break;
                   }
                   WordsRead++;
           }

            return (true);
        }
        public bool Sync_WriteMemory(Variable var)
        {
            switch (var.type)
                        {
                            case "bool":
                                {
                                    SetBit(var.address, true, MemoriaPLC_Escritura, var.valor_escritura);
                                    var.cant_elem = 1;
                                    break;
                                }
                           case "int":
                                    {
                                        SetInt(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 1;
                                        break;
                                    }
                            case "byte":
                                    {
                                        SetByte(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 1;
                                        break;
                                    }
                            case "dint":
                                    {
                                        SetDInt(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 2;
                                        break;
                                    }
                            case "uint":
                                    {
                                        SetUInt(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 1;
                                        break;
                                    }
                            case "udint":
                                    {
                                        SetUDInt(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 2;
                                        break;
                                    }
                            case "real":
                                    {
                                        SetFloat(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 2;
                                        break;
                                    }
                            case "string": 
                                    {
                                        SetString(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 16;
                                        break;
                                    }
                            case "date":
                                    {
                                        SetDate(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 2;
                                        break;
                                    }
                            case "time":
                                    {
                                        SetTime(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 2;
                                        break;
                                    }
                            case "timeofday":
                                    {
                                        SetTimeOfDay(var.address, MemoriaPLC_Escritura, var.valor_escritura);
                                        var.cant_elem = 2;
                                        break;
                                    }
                            case "bit":
                                    {
                                        SetBit(var.address, var.subaddress, MemoriaPLC_Escritura, MemoriaPLC[var.address], var.valor_escritura);
                                        var.cant_elem = 1;
                                        break;
                                    }
                        }
                        
              switch (Protocol)       
              {
                  case "MODBBUS/TCP": if (!WriteHoldingRegisterEx(var.address, var.cant_elem, MemoriaPLC_Escritura, var.address)) return (false); 
                                        return true;
                  case "MODBUS/RTU":
                                        for (int i = 0; i < var.cant_elem; i++)
                                        {
                                            mm.WriteSingleRegister(UnitID, (ushort)(var.address+i), (ushort)MemoriaPLC_Escritura[var.address+i]); 
                                        }
                                        return true;
                  default: return false;
              }
        }
        /// <summary>
        /// Arma intervalos de posiciones de memoria consecutivas.
        /// </summary>
        public List<Intervalo> armarIntervalos()
        {
            
            Variable aux = new Variable();
            for (int i = 0; i < Variables.Count; i++)
               for (int j =0; j < Variables.Count-1; j++)
                   if (Variables[j].address > Variables[j+1].address)
                   {
                       aux = Variables[j+1];
                       Variables[j+1] = Variables[j];
                       Variables[j] = aux;
                   }
            
            var g = new Intervalo();
            List<Intervalo> grupos = new List<Intervalo>();
            int id = 0;
            g.i = id;
            g.variables = new List<Variable> {Variables[0]};
            grupos.Add(g);

            for (int i = 0; i < Variables.Count-1; i++)
            {
                if ((Variables[i + 1].address - Variables[i].address) == 1)
                    grupos[id].variables.Add(Variables[i + 1]);
                
                else
                {
                    id++;
                    g = new Intervalo {i = id, variables = new List<Variable> {Variables[i + 1]}}; //confio
                    grupos.Add(g);
                }
            }
            return grupos;
        }
    }
}
