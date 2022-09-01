using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.Models
{
    public class ImportCourse
    {
        public string CourseTitle { get; set; }
        public string CourseCode { get; set; }      //departamenti + semestri + niveli + gjuha = C + S7 + U + A 
        public string OwnerId { get; set; }
    }
}