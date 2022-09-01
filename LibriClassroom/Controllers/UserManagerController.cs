using LibriClassroom.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Web.Caching;

namespace LibriClassroom.Controllers
{
    public class UserManagerController : Controller
    {
        private static DataClasses1DataContext db = new DataClasses1DataContext();
        //
        // GET: /Person/

        //public static List<SelectListItem> instructorsLocal = new List<SelectListItem>()
        //{
        //    new SelectListItem() { Text="Adrian Besimi", Value="a.besimi" },
        //    new SelectListItem() { Text="Jusuf Zekiri", Value="j.zekiri" },
        //    new SelectListItem() { Text="Edmond Jajaga", Value="e.jajaga" },
        //    new SelectListItem() { Text="Burim Ismaili", Value="b.ismaili" },
        //};

        public ActionResult Index()
        {
            //regjistro sesionin nga SSO nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();


            List<DeanDepartmentsViewModel> lista = new List<DeanDepartmentsViewModel>();

            try
            {

                var query = from d in db.LibriDepDeans
                            select d;

                foreach (LibriDepDean q in query)
                {
                    //string name = instructorsLocal.Where(u => u.Value.Equals(q.username)).Select(v => v.Text).SingleOrDefault();
                    DeanDepartmentsViewModel item = new DeanDepartmentsViewModel
                    {
                        ID = q.ID,
                        Dean = q.deanname,
                        Department = q.depname,
                        DeparmentCode = q.code
                    };
                    lista.Add(item);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return View(lista);
        }

        //
        // GET: /UserManager/Create
        //[Authorize]
        public ActionResult Create()
        {
            //regjistro sesionin nga SSO nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            ViewBag.instruktoret = new SelectList(GetInstructors(), "Value", "Text", "");
            ViewBag.departamentet = new SelectList(GetDepartments(), "Value", "Text", "");

            return View();
        }

        //
        // POST: /UserManager/Create

        [HttpPost]
        //[Authorize]
        public ActionResult Create(LibriDepDean dep)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    dep.ID = Guid.NewGuid();
                    var department = GetDepartments().Where(u=>u.Value.Equals(dep.code)).SingleOrDefault();
                    dep.depname = department.Text;
                    var dean = GetInstructors().Where(u => u.Value.Equals(dep.username)).SingleOrDefault();
                    dep.deanname = dean.Text;
                    db.LibriDepDeans.InsertOnSubmit(dep);
                    db.SubmitChanges();

                    return RedirectToAction("Index");
                }
                catch (AggregateException e)
                {
                    ModelState.AddModelError("_FORM", "Cannot create department!");
                }
            }

            ViewBag.instruktoret = new SelectList(GetInstructors(), "Value", "Text", "");
            ViewBag.departamentet = new SelectList(GetDepartments(), "Value", "Text", "");
            return View(dep);
        }

