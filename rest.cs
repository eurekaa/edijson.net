using System;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Web;
using System.Web.SessionState;
using Edijson.Core;
using log4net;
using System.Data.SqlClient;
using System.Configuration;


namespace Edijson {

    public class Rest : IHttpHandler, IRequiresSessionState {


        #region proprietà

        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion


        #region costruttori

        public Rest() { }

        public bool IsReusable {
            get { return false; }
        }

        #endregion


        #region metodi

        /// <summary>Punto di ingresso di tutte le chiamate ajax, redirige l'esecuzione al metodo indicato nell'uri (method://path1/path2/..).</summary>        
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        /// <returns>void</returns>
        public void ProcessRequest(HttpContext context) {

            // configure lo4net
            log4net.Config.XmlConfigurator.Configure();

            string verb = "";
            string uri = "";
            try {
                Security.Run(context); // controlla gli accessi al rest service                
                string[] path = context.Request.Path.Substring(1, context.Request.Path.Length - 1).Split('/');                
                uri = path[1] + "://";                
                uri += (path.Length > 2) ? path[2] + "/" + path[3] : "";                                
                uri += (path.Length == 5) ? "/" + path[4] : "";
                string method_name = Utils.Capitalize(path[1]);

                Hashtable parameters = (context.Request["parameters"] != null && context.Request["parameters"] != "undefined") ? Serializer.UnserializeObject(context.Request["parameters"], typeof(Hashtable), SerializationType.JSON) : new Hashtable();
                Hashtable options = (context.Request["options"] != null && context.Request["options"] != "undefined") ? Serializer.UnserializeObject(context.Request["options"], typeof(Hashtable), SerializationType.JSON) : new Hashtable();
                verb = (options["verb"] == null) ? context.Request.HttpMethod.ToString().ToUpper() : options["verb"].ToString().ToUpper();
                Type type = this.GetType();
                MethodInfo method = type.GetMethod(method_name);
                object[] args = { verb, uri, parameters, options, context };
                method.Invoke(this, args);
                log.Debug(verb + " " + uri);
            }catch (EdijsonError ex){
                log.Error(verb + " " + uri + " - " + ex.Message, ex);
                EdijsonError error = new EdijsonError();
                error.Message = ex.Message;
                error.Source = ex.Source;
                error.StackTrace = ex.StackTrace;
                string output = Serializer.SerializeObject(error, SerializationType.JSON);
                context.Response.Write(output);
            } catch (Exception ex) {
                // trovo il vero errore all'interno di tutti i rimbalzi di eccezioni causati dalla reflection
                while (ex.Message == "Exception has been thrown by the target of an invocation." || ex.Message == "Eccezione generata dalla destinazione di una chiamata.") {
                    ex = ex.InnerException;
                }
                log.Error(verb + " " + uri + " - " + ex.Message, ex);
                EdijsonError error = new EdijsonError();
                error.Message = ex.Message;
                error.Source = ex.Source;
                error.StackTrace = ex.StackTrace;
                string output = Serializer.SerializeObject(error, SerializationType.JSON);
                context.Response.Write(output);
            }
        }


        /// <summary>Invoca il metodo di un Api (api://classe/metodo?parameters).</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hashtable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        /// <returns>void</returns>
        public void Api(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            log.Debug(verb);
            log.Debug(uri);
            String[] path = uri.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries)[1].Split('/');
            Type type = this.GetType().Assembly.GetType("Edijson.Api." + Utils.Capitalize(path[0]));
            MethodInfo method = type.GetMethod(Utils.Capitalize(path[1]));
            object[] args = { verb, uri, parameters, options, context };
            dynamic resultset = method.Invoke(this, args);
            string output = "";
            if (resultset.GetType().FullName == "System.Data.DataSet") {
                resultset = (DataSet)resultset;
                int tot = resultset.Tables[0].Rows.Count;
                Database database = new Database();
                resultset = database.Paginate(resultset, options["pagination"]);
                output = "{\"count\": " + tot + ", \"data\": " + Serializer.SerializeObject(resultset, SerializationType.JSON) + "}";
            } else {
                output = Serializer.SerializeObject(resultset, SerializationType.JSON);
            }
            context.Response.Write(output);
        }


        /// <summary>Gestisce le transazioni sul database (transaction://method[open,commit,rollback]/id).</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hashtable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        public void Transaction(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            String[] path = uri.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries)[1].Split('/');
            string method = path[0].ToUpper();
            string id = path[1];

            Database database = null;
            SqlConnection connection = null;
            SqlTransaction transaction = null;

