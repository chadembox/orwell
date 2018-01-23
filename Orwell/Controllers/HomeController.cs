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

            return View("Tables", vm);
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
        public ActionResult Generate(PopulateTablesViewModel vm)
        {

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
                foreach (object checkedItem in vm.TableIds)
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

                var genUp = new SpGenerator();

                genUp.GenerateStoreProcedures(conxString, databaseTableList);

            }
            catch (Exception ex)
            {
                return View(ex.Message);
            }

            return View();
        }
    }
}