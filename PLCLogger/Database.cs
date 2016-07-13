using System;
using System.Collections.Generic;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace PLCLogger
{
    public class Database : IDisposable
    {

        protected static Configuration cfg;
        protected static ISessionFactory sessionFactory = null;
        protected static ITransaction transaction = null;
        public static bool error;

        public Log MessageLog;
        public enum Modos
        {
            Guardar, //escribe en la db las variables leidas del PLC
            LeerEscrituras   //obtiene desde la db las variables que deben ser escritas en el PLC  
        }
        public string quotes(string str)
        {
            return ("'" + str + "'");
        }

        public Database()
        {
            this.ConfigurarNHibernate();
            MessageLog = new Log("Database");
           
        }


        public void Dispose()
        {
 
        }
      

        //------------------------------------------------------------------------------------------------------------------
        bool EscribirDatosVariables(PLC_Interface plc)
        {
            bool retval = true;
            plc.Variables_Escritura = new List<Variable>();
            try
            {
                    using(ISession session = sessionFactory.OpenSession())
                    using (ITransaction tx = session.BeginTransaction())
                    {
                        string hql = "from Variable v where v.escribir=:escribir and v.instante_escritura>=:t_max_escritura";
                        IQuery query = session.CreateQuery(hql);
                        query.SetParameter("escribir", true);
                        //define un tiempo máximo dentro del cual la escritura debe ser efectuada
                        query.SetParameter("t_max_escritura", DateTime.Now.AddSeconds(-10));
                        var var_a_escribir = (List<Variable>)query.List<Variable>();
                        foreach (var v in var_a_escribir)
                        {
                            v.address = Config.convertAdrress(v.direccion, 2);
                            if (v.type == "bit") v.subaddress = Config.convertAdrress(v.direccion, 3);
                            plc.Variables_Escritura.Add(v);

                        }

                        query = session.CreateQuery("from Variable v where v.escribir=:escribir");
                        query.SetParameter("escribir", true);
                        List<Variable> var_no_escritas = (List<Variable>)query.List<Variable>();

                        foreach (Variable v in var_no_escritas)
                        {
                            hql = "update Variable set valor_escritura=:valor, instante_escritura=:inst, escribir=:escribir where name=:name ";
                            query = session.CreateQuery(hql);
                            query.SetParameter("valor", null);
                            query.SetParameter("escribir", false);
                            query.SetParameter("name", v.name);
                            query.SetParameter("inst", null);
                            query.ExecuteUpdate();
                        }
                        tx.Commit();
                    }
                }
                
            
            catch (Exception ex)
            {
                MessageLog.Add(ex.Message);
                retval = false;
            }
            return retval;
        }

        
        //------------------------------------------------------------------------------------------------------------------
        bool GuardarDatosVariables(PLC_Interface plc)
        {
           
            var retval = true;
            List<Variable> variables_db = null;
            using(var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                string hql = "from Variable";
                IQuery query = session.CreateQuery(hql).SetMaxResults(32000);
                variables_db = (List<Variable>)query.List<Variable>();
                tx.Commit();
            }
           
            // Lee las variables para comprobar si hay cambios
            // traer las variables de la db para ver si han cambiado

            try
            {

                var valores = new List<string>();
                var session = sessionFactory.OpenSession();
                using (var tx = session.BeginTransaction())
                {
                    for (int i = 0; i < plc.Variables.Count; i++)
                    {

                        plc.Variables[i].fecha = DateTime.Now;
                        var variableDB = variables_db.Find(var => var.name == plc.Variables[i].name);

                        // si no está cargada, la crea
                        if (variableDB == null)
                        {
                            //testear inserts, nhibernate
                            session.Save(plc.Variables[i]);

                        }
                        else
                        {

                            //  session.Update(plc.Variables[i]);

                            string hql = "update Variable set address=:dir,type=:type,valor=:valor, fecha=:fecha where name=:name ";
                            IQuery query = session.CreateQuery(hql);
                            query.SetParameter("valor", plc.Variables[i].valor);
                            query.SetParameter("dir", plc.Variables[i].address);
                            query.SetParameter("type", plc.Variables[i].type);
                            query.SetParameter("name", plc.Variables[i].name);
                            query.SetParameter("fecha", plc.Variables[i].fecha);

                            query.ExecuteUpdate();


                            //actualiza variables_log en caso de un cambio en el valor

                            if (plc.Variables[i].valor != variableDB.valor)
                            {
                                var vl = new VariableLog
                                {
                                    fecha = DateTime.Now,
                                    valor = plc.Variables[i].valor,
                                    name = plc.Variables[i].name
                                };
                                session.Save(vl);
                            }

                        }

                    }
                    tx.Commit();
                }
               
            }

            catch (Exception ex)
            {
                MessageLog.Add(ex.Message);
                retval = false;
            }

            return (retval);
        }

        public bool Sync(PLC_Interface plc, Modos modo)
        {

            var retval = false;
                 switch (modo)
                    {
                        case Modos.Guardar:
                            retval = GuardarDatosVariables(plc);
                            break;
                        case Modos.LeerEscrituras:
                            retval = EscribirDatosVariables(plc);
                            break;
                        default:
                            MessageLog.Add(this.ToString() + ": No se reconoce modo=" + modo.ToString());
                            break;
                    }


            return (retval);
        }
        public void ConfigurarNHibernate()
        {
            try
            {
                cfg = new Configuration();
                cfg.Configure();
                Assembly assembly = typeof(Variable).Assembly;
                cfg.AddAssembly(assembly);
                sessionFactory = cfg.BuildSessionFactory();
                //Crea las tablas con las columnas definidos en los *.hbm.xml. Si existen las deja como estaban.
                new SchemaUpdate(cfg).Execute(true, false);
                error = false;
            }
            catch (Exception e)
            {
                MessageLog.Add(e.Message);
            }
        }
    }
}
