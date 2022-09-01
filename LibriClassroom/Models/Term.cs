using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace LibriClassroom.Models
{
    [Table("Term")]
    public class Term
    {
 
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "Code")]
        public int TermCode { get; set; }

        //[Display(Name = "Code")]
        //public int Code { get; set; }

        [Required]
        [Display(Name = "Term")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string AcademicYear { get; set; }

        public virtual ICollection<LibriCourse> Courses { get; set; }
    }
}
