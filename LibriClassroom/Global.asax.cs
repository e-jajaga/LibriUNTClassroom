using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using LibriClassroom.Models;
using Newtonsoft.Json;
using System.IdentityModel.Tokens;

namespace LibriClassroom
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
              
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterAuth();

           

            ////create cashed dictionaries
            //if (false)
            //{
            //    var seeuService = GetSeeuService();
            //    //teachers
            //    var instructorsJsonString = seeuService.GetInstructors();
            //    DataTable instructorsDataTbl = (DataTable)JsonConvert.DeserializeObject(instructorsJsonString, (typeof(DataTable)));
            //    Dictionary<string, string> listaTeacher = new Dictionary<string, string>();
            //    //List<SEEUteacher>  = new List<SEEUteacher>();
            //    string username = null; string fullname = null;
            //    foreach (DataRow row in instructorsDataTbl.Rows)
            //    {
            //        var values = row.ItemArray;
            //        username = values[0].ToString();
            //        fullname = values[5].ToString();
            //        listaTeacher.Add(username,fullname);
            //    }
            //    Application["Cashed_Teachers"] = listaTeacher;
                
            //    //terms
            //    var termsJsonString = seeuService.GetTerms();
            //    DataTable termsDataTbl = (DataTable)JsonConvert.DeserializeObject(termsJsonString, (typeof(DataTable)));
            //    Dictionary<string, string> listaTerms = new Dictionary<string, string>();

            //    foreach (DataRow row in termsDataTbl.Rows)
            //    {
            //        var values = row.ItemArray;
            //        listaTerms.Add(values[0].ToString(), values[1].ToString());
            //    }
            //    Context.Application["Cashed_Terms"] = listaTerms;

            //    //rolet
            //    Dictionary<string, string> listaDepartments = new Dictionary<string, string>();
            //    listaDepartments.Add("C", "Faculty of Contemporary Sciences and Technologies");
            //    listaDepartments.Add("T", "Faculty of Languages, Cultures and Communication");
            //    listaDepartments.Add("L", "Faculty of Law");
            //    listaDepartments.Add("P", "Faculty of Public Administration and Political Sciences");
            //    listaDepartments.Add("B", "Faculty of Business and Economics");
            //    listaDepartments.Add("Q", "Language Centre");
            //    listaDepartments.Add("E", "E-Learning Centre");
            //    listaDepartments.Add("A", "Administrators");
            //    listaDepartments.Add("R", "Rectorate");
            //    Context.Application["Cashed_Departments"] = listaDepartments;
                
            //}
            //RefreshValidationSettings();
            
        }

        protected void RefreshValidationSettings()
        {
            string configPath = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Web.config";
            string metadataAddress = ConfigurationManager.AppSettings["ida:FederationMetadataLocation"];
            ValidatingIssuerNameRegistry.WriteToConfig(metadataAddress, configPath);
        }

    }
}