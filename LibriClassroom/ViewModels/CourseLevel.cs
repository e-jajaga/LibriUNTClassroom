using LibriClassroom.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.ViewModels
{
    public class CourseLevel
    {
        public CourseLevel()
        {
            Level0 = 0;
            Level1 = 0;
            Level21 = 0;
            Level22 = 0;
            Level23 = 0;
            HasSyllabus = 0;
            nrDepCourses = 0;
        }
        public string department { get; set; }
        public int Level0 { get; set; }
        public int Level1 { get; set; }
        public int Level21 { get; set; }
        public int Level22 { get; set; }
        public int Level23 { get; set; }
        public int HasSyllabus { get; set; }        
        public int nrDepCourses { get; set; }
    }
}