using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using Google.Apis.Drive.v2;
using Google.Apis.Services;
using LibriClassroom.Models;
using LibriClassroom.Models.Entities;
using LibriClassroom.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Auth.OAuth2;
using System.Net;
using System.Data;
using System.IO;
using System.Web.UI;
using System.Diagnostics;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;


namespace LibriClassroom.Controllers
{
    public class DashboardController : Controller
    {

        private static DataClasses1DataContext db = new DataClasses1DataContext();
        private string termid = ConfigurationManager.AppSettings["CurrentTerm"];

        static HttpContext Context
        {
            get { return System.Web.HttpContext.Current; }
        }

        //
        // GET: /Dashboard/

        #region Dashboard

        //[Authorize]
        public ActionResult Index()
        {
            //per current term
            ViewBag.termid = termid;

            //regjistro sesionin nese nuk eshte aktiv
            //if (Session["user"] == null)
            //{
            RegisterSession();
            //}

            //define seeu department
            string seeuDepartment = String.Empty;
            switch (Session["dep"].ToString())
            {
                case "E": seeuDepartment = "ELC"; break;
                case "C": seeuDepartment = "N-CST"; break;
                case "T": seeuDepartment = "N-TT"; break;
                case "L": seeuDepartment = "N-LAW"; break;
                case "P": seeuDepartment = "N-PA"; break;
                case "B": seeuDepartment = "N-BA"; break;
                case "Q": seeuDepartment = "LC"; break;
                default: seeuDepartment = "N-CST"; break;
            }

            // List courses
            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            //i merr te gjitha kurset e semestrit aktual
            foreach (Kursi course in listaKurseve)
            {
                //kontrollo departamentin per dekanet
                //nese useri i loguar eshte admin ose rektorati futi krejt kurset e semestrit
                //perndryshe kaperce kurset qe nuk i perkasin departamentit
                if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                {
                    ////per ALL nxjerrja vetem kurset e CST 
                    //if (courseDepartment != 'C')
                    //    continue;
                }
                else
                {
                    //kaperce kurset qe nuk i perkasin departamentit
                    if (course.depid != Session["dep"].ToString())
                        continue;
                }

                //get course level
                int? courseLvl = 0;
                var eventualCourseLvl = db.CourseStats.Where(co => co.courseId.Equals(course.id)).SingleOrDefault();
                if (eventualCourseLvl != null)
                    courseLvl = eventualCourseLvl.courseLevel;
                CourseViewModel c = new CourseViewModel
                {
                    Id = course.id,
                    Title = course.title,
                    Owner = course.ownerid,
                    CourseCode = course.coursecode,
                    Link = course.link,
                    //DescriptionHeading = course.DescriptionHeading,
                    UpdateTime = course.updatetime,
                    CourseLvl = ((courseLvl.HasValue) ? Int32.Parse(courseLvl.ToString()) : 0)
                };

                //merri teachers of the course
                var courseTeachers = db.CourseDelegations
                    .Join(db.GoogleUsers,
                        del => del.username,
                        guser => guser.Username,
                        (del, guser) => new { delegation = del, GUser = guser })
                    .Where(cc => cc.delegation.kursiid == course.id && cc.GUser.Username != "elc").ToList();

                List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

                foreach (var teacher in courseTeachers)
                {
                    LibriTeacher newt = new LibriTeacher
                    {
                        //GoogleId = teacher.delegation.userid,
                        TeacherName = teacher.GUser.Fullname,
                        TeacherEmail = teacher.GUser.Username + "@" + ConfigurationManager.AppSettings["Domain"]
                    };

                    courseTeachersResult.Add(newt);
                }
                c.Teachers = courseTeachersResult;
                listaKurseveResult.Add(c);
            }

            DashboardViewModel result = new DashboardViewModel();
            result.courses = listaKurseveResult;

            //var serviceInstructorsList = new List<LibriTeacher>();
            //var teachersDB = db.GoogleUsers.ToList();
            //foreach (var t in teachersDB)
            //{
            //    if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
            //    {
            //        //per ALL nxjerrja te gjitha profat

            //    }
            //    else
            //    {
            //        //kaperce profat qe nuk i perkasin departamentit
            //        if (t.depid != Session["dep"].ToString())
            //            continue;
            //    }
            //    var instructor = new LibriTeacher()
            //    {
            //        Username = t.Username,
            //        TeacherName = t.Fullname
            //    };
            //    serviceInstructorsList.Add(instructor);
            //}

            //result.instructors = serviceInstructorsList;
            return View(result);
        }

        public ActionResult Instructors_Read([DataSourceRequest]DataSourceRequest request)
        {
            List<GoogleUser> instructorsLs = new List<GoogleUser>();
            var teachersDB = db.GoogleUsers.ToList();
            foreach (var t in teachersDB)
            {
                if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                {
                    //per ALL nxjerrja te gjitha profat

                }
                else
                {
                    //kaperce profat qe nuk i perkasin departamentit
                    if (t.depid != Session["dep"].ToString())
                        continue;
                }
                var instructor = new GoogleUser()
                {
                    Username = t.Username,
                    Fullname = t.Fullname,
                    GoogleID = t.GoogleID,
                    //depid = t.depid
                };
                switch (t.depid)
                {
                    case "E": instructor.depid = "ELC"; break;
                    case "C": instructor.depid = "CST"; break;
                    case "T": instructor.depid = "LCC"; break;
                    case "L": instructor.depid = "LAW"; break;
                    case "P": instructor.depid = "PAPS"; break;
                    case "B": instructor.depid = "BA"; break;
                    case "Q": instructor.depid = "LC"; break;
                    default: instructor.depid = "N/A"; break;
                }
                instructorsLs.Add(instructor);
            }
            DataSourceResult result = instructorsLs.Select(p => new LibriTeacher
            {
                GoogleId = p.GoogleID,
                DeparmentCode = p.depid,
                TeacherName = p.Fullname,
                Username = p.Username
            }).ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Courses

        public PartialViewResult GetCourses(bool table, string termid)
        {
            //string termid = ConfigurationManager.AppSettings["CurrentTerm"];

            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            //fillon filtrimi i dashboardit sipas rolit te userit
            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                foreach (Kursi course in listaKurseve)
                {
                    //kontrollo departamentin per dekanet
                    //nese useri i loguar eshte admin ose rektorati futi krejt kurset e semestrit
                    //perndryshe kaperce kurset qe nuk i perkasin departamentit
                    if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                    {
                        ////per ALL nxjerrja vetem kurset e CST 
                        //if (courseDepartment != 'C')
                        //    continue;
                    }
                    else
                    {
                        //kaperce kurset qe nuk i perkasin departamentit
                        if (course.depid != Session["dep"].ToString())
                            continue;
                    }

                    //get course level
                    int? courseLvl = 0;
                    var eventualCourseLvl = db.CourseStats.Where(co => co.courseId.Equals(course.id)).SingleOrDefault();
                    if (eventualCourseLvl != null)
                        courseLvl = eventualCourseLvl.courseLevel;
                    CourseViewModel c = new CourseViewModel
                    {
                        Id = course.id,
                        Title = course.title,
                        Owner = course.ownerid,
                        CourseCode = course.coursecode,
                        Link = course.link,
                        //DescriptionHeading = course.DescriptionHeading,
                        UpdateTime = course.updatetime,
                        CourseLvl = ((courseLvl.HasValue) ? Int32.Parse(courseLvl.ToString()) : 0)
                    };

                    //merri teachers of the course
                    var courseTeachers = db.CourseDelegations
                    .Join(db.GoogleUsers,
                        del => del.userid,
                        guser => guser.GoogleID,
                        (del, guser) => new { delegation = del, GUser = guser })
                    .Where(cc => cc.delegation.kursiid == course.id).ToList();

                    List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

                    //mbushe kursin me listen e teachers
                    foreach (var teacher in courseTeachers)
                    {
                        LibriTeacher newt = new LibriTeacher
                        {
                            GoogleId = teacher.delegation.userid,
                            TeacherName = teacher.GUser.Fullname,
                            TeacherEmail = teacher.GUser.Username + "@" + ConfigurationManager.AppSettings["Domain"]
                        };

                        courseTeachersResult.Add(newt);
                    }
                    c.Teachers = courseTeachersResult;
                    listaKurseveResult.Add(c);
                }
                if (table)
                {
                    ViewBag.GetText = "Stats";
                    return PartialView("_CoursesTblList", listaKurseveResult);
                }
                else return PartialView("_CoursesShortList", listaKurseveResult);
            }
            return PartialView();
        }

