using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace blazelogBase.Shared.Models
{
    public class JsonSearchClass : IBelongtoTable
    {
        // table name
        public String tablename { get; set; }
        // table column name
        public String key { get; set; }
        // table column value
        public String value { get; set; }
        // table column datatype
        public String datatype { get; set; }
        // table column operator
        public String op { get; set; }
        // group for multi-select checklist
        public String group { get; set; }
    }
}
