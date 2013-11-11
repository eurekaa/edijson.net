using System;
using System.Web;
using System.Collections;
using System.Data;
using System.Collections.Generic;
using log4net;
using System.Configuration;

namespace Edijson.Core {

    static class Security {

        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Run(HttpContext context) {

            string username = context.Request["EDIJSON_SECURITY_USERNAME"];
            string password = context.Request["EDIJSON_SECURITY_PASSWORD"];

            // se previsto consento l'accesso solo alle connessioni protette (https).
            if (Boolean.Parse(ConfigurationManager.AppSettings["EDIJSON_SECURITY_HTTPS"].ToString()) && !context.Request.IsSecureConnection) {
                throw new EdijsonError("Connessione server negata, client non fidato, richiesto protocollo Https.");
            }
            // controllo che siano presenti nella richiesta username e password
            if (username == null || password == null) {
                throw new EdijsonError("Connessione server negata, client anonimo.");
            }
            // controllo username e password
            if (username != ConfigurationManager.AppSettings["EDIJSON_SECURITY_USERNAME"].ToString() || password != ConfigurationManager.AppSettings["EDIJSON_SECURITY_PASSWORD"].ToString()) {
                throw new EdijsonError("Connessione server negata, client non riconosciuto.");
            }

        }

    }
}
