using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using JsonFx.Json;
using log4net;
using System.Data;


namespace Edijson.Core {

    public enum SerializationType { BINARY, XML, JSON, JSONDB };


    public static class Serializer {

        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public static string SerializeDataSetJsonDb(DataSet dati, bool meta) {
            /* {meta: [.., ..], data: [.., ..] } */
            if (!(dati != null && dati.Tables.Count > 0 && dati.Tables[0].Rows.Count > 0))
                return "[]";
            string output = "";
            if (dati.Tables.Count > 0 && dati.Tables[0].Rows.Count > 0) {
                // intestazione jsondb
                if (meta) {
                    output += "meta:{";
                    output += "parameters:{";
                    string inType = "";
                    string outType = "";
                    for (int i = 0; i < dati.Tables[0].Columns.Count; i++) {
                        output += dati.Tables[0].Columns[i].ColumnName + ":";
                        inType = dati.Tables[0].Columns[i].DataType.ToString().ToUpper();
                        if (inType == "SYSTEM.STRING") { outType = "string"; }
                        if (inType == "SYSTEM.INT" || inType == "SYSTEM.INT32" || inType == "SYSTEM.INT64") { outType = "int"; }
                        if (inType == "SYSTEM.DOUBLE" || inType == "SYSTEM.DOUBLE32" || inType == "SYSTEM.DOUBLE64" ||
                            inType == "SYSTEM.FLOAT" || inType == "SYSTEM.FLOAT32" || inType == "SYSTEM.FLOAT64" ||
                            inType == "SYSTEM.DECIMAL") { outType = "double"; }
                        if (inType == "SYSTEM.DATETIME") { outType = "date"; }
                        if (inType == "SYSTEM.BYTE") { outType = "boolean"; }
                        output += "{type:\"" + outType + "\"}";
                        output += (i < dati.Tables[0].Columns.Count - 1) ? "," : "";
                    }
                    output += "}";
                    output += "},";
                }
                output += "data:[";
                // righe jsondb            
                for (int i = 0; i < dati.Tables[0].Rows.Count; i++) {
                    output += Serializer.SerializeDataRowJsonDb(dati.Tables[0].Rows[i]);
                    output += (i < dati.Tables[0].Rows.Count - 1) ? "," : "";
                }
                output += "]";
            }
            return output;
        }


        public static string SerializeDataRowJsonDb(DataRow row) {
            string output = "[";
            string value = null;
            string type = null;
            for (int x = 0; x < row.ItemArray.Length; x++) {
                if (row.ItemArray[x].GetType() == typeof(DBNull)) {
                    value = "null";
                } else {
                    type = row.Table.Columns[x].DataType.ToString();
                    if (type == "System.Byte") {
                        value = (row.ItemArray[x].ToString() == "0") ? "false" : "true";
                    } else if (type == "System.String") {
                        value = "\"" + row.ItemArray[x].ToString() + "\"";
                    } else if (type == "System.DateTime") {
                        value = "\"" + ((DateTime)(Marshaller.PerformConversion<DateTime?>(row.ItemArray[x]))).ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"";
                    } else if (type == "System.Decimal") {
                        value = row.ItemArray[x].ToString().Replace(",", ".");
                    } else {
                        value = row.ItemArray[x].ToString();
                    }
                }
                output += value;
                output += (x < row.ItemArray.Length - 1) ? "," : "";
            }
            output += "]";
            return output;
        }


        public static string SerializeDataRowJson(DataRow row) {
            string output = "{";
            string value = null;
            string type = null;
            for (int x = 0; x < row.ItemArray.Length; x++) {
                if (row.ItemArray[x].GetType() == typeof(DBNull)) {
                    value = "null";
                } else {
                    type = row.Table.Columns[x].DataType.ToString();
                    if (type == "System.Byte") {
                        value = (row.ItemArray[x].ToString() == "0") ? "false" : "true";
                    } else if (type == "System.String") {
                        value = "\"" + row.ItemArray[x].ToString() + "\"";
                    } else if (type == "System.DateTime") {
                        value = "\"" + ((DateTime)(Marshaller.PerformConversion<DateTime?>(row.ItemArray[x]))).ToString("yyyy-MM-ddTHH:mm:ssZ") + "\"";
                    } else if (type == "System.Decimal") {
                        value = row.ItemArray[x].ToString().Replace(",", ".");
                    } else {
                        value = row.ItemArray[x].ToString();
                    }
                }
                output += "\"" + row.Table.Columns[x].ColumnName + "\":" + value;
                output += (x < row.ItemArray.Length - 1) ? "," : "";
            }
            output += "}";
            return output;
        }


