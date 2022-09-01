using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.Models
{
    public class DeanDepartmentsViewModel
    {
        public Guid ID { get; set; }
        public string Dean { get; set; }
        public string Department { get; set; }
        public string DeparmentCode { get; set; }
    }
}