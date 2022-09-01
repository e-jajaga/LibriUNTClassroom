using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.ViewModels
{
    public class RosterViewModel
    {
        public string UserId { get; set; }      //google userid
        public string Username { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsTeacher { get; set; }
    }
}