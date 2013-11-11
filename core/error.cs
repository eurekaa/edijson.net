
namespace Edijson.Core {

    public class EdijsonError : System.Exception{

        #region PROPRIETA'
        public bool IsError { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
        #endregion

        #region COSTRUTTORI
        public EdijsonError() {
            this.IsError = true;
            this.Message = "";
            this.Source = "";
            this.StackTrace = "";
        }

        public EdijsonError(string message) {
            this.IsError = true;
            this.Message = message;
            this.Source = "";
            this.StackTrace = "";
        }
        #endregion

    }

}
