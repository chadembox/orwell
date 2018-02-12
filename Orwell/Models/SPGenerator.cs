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

        public string timestamp { get; set; }

        public string AppName { get; set; }

        public bool OverwriteExistingSps { get; internal set; }

        public bool IsValidConnectionString(string strConnectionString)
        {
            return true;
        }

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
            var cleanTableName = string.Empty;
            var commandPrefix = string.Empty;
            var commandList = new List<CommandObj>();
            string tempName = String.Empty;
            try
            {
                DataAccessSql.ConnectionString = ConnectionString;
                DataSet tableSchema = DataAccessSql.GetTableSchema(Table.TableName);
                cleanTableName = tableSchema.Tables[0].TableName.Replace("_", "").Replace("-", "");
                commandPrefix = Table.SchemaName + "." + cleanTableName;
                if (CreateSelect)
                {
                    tempName = cleanTableName + "SelectAll";
                    this.GenerateSelectProcedure(tempName, tableSchema, WriteFiles, WriteSP, Table.SchemaName);
                    commandList.Add(new CommandObj() { Title = "GetAllCommand", Value = tempName });
                }
                if (CreateSelectDetails)
                {
                    tempName = cleanTableName + "Select";
                    this.GenerateSelectOneProcedure(tempName, tableSchema, WriteFiles, WriteSP, Table.SchemaName);
                    commandList.Add(new CommandObj() { Title = "FillCommand", Value = tempName });

                    // Index Creation script
                    tempName = cleanTableName + "SelectAll";
                    this.GenerateSelectViews(tempName, tableSchema, WriteFiles, WriteSP, Table.SchemaName);

                    // Collection model script
                    this.GenerateCollectionModel(cleanTableName);
                    
                }
                if (CreateDelete)
                {
                    tempName = cleanTableName + "Delete";
                    this.GenerateDeleteProcedure(tempName, tableSchema, WriteFiles, WriteSP, Table.SchemaName);
                    commandList.Add(new CommandObj() { Title = "DeleteCommand", Value = tempName });
                }
                if (CreateUpdate)
                {
                    tempName = cleanTableName + "Update";
                    this.GenerateUpdateProcedure(tempName, tableSchema, WriteFiles, WriteSP, Table.SchemaName);
                    commandList.Add(new CommandObj() { Title = "UpdateCommand", Value = tempName });

                    this.GenerateUpdateViews(tempName, tableSchema, true, false, Table.SchemaName);
                }
                if (!CreateInsert)
                    return;

                tempName = cleanTableName + "Insert";
                this.GenerateInsertProcedure(tempName, tableSchema, WriteFiles, WriteSP, Table.SchemaName);
                commandList.Add(new CommandObj() { Title = "InsertCommand", Value = tempName });
                // Index Creation script
                this.GenerateInsertViews(tempName, tableSchema, WriteFiles, WriteSP, Table.SchemaName);

                // Generate App User
                GenerateAppUser();

                // Generate DB Context
                GenerateDBContext();

                // Model Creation script
                GenerateModel(cleanTableName, commandList, tableSchema, "int", Table.SchemaName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Models
        private void GenerateAppUser()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("using Karpster.Core.Data.EntityFramework;\n");
            stringBuilder.Append("using Microsoft.AspNet.Identity;\n");
            stringBuilder.Append("using System.Security.Claims;\n");
            stringBuilder.Append("using System.Threading.Tasks;\n");
            stringBuilder.Append("\n\n");
            stringBuilder.Append("   namespace " + AppName + ".Core.Models\n");
            stringBuilder.Append("   {\n");
            stringBuilder.Append("      public class " + AppName + "User : Karpster.Core.Identity.ApplicationUser, IEntity<string>\n");
            stringBuilder.Append("      {\n");
            stringBuilder.Append("         public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<"+AppName+"User, string> manager)\n");
            stringBuilder.Append("         {\n");
            stringBuilder.Append("             ClaimsIdentity userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);\n\n");
            stringBuilder.Append("             return userIdentity;\n");
            stringBuilder.Append("         }\n");
            stringBuilder.Append("\n\n");
            stringBuilder.Append("         public bool Disabled { get; set; }\n\n");
            stringBuilder.Append("         public bool Approved { get; set; }\n\n");
            stringBuilder.Append("         public UserAccountType AccountType { get; set; }\n\n");
            stringBuilder.Append("   }\n");
            stringBuilder.Append("}");
            CreateOutput(AppName + "User", true, false, stringBuilder.ToString(), "data", AppName + "User.cs");

        }

        private void GenerateDBContext()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("using " + AppName + ".Core.Models;\n");
            stringBuilder.Append("using Karpster.Core.Identity;\n");
            stringBuilder.Append("using System.Data.Entity;\n");
            stringBuilder.Append("\n\n");
            stringBuilder.Append("   namespace "+AppName+".Core.Data\n");
            stringBuilder.Append("   {\n");
            stringBuilder.Append("      public class " + AppName + "DbContext : ApplicationDbContext<" + AppName + "User>\n");
            stringBuilder.Append("      {\n");
            stringBuilder.Append("         public static " + AppName + "DbContext Create()\n");
            stringBuilder.Append("         {\n");
            stringBuilder.Append("             return new " + AppName + "DbContext();\n");
            stringBuilder.Append("         }\n");
            stringBuilder.Append("\n\n");
            stringBuilder.Append("         public DbSet<LogEntry> Logs { get; set; }\n\n");
            stringBuilder.Append("         protected override void OnModelCreating(DbModelBuilder modelBuilder)\n");
            stringBuilder.Append("         {\n");
            stringBuilder.Append("            base.OnModelCreating(modelBuilder);\n");
            stringBuilder.Append("         }\n");
            stringBuilder.Append("      }\n");
            stringBuilder.Append("   }\n");
            stringBuilder.Append("}");
            CreateOutput(AppName + "DbContext", true, false, stringBuilder.ToString(), "data", AppName + "DbContext.cs");
        }

        private void GenerateCollectionModel(string objectName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("using Karpster.Core.Data.ObjectModel;\n");
            stringBuilder.Append("using Microsoft.AspNet.Identity;\n");
            stringBuilder.Append("using System.Security.Claims;\n");
            stringBuilder.Append("using System.Threading.Tasks;\n");
            stringBuilder.Append("\n\n");
            stringBuilder.Append("   namespace " + AppName + ".Core.Models\n");
            stringBuilder.Append("   {\n");
            stringBuilder.Append("      public class " + objectName + "Collection : DataModelCollection<" + objectName+", int>\n");
            stringBuilder.Append("      {\n");
            stringBuilder.Append("      }\n");
            stringBuilder.Append("}");
            CreateOutput(objectName + "Collection", true, false, stringBuilder.ToString(), "model", objectName + "Collection.cs");

        }  

        private void GenerateModel(string ObjectName, List<CommandObj> commandList, DataSet tableSchema, string tkey, string schemaName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string header = GetModelHeader();
            stringBuilder.Append(header);

            stringBuilder.Append("\n");
            stringBuilder.Append("   public class " + ObjectName + " : " + AppName + "Model<" + tkey + ">");
            stringBuilder.Append("   { \n");

            foreach (var item in commandList)
            {
                stringBuilder.Append("public const string " + item.Title + " = \"" + item.Value + "\";\n");
            }

            stringBuilder.Append("\n");
            stringBuilder.Append("     public " + ObjectName + "() : base(FillCommand, InsertCommand, UpdateCommand, DeleteCommand)\n");
            stringBuilder.Append("      { \n");
            stringBuilder.Append("           Init(); \n");
            stringBuilder.Append("      }\n");

            stringBuilder.Append("\n");
            stringBuilder.Append("     public " + ObjectName + "(" + tkey + " id) : base(FillCommand, InsertCommand, UpdateCommand, DeleteCommand)\n");
            stringBuilder.Append("      { \n");
            stringBuilder.Append("           Init(); \n");
            stringBuilder.Append("           Fill(id); \n");
            stringBuilder.Append("      }\n");

            stringBuilder.Append("\n");
            stringBuilder.Append("     public static " + ObjectName + "Collection GetAll()\n");
            stringBuilder.Append("      {\n");
            stringBuilder.Append("          return GetCollection<" + ObjectName + "Collection, " + ObjectName + ">(GetAllCommand);\n");
            stringBuilder.Append("      }\n");

            List<DataColumn> allColumns = this.GetAllColumns(tableSchema);
            string dataType = string.Empty;
            foreach (var column in allColumns)
            {
                dataType = GetFriendlySqlDataType(column);

                stringBuilder.Append("\n");
                stringBuilder.Append("         public " + dataType + " " + column.ColumnName);
                stringBuilder.Append("\n");
                stringBuilder.Append("         { \n");

                stringBuilder.Append("             get { return Get<" + dataType + ">(\"" + column.ColumnName + "\"); }\n");
                stringBuilder.Append("             set { Set(\"" + column.ColumnName + "\", value); }\n");
                stringBuilder.Append("\n");
                stringBuilder.Append("         } \n");

            }

            // Footer
            stringBuilder.Append("     }\n");
            stringBuilder.Append("}\n");

            CreateOutput(ObjectName, true, false, stringBuilder.ToString(), "model", ObjectName + ".cs");
        }

        private string GetModelHeader()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("//using Karpster.Core.Data.EntityFramework;\n");
            stringBuilder.Append("//using System;\n");
            stringBuilder.Append("//using System.Collections.Generic;\n");
            stringBuilder.Append("//using System.ComponentModel.DataAnnotations;\n");

            stringBuilder.Append("namespace " + AppName + ".Core.Models \n{ \n");

            return stringBuilder.ToString();
        }
        #endregion

        #region Stored Procedures
        private string GetProcedureHeader(string ProcedureName, string schemaName)
        {
            return "CREATE PROCEDURE  [" + schemaName + "].[" + ProcedureName + "]";
        }

        private void GenerateSelectProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP, string schemaName)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName, schemaName));

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

            stringBuilder.Append("FROM [" + schemaName + "].[" + tableName + "]");
            stringBuilder.Append("\n");
            stringBuilder.Append("\n/*" + this.GetDropProcedureCode(ProcedureName) + "*/");
            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(ProcedureName, WriteFiles, WriteSP, CommandText);
        }

        private void GenerateSelectOneProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP, string schemaName)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName, schemaName));
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

            stringBuilder.Append("FROM [" + schemaName + "].[" + tableName + "]");

            stringBuilder.Append(this.GetSelectWHEREClause(primaryKeys));
            stringBuilder.Append("\n/*" + this.GetDropProcedureCode(ProcedureName) + "*/");
            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(ProcedureName, WriteFiles, WriteSP, CommandText);
        }

        private void GenerateDeleteProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP, string schemaName)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName, schemaName));
            stringBuilder.Append("\n");
            List<DataColumn> primaryKeys = this.GetPrimaryKeys(FieldList);
            if (primaryKeys.Count == 0)
                return;
            List<string> stringList = new List<string>();
            stringBuilder.Append(this.GetParameterListString(primaryKeys));
            stringBuilder.Append("\nAS\n");
            stringBuilder.Append("DELETE FROM  [" + schemaName + "].[" + tableName + "]\n");
            stringBuilder.Append(this.GetWHEREClause(primaryKeys));
            stringBuilder.Append("\n/*" + this.GetDropProcedureCode(ProcedureName) + "*/");
            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(ProcedureName, WriteFiles, WriteSP, CommandText);
        }

        private void GenerateUpdateProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP, string schemaName)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName, schemaName));
            List<DataColumn> allColumns = this.GetAllColumns(FieldList);
            List<DataColumn> primaryKeys = this.GetPrimaryKeys(FieldList);
            List<DataColumn> updatableColumns = this.GetUpdatableColumns(FieldList);
            if (updatableColumns.Count == 0)
                return;
            stringBuilder.Append(this.GetParameterListString(allColumns));
            stringBuilder.Append("\nAS\n");
            stringBuilder.Append("UPDATE [" + schemaName + "].[" + tableName + "] \n");
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

        private void GenerateInsertProcedure(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP, string schemaName)
        {
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.GetProcedureHeader(ProcedureName, schemaName));
            List<DataColumn> allColumns = this.GetAllColumns(FieldList);
            List<DataColumn> primaryKeys = this.GetPrimaryKeys(FieldList);
            List<DataColumn> updatableColumns = this.GetUpdatableColumns(FieldList);
            if (updatableColumns.Count == 0)
                return;
            stringBuilder.Append(this.GetParameterListString(updatableColumns, true, FieldList));
            stringBuilder.Append("\nAS\n");
            stringBuilder.Append("INSERT INTO [" + schemaName + "].[" + tableName + "]\n( \n");
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

        #endregion

        #region Views
        private string GetViewHeader(string Title)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("@{\n   ViewBag.Title = \"" + TitleCase(Title.Replace("_"," ")) + "\"; \n} \n\n");
            stringBuilder.Append("<div class=\"row\">\n");
            stringBuilder.Append("   <div class=\"col-lg-12\">\n");
            stringBuilder.Append("      <div class=\"hpanel\">\n");
            stringBuilder.Append("         <div class=\"panel-body\">\n");
            stringBuilder.Append("            <h1>" + TitleCase(Title.Replace("_", " ")) + "</h1>\n");

            return stringBuilder.ToString();
        }

        private string GetViewFooter()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("@section scripts {\n");
            stringBuilder.Append(" <script type=\"text/javascript\">\n// add scripts here\n</script>\n");
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }

        private void GenerateSelectViews(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP, string schemaName)
        {
            var viewGen = new ViewGenerator();
            string tableName = FieldList.Tables[0].TableName;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("@model List<"+AppName+".Core.Models."+ tableName.Replace("_", "") + ">\n\n");

            stringBuilder.Append(this.GetViewHeader("Manage " + tableName));

            stringBuilder.Append("\n");
            stringBuilder.Append("<table class=\"table table-striped table-bordered DataTable\">\n");
            stringBuilder.Append("   <thead>\n");
            List<DataColumn> selectableColumns = this.GetUpdatableColumns(FieldList);

            // Build Table Header
            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                if (index == 0)
                {
                    stringBuilder.Append("     <tr>");
                    stringBuilder.Append("\n      <th>");
                }
                DataColumn dataColumn = selectableColumns[index];
                stringBuilder.Append(dataColumn.ColumnName.Replace("_", " ").Replace("-", " "));
                if (index < selectableColumns.Count - 1)
                    stringBuilder.Append("</th>\n      <th>");
            }
            stringBuilder.Append("</th>\n      <th>Functions</th>");
            stringBuilder.Append("\n     </tr>\n   </thead>\n   <tbody>");
            stringBuilder.Append("\n");
            stringBuilder.Append("@{\n     foreach(var item in Model){");
            stringBuilder.Append("\n          <tr>\n");

            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                DataColumn dataColumn = selectableColumns[index];
                stringBuilder.Append("             <td>@Html.DisplayFor(modelItem => item." + dataColumn.ColumnName + ")</td>");
                stringBuilder.Append("\n");
            }
            stringBuilder.Append("             <td>\n");
            stringBuilder.Append("                @if(User.IsInRole(\"user.edit.example\"))\n");
            stringBuilder.Append("                {\n");
            stringBuilder.Append("                   <a href=\"@Url.Action(\"Edit\", new { id = item.Id })\" class=\"btn btn-xs btn-default\">Edit</a>\n");
            stringBuilder.Append("                }\n");
            stringBuilder.Append("                @if(User.IsInRole(\"user.delete.example\"))\n");
            stringBuilder.Append("                {\n");
            stringBuilder.Append("                   <a href=\"@Url.Action(\"Delete\", new { id = item.Id })\" class=\"btn btn-xs btn-danger\">Delete</a>\n");
            stringBuilder.Append("                }\n");
            stringBuilder.Append("             </td>");

            stringBuilder.Append("\n");
            stringBuilder.Append("          </tr>\n       } \n}");
            stringBuilder.Append("\n    </tbody>");
            stringBuilder.Append("\n</table>\n\n");
            stringBuilder.Append("<a href = \"@Url.Action(\"Insert\",\""+ tableName.Replace("_", "") + "\")\" class=\"btn btn-primary pull-right\">Add " + tableName.Replace("_", " ") + "</a>");
            stringBuilder.Append("</div>\n");
            stringBuilder.Append("</div>\n");
            stringBuilder.Append("</div>\n");
            stringBuilder.Append("</div>\n\n");

            stringBuilder.Append(viewGen.AddDataTablesJS());

            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(tableName, WriteFiles, false, CommandText, "view", "Index.cshtml");
        }

        private void GenerateUpdateViews(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP, string schemaName)
        {
            string tableName = FieldList.Tables[0].TableName;
            var viewGen = new ViewGenerator();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("@model " + AppName + ".Core.Models.Update" + tableName + "ViewModel\n\n");
            stringBuilder.Append(this.GetViewHeader("Update " + tableName));

            stringBuilder.Append("\n");
            stringBuilder.Append("   @using (Html.BeginForm(\"Edit" + tableName + "\", \"" + tableName + "\", FormMethod.Post, new { id = \"" + tableName + "Form\", @class = \"form form-horizontal\" }))\n");
            stringBuilder.Append("   {\n");


            List<DataColumn> selectableColumns = this.GetUpdatableColumns(FieldList);

            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                DataColumn dataColumn = selectableColumns[index];
                stringBuilder.Append(viewGen.CreateFormElement(dataColumn, "horizontal", "bootstrap"));
            }

            stringBuilder.Append("        <input class=\"btn btn-success\" type=\"submit\" value=\"Update\" /> \n\n");
            stringBuilder.Append("    }\n");

            stringBuilder.Append(GetViewFooter());

            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(tableName, WriteFiles, false, CommandText, "view", "Update.cshtml");
        }

        private void GenerateInsertViews(string ProcedureName, DataSet FieldList, bool WriteFiles, bool WriteSP, string schemaName)
        {
            string tableName = FieldList.Tables[0].TableName;
            var viewGen = new ViewGenerator();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("@model " + AppName + ".Core.Models.Insert" + tableName + "ViewModel\n\n");
            stringBuilder.Append(this.GetViewHeader("Add "+tableName));

            stringBuilder.Append("\n");
            stringBuilder.Append("        @using (Html.BeginForm(\"Insert" + tableName + "\", \"" + tableName + "\", FormMethod.Post, new { id = \"" + tableName + "Form\", @class = \"form form-horizontal\" }))\n");
            stringBuilder.Append("        {\n");


            List<DataColumn> selectableColumns = this.GetUpdatableColumns(FieldList);

            for (int index = 0; index < selectableColumns.Count; ++index)
            {
                DataColumn dataColumn = selectableColumns[index];
                stringBuilder.Append(viewGen.CreateFormElement(dataColumn, "horizontal", "bootstrap"));
            }

            stringBuilder.Append("            <input class=\"btn btn-success\" type=\"submit\" value=\"Add " + tableName+"\" /> \n\n");
            stringBuilder.Append("        }\n");

            stringBuilder.Append(GetViewFooter());

            string CommandText = stringBuilder.ToString().Replace("\n", Environment.NewLine);

            CreateOutput(tableName, WriteFiles, false, CommandText, "view", "Insert.cshtml");
        }
        #endregion

        #region Utilities
        private void CreateOutput(string ProcedureName, bool WriteFiles, bool WriteSP, string CommandText, string outputType = "", string outputFile = "")
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
                if (outputFile != "")
                {
                    SaveToFile(CommandText, ProcedureName, outputType, outputFile);
                }
                else
                {
                    SaveToFile(CommandText, ProcedureName, "sql");
                }
            }
        }

        private void SaveToFile(string CommandText, string ProcedureName, string outputType = "", string outputFile = "")
        {
            var fileName = SetupDirectoriesAndFilename(CommandText, ProcedureName, outputType, outputFile);
            using (StreamWriter outfile = new StreamWriter(fileName))
            {
                outfile.Write(CommandText);
            }
        }

        private string SetupDirectoriesAndFilename(string CommandText, string ProcedureName, string outputType, string outputFile)
        {
            string path = @"c:\Orwell-Generated-" + timestamp;
            string viewDirectory = @"c:\Orwell-Generated-" + timestamp + "\\Views";
            string viewSubDirectory = @"c:\Orwell-Generated-" + timestamp + "\\Views\\" + TitleCase(ProcedureName);
            string modelDirectory = @"c:\Orwell-Generated-" + timestamp + "\\Models";
            string sqlDirectory = @"c:\Orwell-Generated-" + timestamp + "\\Sql";
            string coreDataDirectory = @"c:\Orwell-Generated-" + timestamp + "\\Data";
            string fileName = string.Empty;
            try
            {
                // Main directory
                if (!Directory.Exists(path))
                {
                    DirectoryInfo di = Directory.CreateDirectory(path);
                }
                switch (outputType)
                {
                    case "view":
                        if (!Directory.Exists(viewDirectory))
                        {
                            DirectoryInfo di2 = Directory.CreateDirectory(viewDirectory);
                        }
                        if (!Directory.Exists(viewSubDirectory))
                        {
                            DirectoryInfo di3 = Directory.CreateDirectory(viewSubDirectory);
                        }
                        fileName = viewSubDirectory + "\\" + outputFile;
                        break;
                    case "model":
                        if (!Directory.Exists(modelDirectory))
                        {
                            DirectoryInfo di4 = Directory.CreateDirectory(modelDirectory);
                        }
                        fileName = modelDirectory + "\\" + outputFile;
                        break;
                    case "data":
                        if (!Directory.Exists(coreDataDirectory))
                        {
                            DirectoryInfo di4 = Directory.CreateDirectory(coreDataDirectory);
                        }
                        fileName = coreDataDirectory + "\\" + outputFile;
                        break;
                    case "sql":
                    default:
                        if (!Directory.Exists(sqlDirectory))
                        {
                            DirectoryInfo di4 = Directory.CreateDirectory(sqlDirectory);
                        }
                        fileName = sqlDirectory + "\\" + ProcedureName + ".sql";
                        break;
                }
                return fileName.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            finally { }
            return null;
        }
        
        private string TitleCase(string AnyString)
        {
            var anyString = AnyString.ToLower();
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;
            return textInfo.ToTitleCase(anyString);
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

        private string GetFriendlySqlDataType(DataColumn dc)
        {
            string str = "";
            switch (dc.DataType.ToString().ToLower())
            {
                case "system.boolean":
                    str = "bool";
                    break;
                case "system.datetime":
                    str = "DateTime";
                    break;
                case "system.decimal":
                    str = "float";
                    break;
                case "system.int16":
                    str = "int";
                    break;
                case "system.int32":
                    str = "int";
                    break;
                case "system.int64":
                    str = "int";
                    break;
                case "system.string":
                    str = "string";
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
        #endregion
    }

}