            switch (method) {

                case "OPEN":                
                database = new Database();
                connection = database.Connect();
                transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted, id);
                context.Session["transaction." + id] = transaction;
                break;

                case "COMMIT":
                transaction = (SqlTransaction)context.Session["transaction." + id];
                transaction.Commit();
                //transaction.Connection.Close(); // la connessione viene chiusa automaticamente con commit o rollback???
                transaction.Dispose();
                context.Session.Remove("transaction." + id);
                break;

                case "ROLLBACK":
                transaction = (SqlTransaction)context.Session["transaction." + id];
                transaction.Rollback();
                //transaction.Connection.Close(); // la connessione viene chiusa automaticamente con commit o rollback???
                transaction.Dispose();
                context.Session.Remove("transaction." + id);
                break;
            }
        }


        /// <summary>Interroga una vista sul database (view://schema/view?parameters).</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hashtable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        public void View(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            this.Table(verb, uri, parameters, options, context);
        }


        /// <summary>Legge o scrive dati su una tabella del database (table://schema/table?parameters).</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hashtable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        public void Table(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            String[] path = uri.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries)[1].Split('/');
            string outputType = (options["outputType"] != null) ? options["outputType"].ToString() : "json";
            string schema = path[0];
            string table = path[1];
            string id = (path.Length > 2) ? path[2] : null;
            uri += (id != null) ? "/" + id : "";
            if (id != null && !parameters.Contains(ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"]))
            {
                parameters.Add(ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"], id);
            }
            switch (verb) { // mapping HttpVerb > SqlMethod
                case "GET":
                parameters.Add("@@ACTION", "SELECT");
                break;
                case "POST":
                parameters.Add("@@ACTION", "INSERT");
                break;
                case "PUT":
                parameters.Add("@@ACTION", "UPDATE");
                break;
                case "DELETE":
                parameters.Add("@@ACTION", "DELETE");
                Boolean physical = (options["physical"] != null) ? Boolean.Parse(options["physical"].ToString()) : false;
                parameters.Add("@@PHYSICAL", physical);
                break;
            }
            Database database = new Database();
            String storedProcedure = ConfigurationManager.AppSettings["EDIJSON_DATABASE_SCHEMA"] + "." + schema + "_" + table;
            // gestione transazioni            
            SqlTransaction transaction = null;
            SqlConnection connection = null;
            if (options["transaction"] != null) {
                transaction = (SqlTransaction)context.Session["transaction." + options["transaction"]];
                connection = transaction.Connection;
            }
            DataSet resultset = database.ExecuteStoredProcedure(storedProcedure, parameters, connection, transaction);
            resultset = database.Paginate(resultset, options["pagination"]);
            String output = Serializer.SerializeDataSet(resultset, outputType, true);
            context.Response.Write(output);
        }


        /// <summary>Esegue una stored procedure sul database (procedure://schema/procedure?parameters).</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hashtable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        public void Procedure(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            String[] path = uri.Split(new string[] { "://" }, StringSplitOptions.RemoveEmptyEntries)[1].Split('/');
            string schema = path[0];
            string procedure = path[1];
            string outputType = (options["outputType"] != null) ? options["outputType"].ToString() : "json";
            string storedProcedure = schema + "." + procedure;
            Database database = new Database();
            // gestione transazioni            
            SqlTransaction transaction = null;
            SqlConnection connection = null;
            if (options["transaction"] != null) {
                transaction = (SqlTransaction)context.Session["transaction." + options["transaction"]];
                connection = transaction.Connection;
            }
            DataSet resultset = database.ExecuteStoredProcedure(storedProcedure, parameters, connection, transaction);
            resultset = database.Paginate(resultset, options["pagination"]);
            String output = Serializer.SerializeDataSet(resultset, outputType, true);
            context.Response.Write(output);
        }


        /// <summary>Carica un file nel filesystem del server.</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hashtable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        public void Upload(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            HttpPostedFile file = context.Request.Files["file"];
            string filePath = context.Request["filePath"];
            file.SaveAs(context.Server.MapPath(filePath + "/" + file.FileName));
        }


        /// <summary>Scarica un file dal filesystem del server.</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hashtable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        public void Download(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            string file = context.Request["file"].ToString();            
            string filename = file.Split('/')[file.Split('/').Length - 1];
            context.Response.ContentType = "application/octet-stream";
            context.Response.AddHeader("Content-Disposition", String.Format("attachment;filename=\"{0}\"", filename));
            context.Response.TransmitFile(file);
            context.Response.End();
        }

        #endregion

    }
}
