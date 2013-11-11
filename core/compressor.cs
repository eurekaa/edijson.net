using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using log4net;


namespace Edijson.Core {

    public enum CompressionType { NOTHING, GZIP };

    public static class Compressor {


        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        /// <summary>Funzione generica per la compressione di stringhe (utile per gli oggetti serializzati).</summary>
        /// <param name="data">La stringa da comprimere.</param>        
        /// <param name="compressionType">Il tipo di compressione da usare.</param>        
        /// <returns>string</returns>
        public static string Compress(string data, CompressionType compressionType) {
            if (compressionType == CompressionType.GZIP) {
                data = CompressGzip(data);
            }
            return data;
        }


        /// <summary>Funzione generica per la decompressione di stringhe (utile per gli oggetti serializzati).</summary>
        /// <param name="data">La stringa da comprimere.</param>        
        /// <param name="compressionType">Il tipo di compressione da usare.</param>        
        /// <returns>string</returns>
        public static string Decompress(string data, CompressionType compressionType) {
            if (compressionType == CompressionType.GZIP) {
                data = DecompressGzip(data);
            }
            return data;
        }


        /// <summary>Funzione specifica per la compressione di stringhe con algoritmo GZIP.</summary>
        /// <param name="data">La stringa da comprimere.</param>        
        /// <returns>string</returns>    
        public static string CompressGzip(string data) {
            MemoryStream memStream = null;
            MemoryStream outStream = null;
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            memStream = new MemoryStream();
            using (GZipStream zipStream = new GZipStream(memStream, CompressionMode.Compress, true)) {
                zipStream.Write(buffer, 0, buffer.Length);
                zipStream.Close();
            }
            memStream.Position = 0;
            outStream = new MemoryStream();
            byte[] compressed = new byte[memStream.Length];
            memStream.Read(compressed, 0, compressed.Length);
            byte[] gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            memStream.Close();
            outStream.Close();
            return Convert.ToBase64String(gzBuffer);
        }


        /// <summary>Funzione specifica per la decompressione di stringhe con algoritmo GZIP.</summary>
        /// <param name="data">La stringa da comprimere.</param>        
        /// <returns>string</returns> 
        public static string DecompressGzip(string data) {
            byte[] gzBuffer = Convert.FromBase64String(data);
            using (MemoryStream memStream = new MemoryStream()) {
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                memStream.Write(gzBuffer, 4, (gzBuffer.Length - 4));

                byte[] buffer = new byte[msgLength];

                memStream.Position = 0;
                using (GZipStream zipStream = new GZipStream(memStream, CompressionMode.Decompress)) {
                    zipStream.Read(buffer, 0, buffer.Length);
                    zipStream.Close();
                }
                memStream.Close();
                return Encoding.UTF8.GetString(buffer);
            }
        }

    }
}
