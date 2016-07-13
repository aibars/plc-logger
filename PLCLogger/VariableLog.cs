using System;


namespace PLCLogger
{
    public class VariableLog
    {
        private int _id;
        private string _name;
        private DateTime _fecha;
        private string _valor;

        public virtual string name
        {
            get { return _name; }
            set { _name = value; }
        }
        public virtual int id
        {
            get { return _id; }
            set { _id = value; }
        }
        public virtual DateTime fecha
        {
            get { return _fecha; }
            set { _fecha = value; }
        }
        public virtual string valor
        {
            get
            {
                return _valor;
            }
            set { _valor = value; }
        }
    }
}
