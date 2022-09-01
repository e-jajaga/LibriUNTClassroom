using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.WebPages.Html;

namespace LibriClassroom.ViewModels
{
    public class TermCoursesViewModel
    {
        public IEnumerable<SelectListItem> Terms { get; set; }

        public List<CourseViewModel> Courses { get; set; }
    }
}