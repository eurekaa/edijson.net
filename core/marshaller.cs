using System;

namespace Edijson.Core {

    static class Marshaller {

        /// <summary>
        /// Permette di effettuare cambi sul tipo di dati estendendo la funzione Convert.ChangeType anche per i tipi Nullable
        /// Ex: Marshaller.ChangeType<int?>(value); dove "value" può anche essere 'null'.
        /// </summary>
        /// <param type="dynamic" name="value">Il valore che si vuole convetire.</param>        
        /// <returns>T</returns>
        public static T PerformConversion<T>(dynamic value) {
            Type conversionType = typeof(T);
            if (conversionType.IsGenericType &&
                conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
                if (value == null) { return default(T); }
                conversionType = Nullable.GetUnderlyingType(conversionType);
            }
            return (T)Convert.ChangeType(value, conversionType);
        }



        /// <summary>
        /// Permette di effettuare cambi sul tipo di dati (anche null) senza sapere qual'è il tipo di destinazione
        /// </summary>        
        /// <param type="dynamic" name="value">Il valore che si vuole convetire.</param>        
        /// <param type="Type" name="type">Il tipo in cui si vuole convertire.</param>        
        /// <returns>dynamic</returns>
        public static dynamic ChangeType(dynamic value, Type type) {
            dynamic convertedValue = null;
            if (type != null) {
                switch (type.ToString()) {
                    case "System.Nullable`1[System.Int32]":
                        convertedValue = Marshaller.PerformConversion<int?>(value);
                        break;
                    case "System.Nullable`1[System.Double]":
                        convertedValue = Marshaller.PerformConversion<double?>(value);
                        break;
                    case "System.Nullable`1[System.Boolean]":
                        convertedValue = Marshaller.PerformConversion<bool?>(value);
                        break;
                    case "System.Nullable`1[System.DateTime]":
                        convertedValue = Marshaller.PerformConversion<DateTime?>(value);
                        break;
                    default:
                        convertedValue = Convert.ChangeType(value, type);
                        break;
                }
            }
            return convertedValue;
        }

    }
}
