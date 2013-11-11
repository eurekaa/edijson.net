using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net;
using System.Configuration;

namespace Edijson.Core {

    public class Utils {

        public static string Capitalize(string str) {
            return str.Substring(0, 1).ToUpper() + str.Substring(1, str.Length - 1);
        }

    }
}
