using Google.Apis.Classroom.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.Models
{
    public class LibriStudent
    {
        public string UserId { get; set; }      //google's userid
        public string Username { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
    }
}