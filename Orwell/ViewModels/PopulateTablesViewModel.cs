using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Orwell.ViewModels
{
    public class PopulateTablesViewModel
    {

        [Required]
        [Display(Name = "Server Name")]
        public string ServerName { get; set; }

        [Required]
        [Display(Name = "Database Name")]
        public string DatabaseName { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Application Name")]
        public string AppName { get; set; }

        public string ConxType { get; set; }
        public string ScaffoldType { get; set; }

        public List<string> TableIds { get; set; }

        [Display(Name = "Tables")]
        public MultiSelectList Tables { get; set; }
    }
}