        public PartialViewResult GetDepartmentCourses(string depid, string termid)
        {
            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid && kursi.depid == depid).ToList();

            foreach (Kursi course in listaKurseve)
            {
                //string ownerName = db.GoogleUsers.Where(u => u.GoogleID == course.ownerid).SingleOrDefault().Fullname;
                CourseViewModel c = new CourseViewModel
                {
                    Id = course.id,
                    Title = course.title,
                    Owner = course.ownerid,
                    CourseCode = course.coursecode,
                    Link = course.link,
                    //DescriptionHeading = course.DescriptionHeading,
                    UpdateTime = course.updatetime
                };

                //merri teachers of the course
                var courseTeachers = db.CourseDelegations
                .Join(db.GoogleUsers,
                    del => del.userid,
                    guser => guser.GoogleID,
                    (del, guser) => new { delegation = del, GUser = guser })
                .Where(cc => cc.delegation.kursiid == course.id).ToList();

                List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

                //mbushe kursin me listen e teachers
                foreach (var teacher in courseTeachers)
                {
                    LibriTeacher newt = new LibriTeacher
                    {
                        GoogleId = teacher.delegation.userid,
                        TeacherName = teacher.GUser.Fullname,
                        TeacherEmail = teacher.GUser.Username + "@" + ConfigurationManager.AppSettings["Domain"]
                    };

                    courseTeachersResult.Add(newt);
                }
                c.Teachers = courseTeachersResult;
                listaKurseveResult.Add(c);
            }

            return PartialView("_CoursesTblList", listaKurseveResult);
        }

        //nuk perdoret
        public ActionResult Courses_Read([DataSourceRequest]DataSourceRequest request)
        {

            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            //fillon filtrimi i dashboardit sipas rolit te userit
            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                foreach (Kursi course in listaKurseve)
                {
                    //kontrollo departamentin per dekanet
                    //nese useri i loguar eshte admin ose rektorati futi krejt kurset e semestrit
                    //perndryshe kaperce kurset qe nuk i perkasin departamentit
                    if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                    {
                        ////per ALL nxjerrja vetem kurset e CST 
                        //if (courseDepartment != 'C')
                        //    continue;
                    }
                    else
                    {
                        //kaperce kurset qe nuk i perkasin departamentit
                        if (course.depid != Session["dep"].ToString())
                            continue;
                    }

                    //string ownerName = db.GoogleUsers.Where(u => u.GoogleID == course.ownerid).SingleOrDefault().Fullname;
                    CourseViewModel c = new CourseViewModel
                    {
                        Id = course.id,
                        Title = course.title,
                        Owner = course.ownerid,
                        CourseCode = course.coursecode,
                        Link = course.link,
                        //DescriptionHeading = course.DescriptionHeading,
                        UpdateTime = course.updatetime,
                        termid = course.termid,
                        depid = course.depid
                    };

                    //merri teachers of the course
                    var courseTeachers = db.CourseDelegations
                    .Join(db.GoogleUsers,
                        del => del.userid,
                        guser => guser.GoogleID,
                        (del, guser) => new { delegation = del, GUser = guser })
                    .Where(cc => cc.delegation.kursiid == course.id).ToList();

                    string courseTeachersResult = String.Empty;

                    //mbushe kursin me listen e teachers
                    foreach (var teacher in courseTeachers)
                    {
                        LibriTeacher newt = new LibriTeacher
                        {
                            GoogleId = teacher.delegation.userid,
                            TeacherName = teacher.GUser.Fullname,
                            TeacherEmail = teacher.GUser.Username + "@" + ConfigurationManager.AppSettings["Domain"]
                        };

                        courseTeachersResult += newt.TeacherName + " ";
                        //if(courseTeachers
                    }
                    c.TeachersLs = courseTeachersResult;
                    listaKurseveResult.Add(c);
                }

            }

