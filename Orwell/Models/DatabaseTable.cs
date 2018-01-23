using System;

namespace Orwell.Models
{

    public class DatabaseTable
    {
        public string TableName;
        public string SingularName;
        public string PluralName;
        public bool InsertProcedure;
        public bool SelectProcedure;
        public bool SelectDetailsProcedure;
        public bool DeleteProcedure;
        public bool UpdateProcedure;
        public bool WriteProcedures;
        public bool WriteFiles;

        public DatabaseTable()
        {
        }

        public DatabaseTable(string TableName)
        {
            this.TableName = TableName;
            this.SingularName = TableName;
            this.PluralName = TableName;
            this.DeleteProcedure = true;
            this.InsertProcedure = true;
            this.SelectDetailsProcedure = true;
            this.SelectProcedure = true;
            this.UpdateProcedure = true;
            this.WriteProcedures = false;
            this.WriteFiles = false;
        }

        public string SchemaName
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(this.TableName) || !this.TableName.Contains("."))
                        return "dbo";
                    return this.TableName.Split('.')[0];
                }
                catch (Exception ex)
                {
                    return "dbo";
                }
            }
        }

        public override bool Equals(object obj)
        {
            try
            {
                return ((DatabaseTable)obj).TableName.Equals(this.TableName);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

}