using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using Google.Apis.Drive.v2;
using Google.Apis.Services;
using LibriClassroom.Models;
using LibriClassroom.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LibriClassroom.Controllers
{
    public class HomeController : Controller
    {
        private static DataClasses1DataContext db = new DataClasses1DataContext();

        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to Libri v3 Beta version. Thank you for using Libri as Learning Management System. Please feel free to send us any comments or suggestions by giving feedback on User Voice or by sending us email at libri@seeu.edu.mk<br /><br />Your Libri Team";

            //regjistrimi i sesionit te userit
            RegisterSession();

            return View();
        }

        //nuk perdoret
        public async Task<ActionResult> IndexAsync(CancellationToken cancellationToken)
        {
            var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                AuthorizeAsync(cancellationToken);

            if (result.Credential != null)
            {
                //aktivizimi i Drive servisit
                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = result.Credential,
                    ApplicationName = "ASP.NET MVC Sample"
                });

                // YOUR CODE SHOULD BE HERE..
                // SAMPLE CODE:
                var list = await service.Files.List().ExecuteAsync();
                ViewBag.Message = "FILE COUNT IS: " + list.Items.Count();

                //Classroom 
                // Create Classroom API service.
                var clservice = new ClassroomService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = result.Credential,
                    ApplicationName = "Classroom Sample",
                });

                // Define request parameters.
                CoursesResource.ListRequest request = clservice.Courses.List();
                request.PageSize = 10;

                // List courses.
                ListCoursesResponse response = request.Execute();
                List<CourseViewModel> listaKurseve = new List<CourseViewModel>();
                //Console.WriteLine("Courses:");
                if (response.Courses != null && response.Courses.Count > 0)
                {
                    foreach (var course in response.Courses)
                    {
                        //Debug.WriteLine("{0} ({1})", course.Name, course.Id);
                        CourseViewModel c = new CourseViewModel { Id = course.Id, Title = course.Name };
                        listaKurseve.Add(c);
                    }
                }
                else
                {
                    //Debug.WriteLine("No courses found.");
                }

                return View(listaKurseve);
            }
            else
            {
                return new RedirectResult(result.RedirectUri);
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        #region RegisterSession

        //sesioni per admin merret nga SSO ndersa per dekanet nga DBja e LibriClassroom
        private void RegisterSession()
        {
            //if (User.Identity.IsAuthenticated)
            //{
            //    ClaimsPrincipal claimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            //    //merri te dhenat e logimit
            //    List<int> roles = new List<int>(); string username = String.Empty; Guid userid = Guid.Empty;
            //    string email = String.Empty; string name = String.Empty;
            //    if (claimsPrincipal != null)
            //    {
            //        foreach (Claim claim in claimsPrincipal.Claims)
            //        {
            //            switch (claim.Type)
            //            {
            //                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress":
            //                    email = claim.Value;
            //                    break;
            //                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":
            //                    name = claim.Value;
            //                    break;
            //                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname":
            //                    username = claim.Value;
            //                    Session["user"] = username;
            //                    break;
            //                //case "http://schemas.microsoft.com/ws/2008/06/identity/claims/role":
            //                //    if (claim.Value == "seeu\\libriadmin")
            //                //        Session["role"] = "libriadmin";Session["dep"] = "A";
            //                //    break;
            //            }
            //        }
            //        //merre username-in krahasoje me ato ne tabelen e userave te privilegjuar
            //        var dbUser = (from u in db.GoogleUsers
            //                        where u.Username == username
            //                        select u).SingleOrDefault();

            //        //regjgistro rolin nese eshte priv.user
            //        if (dbUser != null)
            //        {
            //            var privUser = db.LibriDepDeans.Where(u => u.username == dbUser.Username).SingleOrDefault();
            //            Session["dep"] = privUser.code;     //E, C, T, L, P, B, Q, R & A, while O-none
            //            if(privUser.code == "A")
            //                Session["role"] = "libriadmin";Session["dep"] = "A";
            //            else if(privUser.code == "R")
            //                Session["role"] = "rectorate";
            //            else Session["role"] = "authorized";     //authorized -> high mngmnt and deans/directors 
            //            //libriadmin -> admins
            //        }

            //        //Faculty of Contemporary Sciences and Technologies - N-CST -> C
            //        //Faculty of Languages, Cultures and Communication - N-TT -> T
            //        //Faculty of Law - N-LAW -> L
            //        //Faculty of Public Administration and Political Sciences - N-PA -> P
            //        //Faculty of Business and Economics - N-BA -> B
            //        //Language Centre - LC -> Q
            //        //E-Learning Centre - ELC -> E
            //        //Administrators -> A
            //        //Rectorate -> R
            //    }
            //}
            Session["role"] = "libriadmin";Session["dep"] = "A";
        }

        #endregion
    }
}
