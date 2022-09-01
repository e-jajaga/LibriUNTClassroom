using LibriClassroom.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibriClassroom.ViewModels
{
    public class DashboardViewModel
    {
        public List<CourseViewModel> courses { get; set; }
        public List<Feed> feeds { get; set; }
        public List<LibriTeacher> instructors { get; set; }
    }
}
