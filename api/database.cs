using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Edijson.Core;
using System.Data;
using log4net;
using System.Collections;
using System.Web;

namespace Edijson.Api
{

    public enum TableType { TABLE, VIEW };

    class Database
    {

        #region proprietà

        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region publich methods

        public static object Compile(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {        

            try
            {

                log4net.Config.XmlConfigurator.Configure();

                // elenco degli schema da escluere
                ArrayList excludedSchemas = new ArrayList(ConfigurationManager.AppSettings["EDIJSON_DATABASE_EXCLUDED_SCHEMAS"].ToString().ToUpper().Split(','));

                DataTable tables = null;
                DataTable views = null;
                DataTable columns = null;
                string catalog = "";
                string schema = "";
                string table = "";
                string view = "";

                // astraggo le tabelle
                tables = Database.SelectTables();
                for (int i = 0; i < tables.Rows.Count; i++)
                {
                    catalog = tables.Rows[i]["table_catalog"].ToString();
                    schema = tables.Rows[i]["table_schema"].ToString();
                    table = tables.Rows[i]["table_name"].ToString();
                    columns = Database.SelectColumns(schema, table);
                    if (!excludedSchemas.Contains(schema.ToUpper()))
                    {
                        Database.CreateStoredProcedure(TableType.TABLE, catalog, schema, table, columns);
                    }
                }

                // astraggo le viste
                views = Database.SelectViews();
                for (int i = 0; i < views.Rows.Count; i++)
                {
                    catalog = views.Rows[i]["table_catalog"].ToString();
                    schema = views.Rows[i]["table_schema"].ToString();
                    view = views.Rows[i]["table_name"].ToString();
                    columns = Database.SelectColumns(schema, view);
                    if (!excludedSchemas.Contains(schema.ToUpper()))
                    {
                        Database.CreateStoredProcedure(TableType.VIEW, catalog, schema, view, columns);
                    }
                }

            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                return false;
            }

            return true;

        }
        #endregion

        #region private methods
        private static DataTable SelectTables()
        {
            Edijson.Core.Database database = new Edijson.Core.Database();
            String sql = "SELECT table_catalog, table_schema, table_name FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' order by table_name asc";
            DataSet dati = database.ExecuteQuery(sql);
            return dati.Tables[0];
        }


        private static DataTable SelectViews()
        {
            Edijson.Core.Database database = new Edijson.Core.Database();
            String sql = "SELECT table_catalog, table_schema, table_name FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='VIEW' order by table_name asc";
            DataSet dati = database.ExecuteQuery(sql);
            return dati.Tables[0];
        }


        private static DataTable SelectColumns(string schema, string table)
        {
            Edijson.Core.Database database = new Edijson.Core.Database();
            String sql = "SELECT * FROM INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '" + schema + "' AND TABLE_NAME = '" + table + "'";
            DataSet dati = database.ExecuteQuery(sql);
            return dati.Tables[0];
        }


        private static void DropStoredProcedure(string schema, string table)
        {
            Edijson.Core.Database database = new Edijson.Core.Database();
            string sql = "";
            sql = "IF EXISTS ";
            sql += "(SELECT * FROM dbo.sysobjects ";
            sql += "WHERE id = object_id(N'[" + ConfigurationManager.AppSettings["EDIJSON_DATABASE_SCHEMA"] + "].[" + schema + "_" + table + "]') ";
            sql += "and OBJECTPROPERTY(id, N'IsProcedure') = 1) ";
            sql += "DROP PROCEDURE [" + ConfigurationManager.AppSettings["EDIJSON_DATABASE_SCHEMA"] + "].[" + schema + "_" + table + "] ";
            database.ExecuteQuery(sql);
        }


        private static void CreateStoredProcedure(TableType tableType, string catalog, string schema, string table, DataTable columns)
        {

            Edijson.Core.Database database = new Edijson.Core.Database();

            // elimino la stored proedure
            DropStoredProcedure(schema, table);

            // creo la stored procedure
            string sql = "";
            sql += "CREATE PROCEDURE " + "[" + ConfigurationManager.AppSettings["EDIJSON_DATABASE_SCHEMA"] + "].[" + schema + "_" + table + "] " + Environment.NewLine;

            // parametro che identifica il tipo di operazione da eseguire (insert|update|delete)
            sql += "@@ACTION varchar(10) = NULL,";
            if (tableType == TableType.TABLE)
            {
                sql += Environment.NewLine + "@@PHYSICAL tinyint = 0,";
            }

            // parametri 
            string nome_campo = "";
            string tipo_campo = "";
            string char_max_length = "";
            for (int i = 0; i < columns.Rows.Count; i++)
            {
                nome_campo = columns.Rows[i]["COLUMN_NAME"].ToString();
                tipo_campo = columns.Rows[i]["DATA_TYPE"].ToString();
                if (!nome_campo.StartsWith(ConfigurationManager.AppSettings["EDIJSON_DATABASE_CONTROL_FIELD"]))
                {
                    sql += Environment.NewLine;
                    if (columns.Rows[i]["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value && tipo_campo != "text" && tipo_campo != "ntext" && tipo_campo != "geography")
                    {
                        char_max_length = columns.Rows[i]["CHARACTER_MAXIMUM_LENGTH"].ToString();
                        char_max_length = (char_max_length == "-1") ? "MAX" : char_max_length;
                        sql += "@" + nome_campo + " " + tipo_campo + "(" + char_max_length + ") = NULL";
                    }
                    else if (tipo_campo == "decimal")
                    {
                        sql += "@" + nome_campo + " " + tipo_campo + "(" + columns.Rows[i]["NUMERIC_PRECISION"] + "," + columns.Rows[i]["NUMERIC_SCALE"] + ") = NULL";
                    }
                    else
                    {
                        sql += "@" + nome_campo + " " + tipo_campo + " = NULL";
                    }
                    sql += ",";
                }
            }
            sql = sql.Substring(0, sql.Length - 1); // (tolgo l'ultima virgola)
            sql += Environment.NewLine;


            // corpo intestazione
            sql += "AS BEGIN " + Environment.NewLine;
            sql += "SET NOCOUNT ON; " + Environment.NewLine + Environment.NewLine;

            // corpo select
            sql += "IF @@ACTION = 'SELECT' BEGIN" + Environment.NewLine;
            sql += "SELECT " + Environment.NewLine;
            string select = "";
            string from = "FROM " + schema + "." + table + Environment.NewLine + " WHERE 1=1 ";
            string where = "";
            bool hasOrder = false;
            for (int i = 0; i < columns.Rows.Count; i++)
            {
                nome_campo = columns.Rows[i]["COLUMN_NAME"].ToString();
                tipo_campo = columns.Rows[i]["DATA_TYPE"].ToString();
                if (!nome_campo.StartsWith(ConfigurationManager.AppSettings["EDIJSON_DATABASE_CONTROL_FIELD"]))
                { // escludo i campi di controllo
                    select += nome_campo + ",";
                }
                if (!nome_campo.StartsWith(ConfigurationManager.AppSettings["EDIJSON_DATABASE_CONTROL_FIELD"]) && tipo_campo != "text" && tipo_campo != "ntext")
                {
                    where += "AND (" + nome_campo + " = @" + nome_campo + " OR @" + nome_campo + " IS NULL) " + Environment.NewLine;
                }
                if (nome_campo == ConfigurationManager.AppSettings["EDIJSON_DATABASE_ORDER_FIELD"])
                {
                    hasOrder = true;
                }
            }
            sql += select.Substring(0, select.Length - 1) + Environment.NewLine;
            sql += from + Environment.NewLine;
            sql += where;
            if (tableType == TableType.TABLE)
            { // filtro (_deleted = 0) (no viste)
                sql += "AND " + ConfigurationManager.AppSettings["EDIJSON_DATABASE_DELETED_FIELD"] + " = 0 " + Environment.NewLine;
            }
            sql += "ORDER BY "; //order by
            sql += (hasOrder && ConfigurationManager.AppSettings["EDIJSON_DATABASE_ORDER_FIELD"] != "") ? ConfigurationManager.AppSettings["EDIJSON_DATABASE_ORDER_FIELD"] : ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"];
            sql += (!hasOrder) ? " ASC " : " ASC ";
            sql += Environment.NewLine;
            sql += "END" + Environment.NewLine + Environment.NewLine;

            if (tableType == TableType.TABLE)
            {
                // corpo insert
                sql += "ELSE IF @@ACTION = 'INSERT' BEGIN" + Environment.NewLine;
                sql += "INSERT INTO [" + schema + "].[" + table + "] (";
                for (int i = 0; i < columns.Rows.Count; i++)
                {
                    nome_campo = columns.Rows[i]["COLUMN_NAME"].ToString();
                    if (nome_campo != ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] && !nome_campo.StartsWith(ConfigurationManager.AppSettings["EDIJSON_DATABASE_CONTROL_FIELD"]))
                    {
                        sql += nome_campo + ",";
                    }
                }
                sql = sql.Substring(0, sql.Length - 1); // (tolgo l'ultima virgola)
                sql += ") " + Environment.NewLine;
                sql += "OUTPUT INSERTED." + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + Environment.NewLine;
                sql += "VALUES (";
                for (int i = 0; i < columns.Rows.Count; i++)
                {
                    nome_campo = columns.Rows[i]["COLUMN_NAME"].ToString();
                    if (nome_campo != ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] && !nome_campo.StartsWith(ConfigurationManager.AppSettings["EDIJSON_DATABASE_CONTROL_FIELD"]))
                    {
                        sql += "@" + nome_campo + ",";
                    }
                }
                sql = sql.Substring(0, sql.Length - 1); // (tolgo l'ultima virgola)
                sql += ")" + Environment.NewLine;
                sql += "END" + Environment.NewLine + Environment.NewLine;


                // corpo update
                sql += "ELSE IF @@ACTION = 'UPDATE' BEGIN" + Environment.NewLine;
                sql += "UPDATE [" + schema + "].[" + table + "] SET ";
                for (int i = 0; i < columns.Rows.Count; i++)
                {
                    nome_campo = columns.Rows[i]["COLUMN_NAME"].ToString();
                    if (nome_campo != ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] && !nome_campo.StartsWith(ConfigurationManager.AppSettings["EDIJSON_DATABASE_CONTROL_FIELD"]))
                    {
                        sql += Environment.NewLine + nome_campo + " = @" + nome_campo + ",";
                    }
                }
                sql = sql.Substring(0, sql.Length - 1); // (tolgo l'ultima virgola)
                sql += Environment.NewLine;
                sql += "OUTPUT INSERTED." + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + Environment.NewLine;
                sql += "WHERE " + schema + "." + table + "." + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + " = @" + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + Environment.NewLine;
                sql += "END " + Environment.NewLine + Environment.NewLine;


                // corpo logical delete
                sql += "ELSE IF @@ACTION = 'DELETE' AND @@PHYSICAL = 0 BEGIN" + Environment.NewLine;
                sql += "UPDATE [" + schema + "].[" + table + "] SET " + ConfigurationManager.AppSettings["EDIJSON_DATABASE_DELETED_FIELD"] + " = 1 " + Environment.NewLine;
                sql += "OUTPUT INSERTED." + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + Environment.NewLine;
                sql += "WHERE " + schema + "." + table + "." + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + " = @" + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + Environment.NewLine;
                sql += "END " + Environment.NewLine + Environment.NewLine;

                // corpo physical delete
                sql += "ELSE IF @@ACTION = 'DELETE' AND @@PHYSICAL = 1 BEGIN" + Environment.NewLine;
                sql += "DELETE FROM [" + schema + "].[" + table + "] " + Environment.NewLine;
                sql += "OUTPUT DELETED." + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + Environment.NewLine;
                sql += "WHERE " + schema + "." + table + "." + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + "= @" + ConfigurationManager.AppSettings["EDIJSON_DATABASE_IDENTITY_FIELD"] + Environment.NewLine;
                sql += "END " + Environment.NewLine + Environment.NewLine;
            }

            // corpo chiusura
            sql += "END " + Environment.NewLine;

            // installo la stored procedure
            database.ExecuteQuery(sql.ToString());
        }

        #endregion

    }
}
