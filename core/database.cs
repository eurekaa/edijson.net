using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using log4net;

namespace Edijson.Core {

    public class Database {

        #region proprietà

        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string connectionString = "";
        private SqlConnection connection = null;

        #endregion



        #region costruttori

        /// <summary>Istanzia la classe con la connection string di sistema (web.config).</summary>        
        /// <returns>Database</returns>
        public Database() {
            string host = ConfigurationManager.AppSettings["EDIJSON_DATABASE_HOST"].ToString();
            string username = ConfigurationManager.AppSettings["EDIJSON_DATABASE_USERNAME"].ToString();
            string password = ConfigurationManager.AppSettings["EDIJSON_DATABASE_PASSWORD"].ToString();
            string name = ConfigurationManager.AppSettings["EDIJSON_DATABASE_NAME"].ToString();
            this.connectionString = "Data Source=" + host + ";User ID=" + username + ";Password=" + password + ";Initial Catalog=" + name + ";Persist Security Info=True;";
        }


        /// <summary>Istanzia la classe con la connection string in argomento</summary>
        /// <param name="connesctionString">String: stringa di connessione al database.</param>  
        /// <returns>Database</returns>
        public Database(string connectionString) {
            this.connectionString = connectionString;
        }


        /// <summary>Istanzia la classe con una connessione già precedentemente aperta</summary>
        /// <param name="connesctionString">SqlConnection: connessione al database.</param>
        /// <returns>Database</returns>
        public Database(SqlConnection connection) {
            this.connection = connection;
        }

        #endregion



        #region metodi privati

        /// <summary>Crea i parametri sql per la query/stored procedure.</summary>
        /// <param name="command">SqlCommand: oggetto sul quale verranno appesi i parametri.</param>
        /// <param name="parameters">Hashtable: key|value con i parametri da creare.</param>
        /// <returns>Sqlcommand</returns>
        private SqlCommand CreateSqlParameters(SqlCommand command, Hashtable parameters = null) {
            if (parameters != null) {
                String parameterName = null;
                String parameterType = null;
                dynamic parameterValue = null;
                foreach (DictionaryEntry parameter in parameters) {
                    parameterName = (parameter.Key.ToString().Substring(0, 1) != "@") ? "@" + parameter.Key.ToString() : parameter.Key.ToString(); // aggiungo la "@" se non è presente                    
                    parameterValue = (parameter.Value != null) ? parameter.Value.ToString() : null;
                    if (parameterValue == null) {
                        command.Parameters.AddWithValue(parameterName, DBNull.Value);
                    } else {
                        parameterType = parameter.Value.GetType().ToString().Replace("System.", "");
                        if (parameterType == "Int32") {
                            command.Parameters.Add(parameterName, SqlDbType.Int).Value = int.Parse(parameterValue);
                        } else if (parameterType == "String") {
                            command.Parameters.Add(parameterName, SqlDbType.VarChar, parameterValue.Length).Value = parameterValue;
                        } else if (parameterType == "DateTime") {
                            command.Parameters.Add(parameterName, SqlDbType.DateTime).Value = Convert.ToDateTime(parameterValue);
                        } else if (parameterType == "Boolean") {
                            command.Parameters.Add(parameterName, SqlDbType.Bit).Value = bool.Parse(parameterValue);
                        } else {
                            command.Parameters.Add(parameterName, SqlDbType.Decimal).Value = Convert.ToDecimal(parameterValue);
                        }
                    }
                }
            }
            return command;
        }

        #endregion



        #region metodi pubblici

        /// <summary>Apre la connessione al database.</summary>
        /// <returns>Sqlconnection</returns>
        public SqlConnection Connect() {
            this.connection = new SqlConnection();
            this.connection.ConnectionString = this.connectionString;
            this.connection.Open();
            return this.connection;
        }


        /// <summary>Chiude la connessione al database.</summary>
        /// <returns>void</returns>
        public void Disconnect() {
            this.connection.Close();
        }


        /// <summary>Lancia una query sul database e ritorna i risultati in un DataSet.</summary>
        /// <param name="sql">String: comando sql da eseguire sul database.</param>
        /// <param name="parameters">Hashtable: [opzionale] parametri da inviare al comando sql.</param>
        /// <param name="connection">SqlConnection: [opzionale] connessione già aperta al database.</param>
        /// <param name="transaction">SqlTransaction: [opzionale] transazione in corso sul database.</param>
        /// <returns>DataSet</returns>
        public DataSet ExecuteQuery(string sql, Hashtable parameters = null, SqlConnection connection = null, SqlTransaction transaction = null) {
            DataSet resultset = new DataSet();
            try {
                // se viene passata una connessione in argomento uso quella (si presume che sia già aperta)
                if (connection != null) {
                    this.connection = connection;
                }
                // apro la connessione se non è già aperta
                if (this.connection == null || this.connection.State != ConnectionState.Open) {
                    this.Connect();
                }
                SqlCommand command = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                command.CommandType = CommandType.Text;
                command.Connection = this.connection;
                if (transaction != null) {
                    command.Transaction = transaction;
                }
                command.CommandText = sql;
                command = this.CreateSqlParameters(command, parameters);
                adapter.Fill(resultset);
            } finally {
                // chiudo la connessione solo se non c'è una transazione in corso.
                if (transaction == null) {
                    this.Disconnect();
                }
            }
            return resultset;
        }


        /// <summary>Lancia una stored procedure sul database e ritorna i risultati in un dataset.</summary>
        /// <param name="storedprocedure">String: il nome della stored procedure da eseguire sul database.</param>
        /// <param name="parameters">Hashtable: [opzionale] elenco di parametri da passare alla stored procedure.</param>
        /// <param name="connection">SqlConnection: [opzionale] connessione già aperta al database.</param>
        /// <param name="transaction">SqlTransaction: [opzionale] transazione in corso sul database.</param>
        /// <returns>DataSet</returns>
        public DataSet ExecuteStoredProcedure(string storedprocedure, Hashtable parameters = null, SqlConnection connection = null, SqlTransaction transaction = null) {
            DataSet resultset = new DataSet();
            try {
                // se viene passata una connessione in argomento uso quella (si presume che sia già aperta)
                if (connection != null) {
                    this.connection = connection;
                }
                // apro la connessione se non è già aperta
                if (this.connection == null || this.connection.State != ConnectionState.Open) {
                    this.Connect();
                }
                SqlCommand command = new SqlCommand();
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                command.CommandType = CommandType.StoredProcedure;
                command.Connection = this.connection;
                if (transaction != null) {
                    command.Transaction = transaction;
                }
                command.CommandText = storedprocedure;
                command = this.CreateSqlParameters(command, parameters);
                adapter.Fill(resultset);
            } finally {
                // chiudo la connessione solo se non c'è una transazione in corso.
                if (transaction == null) {
                    this.Disconnect();
                }
            }
            return resultset;
        }


        /// <summary>Riduce un DataSet all'intervallo specificato.</summary>
        /// <param name="resultset">DataSet: il dataset da ridurre.</param>
        /// <param name="pagination">Object: oggetto (array) contenente i valori di inizio e fine intervallo ex: "[0,20]".</param>
        /// <returns>DataSet</returns>
        public DataSet Paginate(DataSet resultset, Object pagination) {
            if (pagination != null) {
                int[] range = (int[])pagination;
                for (int i = 0; i < resultset.Tables[0].Rows.Count; i++) {
                    if (!(i >= range[0] && i <= range[1])) {
                        resultset.Tables[0].Rows[i].Delete();
                    }
                }
                resultset.Tables[0].AcceptChanges();
            }
            return resultset;
        }


        #endregion

    }
}