        //
        // GET: /CourseManager/Edit/5
        //[Authorize(Roles = "seeu\\libriadmin")]
        //[Authorize]
        public ActionResult Edit(Guid id)
        {
            if (Session["user"] == null) RegisterSession();
            else
            {
                LibriDepDean d = (from dep in db.LibriDepDeans
                            where dep.ID == id
                            select dep).SingleOrDefault();

                ViewBag.username = new SelectList(GetInstructors(), "Value", "Text", d.username);
                //ViewBag.instruktoret = new SelectList(GetInstructors(), "Value", "Text", "");
                ViewBag.departamentet = new SelectList(GetDepartments(), "Value", "Text", "");

                if (d!=null)
                {
                    return View(d);
                }
                else
                    return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        ////
        //// POST: /CourseManager/Edit/5

        [HttpPost]
        //[Authorize]
        public ActionResult Edit(LibriDepDean dep)
        {
            if (ModelState.IsValid)
            {
                dep.deanname = GetInstructors().Where(u => u.Value.Equals(dep.username)).Select(v => v.Text).SingleOrDefault();
                dep.depname = GetDepartments().Where(d => d.Value.Equals(dep.code)).Select(v => v.Text).SingleOrDefault();
                UpdateModel(dep);
                                
                // Submit the changes to the database.
                try
                {
                    db.SubmitChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    // Provide for exceptions.
                }
                return RedirectToAction("Index");
            }

            ViewBag.instruktoret = new SelectList(GetInstructors(), "Value", "Text", dep.username);
            return View(dep);
        }

        //
        // GET: /CourseManager/Delete/5
        //[Authorize]
        public ActionResult Delete(Guid id)
        {
            if (Session["user"] == null) RegisterSession();
            else
            {
                if (id != null)
                {
                    var query = (from dp in db.LibriDepDeans
                                 where dp.ID == id
                                 select dp).SingleOrDefault();
                    db.LibriDepDeans.DeleteOnSubmit(query);
                    try
                    {
                        db.SubmitChanges();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        // Provide for exceptions.
                    }
                }
            }
            return RedirectToAction("Index");
        }

        #region Helpers

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
            //                case "http://schemas.microsoft.com/ws/2008/06/identity/claims/role":
            //                    if (claim.Value == "seeu\\libriadmin")
            //                        Session["role"] = "libriadmin";Session["dep"] = "A";
            //                    break;
            //            }
            //        }
            //        //nese seshte aktiv sesioni i rolit
            //        if (Session["role"] == null)
            //        {
            //            //merre username-in krahasoje me ato ne tabelen e userave te privilegjuar
            //            var privUser = (from u in db.LibriDepDeans
            //                            where u.username == username
            //                            select u).SingleOrDefault();

            //            //regjgistro rolin nese eshte priv.user
            //            if (privUser != null)
            //            {
            //                Session["dep"] = privUser.code;     //ELC, N-CST, N-TT, N-LAW, N-PA, N-BA, LC & ALL
            //                if (privUser.depname == "Administrators")
            //                    Session["role"] = "libriadmin";
            //                else Session["role"] = "authorized";     //authorized -> high mngmnt and deans/directors 
            //                //libriadmin -> admins
            //            }

            //            //Faculty of Contemporary Sciences and Technologies - N-CST
            //            //Faculty of Languages, Cultures and Communication - N-TT
            //            //Faculty of Law - N-LAW
            //            //Faculty of Public Administration and Political Sciences - N-PA
            //            //Faculty of Business and Economics - N-BA
            //            //Language Centre - LC
            //            //E-Learning Centre - ELC
            //        }
            //    }
            //}
            Session["role"] = "libriadmin"; Session["dep"] = "A"; Session["user"] = "admin";
        }

        #endregion

        private List<SelectListItem> GetInstructors()
        {
            List<SelectListItem> instructorsService = new List<SelectListItem>();
            Dictionary<string, string> teachers = new Dictionary<string, string>();
            teachers = System.Web.HttpContext.Current.Application["Cashed_Teachers"] as Dictionary<string, string>;
            foreach (var teacher in teachers)
            {
                SelectListItem profiri = new SelectListItem
                {
                    Value = teacher.Key,
                    Text = teacher.Value
                };
                instructorsService.Add(profiri);
            }
            //burim edmond
            SelectListItem burim = new SelectListItem
            {
                Value = "b.ismaili",
                Text = "Burim Ismaili"
            };
            instructorsService.Add(burim);
            SelectListItem edmond = new SelectListItem
            {
                Value = "e.jajaga",
                Text = "Edmond Jajaga"
            };
            instructorsService.Add(edmond);

            return instructorsService.OrderBy(l=> l.Value).ToList();
        }


        private List<SelectListItem> GetDepartments()
        {
            List<SelectListItem> depsService = new List<SelectListItem>();
            Dictionary<string, string> deps = new Dictionary<string, string>();
            deps = System.Web.HttpContext.Current.Application["Cashed_Departments"] as Dictionary<string, string>;
            foreach (var teacher in deps)
            {
                SelectListItem depiri = new SelectListItem
                {
                    Value = teacher.Key,
                    Text = teacher.Value
                };
                depsService.Add(depiri);
            }
            return depsService;
        }

        #endregion
    }
}
