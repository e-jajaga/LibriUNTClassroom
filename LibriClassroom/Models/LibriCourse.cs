using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace LibriClassroom.Models
{
    [Table("LibriCourse")]
    public class LibriCourse
    {

        //[Required]
        public string Id { get; set; }

        public string CourseCode { get; set; }

        //[Required]
        public string Term { get; set; }

        //[Required]
        public string OwnerId { get; set; }        //course creator userid

        //[Required]
        public string Title { get; set; }

        public List<LibriTeacher> Teachers { get; set; }

        public List<LibriStudent> Students { get; set; }

        //[Required]
        public DateTime CreationTime { get; set; }

        //[Required]
        public string Link { get; set; }

        public string EnrollmentCode { get; set; }

        //[Required]
        //[Display(Name = "Pin")]
        //public bool RequiresPin { get; set; }

        //[Required]
        //[Display(Name = "Pin Code")]
        //public string PinCode { get; set; }

        //public string ResourceId { get; set; }      //course folder google ID

        //public string FilesFolderId { get; set; }

        ////[Required]
        //[Display(Name = "Initialized")]
        //public bool IsInitialized { get; set; }

        ////[Required]
        //[Display(Name = "Elective")]
        //public bool IsElective { get; set; }

        ////[Required]
        //public bool Status { get; set; }

        


        //[ForeignKey("TermId")]
        //public virtual Term Term { get; set; }

        //[ForeignKey("UserId")]
        //public virtual UserProfile Instructor { get; set; }

        //public virtual ICollection<ACL> ACL { get; set; }
        
        //public virtual ICollection<Enrollment> Roster { get; set; }

        //public virtual ICollection<CourseAssistant> CourseAssistants { get; set; }
    }
}