        public static string SerializeDataSetJson(DataSet dati) {
            if (!(dati != null && dati.Tables.Count > 0 && dati.Tables[0].Rows.Count > 0))
                return "[]";
            string output = "[";
            for (int x = 0; x < dati.Tables[0].Rows.Count; x++) {
                output += SerializeDataRowJson(dati.Tables[0].Rows[x]);
                output += (x < dati.Tables[0].Rows.Count - 1) ? "," : "";
            }
            output += "]";
            return output;
        }


        public static string SerializeDataSet(DataSet data, string serializzationType, bool meta) {
            string output = "";
            switch (serializzationType.ToUpper()) {
                case "JSON":
                output = SerializeDataSetJson(data);
                break;
                case "JSONDB":
                output = SerializeDataSetJsonDb(data, meta);
                break;
                default:
                output = SerializeDataSetJson(data);
                break;
            }
            return output;
        }


        public static string SerializeObjectListXml(dynamic objList) {
            string output = "";
            if (objList.Count > 0) {
                Type objType = objList.GetType().GetGenericArguments()[0];
                MemoryStream memStream = new MemoryStream();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<>).MakeGenericType(objType));
                xmlSerializer.Serialize(memStream, objList);
                byte[] buffer = memStream.ToArray();
                output = Encoding.UTF8.GetString(buffer);
            }
            return output;
        }


        public static string SerializeObjectListBinary(dynamic objList) {
            string output = "";
            if (objList.Count > 0) {
                MemoryStream memStream = new MemoryStream();
                IFormatter binFormatter = new BinaryFormatter();
                binFormatter.Serialize(memStream, objList);
                byte[] buffer = memStream.ToArray();
                output = Convert.ToBase64String(buffer);
                output = Encoding.UTF8.GetString(buffer);
            }
            return output;
        }


        /// <summary>Funzione per la serializzazione di liste di oggetti.</summary>
        /// <param name="objList">La lista di oggetti da serializzare.</param>        
        /// <param name="serializzationType">Il tipo di serializzazione che si vuole ottenere.</param>        
        /// <param name="compressionType">[Optional: default=nothing] Il tipo di compressione da impiegare.</param>        
        /// <returns>string</returns> 
        public static string SerializeObjectList(dynamic objList, SerializationType serializationType, CompressionType compressionType = CompressionType.NOTHING) {
            string output = "";
            if (serializationType == SerializationType.BINARY) { // serializzo BINARY
                output = Serializer.SerializeObjectListBinary(objList);
            } else if (serializationType == SerializationType.XML) { // serializzo XML
                output = Serializer.SerializeObjectListXml(objList);
            } else if (serializationType == SerializationType.JSON) { // serializzo JSON
                if (objList.GetType().FullName == "System.Data.DataSet") {
                    output = Serializer.SerializeDataSetJson((DataSet)objList);
                } else {
                    output = JsonWriter.Serialize(objList);
                }
            } else { // eccezione: tipo serializzazione non specificato
                throw new EdijsonError("Tipo di serializzazione \"" + serializationType.ToString() + "\" non supportato.");
            }
            // comprimo la serializzazione se richiesto               
            output = Compressor.Compress(output, compressionType);
            return output;
        }


        /// <summary>Funzione per la deserializzazione di liste di oggetti.</summary>
        /// <param name="data">La stringa contenente la lista di oggetti serializzata.</param>        
        /// <param name="serializzationType">Il tipo di serializzazione usato.</param>        
        /// <param name="compressionType">[Optional: default=nothing] Il tipo di compressione usato.</param>        
        /// <returns>dynamic</returns> 
        public static dynamic UnserializeObjectList(string data, Type objType, SerializationType serializationType, CompressionType compressionType = CompressionType.NOTHING) {
            dynamic objList = null;
            // decomprimo la serializzazione se richiesto                        
            data = Compressor.Decompress(data, compressionType);
            if (serializationType == SerializationType.BINARY) { // deserializzo da BINARY
                IFormatter binFormatter = new BinaryFormatter();
                byte[] buffer = Convert.FromBase64String(data);
                MemoryStream memStream = new MemoryStream(buffer);
                Type listType = typeof(List<>).MakeGenericType(objType);
                objList = (IList)Activator.CreateInstance(listType);
                objList = binFormatter.Deserialize(memStream);
            } else if (serializationType == SerializationType.XML) { // deserializzo da XML
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<>).MakeGenericType(objType));
                byte[] b = System.Text.Encoding.UTF8.GetBytes(data);
                MemoryStream memStream = new MemoryStream(b);
                Type listType = typeof(List<>).MakeGenericType(objType);
                objList = (IList)Activator.CreateInstance(listType);
                objList = xmlSerializer.Deserialize(memStream);
            } else if (serializationType == SerializationType.JSON) { // deserializzo da JSON
                object[] tempList = (object[])JsonReader.Deserialize(data, typeof(List<>).MakeGenericType(objType));
                Type listType = typeof(List<>).MakeGenericType(objType);
                objList = (IList)Activator.CreateInstance(listType);
                foreach (object value in tempList) {
                    objList.Add((dynamic)value);
                }
            } else { // eccezione: tipo serializzazione non specificato
                throw new EdijsonError("Tipo di serializzazione \"" + serializationType.ToString() + "\" non supportato.");
            }
            return objList;
        }


        /// <summary>Funzione per la serializzazione di oggetti.</summary>
        /// <param name="objList">L'oggetto da serializzare.</param>        
        /// <param name="serializzationType">Il tipo di serializzazione che si vuole ottenere.</param>        
        /// <param name="compressionType">[Optional: default=nothing] Il tipo di compressione da impiegare.</param>        
        /// <returns>string</returns> 
        public static string SerializeObject(Object obj, SerializationType serializationType, CompressionType compressionType = CompressionType.NOTHING) {
            string output = "";
            if (serializationType == SerializationType.BINARY) { // serializzo BINARY
                MemoryStream memStream = new MemoryStream();
                IFormatter binFormatter = new BinaryFormatter();
                binFormatter.Serialize(memStream, obj);
                byte[] buffer = memStream.ToArray();
                output = Convert.ToBase64String(buffer);
            } else if (serializationType == SerializationType.XML) { // serializzo XML
                MemoryStream memStream = new MemoryStream();
                XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
                xmlSerializer.Serialize(memStream, obj);
                byte[] buffer = memStream.ToArray();
                output = Encoding.UTF8.GetString(buffer);
            } else if (serializationType == SerializationType.JSON) { // serializzo JSON
                if (obj.GetType().ToString() == "System.Data.DataSet") {
                    output = Serializer.SerializeDataSetJson((DataSet)obj);
                } else {
                    output = JsonWriter.Serialize(obj);
                }
            } else { // eccezione: tipo serializzazione non specificato
                throw new EdijsonError("Tipo di serializzazione \"" + serializationType.ToString() + "\" non supportato.");
            }
            // comprimo la serializzazione se richiesto
            output = Compressor.Compress(output, compressionType);
            return output;
        }


        /// <summary>Funzione per la deserializzazione di oggetti.</summary>
        /// <param name="data">La stringa contenente l'oggetto serializzato.</param>        
        /// <param name="serializzationType">Il tipo di serializzazione usato.</param>        
        /// <param name="compressionType">[Optional: default=nothing] Il tipo di compressione usato.</param>        
        /// <returns>dynamic</returns> 
        public static dynamic UnserializeObject(string data, Type objType, SerializationType serializationType, CompressionType compressionType = CompressionType.NOTHING) {
            dynamic obj = null;
            // decomprimo la serializzazione se richiesto
            data = Compressor.Decompress(data, compressionType);
            if (serializationType == SerializationType.BINARY) { // deserializzo da BINARY
                IFormatter binFormatter = new BinaryFormatter();
                byte[] buffer = Convert.FromBase64String(data);
                MemoryStream memStream = new MemoryStream(buffer);
                obj = binFormatter.Deserialize(memStream);
            } else if (serializationType == SerializationType.XML) { // deserializzo da XML
                XmlSerializer xmlSerializer = new XmlSerializer(objType);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
                MemoryStream memStream = new MemoryStream(buffer);
                obj = xmlSerializer.Deserialize(memStream);
            } else if (serializationType == SerializationType.JSON) { // deserializzo da JSON
                obj = JsonReader.Deserialize(data, objType);
            } else { // eccezione: tipo serializzazione non specificato
                throw new EdijsonError("Tipo di serializzazione \"" + serializationType.ToString() + "\" non supportato.");
            }
            return obj;
        }


        /// <summary>Funzione per la clonazione di oggetti.</summary>
        /// <param name="obj">L'oggetto da clonare.</param>                        
        /// <returns>dynamic</returns> 
        public static dynamic Copy(dynamic obj) {
            using (MemoryStream memStream = new MemoryStream()) {
                IFormatter binFormatter = new BinaryFormatter();
                binFormatter.Serialize(memStream, obj);
                memStream.Position = 0;
                return binFormatter.Deserialize(memStream);
            }
        }

    }
}
