using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibriClassroom.Models
{
    public class AllCourseStats
    {
        public AllCourseStats()
        {
            hasSyllabus = 0;
            //hasAssignments = false;
            //hasResources = false;
            Streams = 0;
            Students = 0;
            Resources = 0;
            Assignments = 0;
            Level = 0;
        }

        public string Course { get; set; }
        public string CourseCode { get; set; }
        //public DateTime LastUpdated { get; set; }
        public string Instructor { get; set; }
        public string Department { get; set; }
        public int Students { get; set; }
        public int hasSyllabus { get; set; }
        //public bool hasResources { get; set; }
        //public bool hasAssignments { get; set; }
        public int Resources { get; set; }
        public int Assignments { get; set; }
        public int Streams { get; set; }
        public int Level { get; set; }        //0 -> bosh, 1 -> hasSyllabus = true + nrResources(+3), 2 -> L1 + nrResources (+3)
               
    }
}
