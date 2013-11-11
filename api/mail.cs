using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net;
using System.Configuration;
using System.Collections;
using System.Web;
using log4net;

namespace Edijson.Api {

    public class Mail {

        #region proprietà

        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        /// <summary>Invia una mail.</summary>
        /// <param name="verb">String: verbo http della chiamata.</param>
        /// <param name="uri">String: uri della chiamata.</param>
        /// <param name="parameters">Hastable: parametri dell'uri.</param>
        /// <param name="options">Hashtable: opzioni della chiamata (outputType, paginazione, transazioni, ecc..).</param>
        /// <param name="context">HttpContext: contesto della chiamata http.</param>
        /// <returns>Boolean</returns>
        public static object Send(String verb, String uri, Hashtable parameters, Hashtable options, HttpContext context) {
            SmtpClient smtpServer = new SmtpClient(ConfigurationManager.AppSettings["EDIJSON_SMTP_HOST"]);
            smtpServer.Port = Convert.ToInt32(ConfigurationManager.AppSettings["EDIJSON_SMTP_PORT"]);
            smtpServer.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["EDIJSON_SMTP_USERNAME"], ConfigurationManager.AppSettings["EDIJSON_SMTP_PASSWORD"]);
            smtpServer.EnableSsl = false;
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(ConfigurationManager.AppSettings["EDIJSON_SMTP_FROM"]);
            string[] to = parameters["to"].ToString().Split(';');
            for (int i = 0; i < to.Length; i++) {
                mail.To.Add(to[i]);
            }
            mail.Subject = parameters["subject"].ToString();
            mail.Body = parameters["body"].ToString();
            mail.IsBodyHtml = true;
            smtpServer.Send(mail);

            return true;
        }

    }
}
