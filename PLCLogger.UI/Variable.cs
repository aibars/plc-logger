using System;

namespace PLCLogger
{
    public class Variable
    {
        public virtual DateTime InstanteEscritura { get; set; }

        public virtual bool Escribir { get; set; }

        public virtual string ValorEscritura { get; set; }

        public virtual string Name { get; set; }

        public virtual DateTime Fecha { get; set; }

        public virtual int Id { get; set; }

        public virtual bool Lh { get; set; }

        public virtual string Type { get; set; }

        public virtual string Direccion { get; set; }

        public virtual int Address { get; set; }

        public virtual int Subaddress { get; set; }

        public virtual string Valor { get; set; }

        public virtual int CantElem { get; set; }
    }
}