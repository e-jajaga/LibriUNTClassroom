using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.ViewModels
{
    public class CourseSyllabus
    {
        public string courseId { get; set; }
        public string courseName { get; set; }
        public string instructors { get; set; }
        public bool? hasSyllabus { get; set; }
    }
}