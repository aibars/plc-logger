using System;


namespace PLCLogger
{
    public class Variable
    {
        
        private string _name;
        private  string _type;
        private string _direccion;
        private int _address;
        private int _subaddress;
        private string _valor;
        private int _cant_elem;
        private bool _lh;
        private int _id;
        private DateTime _fecha;
        private DateTime _instante_escritura;
        private bool _escribir;
        private string _valor_escritura;

        public virtual DateTime instante_escritura
        {
            get { return _instante_escritura; }
            set { _instante_escritura = value; }
        }
        public virtual bool escribir
        {
            get { return _escribir; }
            set { _escribir = value; }
        }
        public virtual string valor_escritura
        {
            get { return _valor_escritura; }
            set { _valor_escritura = value; }
        }
       public virtual string name
        {
            get { return _name; }
            set { _name = value; }
        }
       public virtual DateTime fecha
       {
           get { return _fecha; }
           set { _fecha = value; }
       }
       public virtual int id
       {
           get { return _id; }
           set { _id=value; }
       }
       public virtual bool lh
       {
           get { return _lh; }
           set { _lh = value;}
       }

       public virtual string type
        {
            get { return _type; }
            set { _type = value; }
        }
       public virtual string direccion
        {
            get
            {
                return _direccion;
            }
            set
            {
                _direccion = value;
            }
        
        }
       public virtual int address
        {
            get { return _address; }
            set
            {
                _address = value;
            }
        }
       public virtual int subaddress
        {
            get { return _subaddress; }
            set
            {
                _subaddress = value;
            }
        }
       public virtual string valor
        {
            get { return _valor; }
            set { _valor = value; }
        }
        public virtual int cant_elem
        {
            get { return _cant_elem; }
            set { _cant_elem = value; }
        }
    }
}
