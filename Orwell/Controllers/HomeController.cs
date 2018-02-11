using Orwell.Models;
using Orwell.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Orwell.Controllers
{
    public class HomeController : Controller
    {

        MultiSelectList tableList { get; set; }
        
        public ActionResult Index()
        {
            var vm = new PopulateTablesViewModel();
            vm.Tables = null;
            List<SelectListItem> items = new List<SelectListItem>();
            var item = new SelectListItem() { Text = "None", Value = "None" };
            items.Add(item);
            vm.Tables = new MultiSelectList(items.OrderBy(i => i.Text), "Value", "Text");
            vm.TableIds = new List<string>();
            return View(vm);
        }

        [HttpPost]
        public ActionResult PopulateTables(PopulateTablesViewModel vm)
        {
            List<SelectListItem> items = new List<SelectListItem>();
            var conxString = String.Empty;
            conxString = GetConnectionString(vm);
            DataAccessSql.ConnectionString = conxString;
            try
            {
                foreach (string str2 in DataAccessSql.GetTableNamesFromDatabase())
                {   
                    if (!str2.StartsWith("sys"))
                    {

                        var item = new SelectListItem
                        {
                            Value = str2,
                            Text = str2
                        };
                        items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            tableList = new MultiSelectList(items.OrderBy(i => i.Text), "Value", "Text");
            vm.Tables = tableList;

            return PartialView("_selectTables", vm);
        }

        private static string GetConnectionString(PopulateTablesViewModel vm)
        {
            string conxString;
            if (vm.ConxType == "Integrated")
            {
                conxString = String.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;MultipleActiveResultSets=true", vm.ServerName, vm.DatabaseName);
            }
            else
            {
                conxString = String.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3};Application Name={4};MultipleActiveResultSets=True;", vm.ServerName, vm.DatabaseName, vm.Username, vm.Password, vm.AppName);
            }

            return conxString;
        }

        [HttpPost]
        public ActionResult Generate(FormCollection form)
        {
            var vm = new PopulateTablesViewModel();
            vm.AppName = form["AppName"];
            vm.ConxType = form["ConxType"];
            vm.DatabaseName = form["DatabaseName"];
            vm.ScaffoldType = form["ScaffoldType"];
            vm.ServerName = form["ServerName"];
            vm.Username = form["Username"];
            vm.Password = form["Password"];
            var tables = form["Tables"];
            
            var formatedList = tables.Split(',');
            try
            {
                var conxString = GetConnectionString(vm);
                DataAccessSql.ConnectionString = conxString;
                bool writeFiles = false;
                bool writeSP = false;
                switch (vm.ScaffoldType)
                {
                    case "Both":    
                        writeFiles = true;
                        writeSP = true;
                        break;
                    case "SP Only":
                        writeSP = true;
                        break;
                    case "Files Only":
                        writeFiles = true;
                        break;

                }

                List<DatabaseTable> databaseTableList = new List<DatabaseTable>();
                var genUp = new SpGenerator();
                genUp.timestamp = DateTime.Now.ToString("yyyyMMddhhmmss");
                genUp.AppName = vm.AppName;
                foreach (string checkedItem in formatedList)
                {
                    databaseTableList.Add(new DatabaseTable()
                    {
                        TableName = checkedItem.ToString(),
                        SingularName = checkedItem.ToString().Replace("dbo.", "").Replace(".", "").Trim(),
                        PluralName = checkedItem.ToString().Replace("dbo.", "").Replace(".", "").Trim(),
                        DeleteProcedure = true,
                        UpdateProcedure = true,
                        InsertProcedure = true,
                        SelectProcedure = false,
                        SelectDetailsProcedure = true,
                        WriteFiles = writeFiles,
                        WriteProcedures = writeSP
                    });
                }
                genUp.GenerateStoreProcedures(conxString, databaseTableList);
            }
            catch (Exception ex)
            {
                return View("Generate", ex.Message);
            }

            return View("Generate");
        }
    }
}