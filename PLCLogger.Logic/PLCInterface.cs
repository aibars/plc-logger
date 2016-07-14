using System;
using System.Collections.Generic;
using System.Linq;
using PLCLogger.Entities;

namespace PLCLogger.Logic
{
    public class PLCInterface : PLC
    {
        
        public List<Variable> Variables;
        public List<Variable> Variables_Escritura;
        public UInt16[] MemoriaPLC;
        public UInt16[] MemoriaPLC_Escritura; //WriteHoldingRegisterEx requiere una estructura del mismo tipo que la memoria del plc
        private readonly Config config;
        public struct Intervalo
        {
            public int i;
            public List<Variable> variables;
        }
        public List<Intervalo> intervalos;
        public PLCInterface(Config config) 
        {
            MemoriaPLC = new UInt16[32000];
            MemoriaPLC_Escritura = new UInt16[32000];
            Variables = new List<Variable>();
            this.config = config;
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
                   
                    bool retval = ReadHoldingRegisterEx(Variables[0].Address, Variables[Variables.Count-1].Address, MemoriaPLC, Variables[0].Address);
                   
                   
                    if (!retval) return (false);
                  
                break;
                case "MODBUS/RTU":
                            foreach (Intervalo intervalo in intervalos)
                            {
                                if (intervalo.variables.Count == 1)
                                {
                                    if (intervalo.variables[0].CantElem == 1)
                                    {
                                        //leer una variable de un elemento
                                        ushort Address = (ushort)intervalo.variables[0].Address;
                                        MemoriaPLC[intervalo.variables[0].Address] = mm.ReadHoldingRegisters(1, Address, 1).First();
                                    }
                                    else
                                    {
                                        //leer la CantElem de una variable
                                        for (int i = intervalo.variables[0].Address; i < intervalo.variables[0].Address+intervalo.variables[0].CantElem; i++)
                                            MemoriaPLC[i] = mm.ReadHoldingRegisters(1, (ushort)i, 1).First();
                                    }
                                }
                                else
                                {
                                    if (intervalo.variables[intervalo.variables.Count - 1].CantElem == 1)
                                    {
                                        //leer desde el comienzo del intervalo hasta la posición final
                                        for (int i = intervalo.variables[0].Address, j = 0; i < intervalo.variables.Count + intervalo.variables[0].Address; i++, j++)
                                            MemoriaPLC[i] = mm.ReadHoldingRegisters(1, (ushort)i, (ushort)intervalo.variables[j].CantElem).First();
                                    }
                                    else
                                    {
                                        //leer desde el comienzo del intervalo hasta la posición final más CantElem de la última variable; 
                                        int max = intervalo.variables.Count;
                                        for (int i = intervalo.variables[0].Address; i < intervalo.variables[0].Address + max + intervalo.variables[max].CantElem; i++)
                                            MemoriaPLC[i] = mm.ReadHoldingRegisters(1, (ushort)i, 1).First();
                                    }
                                }
                            }
                break;
        }

         
            foreach (Variable var in Variables)
               {
                   
                   switch (var.Type)
                   {
                       case "bool":
                               var.Valor= GetBit(var.Address, var.Lh, MemoriaPLC).ToString();
                               break;
                       case "int":
                               var.Valor = GetInt(var.Address, MemoriaPLC).ToString();
                               break;
                       case "byte": 
                               var.Valor = GetByte(var.Address, var.Lh, MemoriaPLC).ToString();
                               break;
                       case "dint":
                               var.Valor = GetDInt(var.Address, MemoriaPLC).ToString();
                               break;
                       case "uint":
                               var.Valor = MemoriaPLC[var.Address].ToString();
                               break;
                       case "udint":
                               var.Valor = GetUDInt(var.Address, MemoriaPLC).ToString();
                               break;
                       case "real":
                               var.Valor = GetFloat(var.Address, MemoriaPLC).ToString();
                               break;
                       case "string":
                               var.Valor = GetString(var.Address, 50, MemoriaPLC);
                               break;
                       case "date":
                               var.Valor = GetDate(var.Address, MemoriaPLC);
                               break;
                       case "time":
                               var.Valor = GetTime(var.Address, MemoriaPLC);
                               break;
                       case "timeofday":
                               var.Valor = GetTimeOfDay(var.Address, MemoriaPLC);
                               break;
                       case "bit": 
                               var.Valor = GetBit(var.Address, var.Subaddress, MemoriaPLC).ToString();
                               break;
                   }
                   WordsRead++;
           }

            return (true);
        }
        public bool Sync_WriteMemory(Variable var)
        {
            switch (var.Type)
                        {
                            case "bool":
                                {
                                    SetBit(var.Address, true, MemoriaPLC_Escritura, var.ValorEscritura);
                                    var.CantElem = 1;
                                    break;
                                }
                           case "int":
                                    {
                                        SetInt(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 1;
                                        break;
                                    }
                            case "byte":
                                    {
                                        SetByte(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 1;
                                        break;
                                    }
                            case "dint":
                                    {
                                        SetDInt(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 2;
                                        break;
                                    }
                            case "uint":
                                    {
                                        SetUInt(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 1;
                                        break;
                                    }
                            case "udint":
                                    {
                                        SetUDInt(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 2;
                                        break;
                                    }
                            case "real":
                                    {
                                        SetFloat(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 2;
                                        break;
                                    }
                            case "string": 
                                    {
                                        SetString(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 16;
                                        break;
                                    }
                            case "date":
                                    {
                                        SetDate(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 2;
                                        break;
                                    }
                            case "time":
                                    {
                                        SetTime(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 2;
                                        break;
                                    }
                            case "timeofday":
                                    {
                                        SetTimeOfDay(var.Address, MemoriaPLC_Escritura, var.ValorEscritura);
                                        var.CantElem = 2;
                                        break;
                                    }
                            case "bit":
                                    {
                                        SetBit(var.Address, var.Subaddress, MemoriaPLC_Escritura, MemoriaPLC[var.Address], var.ValorEscritura);
                                        var.CantElem = 1;
                                        break;
                                    }
                        }
                        
              switch (Protocol)       
              {
                  case "MODBBUS/TCP": if (!WriteHoldingRegisterEx(var.Address, var.CantElem, MemoriaPLC_Escritura, var.Address)) return (false); 
                                        return true;
                  case "MODBUS/RTU":
                                        for (int i = 0; i < var.CantElem; i++)
                                        {
                                            mm.WriteSingleRegister(UnitID, (ushort)(var.Address+i), (ushort)MemoriaPLC_Escritura[var.Address+i]); 
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
                   if (Variables[j].Address > Variables[j+1].Address)
                   {
                       aux = Variables[j+1];
                       Variables[j+1] = Variables[j];
                       Variables[j] = aux;
                   }
            
            var g = new Intervalo();
            var grupos = new List<Intervalo>();
            var id = 0;
            g.i = id;
            g.variables = new List<Variable> {Variables[0]};
            grupos.Add(g);

            for (int i = 0; i < Variables.Count-1; i++)
            {
                if ((Variables[i + 1].Address - Variables[i].Address) == 1)
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