            DataSourceResult result = listaKurseveResult.Select(p => new CourseViewModel
            {
                Id = p.Id,
                Title = p.Title,
                TeachersLs = p.TeachersLs,
                depid = p.depid,
                termid = p.termid
            }).ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult GetCourseList(string username, string termid)
        {
            if (username == "" || username == null)
                return GetCourses(true, termid);

            //string termid = ConfigurationManager.AppSettings["CurrentTerm"];
            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            //fillon filtrimi i dashboardit sipas rolit te userit
            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                //filtro kurset e semestrit aktual
                foreach (Kursi course in listaKurseve)
                {
                    //kontrollo departamentin per dekanet
                    //nese useri i loguar eshte admin ose rektorati futi krejt kurset e semestrit
                    //perndryshe kaperce kurset qe nuk i perkasin departamentit
                    if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                    {
                        ////per ALL nxjerrja vetem kurset e CST 
                        //if (courseDepartment != 'C')
                        //    continue;
                    }
                    else
                    {
                        //kaperce kurset qe nuk i perkasin departamentit
                        if (course.depid != Session["dep"].ToString())
                            continue;
                    }

                    //merri teachers of the course
                    var courseTeachers = db.CourseDelegations
                    .Join(db.GoogleUsers,
                        del => del.userid,
                        guser => guser.GoogleID,
                        (del, guser) => new { delegation = del, GUser = guser })
                    .Where(cc => cc.delegation.kursiid == course.id).ToList();

                    List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

                    //mbushe kursin me listen e teachers
                    foreach (var teacher in courseTeachers)
                    {
                        if (teacher.GUser.Username.Equals(username))
                        {
                            //string ownerName = db.GoogleUsers.Where(u => u.GoogleID == course.ownerid).SingleOrDefault().Fullname;
                            int? courseLvl = db.CourseStats.Where(co => co.courseId.Equals(course.id)).SingleOrDefault().courseLevel;
                            CourseViewModel c = new CourseViewModel
                            {
                                Id = course.id,
                                Title = course.title,
                                Owner = course.ownerid,
                                CourseCode = course.coursecode,
                                Link = course.link,
                                //DescriptionHeading = course.DescriptionHeading,
                                UpdateTime = course.updatetime,
                                CourseLvl = ((courseLvl.HasValue) ? Int32.Parse(courseLvl.ToString()) : 0)
                            };

                            LibriTeacher newt = new LibriTeacher
                            {
                                GoogleId = teacher.delegation.userid,
                                TeacherName = teacher.GUser.Fullname,
                                TeacherEmail = teacher.GUser.Username + "@" + ConfigurationManager.AppSettings["Domain"]
                            };

                            courseTeachersResult.Add(newt);

                            c.Teachers = courseTeachersResult;
                            listaKurseveResult.Add(c);
                        }
                    }
                }
            }
            ViewBag.GetText = "Feeds";
            return PartialView("_CoursesTblList", listaKurseveResult);
        }

        #endregion

        public PartialViewResult LoadCourseStats(string id)
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            CourseStat listaStats =
                db.CourseStats.Where(s => s.courseId.Equals(id)).SingleOrDefault();

            return PartialView("_Stats", listaStats);
        }

        #region Search Courses

        [HttpPost]
        public JsonResult SearchCourses(string searchterm, string termid)
        {
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];
            var getrequest = clservice.Courses.List();
            getrequest.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];
            ListCoursesResponse result = getrequest.Execute();
            List<CourseViewModel> courses = new List<CourseViewModel>();

            foreach (var course in result.Courses.Where(c => c.DescriptionHeading == termid && c.Name.Contains(searchterm)))
            {
                CourseViewModel c = new CourseViewModel
                {
                    Id = course.Id,
                    Title = course.Name,
                    Owner = course.OwnerId,
                    DescriptionHeading = course.Section,
                    Link = course.AlternateLink,
                    Enrolled = false
                };
                //nese eshte Classroom admin beje enrolled
                if (Session["role"].ToString() == "libriadmin") c.Enrolled = true;

                //merri teachers of the course
                var teachersRq = clservice.Courses.Teachers.List(course.Id);
                ListTeachersResponse teachersLs = teachersRq.Execute();
                List<LibriTeacher> courseTeachers = new List<LibriTeacher>();
                foreach (var teacher in teachersLs.Teachers)
                {
                    LibriTeacher newt = new LibriTeacher
                    {
                        GoogleId = teacher.UserId,
                        TeacherName = teacher.Profile.Name.FullName,
                        TeacherEmail = teacher.Profile.EmailAddress,
                        PhotoUrl = teacher.Profile.PhotoUrl
                    };
                    //kontrollo a eshte useri aktual instruktor i kursit 
                    MailAddress instEmail = new MailAddress(newt.TeacherEmail);
                    string instUsername = instEmail.User;
                    if (instUsername == Session["user"].ToString()) c.Enrolled = true;
                    courseTeachers.Add(newt);
                }
                c.Teachers = courseTeachers;
                courses.Add(c);
            }

            return Json(JsonConvert.SerializeObject(courses), JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult RenderCourses(string jsoncoursels)
        {
            List<CourseViewModel> m = JsonConvert.DeserializeObject<List<CourseViewModel>>(jsoncoursels);
            return PartialView("_CoursesList", m);
        }

        #endregion

        #region Feeds

        public PartialViewResult LoadCourseFeeds(string id)
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            List<FeedViewModel> listaFeeds =
                db.Feeds
                    .Join(db.Kursis,
                        feed => feed.courseId,
                        kursi => kursi.id,
                        (feed, kursi) => new FeedViewModel
                        {
                            alternateLink = feed.alternateLink,
                            id = feed.id,
                            courseId = kursi.id,
                            courseName = kursi.title,
                            courseLink = kursi.link,
                            matSetName = feed.matSetName,
                            title = feed.title,
                            updateTime = (DateTime)feed.updateTime,
                            workType = feed.workType,
                            ResponseUrl = feed.ResponseUrl
                        })
                    .Where(newf => newf.courseId == id).ToList();


            return PartialView("_Feeds", listaFeeds.OrderByDescending(l => l.updateTime));
        }

        public PartialViewResult LoadUserFeeds(string username, string termid)
        {
            List<FeedViewModel> listaFeeds = new List<FeedViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                //per cdo kurs te semestrit aktual kontrollo feed-at e profit
                foreach (Kursi course in listaKurseve)
                {
                    //merri teachers of the course
                    List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

                    var courseTeachers = db.CourseDelegations
                                            .Join(db.GoogleUsers,
                                                del => del.userid,
                                                guser => guser.GoogleID,
                                                (del, guser) => new { delegation = del, GUser = guser })
                                            .Where(cc => cc.delegation.kursiid == course.id).ToList();

                    //per cdo teacher te kursit shif mos ndoshta eshte ndonjeri ai me username si profi per te cilin duhet telistohen feedat
                    //nese po atehere listoi ato feeda
                    foreach (var teacher in courseTeachers)
                    {
                        if (teacher.GUser.Username.Equals(username))
                        {
                            //merri feeds te kursit
                            var feeds = db.Feeds.Where(f => f.courseId.Equals(course.id)).ToList();
                            foreach (var feed in feeds)
                            {
                                FeedViewModel newf = new FeedViewModel()
                                {
                                    alternateLink = feed.alternateLink,
                                    id = feed.id,
                                    courseName = course.title,
                                    courseLink = course.link,
                                    matSetName = feed.matSetName,
                                    title = feed.title,
                                    updateTime = (DateTime)feed.updateTime,
                                    workType = feed.workType,
                                    ResponseUrl = feed.ResponseUrl
                                };
                                listaFeeds.Add(newf);
                            }
                        }
                    }
                } //end of courses cycle
                return PartialView("_Feeds", listaFeeds.OrderByDescending(l => l.updateTime));
            }

            return PartialView("_Feeds", listaFeeds.OrderByDescending(l => l.updateTime));
        }

        public PartialViewResult LoadDepartmentFeeds(string depid, string termid)
        {
            List<FeedViewModel> listaFeeds = new List<FeedViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid && kursi.depid == depid).Take(30).ToList();

            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                //per cdo kurs te semestrit aktual kontrollo feed-at e profit
                foreach (Kursi course in listaKurseve)
                {
                    //merri teachers of the course
                    List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

                    var courseTeachers = db.CourseDelegations
                                            .Join(db.GoogleUsers,
                                                del => del.userid,
                                                guser => guser.GoogleID,
                                                (del, guser) => new { delegation = del, GUser = guser })
                                            .Where(cc => cc.delegation.kursiid == course.id).ToList();

                    //per cdo teacher te kursit shif mos ndoshta eshte ndonjeri ai me username si profi per te cilin duhet telistohen feedat
                    //nese po atehere listoi ato feeda
                    foreach (var teacher in courseTeachers)
                    {
                        //merri feeds te kursit
                        var feeds = db.Feeds.Where(f => f.courseId.Equals(course.id)).ToList();
                        foreach (var feed in feeds)
                        {
                            FeedViewModel newf = new FeedViewModel()
                            {
                                alternateLink = feed.alternateLink,
                                id = feed.id,
                                courseName = course.title,
                                courseLink = course.link,
                                matSetName = feed.matSetName,
                                title = feed.title,
                                updateTime = (DateTime)feed.updateTime,
                                workType = feed.workType,
                                ResponseUrl = feed.ResponseUrl
                            };
                            listaFeeds.Add(newf);
                        }
                    }
                } //end of courses cycle
                return PartialView("_Feeds", listaFeeds.OrderByDescending(l => l.updateTime));
            }

            return PartialView("_Feeds", listaFeeds.OrderByDescending(l => l.updateTime));
        }

        public PartialViewResult LoadTermFeeds(string termid)
        {
            List<FeedViewModel> listaFeeds = new List<FeedViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).Take(30).ToList();

            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                //per cdo kurs te semestrit aktual kontrollo feed-at e profit
                foreach (Kursi course in listaKurseve)
                {
                    //kontrollo departamentin per dekanet
                    //nese useri i loguar eshte admin ose rektorati futi krejt kurset e semestrit
                    //perndryshe kaperce kurset qe nuk i perkasin departamentit
                    if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                    {
                    }
                    else
                    {
                        //kaperce kurset qe nuk i perkasin departamentit
                        if (course.depid != Session["dep"].ToString())
                            continue;
                    }
                    //merri teachers of the course
                    List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

                    var courseTeachers = db.CourseDelegations
                                            .Join(db.GoogleUsers,
                                                del => del.userid,
                                                guser => guser.GoogleID,
                                                (del, guser) => new { delegation = del, GUser = guser })
                                            .Where(cc => cc.delegation.kursiid == course.id).ToList();

                    //per cdo teacher te kursit shif mos ndoshta eshte ndonjeri ai me username si profi per te cilin duhet telistohen feedat
                    //nese po atehere listoi ato feeda
                    foreach (var teacher in courseTeachers)
                    {
                        //merri feeds te kursit
                        var feeds = db.Feeds.Where(f => f.courseId.Equals(course.id)).ToList();
                        foreach (var feed in feeds)
                        {
                            FeedViewModel newf = new FeedViewModel()
                            {
                                alternateLink = feed.alternateLink,
                                id = feed.id,
                                courseName = course.title,
                                courseLink = course.link,
                                matSetName = feed.matSetName,
                                title = feed.title,
                                updateTime = (DateTime)feed.updateTime,
                                workType = feed.workType,
                                ResponseUrl = feed.ResponseUrl
                            };
                            listaFeeds.Add(newf);
                        }
                    }
                } //end of courses cycle
                return PartialView("_Feeds", listaFeeds.OrderByDescending(l => l.updateTime));
            }

            return PartialView("_Feeds", listaFeeds.OrderByDescending(l => l.updateTime));
        }

        #endregion

        #region ReportingAnalysis

        //[Authorize]
        public ActionResult ReportingAnalysis(string tid)
        {
            //per current term
            //string seeutermid = ConfigurationManager.AppSettings["SEEUCurrentTerm"];
            if (tid != null && tid != string.Empty)
            {
                ViewBag.termid = tid;
                termid = tid;
            }
            else
                ViewBag.termid = termid;
            string msg;
            if (DateTime.Now.Hour < 12)
            {
                msg = "Good morning, ";
            }
            else if (DateTime.Now.Hour < 17)
            {
                msg = "Good afternoon, ";
            }
            else
            {
                msg = "Good evening, ";
            }
            msg += User.Identity.Name + "!";
            ViewBag.message = msg;

            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            string tipiRaportit;
            if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                tipiRaportit = "A";
            else
                tipiRaportit = Session["dep"].ToString();

            var report =
                (from months in db.KursiMeMateriales
                 join rep in db.Reportings on months.reportingid equals rep.id
                 where rep.department == tipiRaportit && rep.termid == termid
                 orderby rep.timeStamp descending
                 select new { nrKurseveMeMaterialePerMuaj = months, Reporting = rep }).FirstOrDefault();

            if (report != null)
            {
                KursiMeMateriale nrKurseveMeMaterialePerMuaj = report.nrKurseveMeMaterialePerMuaj;
                Reporting raporti = report.Reporting;

                ReportingViewModel model = new ReportingViewModel
                {
                    nrKursetMeMaterialePerMuaj = nrKurseveMeMaterialePerMuaj,
                    nrLendeMeSyllPaMat = Convert.ToInt32(raporti.nrLendeMeSyllPaMat),
                    perqindjaKurseveMeMateriale = Convert.ToInt32(raporti.perqindjaKurseveMeMateriale),
                    totalDetyra = Convert.ToInt32(raporti.totalDetyra),
                    totalDetyraJavaFundit = Convert.ToInt32(raporti.totalDetyraJavaFundit),
                    totalLende = Convert.ToInt32(raporti.totalLende),
                    totalLendePaMateriale = Convert.ToInt32(raporti.totalLendePaMateriale),
                    totalMateriale = Convert.ToInt32(raporti.totalMateriale),
                    totalMaterialeJavaFundit = Convert.ToInt32(raporti.totalMaterialeJavaFundit),
                };

                //per current term
                if (tid != null && tid != string.Empty)
                {
                    model.termid = tid;
                }
                else
                    model.termid = termid;

                //var terms = GetSeeuTerms();
                //model.Terms = terms.Select(x => new SelectListItem { Text = x.Description, Value = x.TermCode.ToString(), Selected = x.TermCode.Equals(Int32.Parse(ConfigurationManager.AppSettings["SEEUCurrentTerm"])) }).ToList();
                //ViewBag.TermCode = new SelectList(terms, "TermCode", "Description", Int32.Parse(ConfigurationManager.AppSettings["SEEUCurrentTerm"])); 

                //calculate course levels %
                var courseLvlList =
                    (from levels in db.CourseStats
                     join kurset in db.Kursis on levels.courseId equals kurset.id
                     where kurset.termid == termid
                     select new
                     {
                         department = kurset.depid,
                         level = levels.courseLevel,
                         hasSyllabus = levels.hasSyllabus,
                         hasResources = levels.hasResources,
                         hasAssignments = levels.hasAssignments
                     }).ToList();
                model.Courselevels = new List<CourseLevel>();
                if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                {
                    CourseLevel cst = new CourseLevel(); CourseLevel ba = new CourseLevel(); CourseLevel law = new CourseLevel();
                    CourseLevel paps = new CourseLevel(); CourseLevel tt = new CourseLevel(); CourseLevel elc = new CourseLevel();
                    CourseLevel lc = new CourseLevel();
                    foreach (var clvl in courseLvlList)
                    {
                        switch (clvl.department)
                        {
                            case "E":
                                elc.department = "ELC"; elc.nrDepCourses += 1;
                                if (clvl.hasSyllabus == true)
                                {
                                    elc.HasSyllabus += 1;
                                    switch (clvl.level)
                                    {
                                        case 1:
                                            elc.Level1 += 1;
                                            break;
                                        case 21:
                                            elc.Level21 += 1;
                                            break;
                                        case 22:
                                            elc.Level22 += 1;
                                            break;
                                        case 23:
                                            elc.Level23 += 1;
                                            break;
                                    }
                                }
                                else if (clvl.hasSyllabus == false && clvl.hasResources == false && clvl.hasAssignments == false)
                                    elc.Level0 += 1;
                                break;
                            case "C":
                                cst.department = "CST"; cst.nrDepCourses += 1;
                                if (clvl.hasSyllabus == true)
                                {
                                    cst.HasSyllabus += 1;
                                    switch (clvl.level)
                                    {
                                        case 1:
                                            cst.Level1 += 1;
                                            break;
                                        case 21:
                                            cst.Level21 += 1;
                                            break;
                                        case 22:
                                            cst.Level22 += 1;
                                            break;
                                        case 23:
                                            cst.Level23 += 1;
                                            break;
                                    }
                                }
                                else if (clvl.hasSyllabus == false && clvl.hasResources == false && clvl.hasAssignments == false)
                                    cst.Level0 += 1;
                                break;
                            case "T":
                                tt.department = "LCC"; tt.nrDepCourses += 1;
                                if (clvl.hasSyllabus == true)
                                {
                                    tt.HasSyllabus += 1;
                                    switch (clvl.level)
                                    {
                                        case 1:
                                            tt.Level1 += 1;
                                            break;
                                        case 21:
                                            tt.Level21 += 1;
                                            break;
                                        case 22:
                                            tt.Level22 += 1;
                                            break;
                                        case 23:
                                            tt.Level23 += 1;
                                            break;
                                    }
                                }
                                else if (clvl.hasSyllabus == false && clvl.hasResources == false && clvl.hasAssignments == false)
                                    tt.Level0 += 1;
                                break;
                            case "L":
                                law.department = "LAW"; law.nrDepCourses += 1;
                                if (clvl.hasSyllabus == true)
                                {
                                    law.HasSyllabus += 1;
                                    switch (clvl.level)
                                    {
                                        case 1:
                                            law.Level1 += 1;
                                            break;
                                        case 21:
                                            law.Level21 += 1;
                                            break;
                                        case 22:
                                            law.Level22 += 1;
                                            break;
                                        case 23:
                                            law.Level23 += 1;
                                            break;
                                    }
                                }
                                else if (clvl.hasSyllabus == false && clvl.hasResources == false && clvl.hasAssignments == false)
                                    law.Level0 += 1;
                                break;
                            case "P":
                                paps.department = "PAPS"; paps.nrDepCourses += 1;
                                if (clvl.hasSyllabus == true)
                                {
                                    paps.HasSyllabus += 1;
                                    switch (clvl.level)
                                    {
                                        case 1:
                                            paps.Level1 += 1;
                                            break;
                                        case 21:
                                            paps.Level21 += 1;
                                            break;
                                        case 22:
                                            paps.Level22 += 1;
                                            break;
                                        case 23:
                                            paps.Level23 += 1;
                                            break;
                                    }
                                }
                                else if (clvl.hasSyllabus == false && clvl.hasResources == false && clvl.hasAssignments == false)
                                    paps.Level0 += 1;
                                break;
                            case "B":
                                ba.department = "BA"; ba.nrDepCourses += 1;
                                if (clvl.hasSyllabus == true)
                                {
                                    ba.HasSyllabus += 1;
                                    switch (clvl.level)
                                    {
                                        case 1:
                                            ba.Level1 += 1;
                                            break;
                                        case 21:
                                            ba.Level21 += 1;
                                            break;
                                        case 22:
                                            ba.Level22 += 1;
                                            break;
                                        case 23:
                                            ba.Level23 += 1;
                                            break;
                                    }
                                }
                                else if (clvl.hasSyllabus == false && clvl.hasResources == false && clvl.hasAssignments == false)
                                    ba.Level0 += 1;
                                break;
                            case "Q":
                                lc.department = "LC"; lc.nrDepCourses += 1;
                                if (clvl.hasSyllabus == true)
                                {
                                    lc.HasSyllabus += 1;
                                    switch (clvl.level)
                                    {
                                        case 1:
                                            lc.Level1 += 1;
                                            break;
                                        case 21:
                                            lc.Level21 += 1;
                                            break;
                                        case 22:
                                            lc.Level22 += 1;
                                            break;
                                        case 23:
                                            lc.Level23 += 1;
                                            break;
                                    }
                                }
                                else if (clvl.hasSyllabus == false && clvl.hasResources == false && clvl.hasAssignments == false)
                                    lc.Level0 += 1;
                                break;
                        }
                    }
                    //convert into %
                    cst.Level23 = cst.Level23 * 100 / cst.nrDepCourses;
                    cst.Level22 = cst.Level22 * 100 / cst.nrDepCourses;
                    cst.Level21 = cst.Level21 * 100 / cst.nrDepCourses;
                    cst.Level1 = cst.Level1 * 100 / cst.nrDepCourses;
                    cst.Level0 = cst.Level0 * 100 / cst.nrDepCourses;
                    cst.HasSyllabus = cst.HasSyllabus * 100 / cst.nrDepCourses;
                    model.Courselevels.Add(cst);

                    paps.Level23 = paps.Level23 * 100 / paps.nrDepCourses;
                    paps.Level22 = paps.Level22 * 100 / paps.nrDepCourses;
                    paps.Level21 = paps.Level21 * 100 / paps.nrDepCourses;
                    paps.Level1 = paps.Level1 * 100 / paps.nrDepCourses;
                    paps.HasSyllabus = paps.HasSyllabus * 100 / paps.nrDepCourses;
                    paps.Level0 = paps.Level0 * 100 / paps.nrDepCourses;
                    model.Courselevels.Add(paps);

                    ba.Level23 = ba.Level23 * 100 / ba.nrDepCourses;
                    ba.Level22 = ba.Level22 * 100 / ba.nrDepCourses;
                    ba.Level21 = ba.Level21 * 100 / ba.nrDepCourses;
                    ba.Level1 = ba.Level1 * 100 / ba.nrDepCourses;
                    ba.HasSyllabus = ba.HasSyllabus * 100 / ba.nrDepCourses;
                    ba.Level0 = ba.Level0 * 100 / ba.nrDepCourses;
                    model.Courselevels.Add(ba);

                    lc.Level23 = lc.Level23 * 100 / lc.nrDepCourses;
                    lc.Level22 = lc.Level22 * 100 / lc.nrDepCourses;
                    lc.Level21 = lc.Level21 * 100 / lc.nrDepCourses;
                    lc.Level1 = lc.Level1 * 100 / lc.nrDepCourses;
                    lc.HasSyllabus = lc.HasSyllabus * 100 / lc.nrDepCourses;
                    lc.Level0 = lc.Level0 * 100 / lc.nrDepCourses;
                    model.Courselevels.Add(lc);

                    tt.Level23 = tt.Level23 * 100 / tt.nrDepCourses;
                    tt.Level22 = tt.Level22 * 100 / tt.nrDepCourses;
                    tt.Level21 = tt.Level21 * 100 / tt.nrDepCourses;
                    tt.Level1 = tt.Level1 * 100 / tt.nrDepCourses;
                    tt.HasSyllabus = tt.HasSyllabus * 100 / tt.nrDepCourses;
                    tt.Level0 = tt.Level0 * 100 / tt.nrDepCourses;
                    model.Courselevels.Add(tt);

                    law.Level23 = law.Level23 * 100 / law.nrDepCourses;
                    law.Level22 = law.Level22 * 100 / law.nrDepCourses;
                    law.Level21 = law.Level21 * 100 / law.nrDepCourses;
                    law.Level1 = law.Level1 * 100 / law.nrDepCourses;
                    law.HasSyllabus = law.HasSyllabus * 100 / law.nrDepCourses;
                    law.Level0 = law.Level0 * 100 / law.nrDepCourses;
                    model.Courselevels.Add(law);

                    elc.Level23 = elc.Level23 * 100 / elc.nrDepCourses;
                    elc.Level22 = elc.Level22 * 100 / elc.nrDepCourses;
                    elc.Level21 = elc.Level21 * 100 / elc.nrDepCourses;
                    elc.Level1 = elc.Level1 * 100 / elc.nrDepCourses;
                    elc.HasSyllabus = elc.HasSyllabus * 100 / elc.nrDepCourses;
                    elc.Level0 = elc.Level0 * 100 / elc.nrDepCourses;
                    model.Courselevels.Add(elc);
                }
                else
                {
                    CourseLevel singleDep = new CourseLevel();
                    singleDep.department = Session["dep"].ToString();
                    foreach (var clvl in courseLvlList)
                    {
                        if (clvl.department == Session["dep"].ToString())
                        {
                            singleDep.nrDepCourses += 1;
                            if (clvl.hasSyllabus == true)
                            {
                                singleDep.HasSyllabus += 1;
                                switch (clvl.level)
                                {
                                    case 1:
                                        singleDep.Level1 += 1;
                                        break;
                                    case 21:
                                        singleDep.Level21 += 1;
                                        break;
                                    case 22:
                                        singleDep.Level22 += 1;
                                        break;
                                    case 23:
                                        singleDep.Level23 += 1;
                                        break;
                                }
                            }
                            else if (clvl.hasSyllabus == false && clvl.hasResources == false && clvl.hasAssignments == false)
                                singleDep.Level0 += 1;
                        }
                    }
                    singleDep.Level21 = singleDep.Level21 * 100 / singleDep.nrDepCourses;
                    singleDep.Level1 = singleDep.Level1 * 100 / singleDep.nrDepCourses;
                    singleDep.HasSyllabus = singleDep.HasSyllabus * 100 / singleDep.nrDepCourses;
                    singleDep.Level0 = singleDep.Level0 * 100 / singleDep.nrDepCourses;
                    model.Courselevels.Add(singleDep);
                }
                return View(model);
            }

            return View();
        }

        public ActionResult PublishedResourcesStats_Read([DataSourceRequest]DataSourceRequest request)
        {
            List<Reporting> listRepDepartments = new List<Reporting>();
            if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
            {
                //get reports individually
                Reporting cst = db.Reportings.Where(r => r.department.Equals("C")).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                cst.department = "CST";
                listRepDepartments.Add(cst);
                Reporting ba = db.Reportings.Where(r => r.department.Equals("B")).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                ba.department = "BA";
                listRepDepartments.Add(ba);
                Reporting paps = db.Reportings.Where(r => r.department.Equals("P")).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                paps.department = "PAPS";
                listRepDepartments.Add(paps);
                Reporting law = db.Reportings.Where(r => r.department.Equals("L")).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                law.department = "LAW";
                listRepDepartments.Add(law);
                Reporting elc = db.Reportings.Where(r => r.department.Equals("E")).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                elc.department = "ELC";
                listRepDepartments.Add(elc);
                Reporting tt = db.Reportings.Where(r => r.department.Equals("T")).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                tt.department = "LCC";
                listRepDepartments.Add(tt);
                Reporting q = db.Reportings.Where(r => r.department.Equals("Q")).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                q.department = "LC";
                listRepDepartments.Add(q);
                Reporting all = db.Reportings.Where(r => r.department.Equals("A")).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                all.department = "ALL";
                listRepDepartments.Add(all);
            }
            else
            {
                Reporting customDep = db.Reportings.Where(r => r.department.Equals(Session["dep"].ToString())).OrderByDescending(r => r.timeStamp).FirstOrDefault();
                switch (Session["dep"].ToString())
                {
                    case "E": customDep.department = "ELC"; break;
                    case "C": customDep.department = "CST"; break;
                    case "T": customDep.department = "TT"; break;
                    case "L": customDep.department = "LAW"; break;
                    case "P": customDep.department = "PAPS"; break;
                    case "B": customDep.department = "BA"; break;
                    case "Q": customDep.department = "LC"; break;
                    default: customDep.department = "N/A"; break;
                }
                listRepDepartments.Add(customDep);
            }

            DataSourceResult result = listRepDepartments.Select(p => new Reporting
            {
                department = p.department,
                perqindjaKurseveMeMateriale = p.perqindjaKurseveMeMateriale,
                totalLendePaMateriale = p.totalLendePaMateriale,
                totalLende = p.totalLende,
                totalDetyra = p.totalDetyra,
                totalMateriale = p.totalMateriale
            }).ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult levelDetails(int levelId, string department)
        {
            string depid = string.Empty;
            switch (department)
            {
                case "ELC": depid = "E"; break;
                case "CST": depid = "C"; break;
                case "LCC": depid = "T"; break;
                case "LAW": depid = "L"; break;
                case "PAPS": depid = "P"; break;
                case "BA": depid = "B"; break;
                case "LC": depid = "Q"; break;
                //default: depid = "N/A"; break;
            }

            //Level 2, 1, 0
            if (levelId != 3)
            {
                List<CourseInstructorVM> courseLvlLs = (from s in db.CourseStats
                                                        join c in db.Kursis on s.courseId equals c.id
                                                        join i in db.GoogleUsers on c.ownerid equals i.GoogleID
                                                        where s.courseLevel == levelId && c.depid == depid && c.termid == termid
                                                        select new CourseInstructorVM
                                                        {
                                                            kursi = c.title,
                                                            instruktori = i.Fullname
                                                        }).ToList();

                return PartialView(courseLvlLs);
            }//has syllabus
            else
            {
                List<CourseInstructorVM> courseLvlLs = (from s in db.CourseStats
                                                        join c in db.Kursis on s.courseId equals c.id
                                                        join i in db.GoogleUsers on c.ownerid equals i.GoogleID
                                                        where s.hasSyllabus == true && c.depid == depid && c.termid == termid
                                                        select new CourseInstructorVM
                                                        {
                                                            kursi = c.title,
                                                            instruktori = i.Fullname
                                                        }).ToList();

                return PartialView(courseLvlLs);
            }
        }

        public PartialViewResult CoursesWithSyllabus(bool mepa, string termid)
        {
            ViewBag.with = mepa;

            SyllabusViewModel listaKurseve = new SyllabusViewModel();
            listaKurseve.withsyl = mepa; listaKurseve.courses = new List<CourseSyllabus>();

            //get and filter term courses by department
            List<Kursi> allTermCourses = new List<Kursi>();
            if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                allTermCourses = db.Kursis.Where(k => k.termid.Equals(termid)).ToList();
            else
                allTermCourses = db.Kursis.Where(k => k.termid.Equals(termid) && k.depid.Equals(Session["dep"].ToString())).ToList();

            foreach (Kursi k in allTermCourses)
            {
                var kursStats = db.CourseStats.Where(g => g.courseId.Equals(k.id)).SingleOrDefault();
                List<LibriTeacher> courseInstr = GetCourseInstructors(k.id);
                string instructors = String.Empty;
                int teachnr = courseInstr.Count(); int i = 1;
                foreach (var instr in courseInstr)
                {
                    instructors += instr.TeacherName;
                    if (i < teachnr)
                    {
                        instructors += ", ";
                    }
                    i++;

                }
                if (kursStats != null)
                {
                    CourseSyllabus syll = new CourseSyllabus
                    {
                        courseId = k.id,
                        courseName = k.title,
                        instructors = instructors,
                        hasSyllabus = kursStats.hasSyllabus
                    };
                    if (syll.hasSyllabus == mepa)
                        listaKurseve.courses.Add(syll);
                }
            }

            return PartialView("_Syllabus", listaKurseve);
        }

        

        #endregion

        //nuk eshte ne rregull
        #region GetTermCourses

        public PartialViewResult GetTermCourses(string termid)
        {
            List<CourseViewModel> courses = new List<CourseViewModel>();
            if (Session["role"].ToString() == "teacher" || Session["role"].ToString() == "libriadmin")
            {
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];
                var getrequest = clservice.Courses.List();
                getrequest.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];
                ListCoursesResponse coursesLs = getrequest.Execute();
                List<CourseViewModel> listaKurseve = new List<CourseViewModel>();

                if (coursesLs.Courses != null && coursesLs.Courses.Count > 0)
                {   //filtro kurset e semestrit aktual
                    foreach (var course in coursesLs.Courses.Where(cs => cs.DescriptionHeading == termid))
                    {
                        CourseViewModel c = new CourseViewModel
                        {
                            Id = course.Id,
                            Title = course.Name,
                            Owner = course.OwnerId,
                            DescriptionHeading = course.Section,
                            Link = course.AlternateLink,
                            Enrolled = false
                        };
                        //nese eshte Classroom admin beje enrolled
                        if (Session["role"].ToString() == "libriadmin") c.Enrolled = true;

                        //merri teachers of the course
                        var teachersRq = clservice.Courses.Teachers.List(course.Id);
                        ListTeachersResponse teachersLs = teachersRq.Execute();
                        List<LibriTeacher> courseTeachers = new List<LibriTeacher>();
                        foreach (var teacher in teachersLs.Teachers)
                        {
                            LibriTeacher newt = new LibriTeacher
                            {
                                GoogleId = teacher.UserId,
                                TeacherName = teacher.Profile.Name.FullName,
                                TeacherEmail = teacher.Profile.EmailAddress,
                                PhotoUrl = teacher.Profile.PhotoUrl
                            };
                            //kontrollo a eshte useri aktual instruktor i kursit 
                            MailAddress instEmail = new MailAddress(newt.TeacherEmail);
                            string instUsername = instEmail.User;
                            if (instUsername == Session["user"].ToString()) c.Enrolled = true;
                            courseTeachers.Add(newt);
                        }
                        c.Teachers = courseTeachers;
                        listaKurseve.Add(c);
                    }
                    return PartialView("_CoursesList", listaKurseve);
                }
            }
            return PartialView();
        }

        #endregion

        #region Helpers

        public List<LibriTeacher> GetCourseInstructors(string courseId)
        {
            //merri teachers of the course
            var courseTeachers = db.CourseDelegations
                .Join(db.GoogleUsers,
                    del => del.userid,
                    guser => guser.GoogleID,
                    (del, guser) => new { delegation = del, GUser = guser })
                .Where(cc => cc.delegation.kursiid == courseId).ToList();

            List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

            foreach (var teacher in courseTeachers)
            {
                LibriTeacher newt = new LibriTeacher
                {
                    GoogleId = teacher.delegation.userid,
                    TeacherName = teacher.GUser.Fullname,
                    TeacherEmail = teacher.GUser.Username + "@" + ConfigurationManager.AppSettings["Domain"]
                };

                courseTeachersResult.Add(newt);
            }
            return courseTeachersResult;
        }

        //te perpunohet
        public ActionResult Autocomplete()
        {
            if (Session["user"] == null) RegisterSession();
            else
            {
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];
                var getrequest = clservice.Courses.List();
                getrequest.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];
                ListCoursesResponse courses = getrequest.Execute();
                List<string> lscourseNames = new List<string>();
                if (courses != null)
                {
                    foreach (var course in courses.Courses)
                        lscourseNames.Add(course.Name);
                }
                return Json(lscourseNames, JsonRequestBehavior.AllowGet);
            }
            return View();
        }

        
        public string GetCourseTerm(string courseCode)
        {
            if (courseCode != null)
            {
                if (3 >= courseCode.Length) return courseCode;
                return courseCode.Substring(1, 2);
            }
            return null;
        }

        public string GetCourseDepID(string courseCode)
        {
            if (courseCode != null)
            {
                if (3 >= courseCode.Length) return courseCode;
                return courseCode.Substring(0, 1);
            }
            return null;
        }

        #region GetUsernameFromSession

        public string GetUsernameFromSession()
        {
            string username = String.Empty;
            ClaimsPrincipal claimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            if (claimsPrincipal != null)
            {
                foreach (Claim claim in claimsPrincipal.Claims)
                {
                    switch (claim.Type)
                    {
                        case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname":
                            username = claim.Value;
                            break;
                    }
                }
            }

            return username;
        }

        #endregion

        #region GetEmailFromSession

        public string GetEmailFromSession()
        {
            string email = String.Empty;
            ClaimsPrincipal claimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            if (claimsPrincipal != null)
            {
                foreach (Claim claim in claimsPrincipal.Claims)
                {
                    switch (claim.Type)
                    {
                        case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress":
                            email = claim.Value;
                            break;
                    }
                }
            }
            return email;
        }

        #endregion

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

            //        //regjgistro rolin nese eshte priv.user
            //        var privUser = db.LibriDepDeans.Where(u => u.username == username).SingleOrDefault();
            //        Session["dep"] = privUser.code;     //E, C, T, L, P, B, Q, R & A, while O-none
            //        if (privUser.code == "A")
            //            Session["role"] = "libriadmin";Session["dep"] = "A";
            //        else if (privUser.code == "R")
            //            Session["role"] = "rectorate";
            //        else Session["role"] = "authorized";     //authorized -> high mngmnt and deans/directors 
            //        //libriadmin -> admins

            //        //Faculty of Contemporary Sciences and Technologies - N-CST -> C
            //        //Faculty of Languages, Cultures and Communication - N-TT -> T
            //        //Faculty of Law - N-LAW -> L
            //        //Faculty of Public Administration and Political Sciences - N-PA -> P
            //        //Faculty of Business and Economics - N-BA -> B
            //        //Language Centre - LC -> Q
            //        //E-Learning Centre - ELC -> E
            //        //Administrators -> A
            //        //Rectorate -> R
            //        //}
            //    }
            //}
            Session["role"] = "libriadmin";Session["dep"] = "A";
        }

        #endregion

        #region Register Google Services

        private void RegisterClassroomSvc()
        {
            //autorizim per Google token
            ClassroomService clservice;
            if (Session["clservice"] == null)
            {
                //var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                //    AuthorizeAsync(cancellationToken);

                String serviceAccountEmail = ConfigurationManager.AppSettings["AdminServiceAccount"];//serviceAccount
                String userAccountEmail = ConfigurationManager.AppSettings["AdminUserAccount"];//userAccount

                var certificate = new X509Certificate2(Server.MapPath(@"~/resources/unt-gc-admin.p12"), "notasecret", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);//elckey

                ServiceAccountCredential credential = new ServiceAccountCredential(
                   new ServiceAccountCredential.Initializer(serviceAccountEmail)
                   {
                       User = userAccountEmail,
                       Scopes = new[] { 
                            DriveService.Scope.Drive, 
                            ClassroomService.Scope.ClassroomCourses,
                            ClassroomService.Scope.ClassroomCourseworkMe,
                            ClassroomService.Scope.ClassroomCourseworkStudents,
                            ClassroomService.Scope.ClassroomProfileEmails,
                            ClassroomService.Scope.ClassroomProfilePhotos,
                            ClassroomService.Scope.ClassroomRosters
                       }
                   }.FromCertificate(certificate));

                //if (result.Credential != null)
                //{
                // Create Classroom API service.
                clservice = new ClassroomService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Libri Classroom"
                });

                //regjistro sesionin per aksione ne Classroom
                Session["clservice"] = clservice;
                //}
                //else
                //{
                //    return new RedirectResult(result.RedirectUri);
                //}

            }
            else
            {
                clservice = (ClassroomService)Session["clservice"];
            }
        }

        #endregion

        #region GetUserId

        public string GetUsersGoogleId(string username)
        {
            string id = string.Empty;
            //merri nga DB
            var user = db.GoogleUsers.Where(u => u.Username.Equals(username)).SingleOrDefault();
            if (user != null)
                id = user.GoogleID;
            return id;
        }

        #endregion

        #endregion
    }
}
