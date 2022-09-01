using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Classroom.v1.Data;
using LibriClassroom.Models;

namespace LibriClassroom.ViewModels
{
    public class CourseViewModel
    {
        public string Id { get; set; }
        //public string CourseCode { get; set; }
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string DescriptionHeading { get; set; }
        public string Owner { get; set; }
        public string Link { get; set; }
        public bool Enrolled { get; set; }
        public DateTime UpdateTime { get; set; }
        //public List<CourseAssistant> Assistants { get; set; }
        public List<LibriTeacher> Teachers { get; set; }

        public string TeachersLs { get; set; }
        public string depid { get; set; }
        public string termid { get; set; }
        public int CourseLvl { get; set; }
    }
}
