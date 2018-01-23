using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.IO;
using System.Globalization;
using System.Threading;

namespace Orwell.Models
{

    internal class SpGenerator
    {
        public string ProcedurePrefix = String.Empty;

        public bool OverwriteExistingSps { get; internal set; }

        public bool IsValidConnectionString(string strConnectionString)
        {
            return true;
        }

        public event SpGenerator.ProcedureCreatedEventHandler ProcedureCreated;

        private void DropProcedure(string Name)
        {
            try
            {
                DataAccessSql.ExecuteNonQuery("DROP PROCEDURE " + Name, CommandType.Text);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void GenerateStoreProcedures(string ConnectionString, List<DatabaseTable> DatabaseTables)
        {
            OverwriteExistingSps = false;
            foreach (DatabaseTable databaseTable in DatabaseTables)
                this.GenerateStoreProcedures(ConnectionString, databaseTable, databaseTable.InsertProcedure, databaseTable.SelectProcedure, databaseTable.UpdateProcedure, databaseTable.DeleteProcedure, databaseTable.SelectDetailsProcedure, databaseTable.WriteFiles, databaseTable.WriteProcedures);
        }

        public void GenerateStoreProcedures(string ConnectionString, DatabaseTable Table, bool CreateInsert, bool CreateSelect, bool CreateUpdate, bool CreateDelete, bool CreateSelectDetails, bool WriteFiles, bool WriteSP)
        {
            var cleanTableName = Table.PluralName.Replace(" ", "").Replace("_", "");
            var commandPrefix = Table.SchemaName + "." + this.ProcedurePrefix + cleanTableName;
            var commandList = new List<CommandObj>();
            string tempName = String.Empty;
            try
            {
                DataAccessSql.ConnectionString = ConnectionString;
                DataSet tableSchema = DataAccessSql.GetTableSchema(Table.TableName);
                if (CreateSelect)
                {
                    tempName = commandPrefix + "SelectAll";
                    this.GenerateSelectProcedure(tempName, tableSchema, WriteFiles, WriteSP);
                    commandList.Add(new CommandObj() { Title = "GetAllCommand", Value = tempName });
                }
                if (CreateSelectDetails)
                {
                    tempName = commandPrefix + "Select";
                    this.GenerateSelectOneProcedure(tempName, tableSchema, WriteFiles, WriteSP);
                    commandList.Add(new CommandObj() { Title = "FillCommand", Value = tempName });


                    // Index Creation script
                    tempName = commandPrefix + "SelectAll";
                    this.GenerateSelectViews(tempName, tableSchema, WriteFiles, WriteSP);
                }
                if (CreateDelete)
                {
                    tempName = commandPrefix + "Delete";
                    this.GenerateDeleteProcedure(tempName, tableSchema, WriteFiles, WriteSP);
                    commandList.Add(new CommandObj() { Title = "DeleteCommand", Value = tempName });
                }
                if (CreateUpdate)
                {
                    tempName = commandPrefix + "Update";
                    this.GenerateUpdateProcedure(tempName, tableSchema, WriteFiles, WriteSP);
                    commandList.Add(new CommandObj() { Title = "UpdateCommand", Value = tempName });

                    this.GenerateUpdateViews(tempName, tableSchema, true, false);
                }

                if (!CreateInsert)
                    return;
                tempName = commandPrefix + "Insert";
                this.GenerateInsertProcedure(tempName, tableSchema, WriteFiles, WriteSP);
                commandList.Add(new CommandObj() { Title = "InsertCommand", Value = tempName });


                // Model Creation script
                GenerateModel(cleanTableName, commandList, tableSchema, "int");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void GenerateModel(string ObjectName, List<CommandObj> commandList, DataSet tableSchema, string tkey)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string header = GetModelHeader("Apollo");
            stringBuilder.Append(header);

            stringBuilder.Append("\n      public class "+ ObjectName +" : ApolloModel<"+tkey+">");
            stringBuilder.Append("   { \n");

            foreach (var item in commandList)
            {
                stringBuilder.Append("public const string "+item.Title+" = \"" + item.Value +" \";\n");
            }

            stringBuilder.Append("\n      public ");
            stringBuilder.Append(ObjectName + "() : base(FillCommand, InsertCommand, UpdateCommand, DeleteCommand)\n");
            stringBuilder.Append("      { \n");
            stringBuilder.Append("           Init(); \n");
            stringBuilder.Append("      }\n");

            stringBuilder.Append("\n      public ");
            stringBuilder.Append(ObjectName + "("+ tkey +" id) : base(FillCommand, InsertCommand, UpdateCommand, DeleteCommand)\n");
            stringBuilder.Append("      { \n");
            stringBuilder.Append("           Init(); \n");
            stringBuilder.Append("           Fill(id); \n");
            stringBuilder.Append("      }\n");


            stringBuilder.Append("\n      public ");
            stringBuilder.Append("static "+ ObjectName +"Collection GetAll()\n");
            stringBuilder.Append("      {\n");
            stringBuilder.Append("          return GetCollection<" + ObjectName +"Collection, "+ObjectName+">(GetAllCommand);\n");
            stringBuilder.Append("      }\n");

            List<DataColumn> allColumns = this.GetAllColumns(tableSchema);
            string dataType = string.Empty;
            foreach (var column in allColumns)
            {
                dataType = column.DataType.ToString().ToLower().Replace("system.","").Replace("boolean", "bool");

                stringBuilder.Append("\n         public " + dataType +" ");
                stringBuilder.Append(column.ColumnName);
                stringBuilder.Append("\n         { \n");

                stringBuilder.Append("             get { return Get<" + dataType + ">(\"" + column.ColumnName + "\"); }\n");
                stringBuilder.Append("             set { Set(\"" + column.ColumnName+"\", value); }\n");

                stringBuilder.Append("\n         } \n");

            }


            // Footer
            stringBuilder.Append("     }\n");
            stringBuilder.Append("}\n");

            CreateOutput(ObjectName, true, false, stringBuilder.ToString(), ObjectName+ ".cs");
        }

        private string GetModelHeader(string projectName)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("using System;\n");
            stringBuilder.Append("using System.Collections.Generic;\n");
            stringBuilder.Append("using System.ComponentModel.DataAnnotations;\n");
            stringBuilder.Append("using System.Linq;\n\n");

            stringBuilder.Append("namespace "+ projectName + ".Core.Models \n{ \n");

            return stringBuilder.ToString();
        }

        private void GenerateSelectProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName));

