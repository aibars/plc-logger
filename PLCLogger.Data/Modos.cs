namespace PLCLogger.Data
{
    public enum Modos
    {
        Guardar, //escribe en la db las variables leidas del PLC
        LeerEscrituras //obtiene desde la db las variables que deben ser escritas en el PLC  
    }
}
