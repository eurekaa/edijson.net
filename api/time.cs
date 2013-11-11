using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Web;
using log4net;

namespace Edijson.Api {

    class Time {

        #region proprietà

        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        /// <summary>Ritorna la data del server.</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hastable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        /// <returns>DateTime</returns>
        public static object Now(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            return DateTime.Now;
        }

    }
}
