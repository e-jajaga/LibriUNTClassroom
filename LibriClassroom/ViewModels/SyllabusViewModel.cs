using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.ViewModels
{
    public class SyllabusViewModel
    {
        public List<CourseSyllabus> courses { get; set; }
        public bool withsyl { get; set; }
    }
}