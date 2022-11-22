using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMG.Models
{
    public class Category
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ParentCategory { get; set; }
    }
}
