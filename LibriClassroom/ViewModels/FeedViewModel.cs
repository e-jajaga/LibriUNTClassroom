using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibriClassroom.ViewModels
{
    public class FeedViewModel
    {
        [Key, Required]
        public string id { get; set; }
        public string courseId { get; set; }        
        public string courseName { get; set; }      
        public string courseLink { get; set; }
        public string matSetName { get; set; }
        public string workType { get; set; }  
        public string title { get; set; }
        public string alternateLink { get; set; }
        public DateTime updateTime { get; set; }
        public string ThumbnailUrl { get; set; }
        public string ResponseUrl { get; set; }     //per Form types

       
    }
}
