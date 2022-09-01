using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibriClassroom.Models
{
    public class Stat
    {
        public Stat()
        {
            hasSyllabus = false;
            hasAssignments = false;
            hasResources = false;
            hasDiffAssignmentTypes = false;
            nrStreams = 0;
            nrStudents = 0;
            nrResources = 0;
            nrAssignments = 0;
            courseLevel = 0;
        }

        public string courseId { get; set; }        
        public string userId { get; set; }
        public int nrStudents { get; set; }
        public bool hasSyllabus { get; set; }
        public bool hasResources { get; set; }
        public bool hasAssignments { get; set; }
        public bool hasDiffAssignmentTypes { get; set; }
        public int nrResources { get; set; }
        public int nrAssignments { get; set; }
        public int nrStreams { get; set; }
        public int courseLevel { get; set; }        //0 -> bosh, 1 -> hasSyllabus = true + nrResources(+3), 2 -> L1 + nrResources (+3)
               
    }
}
