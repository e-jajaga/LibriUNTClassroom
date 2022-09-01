using Google.Apis.Classroom.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.Models
{
    public class LibriTeacher
    {
        public string GoogleId { get; set; }      //google userid
        public string Username { get; set; }
        public string TeacherName { get; set; }
        public string TeacherEmail { get; set; }
        public string PhotoUrl { get; set; }
        public string DeparmentCode { get; set; }
    }
}