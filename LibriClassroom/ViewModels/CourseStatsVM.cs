using LibriClassroom.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LibriClassroom.ViewModels
{
    public class CourseStatsVM
    {
        public string Id { get; set; }
        //public string CourseCode { get; set; }
        public string Title { get; set; }
        public string Section { get; set; }
        public bool IsCourseEmpty { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime UpdateTime { get; set; }
        //public List<CourseAssistant> Assistants { get; set; }
        public string InvitedTeachers { get; set; }
    }
}