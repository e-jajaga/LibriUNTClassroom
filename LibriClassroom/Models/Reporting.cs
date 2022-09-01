using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LibriClassroom.Models;

namespace LibriClassroom.ViewModels
{
    public class Reporting
    {
        public Reporting()
        {
            totalMateriale = 0;
            totalMaterialeJavaFundit = 0;
            totalDetyra = 0;
            totalDetyraJavaFundit = 0;
        }
        public KursiMeMateriale nrKursetMeMaterialePerMuaj { get; set; }
        //public List<CourseViewModel> listaKurseve { get; set; }
        public int perqindjaKurseveMeMateriale { get; set; }
        public int totalMateriale { get; set; }
        public int totalMaterialeJavaFundit { get; set; }
        public int totalDetyra { get; set; }
        public int totalDetyraJavaFundit { get; set; }
        public int totalLende { get; set; }
        public int totalLendePaMateriale { get; set; }
        public int nrLendeMeSyllPaMat { get; set; }

    }
}