using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibriClassroom.Models.Entities
{
    [Table("Announcement")]
    public class Announcement1
    {
        [Key, Required]
        public int Id { get; set; }
        public string Title { get; set; }
        
        public string Body { get; set; }
        [Required]
        public string CourseID { get; set; }
        //public string CourseTitle { get; set; }
        public string Username { get; set; }
        public DateTime DatePosted { get; set; }
        //public bool Mode { get; set; }
        //public string Gravatar
        //{
        //    get
        //    {
        //        return GravatarHelper.GetUserGratavar(Username);
        //    }
        //}
        //public bool IsRead { get; set; }

        [NotMapped]
        public string ShortBody
        {
            get
            {
                string _short = String.Empty;
                if (Body.Length >= 100)
                {
                    _short = Body.Substring(0, 100);
                }
                else
                {
                    _short = Body;
                }
                return _short;
            }
        }

        [ForeignKey("CourseID")]
        public virtual LibriCourse Course { get; set; }
    }
}
