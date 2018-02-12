using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace Orwell.Models
{
    public class ViewGenerator
    {

        public string AddDataTablesJS()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("@section scripts {\n");
            stringBuilder.Append("   <link href = \"//cdn.datatables.net/1.10.11/css/jquery.dataTables.min.css\" rel=\"stylesheet\">\n");
            stringBuilder.Append("   <link href=\"//cdn.datatables.net/1.10.11/css/dataTables.bootstrap.min.css\" rel=\"stylesheet\">\n");
            stringBuilder.Append("   <script src = \"//cdn.datatables.net/1.10.11/js/jquery.dataTables.min.js\"></script>\n");
            stringBuilder.Append("   <script>\n");
            stringBuilder.Append("      $(document).ready(function() {\n");
            stringBuilder.Append("        $('.DataTable').DataTable(\n");
            stringBuilder.Append("        {\n");
            stringBuilder.Append("            \"pagingType\": \"full_numbers\",\n");
            stringBuilder.Append("            \"lengthMenu\": [[25, 50, 100, -1], [25, 50, 100, \"All\"]]\n");
            stringBuilder.Append("        }\n");
            stringBuilder.Append("      );\n");
            stringBuilder.Append("    });\n");
            stringBuilder.Append("   </script>\n");
            stringBuilder.Append("}\n");
            return stringBuilder.ToString();
        }



        public string CreateFormElement(DataColumn dataColumn, string layout = "horizontal", string style = "bootstrap")
        {
            StringBuilder stringBuilder = new StringBuilder();
            switch (style)
            {
                case "bootstrap":
                default:
                    if (layout == "horizontal")
                    {
                        stringBuilder.Append("         <div class=\"form-group\">\n");
                        stringBuilder.Append("             @Html.LabelFor(model => model." + dataColumn.ColumnName + ", htmlAttributes: new { @class = \"control-label col-md-3\" })\n");
                        stringBuilder.Append("             <div class=\"col-md-9\">\n");
                        stringBuilder.Append("                 @Html.EditorFor(model => model." + dataColumn.ColumnName + ", new { htmlAttributes = new { @class = \"form-control\" } })\n");
                        stringBuilder.Append("                 @Html.ValidationMessageFor(model => model." + dataColumn.ColumnName + ", \"\", new { @class = \"text-danger\" })\n");
                        stringBuilder.Append("            </div>\n");
                        stringBuilder.Append("         </div>\n\n");
                    }
                    break;
            }


            return stringBuilder.ToString();
        }


    }
}