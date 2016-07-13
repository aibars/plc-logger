using System;

namespace PLCLogger
{
    public class VariableLog
    {
        public virtual string Name { get; set; }

        public virtual int Id { get; set; }

        public virtual DateTime Fecha { get; set; }

        public virtual string Valor { get; set; }
    }
}