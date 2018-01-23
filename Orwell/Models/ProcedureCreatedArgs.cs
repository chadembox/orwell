using System;

namespace Orwell.Models
{

    public class ProcedureCreatedEventArgs
    {
        public string TableName;
        public ProcedureCreatedEventArgs.ProcedureTypes ProcedureType;
        public bool Success;
        public Exception Error;

        public ProcedureCreatedEventArgs(string TableName, ProcedureCreatedEventArgs.ProcedureTypes ProcedureType, bool Success, Exception Error)
        {
            this.TableName = TableName;
            this.ProcedureType = ProcedureType;
            this.Success = Success;
            this.Error = Error;
        }

        public enum ProcedureTypes
        {
            Select = 1,
            Update = 2,
            Delete = 3,
            Insert = 4,
            SelectDetails = 5,
        }
    }

}