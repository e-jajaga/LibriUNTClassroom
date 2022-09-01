using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LibriClassroom.Models
{
    public class ServiceCourse
    {
        public int id { get; set; }
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public string StudyType { get; set; }
        public string Campus { get; set; }
        public string StudyLanguage { get; set; }
        public string LessonType { get; set; }
        public int GroupNumber { get; set; }
        public int SubGroup { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxSeets { get; set; }
        public string Teacher { get; set; }
        public string TeacherName { get; set; }
        public string Room { get; set; }
    }
}