            stringBuilder.Append("\nAS\n");
            stringBuilder.Append("\n");
            stringBuilder.Append("SELECT");
            stringBuilder.Append("\n");
            List<DataColumn> selectableColumns = this.GetUpdatableColumns(FieldList);

            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                DataColumn dataColumn = selectableColumns[index];
                stringBuilder.Append(dataColumn.ColumnName);
                if (index < selectableColumns.Count - 1)
                    stringBuilder.Append(",");
                stringBuilder.Append("\n");
            }

            stringBuilder.Append("FROM [" + tableName + "]");
            stringBuilder.Append("\n");
            stringBuilder.Append("\n/*" + this.GetDropProcedureCode(ProcedureName) + "*/");
            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(ProcedureName, WriteFiles, WriteSP, CommandText);
        }

        private void GenerateSelectViews(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetViewHeader(tableName));

            stringBuilder.Append("\n");
            stringBuilder.Append("<table class=\"table DataTable\">\n");
            stringBuilder.Append("   <thead>\n");
            List<DataColumn> selectableColumns = this.GetUpdatableColumns(FieldList);

            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                if (index == 0)
                {
                    stringBuilder.Append("     <tr>");
                    stringBuilder.Append("\n      <th>");
                }
                DataColumn dataColumn = selectableColumns[index];
                stringBuilder.Append(dataColumn.ColumnName);
                if (index < selectableColumns.Count - 1)
                    stringBuilder.Append("</th>\n      <th>");
            }
            stringBuilder.Append("</th>\n      <th>Functions</th>");
            stringBuilder.Append("\n     </tr>\n   </thead>\n   <tbody>");
            stringBuilder.Append("\n");
            stringBuilder.Append("@{\n     foreach(var item in items){");
            stringBuilder.Append("\n          <tr>\n");

            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                DataColumn dataColumn = selectableColumns[index];
                stringBuilder.Append("             <td>@item." + dataColumn.ColumnName + "</td>");
                stringBuilder.Append("\n");
            }
            stringBuilder.Append("             <td><a href=\"#\" class=\"btn btn-xs btn-default\">Edit</a></td>");

            stringBuilder.Append("\n");
            stringBuilder.Append("          </tr>\n       } \n}");
            stringBuilder.Append("\n    </tbody>");
            stringBuilder.Append("\n</table>\n\n");

            stringBuilder.Append("@section scripts {\n");
            stringBuilder.Append("   <link href = \"//cdn.datatables.net/1.10.11/css/jquery.dataTables.min.css\" rel=\"stylesheet\">\n");
            stringBuilder.Append("   <script src = \"//cdn.datatables.net/1.10.11/js/jquery.dataTables.min.js\"></script>\n");
            stringBuilder.Append("   <script>\n");
            stringBuilder.Append("      $(document).ready(function() {\n");
            stringBuilder.Append("         $('.DataTable').DataTable();\n");
            stringBuilder.Append("      });\n");
            stringBuilder.Append("   </script>\n");
            stringBuilder.Append("}");

            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(tableName, WriteFiles, false, CommandText, "Index.cshtml");
        }
        
        private void GenerateUpdateViews(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetViewHeader(tableName));

            stringBuilder.Append("\n");
            stringBuilder.Append("   @using (Html.BeginForm(\"Edit"+tableName+"\", \""+tableName+"\", FormMethod.Post, new { id = \""+tableName+"Form\" }))\n");
            stringBuilder.Append("   {\n");
            List<DataColumn> selectableColumns = this.GetUpdatableColumns(FieldList);

            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                DataColumn dataColumn = selectableColumns[index];

                stringBuilder.Append("         <div class=\"form-group\">\n");
                stringBuilder.Append("             @Html.LabelFor(model => model." + dataColumn.ColumnName+", htmlAttributes: new { @class = \"control -label col-md-3\" })\n");
                stringBuilder.Append("             <div class=\"col-md-9\">\n");
                stringBuilder.Append("                 @Html.EditorFor(model => model." + dataColumn.ColumnName + ", new { htmlAttributes = new { @class = \"form-control disabled\" } })\n");
                stringBuilder.Append("                 @Html.ValidationMessageFor(model => model." + dataColumn.ColumnName + ", \"\", new { @class = \"text-danger\" })\n");
                stringBuilder.Append("            </div>\n");
                stringBuilder.Append("         </div>\n\n");
            }

            stringBuilder.Append("<input class=\"btn btn-success\" type=\"submit\" value=\"Update\" /> \n\n");
            stringBuilder.Append("    }\n");

            stringBuilder.Append("@section scripts {\n");
            
            stringBuilder.Append("}");

            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(tableName, WriteFiles, false, CommandText, "Update.cshtml");
        }

        private void GenerateSelectOneProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName));
            stringBuilder.Append("\n");
            List<DataColumn> primaryKeys = this.GetPrimaryKeys(FieldList);
            if (primaryKeys.Count == 0)
                return;
            List<string> stringList = new List<string>();
            stringBuilder.Append(this.GetParameterListString(primaryKeys));
            stringBuilder.Append("\nAS\n");

            stringBuilder.Append("\n");
            stringBuilder.Append("SELECT");
            stringBuilder.Append("\n");

            List<DataColumn> selectableColumns = this.GetUpdatableColumns(FieldList);

            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                DataColumn dataColumn = selectableColumns[index];
                stringBuilder.Append("[" + dataColumn.ColumnName + "]");
                if (index < selectableColumns.Count - 1)
                    stringBuilder.Append(",");
                stringBuilder.Append("\n");
            }

            stringBuilder.Append("FROM [" + tableName + "]");

            stringBuilder.Append(this.GetSelectWHEREClause(primaryKeys));
            stringBuilder.Append("\n/*" + this.GetDropProcedureCode(ProcedureName) + "*/");
            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(ProcedureName, WriteFiles, WriteSP, CommandText);
        }

        private void GenerateDeleteProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName));
            stringBuilder.Append("\n");
            List<DataColumn> primaryKeys = this.GetPrimaryKeys(FieldList);
            if (primaryKeys.Count == 0)
                return;
            List<string> stringList = new List<string>();
            stringBuilder.Append(this.GetParameterListString(primaryKeys));
            stringBuilder.Append("\nAS\n");
            stringBuilder.Append("DELETE FROM [" + tableName + "]\n");
            stringBuilder.Append(this.GetWHEREClause(primaryKeys));
            stringBuilder.Append("\n/*" + this.GetDropProcedureCode(ProcedureName) + "*/");
            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(ProcedureName, WriteFiles, WriteSP, CommandText);
        }

        private void GenerateUpdateProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName));
            List<DataColumn> allColumns = this.GetAllColumns(FieldList);
            List<DataColumn> primaryKeys = this.GetPrimaryKeys(FieldList);
            List<DataColumn> updatableColumns = this.GetUpdatableColumns(FieldList);
            if (updatableColumns.Count == 0)
                return;
            stringBuilder.Append(this.GetParameterListString(allColumns));
            stringBuilder.Append("\nAS\n");
            stringBuilder.Append("UPDATE [" + tableName + "] \n");
            stringBuilder.Append("SET \n");
            for (int index = 0; index < updatableColumns.Count; ++index)
            {
                DataColumn dataColumn = updatableColumns[index];
                stringBuilder.Append("[" + dataColumn.ColumnName + "] = @" + dataColumn.ColumnName);
                if (index < updatableColumns.Count - 1)
                    stringBuilder.Append(",");
                stringBuilder.Append("\n");
            }
            stringBuilder.Append(this.GetWHEREClause(primaryKeys));
            stringBuilder.Append("\n/*" + this.GetDropProcedureCode(ProcedureName) + "*/");
            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(ProcedureName, WriteFiles, WriteSP, CommandText);
        }

        private void GenerateInsertProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName));
            List<DataColumn> allColumns = this.GetAllColumns(FieldList);
            List<DataColumn> primaryKeys = this.GetPrimaryKeys(FieldList);
            List<DataColumn> updatableColumns = this.GetUpdatableColumns(FieldList);
            if (updatableColumns.Count == 0)
                return;
            stringBuilder.Append(this.GetParameterListString(updatableColumns, true, FieldList));
            stringBuilder.Append("\nAS\n");
            stringBuilder.Append("INSERT INTO [" + tableName + "]\n( \n");
            for (int index = 0; index < updatableColumns.Count; ++index)
            {
                stringBuilder.Append("\t");
                DataColumn dataColumn = updatableColumns[index];
                stringBuilder.Append("[" + dataColumn.ColumnName + "]");
                if (index < updatableColumns.Count - 1)
                    stringBuilder.Append(",");
                stringBuilder.Append("\n");
            }
            stringBuilder.Append("\n)\n");
            stringBuilder.Append("VALUES \n(\n");
            for (int index = 0; index < updatableColumns.Count; ++index)
            {
                DataColumn dataColumn = updatableColumns[index];
                stringBuilder.Append("\t");
                stringBuilder.Append("@");
                stringBuilder.Append(dataColumn.ColumnName);
                if (index < updatableColumns.Count - 1)
                    stringBuilder.Append(",");
                stringBuilder.Append("\n");
            }
            stringBuilder.Append(")");
            if (updatableColumns.Count < allColumns.Count)
            {
                stringBuilder.Append("\n\n\n");
                if (primaryKeys.Count > 0)
                {
                    string putParameterName = this.GetOutPutParameterName(primaryKeys);
                    if (putParameterName.Trim() != "")
                    {
                        stringBuilder.Append("SET " + putParameterName + "= @@IDENTITY");
                        stringBuilder.Append("\n Return " + putParameterName);
                    }
                    else
                        stringBuilder.Append("SELECT @@IDENTITY");
                }
                else
                    stringBuilder.Append("SELECT @@IDENTITY");
            }
            stringBuilder.Append("\n/*" + this.GetDropProcedureCode(ProcedureName) + "*/");
            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(ProcedureName, WriteFiles, WriteSP, CommandText);
        }

        private void CreateOutput(string ProcedureName, bool WriteFiles, bool WriteSP, string CommandText, string outputType="")
        {
            if (WriteSP)
            {
                if (this.OverwriteExistingSps)
                    this.DropProcedure(ProcedureName);
                int num = 1;
                DataAccessSql.ExecuteNonQuery(CommandText, (CommandType)num);
            }

            if (WriteFiles)
            {
                if (outputType != "")
                {
                    SaveToFile(CommandText, ProcedureName, outputType);
                }
                else
                {
                    SaveToFile(CommandText, ProcedureName);
                }
            }
        }

        private void SaveToFile(string CommandText, string ProcedureName, string outputType="")
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd");
            string path = @"c:\Orwell-Generated-" + timestamp;
            string viewDirectory = @"c:\Orwell-Generated-" + timestamp +"\\Views";
            string viewPath = @"c:\Orwell-Generated-" + timestamp +"\\Views\\"+TitleCase(ProcedureName);

            try
            {
                string fileName = path + "\\" + ProcedureName + ".sql";

                if (!Directory.Exists(path))
                {
                    DirectoryInfo di = Directory.CreateDirectory(path);
                }
                if (outputType != "")
                {
                    if (!Directory.Exists(viewDirectory))
                    {
                        DirectoryInfo di2 = Directory.CreateDirectory(viewDirectory);
                    }
                    if (!Directory.Exists(viewPath))
                    {
                        DirectoryInfo di3 = Directory.CreateDirectory(viewPath);
                    }
                    fileName = viewPath + "\\" + outputType;
                }

                using (StreamWriter outfile = new StreamWriter(fileName))
                {
                    outfile.Write(CommandText);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            finally { }

        }

        private string GetProcedureHeader(string ProcedureName)
        {
            return "CREATE PROCEDURE " + ProcedureName;
        }

        private string TitleCase(string AnyString)
        {
            var anyString = AnyString.ToLower();
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;
            return textInfo.ToTitleCase(anyString);
        }

        private string GetViewHeader(string TableName)
        {
            return "@{\n   ViewBag.Title = \"" + TitleCase(TableName) + "\"; \n}";
        }

        private string GetDropProcedureCode(string ProcedureName)
        {
            return "DROP PROC " + ProcedureName;
        }

        private List<DataColumn> GetPrimaryKeys(DataSet FieldList)
        {
            List<DataColumn> dataColumnList = new List<DataColumn>();
            foreach (Constraint constraint in (InternalDataCollectionBase)FieldList.Tables[0].Constraints)
            {
                if (constraint.GetType().ToString() == "System.Data.UniqueConstraint")
                {
                    foreach (DataColumn column in ((UniqueConstraint)constraint).Columns)
                        dataColumnList.Add(column);
                }
            }
            return dataColumnList;
        }

        private List<DataColumn> GetAllColumns(DataSet FieldList)
        {
            List<DataColumn> dataColumnList = new List<DataColumn>();
            foreach (DataColumn column in (InternalDataCollectionBase)FieldList.Tables[0].Columns)
                dataColumnList.Add(column);
            return dataColumnList;
        }

        private List<DataColumn> GetUpdatableColumns(DataSet FieldList)
        {
            List<DataColumn> dataColumnList = new List<DataColumn>();
            foreach (DataColumn column in (InternalDataCollectionBase)FieldList.Tables[0].Columns)
            {
                if (!column.AutoIncrement)
                    dataColumnList.Add(column);
            }
            return dataColumnList;
        }

        private string GetSqlDataType(DataColumn dc)
        {
            string str = "";
            switch (dc.DataType.ToString().ToLower())
            {
                case "system.boolean":
                    str = "bit";
                    break;
                case "system.datetime":
                    str = "datetime";
                    break;
                case "system.decimal":
                    str = "money";
                    break;
                case "system.int16":
                    str = "smallint";
                    break;
                case "system.int32":
                    str = "int";
                    break;
                case "system.int64":
                    str = "bigint";
                    break;
                case "system.string":
                    str = "varchar";
                    break;
            }
            return str;
        }

        private string GetSelectWHEREClause(List<DataColumn> lstColumns)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\n");
            if (lstColumns.Count == 0)
                return "";
            stringBuilder.Append("WHERE\n");
            for (int index = 0; index < lstColumns.Count; ++index)
            {
                DataColumn lstColumn = lstColumns[index];
                string str1 = "@" + lstColumn.ColumnName;
                string str2 = " [" + lstColumn.ColumnName + "] = " + str1;
                str2 += "\n OR @" + lstColumn.ColumnName + " IS NULL ";

                if (index < lstColumns.Count - 1)
                    str2 += "\nAND ";
                string str3 = str2 + "\n";
                stringBuilder.Append(str3);
            }
            stringBuilder.Append("\n");
            return stringBuilder.ToString();
        }

        private string GetWHEREClause(List<DataColumn> lstColumns)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\n");
            if (lstColumns.Count == 0)
                return "";
            stringBuilder.Append("WHERE\n");
            for (int index = 0; index < lstColumns.Count; ++index)
            {
                DataColumn lstColumn = lstColumns[index];
                string str1 = "@" + lstColumn.ColumnName;
                string str2 = " [" + lstColumn.ColumnName + "] = " + str1;
                if (index < lstColumns.Count - 1)
                    str2 += "\nAND ";
                string str3 = str2 + "\n";
                stringBuilder.Append(str3);
            }
            stringBuilder.Append("\n");
            return stringBuilder.ToString();
        }

        private string GetParameterListString(List<DataColumn> lstColumns)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\n(\n");
            for (int index = 0; index < lstColumns.Count; ++index)
            {
                DataColumn lstColumn = lstColumns[index];
                string str1 = "\t" + ("@" + lstColumn.ColumnName) + " " + this.GetSqlDataType(lstColumn);
                if (lstColumn.DataType.ToString().ToLower() == "system.string")
                    str1 = str1 + "(" + (object)lstColumn.MaxLength + ") = NULL";
                if (index < lstColumns.Count - 1)
                {
                    str1 += ",";
                }

                string str2 = str1 + "\n";
                stringBuilder.Append(str2);
            }
            stringBuilder.Append(")\n");
            return stringBuilder.ToString();
        }

        private string GetOutPutParameterName(List<DataColumn> lstPrimaryKeys)
        {
            string str = "";
            if (lstPrimaryKeys.Count > 0)
            {
                foreach (DataColumn lstPrimaryKey in lstPrimaryKeys)
                {
                    if (lstPrimaryKey.AutoIncrement)
                        str = "@" + lstPrimaryKey.ColumnName;
                }
            }
            return str;
        }

        private string GetParameterListString(List<DataColumn> lstColumns, bool IsInserted, DataSet FieldList)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\n(\n");
            for (int index = 0; index < lstColumns.Count; ++index)
            {
                DataColumn lstColumn = lstColumns[index];
                string str = "\t" + ("@" + lstColumn.ColumnName) + " " + this.GetSqlDataType(lstColumn);
                if (lstColumn.DataType.ToString().ToLower() == "system.string")
                    str = str + "(" + (object)lstColumn.MaxLength + ")";
                if (index < lstColumns.Count - 1)
                    str = str + "," + "\n";
                stringBuilder.Append(str);
            }
            if (IsInserted)
            {
                string outPutParamaters = this.GetOutPutParamaters(FieldList);
                stringBuilder.Append(outPutParamaters);
            }
            stringBuilder.Append("\n)\n");
            return stringBuilder.ToString();
        }

        private string GetOutPutParamaters(DataSet FieldList)
        {
            List<DataColumn> primaryKeys = this.GetPrimaryKeys(FieldList);
            StringBuilder stringBuilder = new StringBuilder();
            if (primaryKeys.Count > 0)
            {
                foreach (DataColumn dc in primaryKeys)
                {
                    if (dc.AutoIncrement)
                    {
                        stringBuilder.Append(",\n\t");
                        string str1 = "@" + dc.ColumnName + "  " + this.GetSqlDataType(dc);
                        if (dc.DataType.ToString().ToLower() == "system.string")
                            str1 = str1 + "(" + (object)dc.MaxLength + ")";
                        string str2 = str1 + " OUTPUT";
                        stringBuilder.Append(str2);
                    }
                }
            }
            return stringBuilder.ToString();
        }

        public delegate void ProcedureCreatedEventHandler(object sender, ProcedureCreatedEventArgs e);
    }

}