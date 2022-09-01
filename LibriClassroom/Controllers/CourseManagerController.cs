using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using LibriClassroom.Models;
using LibriClassroom.Models.Entities;
using LibriClassroom.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Data;
using Newtonsoft.Json;
using System.Text;
using Google.Apis.Drive.v2;

namespace LibriClassroom.Controllers
{
    public class CourseManagerController : Controller
    {

        private static DataClasses1DataContext db = new DataClasses1DataContext();
        private string termid = ConfigurationManager.AppSettings["CurrentTerm"];

        //
        // GET: /CourseManager/

        ////[Authorize]
        //-----
        public ActionResult Index(CancellationToken cancellationToken)
        {
            RegisterSession();
            if (Session["role"].ToString() == "libriadmin")
            {
                //per current term
                //string seeutermid = ConfigurationManager.AppSettings["SEEUCurrentTerm"];
                //ViewBag.termid = seeutermid;

                //prej ne service
                //var seeuService = GetSeeuService();
                //var termsJsonString = seeuService.GetTerms();
                //Term termiaktual = new Term { TermCode = 1169, Description = "FALL- 16/17", AcademicYear = "16/17" };
                //var semesters = seeuService.GetSemesters("1169");
                //DataTable coursesDataTbl = (DataTable)JsonConvert.DeserializeObject(coursesJsonString, (typeof(DataTable)));

                string termid = ConfigurationManager.AppSettings["CurrentTerm"];
                ViewBag.termid = termid;
                //regjistro sesionin nese nuk eshte aktiv
                if (Session["user"] == null) RegisterSession();

                // List courses
                List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
                List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid || kursi.title == "Tutoring").ToList();

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
                            del => del.username,
                            guser => guser.Username,
                            (del, guser) => new { delegation = del, GUser = guser })
                        .Where(cc => cc.delegation.kursiid == course.id).ToList();

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
                return View(listaKurseveResult);
            }
            else return RedirectToAction("Index", "Dashboard");
        }

        // GET: /CourseManager/Details/5
        //[Authorize]
        public ActionResult Details(string id)
        {
            if (Session["user"] == null) RegisterSession();
            else
            {
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];
                var getrequest = clservice.Courses.Get(id);
                Course course = getrequest.Execute();
                if (course != null)
                {
                    var c = new LibriCourse
                    {
                        CourseCode = course.Section,
                        Term = course.Section.Substring(course.Section.Length - 3),
                        OwnerId = course.OwnerId,
                        Title = course.Name,
                        CreationTime = DateTime.Parse(course.UpdateTime),
                        Id = course.Id,
                        Link = course.AlternateLink,
                        EnrollmentCode = course.EnrollmentCode
                    };

                    //get teachers
                    var teachersRq = clservice.Courses.Teachers.List(course.Id);
                    ListTeachersResponse teachersLs = teachersRq.Execute();
                    List<LibriTeacher> courseTeachers = new List<LibriTeacher>();
                    if (teachersLs != null)
                        foreach (var teacher in teachersLs.Teachers)
                        {
                            string username = String.Empty;
                            if (teacher.Profile.EmailAddress != null)
                                username = new MailAddress(teacher.Profile.EmailAddress).User;
                            LibriTeacher newt = new LibriTeacher
                            {
                                GoogleId = teacher.UserId,
                                TeacherName = teacher.Profile.Name.FullName,
                                TeacherEmail = teacher.Profile.EmailAddress,
                                Username = username
                            };
                            courseTeachers.Add(newt);
                        }
                    c.Teachers = courseTeachers;

                    //get students
                    var studentsRq = clservice.Courses.Students.List(course.Id);
                    ListStudentsResponse studentsLs = studentsRq.Execute();
                    List<LibriStudent> courseStudents = new List<LibriStudent>();
                    if (courseStudents.Count > 0)
                    {
                        foreach (var student in studentsLs.Students)
                        {
                            string username = new MailAddress(student.Profile.EmailAddress).User;
                            LibriStudent news = new LibriStudent
                            {
                                UserId = student.UserId,
                                StudentName = student.Profile.Name.FullName,
                                StudentEmail = student.Profile.EmailAddress,
                                Username = username
                            };
                            courseStudents.Add(news);
                        }
                        c.Students = courseStudents;
                    }
                    return View(c);
                }
            }
            return RedirectToAction("Index");
        }

        #region CRUD courses

        //
        // GET: /CourseManager/Create
        //[Authorize]
        public ActionResult Create()
        {
            //regjistro sesionin nga SSO nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //ViewBag.semestrat = new SelectList(_courseService.GetAllTerms(), "Id", "Description", Int32.Parse(ConfigurationManager.AppSettings["CurrentTerm"]));
            //ViewBag.semestrat = new SelectList(termsLocal, "Value", "Text", Int32.Parse(ConfigurationManager.AppSettings["CurrentTerm"]));

            //build instructors list
            //ViewBag.msuset = new SelectList(_authService.GetInstructors(), "Id", "Username", String.Empty);
            //ViewBag.msuset = new SelectList(instructorsLocal, "Value", "Text", String.Empty);
            return View();
        }

        //
        // POST: /CourseManager/Create

        [HttpPost]
        //[Authorize]
        public ActionResult Create(LibriCourse course)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var c = new Course
                    {
                        DescriptionHeading = course.Title,
                        OwnerId = course.OwnerId + "@" + ConfigurationManager.AppSettings["UNTDomain"],
                        Name = course.Title,
                        Section = course.CourseCode,
                        //Description = "We'll be learning about about the structure of living creatures "
                        //    + "from a combination of textbooks, guest lectures, and lab work. Expect "
                        //    + "to be excited!",
                        //Room = "301",
                        //CourseState = "PROVISIONED"
                    };
                    //_courseService.CreateCourse(c);

                    //register course in Classroom and DB
                    string isInitialized = String.Empty;
                    try
                    {
                        if (Session["user"] == null) RegisterSession();
                        RegisterClassroomSvc();
                        ClassroomService clservice = (ClassroomService)Session["clservice"];
                        //var course1 = new Course
                        //{
                        //    Name = "10th Grade Biology",
                        //    Section = "Period 2",
                        //    DescriptionHeading = "Welcome to 10th Grade Biology",
                        //    Description = "We'll be learning about about the structure of living creatures "
                        //        + "from a combination of textbooks, guest lectures, and lab work. Expect "
                        //        + "to be excited!",
                        //    Room = "301",
                        //    OwnerId = "e.jajaga@seeu.edu.mk",
                        //    CourseState = "PROVISIONED"
                        //};

                        //course1 = clservice.Courses.Create(course1).Execute();

                        if (clservice != null)
                        {
                            var request = clservice.Courses.Create(c);
                            var response = request.Execute();
                            if (response != null)
                            {
                                //add to DB Kursis
                                Kursi kursiRi = new Kursi()
                                {
                                    id = response.Id,
                                    termid = termid,
                                    ownerid = response.OwnerId,
                                    coursecode = response.Section,
                                    title = response.Name,
                                    link = response.AlternateLink,
                                    updatetime = DateTime.Now,
                                    depid = GetCourseDepID(course.CourseCode),
                                    username = course.OwnerId
                                };
                                db.Kursis.InsertOnSubmit(kursiRi);
                                //add to delegations
                                CourseDelegation delegim = new CourseDelegation()
                                {
                                    id = Guid.NewGuid(),
                                    kursiid = response.Id,
                                    //userid = response.OwnerId,
                                    username = kursiRi.username
                                };
                                db.CourseDelegations.InsertOnSubmit(delegim);
                                db.SubmitChanges();
                                return RedirectToAction("Details", new { id = response.Id });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    return RedirectToAction("Index");
                }
                catch (AggregateException e)
                {
                    ModelState.AddModelError("_FORM", "Cannot create course!");
                }
            }

            //ViewBag.semestrat = new SelectList(termsLocal, "Value", "Text", Int32.Parse(ConfigurationManager.AppSettings["CurrentTerm"]));
            //var users = _authService.GetInstructors();
            //build instructors list
            //ViewBag.msuset = new SelectList(instructorsLocal, "Value", "Text", String.Empty);
            return View(course);
        }

        //create multiple courses
        public ActionResult ViewTermCourses(string term)
        {
            ////prej ne service
            //var seeuService = GetSeeuService();
            //var coursesJsonString = seeuService.GetActivities(ConfigurationManager.AppSettings["CurrentTerm"]);
            //DataTable coursesDataTbl = (DataTable)JsonConvert.DeserializeObject(coursesJsonString, (typeof(DataTable)));


            //prej ne CSV data te krijohet
            var yourData = System.IO.File.ReadAllLines(Server.MapPath(@"../csv/unt-sum1819.csv"))
                .Skip(1)
           .Select(x => x.Split('	'))
           .Select(x => new
           {
               //Semestri = x[0],
               Departamenti = x[0],     //Faculty of...
               Niveli = x[1],       //UNDERGRADUATE
               //Kodi = x[2],
               Titulli = x[2],
               Gjuha = x[3],
               Kampusi = x[4],
               Profi = x[5] + "@" + ConfigurationManager.AppSettings["Domain"] 
           });

            List<ImportCourse> lista = new List<ImportCourse>();

            foreach (var c in yourData)
            {
                ImportCourse newc = new ImportCourse
                {
                    CourseTitle = "U 19 " + c.Titulli,
                    //CourseCode = c.DepartmentTerm,
                    OwnerId = c.Profi
                };
                //buil CourseCode
                //departamenti + semestri + niveli + gjuha = C + S7 + U + A 
                //ELC, N-CST, N-TT, N-LAW, N-PA, N-BA, LC ose ALL
                switch (c.Departamenti)
                {
                    case "Fakulteti i Shkencave të Informatikës": newc.CourseCode = "C"; break;
                    case "Fakulteti i Shkencave Teknike": newc.CourseCode = "T"; break;
                    case "Fakulteti i Ndërtimtarisë dhe Arkitekturës": newc.CourseCode = "N"; break;
                    case "Fakulteti i Shkencave Teknologjike": newc.CourseCode = "K"; break;
                    case "Fakulteti i Shkencave Sociale": newc.CourseCode = "S"; break;
                }
                //shto termin
                newc.CourseCode += "S8";
                //proces undergraduate, master, phd
                switch (c.Niveli)
                {
                    case "Undergraduate": newc.CourseCode += "U"; break;
                    case "Phd": newc.CourseCode += "D"; break;
                    case "Master": newc.CourseCode += "P"; break;
                }
                //add language
                switch (c.Gjuha)
                {
                    case "EN": newc.CourseCode += "E"; break;
                    case "AL": newc.CourseCode += "A"; break;
                    case "IT": newc.CourseCode += "I"; break;
                    case "MK": newc.CourseCode += "M"; break;
                    case "TR": newc.CourseCode += "T"; break;
                    case "DE": newc.CourseCode += "D"; break;
                }
                //add campus
                //newc.CourseCode += c.Kampusi;
                lista.Add(newc);
            }
            return View(lista);

        }

        // CourseManager/CreateMultipleCoursesAsync
        public async Task CreateMultipleCoursesAsync()
        {
            //regjistro sesionin nga SSO nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //inicializo classroom servisin e Domain Adminit google@seeu.edu.mk
            String serviceAccountEmail = ConfigurationManager.AppSettings["AdminServiceAccount"];
            String userAccountEmail = ConfigurationManager.AppSettings["AdminUserAccount"];

            var certificate = new X509Certificate2(Server.MapPath(@"../resources/unt-gc-admin.p12"), "notasecret", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(serviceAccountEmail)
               {
                   User = userAccountEmail,
                   Scopes = new[] { 
                            //DriveService.Scope.Drive, 
                            ClassroomService.Scope.ClassroomCourses,
                            ClassroomService.Scope.ClassroomCourseworkMe,
                            ClassroomService.Scope.ClassroomCourseworkStudents,
                            //ClassroomService.Scope.ClassroomProfileEmails,
                            //ClassroomService.Scope.ClassroomProfilePhotos,
                            ClassroomService.Scope.ClassroomRosters
                       }
               }.FromCertificate(certificate));


            // Create Classroom API service.
            ClassroomService adminclservice = new ClassroomService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Libri Classroom",
            });

            try
            {
                //ClassroomService clservice = (ClassroomService)Session["clservice"];
                if (adminclservice != null)
                {
                    //prej ne CSV data te krijohet
                    var yourData = System.IO.File.ReadAllLines(Server.MapPath(@"../csv/unt-sum1819.csv"))
                        .Skip(1)
                   .Select(x => x.Split('	'))
                   .Select(x => new
                   {
                       //Semestri = x[0],
                       Departamenti = x[0],     //Faculty of...
                       Niveli = x[1],       //UNDERGRADUATE
                       //Kodi = x[2],
                       Titulli = x[2],
                       Gjuha = x[3],
                       Kampusi = x[4],
                       Profi = x[5] + "@" + ConfigurationManager.AppSettings["Domain"]
                   });

                    List<ImportCourse> lista = new List<ImportCourse>();

                    foreach (var c in yourData)
                    {
                        ImportCourse newc = new ImportCourse
                        {
                            CourseTitle = "U 19 " + c.Titulli,
                            //CourseCode = c.DepartmentTerm,
                            OwnerId = c.Profi
                        };
                        //buil CourseCode
                        //departamenti + semestri + niveli + gjuha = C + S7 + U + A 
                        //ELC, N-CST, N-TT, N-LAW, N-PA, N-BA, LC ose ALL
                        switch (c.Departamenti)
                        {

                            case "Fakulteti i Shkencave të Informatikës": newc.CourseCode = "C"; break;
                            case "Fakulteti i Shkencave Teknike": newc.CourseCode = "T"; break;
                            case "Fakulteti i Ndërtimtarisë dhe Arkitekturës": newc.CourseCode = "N"; break;
                            case "Fakulteti i Shkencave Teknologjike": newc.CourseCode = "K"; break;
                            case "Fakulteti i Shkencave Sociale": newc.CourseCode = "S"; break;

                        }
                        //shto termin
                        newc.CourseCode += "S8";
                        //proces undergraduate, master, phd
                        switch (c.Niveli)
                        {
                            case "Undergraduate": newc.CourseCode += "U"; break;
                            case "Phd": newc.CourseCode += "D"; break;
                            case "Master": newc.CourseCode += "P"; break;
                        }
                        //add language
                        switch (c.Gjuha)
                        {
                            case "EN": newc.CourseCode += "E"; break;
                            case "AL": newc.CourseCode += "A"; break;
                            case "IT": newc.CourseCode += "I"; break;
                            case "MK": newc.CourseCode += "M"; break;
                            case "TR": newc.CourseCode += "T"; break;
                            case "DE": newc.CourseCode += "D"; break;
                        }
                        //add campus
                        //newc.CourseCode += c.Kampusi;
                        lista.Add(newc);
                    }

                    var batch = new BatchRequest(adminclservice, "https://classroom.googleapis.com/batch");
                    BatchRequest.OnResponse<Course> callback = (course, error, i, message) =>
                    {
                        if (error != null)
                        {
                            System.Diagnostics.Debug.Write("Error adding a course: {0}", error.Message);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Write("Course {0} was added.", course.Name);
                        }
                    };
                    foreach (var newcourse in lista)
                    {
                        var course = new Course()
                        {
                            Name = newcourse.CourseTitle,
                            Section = newcourse.CourseCode,
                            DescriptionHeading = newcourse.CourseTitle,
                            //Description = null,
                            //Room = newcourse.Room,
                            OwnerId = newcourse.OwnerId
                        };

                        var request = adminclservice.Courses.Create(course);
                        batch.Queue<Course>(request, callback);
                    }
                    await batch.ExecuteAsync();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //return RedirectToAction("Details", new { id = "4303073990" });
        }

        //
        // GET: /CourseManager/Edit/5
        //[Authorize]
        public ActionResult Edit(string id)
        {
            if (Session["user"] == null) RegisterSession();
            else
            {
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];
                var getrequest = clservice.Courses.Get(id);
                Course course = getrequest.Execute();
                if (course != null)
                {
                    var ownerEmail = clservice.UserProfiles.Get(course.OwnerId).Execute().EmailAddress;
                    var c = new LibriCourse
                    {
                        CourseCode = course.Section,
                        //Term = course.DescriptionHeading,
                        OwnerId = ownerEmail.Substring(0, ownerEmail.IndexOf("@")),
                        Title = course.Name,
                        CreationTime = DateTime.Parse(course.UpdateTime),
                        Id = course.Id,
                        Link = course.AlternateLink
                    };
                    ViewBag.Username = Session["user"];
                    //ViewBag.semestrat = new SelectList(termsLocal, "Value", "Text", c.Term);
                    //build instructors list
                    //get course teachers from classroom
                    var teachersRq = clservice.Courses.Teachers.List(course.Id);
                    ListTeachersResponse teachersLs = teachersRq.Execute();
                    //get the first teacher's username
                    MailAddress teacherEmail = new MailAddress(teachersLs.Teachers.First().Profile.EmailAddress);
                    string firstTeacherUsername = teacherEmail.User;
                    //ViewBag.msuset = new SelectList(instructorsLocal, "Value", "Text", firstTeacherUsername);
                    return View(c);
                }
                else
                    return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /CourseManager/Edit/5

        [HttpPost]
        //[Authorize]
        public ActionResult Edit(LibriCourse course)
        {
            if (ModelState.IsValid)
            {
                course.CreationTime = DateTime.Now;
                var c = new Course
                {
                    DescriptionHeading = course.Title,
                    OwnerId = course.OwnerId + "@" + ConfigurationManager.AppSettings["Domain"],
                    Name = course.Title,
                    Section = course.CourseCode
                };
                try
                {
                    RegisterClassroomSvc();
                    ClassroomService clservice = (ClassroomService)Session["clservice"];
                    var request = clservice.Courses.Update(c, course.Id);
                    var result = request.Execute();
                    if (result != null)
                    {
                        //update on DB
                        Kursi exC = db.Kursis.Where(i => i.id == course.Id).SingleOrDefault();
                        if (exC != null)
                        {
                            // db.Kursis.Attach(exC);
                            exC.title = result.Name; exC.coursecode = result.Section; //exC.ownerid = result.OwnerId;
                            ////update to delegations
                            //CourseDelegation delegim = db.CourseDelegations.Where(g => g.kursiid == result.Id).SingleOrDefault();
                            //if (delegim != null)
                            //{
                            //    //db.CourseDelegations.Attach(delegim);
                            //    delegim.userid = result.OwnerId;
                            //}
                            db.SubmitChanges();
                            return RedirectToAction("Details", new { id = course.Id });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            //ViewBag.semestrat = new SelectList(termsLocal, "Value", "Text", Int32.Parse(ConfigurationManager.AppSettings["CurrentTerm"]));
            //build instructors list
            //ViewBag.msuset = new SelectList(instructorsLocal, "Value", "Text", String.Empty);
            return View(course);
        }

        public ActionResult DeleteALLTermCourses(string termcode)
        {
            //termcode = s8
            try
            {
                if (Session["user"] == null) RegisterSession();
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];
                if (clservice != null)
                {
                    // Define request parameters.
                    CoursesResource.ListRequest request = clservice.Courses.List();
                    //request.PageSize = 50;
                    request.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];

                    // List courses.
                    ListCoursesResponse response = request.Execute();
                    //response = null;
                    List<CourseViewModel> listaKurseve = new List<CourseViewModel>();

                    if (response.Courses != null || response.Courses.Count > 0)
                    {
                        //i merr te gjitha kurset e semestrit aktual
                        foreach (Course course in response.Courses.Where(k => GetCourseTerm(k.Section) == termcode))
                        {
                            Delete(course.Id);
                        }
                    }
                    return View(true);
                }
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            return View(false);
        }
        //
        // GET: /CourseManager/Delete/5
        //[Authorize]
        public ActionResult Delete(string id)
        {
            if (Session["user"] == null) RegisterSession();
            else
            {
                //delete in LOCAL DB
                //delete course delegations
                List<CourseDelegation> delegimet = db.CourseDelegations.Where(g => g.kursiid == id).ToList();
                if (delegimet != null)
                {
                    foreach (var d in delegimet)
                    {
                        db.CourseDelegations.DeleteOnSubmit(d);
                    }
                }
                //delete course stats
                CourseStat stats = db.CourseStats.Where(c => c.courseId == id).SingleOrDefault();
                if (stats != null)
                {
                    db.CourseStats.DeleteOnSubmit(stats);
                }
                //delete course feeds
                var feeds = db.Feeds.Where(c => c.courseId == id).ToList();
                if (feeds != null && feeds.Count > 0)
                {
                    foreach (var f in feeds)
                        db.Feeds.DeleteOnSubmit(f);
                }

                //delete course on DB
                Kursi delC = db.Kursis.Where(i => i.id == id).SingleOrDefault();
                if (delC != null)
                {
                    db.Kursis.DeleteOnSubmit(delC);
                }
                db.SubmitChanges();

                //delete on GC
                try
                {
                    RegisterClassroomSvc();
                    ClassroomService clservice = (ClassroomService)Session["clservice"];
                    var getrequest = clservice.Courses.Get(id);
                    Course course = getrequest.Execute();
                    if (course != null)
                    {
                        var delrequest = clservice.Courses.Delete(id);
                        delrequest.Execute();
                    }
                    return View(true);
                }
                catch (Exception e)
                {
                    //throw e;
                }
            }
            return View(false);
        }

        public bool AddMaterial2Course(string courseid)
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            RegisterELCClassroomSvc();
            ClassroomService elcclservice = (ClassroomService)Session["elcclservice"];
            //DriveService drvservice = (DriveService)Session["drvservice"];
            //get course details
            var courseRq = elcclservice.Courses.Get(courseid);

            try
            {
                Course course = courseRq.Execute();

                //List<CourseMaterial> lsMat = new List<CourseMaterial>();

                //CourseMaterial linkuTutorialit = new CourseMaterial()
                //{
                //    Link = new Link { Url = "https://seeu.edu.mk", Title = "GC Tutorial" }
                //};

                //lsMat.Add(linkuTutorialit);

                //CourseMaterialSet newm = new CourseMaterialSet()
                //{
                //    Title = "Syllabus",
                //    Materials = lsMat
                //};

                //List<CourseMaterialSet> cms = new List<CourseMaterialSet>();
                //cms.Add(newm);

                //course.CourseMaterialSets = new List<CourseMaterialSet>();
                //course.CourseMaterialSets = cms;
                ////patch or update
                //var updReq = elcclservice.Courses.Update(course, course.Id);
                //var updExc = updReq.Execute();

                List<Material> lsMat = new List<Material>()
                    {
                        new Material()
                        {
                            Link = new Link { Url = "https://seeu.edu.mk", Title = "Sillabusi" }
                        }
                    };

                //krijo courseWork resurs
                CourseWork res = new CourseWork()
                {
                    CourseId = course.Id,
                    State = "PUBLISHED",
                    WorkType = "COURSE_WORK_TYPE_UNSPECIFIED",
                    Title = "Syllabus",
                    Materials = lsMat
                };

                //Google.Apis.Classroom.v1.CoursesResource.CourseWorkResource.

                //Announcement silAnn = new Announcement()
                //{
                //    Title = "Syllabus", CourseID = course.Id
                //};
                // Define request parameters.
                var request = elcclservice.Courses.CourseWork.Create(res, course.Id);
                //request.QuotaUser = ConfigurationManager.AppSettings["userAccount"];

                // List courses.
                var response = request.Execute();
            }
            catch (Exception ex)
            {
                //throw ex;
                return false;
            }
            return true;
        }

        #endregion

        #region Roster Manager
        //
        // GET: /CourseManager/Delegate/5
        //[Authorize]
        public ActionResult Delegate(string id)
        {
            if (Session["user"] == null) RegisterSession();
            else
            {
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];
                //var certificate1 = new X509Certificate2(Server.MapPath(@"resources/elckey.p12"), "notasecret", X509KeyStorageFlags.Exportable);

                var getrequest = clservice.Courses.Get(id);
                Course course = getrequest.Execute();
                if (course != null)
                {
                    //get course teachers
                    var teachersRq = clservice.Courses.Teachers.List(id);
                    ListTeachersResponse teachersRs = teachersRq.Execute();
                    List<RosterViewModel> roster = new List<RosterViewModel>();
                    if (teachersRs.Teachers != null)
                    {
                        foreach (var teacher in teachersRs.Teachers)
                        {
                            string username = String.Empty;
                            if (teacher.Profile.EmailAddress != null)
                                username = new MailAddress(teacher.Profile.EmailAddress).User;
                            RosterViewModel newt = new RosterViewModel
                            {
                                //UserId = teacher.UserId,
                                Name = teacher.Profile.Name.FullName,
                                Email = teacher.Profile.EmailAddress,
                                Username = username,
                                IsTeacher = true
                            };
                            roster.Add(newt);
                        }
                    }
                    ViewBag.courseid = id;

                    //get course students
                    var studentsRq = clservice.Courses.Students.List(id);
                    ListStudentsResponse studentsRs = studentsRq.Execute();
                    if (studentsRs.Students != null)
                    {
                        foreach (var student in studentsRs.Students)
                        {
                            string username = new MailAddress(student.Profile.EmailAddress).User;
                            RosterViewModel newst = new RosterViewModel
                            {
                                //UserId = student.UserId,
                                Name = student.Profile.Name.FullName,
                                Email = student.Profile.EmailAddress,
                                Username = username,
                                IsTeacher = false
                            };
                            roster.Add(newst);
                        }
                    }

                    return View(roster);
                }
            }

            return RedirectToAction("Index");
        }

        //
        // POST: /Admin/CourseManager/Edit/5

        [HttpPost]
        //[Authorize]
        public JsonResult Delegate(string username, string courseid)
        {
            //inicializo classroom servisin e Domain Adminit google@seeu.edu.mk
            if (Session["clservice"] == null)
                RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            //nese username permban numra ai eshte student, ne te kundert instruktor
            string userRole = string.Empty; string userEmail = string.Empty; string enrollmentCode = string.Empty;

            var useriReq = clservice.UserProfiles.Get(username + "@" + ConfigurationManager.AppSettings["Domain"]);
            var useri = useriReq.Execute();

            Teacher teacher = new Teacher
            {
                UserId = username + "@" + ConfigurationManager.AppSettings["Domain"],
                CourseId = courseid,
                Profile = useri
            };
            try
            {
                //ClassroomService clservice = (ClassroomService)Session["clservice"];
                //create student
                var request = clservice.Courses.Teachers.Create(teacher, courseid);
                request.Execute();

                //add to DB delegations
                CourseDelegation delegim = new CourseDelegation()
                {
                    kursiid = courseid,
                    id = Guid.NewGuid()
                };
                var user = db.GoogleUsers.Where(u => u.Username == username).SingleOrDefault();
                if (user != null)
                {
                    if (user.GoogleID != null)
                        delegim.userid = user.GoogleID;
                }
                delegim.username = username;
                db.CourseDelegations.InsertOnSubmit(delegim);
                db.SubmitChanges();
                return Json(true);
            }
            catch (GoogleApiException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.Conflict)
                {
                    Console.WriteLine("You are already a member of this course.\n");
                }
                else
                {
                    throw e;
                }
            }
            return Json(false);
        }

        [HttpPost]
        //[Authorize]
        public JsonResult GetCourseRoster(string courseid)
        {
            if (courseid != String.Empty)
            {
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];
                var getrequest = clservice.Courses.Get(courseid);
                Course course = getrequest.Execute();
                if (course != null)
                {
                    //get course teachers
                    var teachersRq = clservice.Courses.Teachers.List(courseid);
                    ListTeachersResponse teachersRs = teachersRq.Execute();
                    List<RosterViewModel> roster = new List<RosterViewModel>();
                    if (teachersRs.Teachers != null)
                    {
                        foreach (var teacher in teachersRs.Teachers)
                        {
                            string username = String.Empty;
                            if (teacher.Profile.EmailAddress != null)
                                username = new MailAddress(teacher.Profile.EmailAddress).User;
                            RosterViewModel newt = new RosterViewModel
                            {
                                UserId = teacher.UserId,
                                Name = teacher.Profile.Name.FullName,
                                Email = teacher.Profile.EmailAddress,
                                Username = username,
                                IsTeacher = true
                            };
                            roster.Add(newt);
                        }
                    }

                    //get course students
                    var studentsRq = clservice.Courses.Students.List(courseid);
                    ListStudentsResponse studentsRs = studentsRq.Execute();
                    if (studentsRs.Students != null)
                    {
                        foreach (var student in studentsRs.Students)
                        {
                            string username = new MailAddress(student.Profile.EmailAddress).User;
                            RosterViewModel newst = new RosterViewModel
                            {
                                UserId = student.UserId,
                                Name = student.Profile.Name.FullName,
                                Email = student.Profile.EmailAddress,
                                Username = username,
                                IsTeacher = false
                            };
                            roster.Add(newst);
                        }
                    }
                    return Json(roster, JsonRequestBehavior.AllowGet);
                }
                else return Json(false);
            }
            else return Json(false);
        }

        [HttpPost]
        //[Authorize]
        public JsonResult DeleteRoster(string username, string courseid)
        {
            bool result = false;
            if (courseid != String.Empty)
            {
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];

                string teacherEmail = username + "@" + ConfigurationManager.AppSettings["Domain"];

                var request = clservice.Courses.Teachers.Get(courseid, teacherEmail).Execute();
                if (request != null)
                {
                    var delReq = clservice.Courses.Teachers.Delete(courseid, teacherEmail).Execute();

                    //delete course delegation
                    var deldb = db.CourseDelegations.Where(c => c.kursiid == courseid && c.username == username).ToList();
                    if (deldb != null)
                    {
                        foreach (CourseDelegation del in deldb)
                        {
                            db.CourseDelegations.DeleteOnSubmit(del);
                            db.SubmitChanges();
                        }
                    }

                    result = true;
                }
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Autocomplete()
        {
            ////var seeuService = ServiceManager.GetService();
            ////Student[] students = seeuService.GetStudents();
            //LibriStudent[] students = new LibriStudent[]{
            //    new LibriStudent{ StudentName = "Edmond Student", StudentEmail="ej16374@seeu.edu.mk", Username = "ej16374" }
            //};
            //List<string> Usernames = new List<string>();
            //foreach (var st in students)
            //{
            //    Usernames.Add(st.Username);
            //}
            //Teacher[] teachers = seeuService.GetTeachers();
            List<string> Usernames = db.GoogleUsers.Select(s => s.Username).ToList();
            //LibriTeacher[] teachers = new LibriTeacher[]{
            //    new LibriTeacher{ TeacherName = "Edmond Teacher", Username="e.jajaga", TeacherEmail="e.jajaga@seeu.edu.mk" }
            //};
            //foreach (var te in teachers)
            //{
            //    Usernames.Add(te.Username);
            //}
            return Json(Usernames, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Synchronizations

        //Sync page
        public ActionResult Synchronizations()
        {
            return View();
        }

        public ActionResult SyncEverything()
        {
            //if (SyncGoogleUsers())
            //{
            //ViewBag.msg = "SyncGoogleUsers";
            //if (SyncGoogleCourses())
            //{
            //    ViewBag.msg = "SyncGoogleCourses";
            //    if (SyncGCourseDelegations())
            //    {
            //        ViewBag.msg = "SyncGCourseDelegations";
            //        if (SyncTermCoursesFeeds())
            //        {
            ViewBag.msg = "SyncTermCoursesFeeds";
            if (SyncGenerateALLCoursesStats())
            {
                ViewBag.msg = "SyncGenerateALLCoursesStats";
                if (SyncGenerateReports())
                {
                    ViewBag.msg = "SyncGenerateReports";
                    return View(true);
                }
                else
                {
                    ViewBag.msg = "SyncGenerateReports";
                }
            }
            else
            {
                ViewBag.msg = "SyncGenerateALLCoursesStats";
            }
            //            }
            //            else
            //            {
            //                ViewBag.msg = "SyncTermCoursesFeeds";
            //            }
            //        }
            //        else
            //        {
            //            ViewBag.msg = "SyncGCourseDelegations";
            //        }

            //    }
            //    else
            //    {
            //        ViewBag.msg = "SyncGoogleCourses";
            //    }
            //}
            //else ViewBag.msg = "SyncGoogleUsers";
            return View(false);
        }

        #region 0. Google Users SYNC
        //sinkronizimi i google userave
        public bool SyncGoogleUsers()
        {
            var owners = SyncGoogleCourseOwners();
            Thread.Sleep(10000);
            var teachers = SyncGoogleTeachers();
            Thread.Sleep(10000);
            if (owners && teachers)
                return true;
            else
                return false;
        }

        public bool SyncGoogleCourseOwners()
        {
            //per current term
            string termid = ConfigurationManager.AppSettings["CurrentTerm"];
            ViewBag.termid = termid;

            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            // Define request parameters.
            CoursesResource.ListRequest request = clservice.Courses.List();
            //request.PageSize = 50;
            request.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];

            // List courses.
            ListCoursesResponse response = request.Execute();
            //response = null;
            List<CourseViewModel> listaKurseve = new List<CourseViewModel>();

            if (response.Courses != null || response.Courses.Count > 0)
            {
                //i merr te gjitha kurset e semestrit aktual
                foreach (Course course in response.Courses.Where(k => GetCourseTerm(k.Section) == termid))
                {
                    try
                    {
                        var owner = clservice.UserProfiles.Get(course.OwnerId).Execute();
                        MailAddress addr = new MailAddress(owner.EmailAddress);
                        string ownerUsername = addr.User;
                        //kontrollo mos eshte ai user me ate ID ne DB
                        var pyetje = db.GoogleUsers.Where(u => u.Username == ownerUsername).SingleOrDefault();
                        if (pyetje == null) //new user
                        {
                            GoogleUser newUser = new GoogleUser()
                            {
                                GoogleID = course.OwnerId,
                                Username = ownerUsername,
                                Fullname = owner.Name.FullName,
                                depid = GetCourseDepID(course.Section)
                            };
                            db.GoogleUsers.InsertOnSubmit(newUser);
                            db.SubmitChanges();

                        }
                        //else //update google userid
                        //{
                        //    //db.GoogleUsers.Attach(pyetje);
                        //    //var dbOwner = db.GoogleUsers.Where(u => u.Username == teacherUsername).SingleOrDefault();
                        //    //dbOwner.GoogleID = course.OwnerId;
                        //    pyetje.GoogleID = course.OwnerId;
                        //    pyetje.Username = ownerUsername;
                        //    pyetje.Fullname = owner.Name.FullName;
                        //    pyetje.depid = GetCourseDepID(course.Section);
                        //    db.SubmitChanges();
                        //}
                    }
                    catch (Exception ex)
                    {
                        //throw ex;
                    }

                }
            }


            return true;
        }

        public bool SyncGoogleTeachers()
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            // Define request parameters.
            CoursesResource.ListRequest request = clservice.Courses.List();
            //request.PageSize = 50;
            request.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];

            // List courses.
            ListCoursesResponse response = request.Execute();
            //response = null;
            List<CourseViewModel> listaKurseve = new List<CourseViewModel>();

            if (response.Courses != null || response.Courses.Count > 0)
            {
                //i merr te gjitha kurset e semestrit aktual
                foreach (Course course in response.Courses.Where(k => GetCourseTerm(k.Section) == termid))
                {
                    try
                    {
                        //kontrollo mos eshte ai kurs me ate ID ne DB
                        //merri teachers of the course
                        var teachersRq = clservice.Courses.Teachers.List(course.Id);
                        ListTeachersResponse teachersLs = teachersRq.Execute();

                        foreach (var teacher in teachersLs.Teachers)
                        {
                            //kontrollo mos eshte ai perdorues me ate ID ne DB
                            var pyetje = db.GoogleUsers.Where(u => u.GoogleID == teacher.UserId).SingleOrDefault();
                            if (pyetje == null)
                            {
                                MailAddress addr = new MailAddress(teacher.Profile.EmailAddress);
                                string teacherUsername = addr.User;
                                GoogleUser newUser = new GoogleUser()
                                {
                                    GoogleID = teacher.UserId,
                                    Username = teacherUsername,
                                    Fullname = teacher.Profile.Name.FullName,
                                    depid = GetCourseDepID(course.Section)
                                };
                                db.GoogleUsers.InsertOnSubmit(newUser);
                                db.SubmitChanges();
                            }
                            //else //update google userid
                            //{
                            //    MailAddress addr = new MailAddress(teacher.Profile.EmailAddress);
                            //    string teacherUsername = addr.User;
                            //    if (teacherUsername == "e.murtezani")
                            //    {
                            //        db.GoogleUsers.Attach(pyetje);
                            //        pyetje.GoogleID = teacher.UserId;
                            //        db.SubmitChanges();
                            //    }
                            //}
                        }
                    }
                    catch (Exception e)
                    { }
                }
            }
            return true;
        }

        #endregion

        #region 1. Courses SYNC

        //momentalisht vetem INSERTon kurse
        public bool SyncGoogleCourses()
        {
            //per current term
            string termid = ConfigurationManager.AppSettings["CurrentTerm"];
            ViewBag.termid = termid;

            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            // Define request parameters.
            CoursesResource.ListRequest request = clservice.Courses.List();
            //request.PageSize = 50;
            request.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];

            // List courses.
            ListCoursesResponse response = request.Execute();
            //response = null;
            List<CourseViewModel> listaKurseve = new List<CourseViewModel>();

            char courseDepartment;

            if (response.Courses != null || response.Courses.Count > 0)
            {
                //i merr te gjitha kurset e semestrit aktual
                foreach (Course course in response.Courses.Where(k => GetCourseTerm(k.Section) == termid))
                {
                    courseDepartment = Char.Parse(course.Section.Substring(0, 1));
                    var ownerEmail = clservice.UserProfiles.Get(course.OwnerId).Execute().EmailAddress;
                    var ownerUsername = new MailAddress(ownerEmail).User;
                    Kursi c = new Kursi()
                    {
                        id = course.Id,
                        title = course.Name,
                        ownerid = course.OwnerId,
                        coursecode = course.Section,
                        link = course.AlternateLink,
                        termid = termid,
                        updatetime = DateTime.Parse(course.UpdateTime),
                        depid = courseDepartment.ToString(),
                        username = ownerUsername,
                        EnrollmentCode = course.EnrollmentCode
                    };

                    try
                    {
                        //kontrollo mos eshte ai kurs me ate ID ne DB
                        var pyetje = db.Kursis.Where(kursi => kursi.id == c.id).SingleOrDefault();
                        if (pyetje == null)
                        {
                            db.Kursis.InsertOnSubmit(c);
                            db.SubmitChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        //throw ex;
                    }
                }
            }
            return true;
        }

        public bool removeCoursesNotInGC()
        {
            //per current term
            string termid = ConfigurationManager.AppSettings["CurrentTerm"];
            ViewBag.termid = termid;

            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            //fillon filtrimi i dashboardit sipas rolit te userit
            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                foreach (Kursi course in listaKurseve)
                {
                    try
                    {
                        var gc = clservice.Courses.Get(course.id).Execute();
                    }
                    catch (Exception ex)
                    {
                        //delete all Course data in DB
                        Delete(course.id);
                    }
                }
            }
            return true;
        }



        #endregion

        #region 2. Delegations SYNC
        //teachers SYNC
        public bool SyncGCourseDelegations()
        {
            //per current term
            string termid = ConfigurationManager.AppSettings["CurrentTerm"];
            ViewBag.termid = termid;

            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            // Define request parameters.
            CoursesResource.ListRequest request = clservice.Courses.List();
            //request.PageSize = 50;
            request.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];

            // List courses.
            ListCoursesResponse response = request.Execute();
            //response = null;
            List<CourseViewModel> listaKurseve = new List<CourseViewModel>();

            if (response.Courses != null || response.Courses.Count > 0)
            {
                //i merr te gjitha kurset e semestrit aktual
                foreach (Course course in response.Courses.Where(k => GetCourseTerm(k.Section) == termid))
                {
                    try
                    {
                        //Course Owner SYNC
                        Google.Apis.Classroom.v1.Data.UserProfile userProfile = clservice.UserProfiles.Get(course.OwnerId).Execute();
                        string ownerUsername = new MailAddress(userProfile.EmailAddress).User;
                        //kontrollo mos eshte ai kurs me ate ID dhe ownerid ne DB
                        var pyetje = db.CourseDelegations.Where(u => u.username == ownerUsername && u.kursiid == course.Id).SingleOrDefault();
                        //nese nuk eshte shtoje
                        if (pyetje == null)
                        {   //insert
                            CourseDelegation newDelegation = new CourseDelegation()
                            {
                                id = Guid.NewGuid(),
                                kursiid = course.Id,
                                userid = course.OwnerId,
                                username = ownerUsername
                            };
                            db.CourseDelegations.InsertOnSubmit(newDelegation);
                            db.SubmitChanges();
                        }
                        //else
                        //{//update
                        //    pyetje.userid = course.OwnerId;
                        //    pyetje.username = ownerUsername;
                        //    //check.kursiid
                        //    db.SubmitChanges();
                        //}

                        //Course Teachers SYNC
                        //merri teachers of the course
                        var teachersRq = clservice.Courses.Teachers.List(course.Id);
                        ListTeachersResponse teachersLs = teachersRq.Execute();

                        foreach (var teacher in teachersLs.Teachers)
                        {
                            string teacherUsername = new MailAddress(teacher.Profile.EmailAddress).User;
                            //kontrollo mos eshte ai kurs me ate ID dhe username ne DB
                            var check = db.CourseDelegations.Where(u => u.username == teacherUsername && u.kursiid == teacher.CourseId).SingleOrDefault();
                            if (check == null)
                            {   //insert
                                CourseDelegation newDelegation = new CourseDelegation()
                                {
                                    id = Guid.NewGuid(),
                                    kursiid = teacher.CourseId,
                                    userid = teacher.UserId,
                                    username = teacherUsername
                                };
                                db.CourseDelegations.InsertOnSubmit(newDelegation);
                                db.SubmitChanges();
                            }
                            //else
                            //{//update
                            //    check.userid = teacher.UserId;
                            //    check.username = new MailAddress(teacher.Profile.EmailAddress).User;
                            //    //check.kursiid
                            //    db.SubmitChanges();
                            //}

                        }

                        //REMOVE delegations in DB removed in GC
                        var courseDBdel = db.CourseDelegations.Where(d => d.kursiid == course.Id).ToList();
                        if (courseDBdel != null)
                        {
                            //per cdo teacher te course-it ne DB kontrollo a eshte ende i deleguar ne GC
                            foreach (var t in courseDBdel)
                            {
                                var userExistsInGC = teachersLs.Teachers.Where(u => u.Profile.EmailAddress == t.username + "@" + ConfigurationManager.AppSettings["Domain"]).SingleOrDefault();
                                //nese nuk eshte me i deleguar ne GC atehere fshije ne DB
                                if (userExistsInGC == null)
                                {
                                    var delDBDelegation = db.CourseDelegations.Where(d => d.username == t.username && d.kursiid == course.Id).SingleOrDefault();
                                    if (delDBDelegation != null)
                                    {
                                        db.CourseDelegations.DeleteOnSubmit(delDBDelegation);
                                        db.SubmitChanges();
                                    }
                                }

                            }
                        }
                    }
                    catch (Exception e)
                    { }
                }
            }
            return true;
        }

        public ActionResult SyncDelegations2DBMissingInGC()
        {

            return View();
        }

        //ne fillim te semestrit te ekzekutoet
        public bool DelegateAllTeachers()
        {
            var delELC = DelegateELC2TermCourses();
            var cst = DelegateDepartment2TermCourses('C');
            //Thread.Sleep(10000);
            //var elc = SyncGenerateReportsDepartment("E");
            //Thread.Sleep(10000);
            var paps = DelegateDepartment2TermCourses('P');
            //Thread.Sleep(10000);
            var law = DelegateDepartment2TermCourses('L');
            //Thread.Sleep(10000);
            var cen = DelegateDepartment2TermCourses('Q');
            //Thread.Sleep(10000);
            var ba = DelegateDepartment2TermCourses('B');
            //Thread.Sleep(10000);
            var tt = DelegateDepartment2TermCourses('T');
            //Thread.Sleep(10000);
            if (delELC && cst && law && paps && ba && cen && tt)
                return true;
            else
                return false;
        }

        public bool DelegateELC2TermCourses()
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            //fillon filtrimi i kurseve sipas rolit te userit
            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                //i merr te gjitha kurset e semestrit aktual
                foreach (Kursi course in listaKurseve)
                {
                    //kontrollo a eshte i deleguar apo jo
                    var checkDel = db.CourseDelegations.Where(cd => cd.username == "classroom" && cd.kursiid == course.id).SingleOrDefault();
                    //bej delegim nese nuk ka
                    if (checkDel == null)
                        Delegate("classroom", course.id);
                }
            }

            return true;
        }

        public bool DelegateDepartment2TermCourses(char depid)
        {
            //Faculty of Contemporary Sciences and Technologies - N-CST -> C    cstfaculty@seeu.edu.mk
            //Faculty of Languages, Cultures and Communication - N-TT -> T      lccfaculty@seeu.edu.mk
            //Faculty of Law - N-LAW -> L       lawfaculty@seeu.edu.mk
            //Faculty of Public Administration and Political Sciences - N-PA -> P   papsfaculty@seeu.edu.mk
            //Faculty of Business and Economics - N-BA -> B     befaculty@seeu.edu.mk
            //Language Centre - LC -> Q                         lc@seeu.edu.mk
            string username = string.Empty;
            switch (depid)
            {
                case 'C': username = "cstfaculty"; break;
                case 'T': username = "lccfaculty"; break;
                case 'L': username = "lawfaculty"; break;
                case 'P': username = "papsfaculty"; break;
                case 'B': username = "befaculty"; break;
                case 'Q': username = "lc"; break;
            }

            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid && kursi.depid == depid.ToString()).ToList();

            //fillon filtrimi i kurseve sipas rolit te userit
            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                //i merr te gjitha kurset e semestrit aktual
                foreach (Kursi course in listaKurseve)
                {
                    //kontrollo a eshte i deleguar apo jo
                    var checkDel = db.CourseDelegations.Where(cd => cd.username == username && cd.kursiid == course.id).SingleOrDefault();
                    //bej delegim nese nuk ka
                    if (checkDel == null)
                        Delegate(username, course.id);
                }
            }

            return true;
        }

        #endregion

        #region 3. Feeds SYNC

        //dashboard/SyncTermCoursesFeeds
        public bool SyncTermCoursesFeeds()
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            //fillon filtrimi i kurseve sipas rolit te userit
            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                //i merr te gjitha kurset e semestrit aktual
                foreach (Kursi course in listaKurseve)
                {
                    SyncCourseFeeds(course.id);
                }
            }

            return true;
        }
        //cdo dite
        public void SyncCourseFeeds(string id)
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            List<Feed> listaFeeds = new List<Feed>();

            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            //get course details
            var courseRq = clservice.Courses.Get(id);
            try
            {
                Course course = courseRq.Execute();

                //get course materials = "driveFile" "youTubeVideo" "link" "form"
                if (course.CourseMaterialSets != null)
                    foreach (var matset in course.CourseMaterialSets)
                    {
                        foreach (var mat in matset.Materials)
                        {
                            if (mat.DriveFile != null)
                            {
                                //DriveService cldrv = 
                                Feed newFeed = new Feed
                                {
                                    alternateLink = mat.DriveFile.AlternateLink,
                                    courseId = id,
                                    title = mat.DriveFile.Title,
                                    updateTime = new DateTime(2001, 1, 1),
                                    workType = "Drive File",
                                    matSetName = matset.Title,
                                    id = mat.DriveFile.Id,
                                    ThumbnailUrl = mat.DriveFile.ThumbnailUrl
                                };
                                listaFeeds.Add(newFeed);
                            }
                            else if (mat.Form != null)
                            {
                                Feed newFeed = new Feed
                                {
                                    id = Guid.NewGuid().ToString(),
                                    alternateLink = mat.Form.FormUrl,
                                    courseId = id,
                                    title = mat.Form.Title,
                                    updateTime = new DateTime(2001, 1, 1),
                                    workType = "Form",
                                    matSetName = matset.Title,
                                    ThumbnailUrl = mat.Form.ThumbnailUrl,
                                    ResponseUrl = mat.Form.ResponseUrl
                                };
                                listaFeeds.Add(newFeed);
                            }
                            else if (mat.Link != null)
                            {
                                Feed newFeed = new Feed
                                {
                                    id = Guid.NewGuid().ToString(),
                                    alternateLink = mat.Link.Url,
                                    courseId = id,
                                    title = mat.Link.Title,
                                    updateTime = new DateTime(2001, 1, 1),
                                    workType = "Link",
                                    matSetName = matset.Title
                                };
                                listaFeeds.Add(newFeed);
                            }
                        }
                    }

                //get course works = ASSIGNMENT SHORT_ANSWER_QUESTION MULTIPLE_CHOICE_QUESTION COURSE_WORK_TYPE_UNSPECIFIED
                var courseWorksRq = clservice.Courses.CourseWork.List(course.Id);
                ListCourseWorkResponse courseWorksRs = courseWorksRq.Execute();
                if (courseWorksRs.CourseWork != null)
                {
                    foreach (CourseWork cw in courseWorksRs.CourseWork)
                    {
                        Feed newFeed = new Feed
                        {
                            id = cw.Id,
                            alternateLink = cw.AlternateLink,
                            courseId = cw.CourseId,
                            description = cw.Description,
                            title = cw.Title,
                            updateTime = DateTime.Parse(cw.UpdateTime),
                            workType = cw.WorkType
                        };
                        listaFeeds.Add(newFeed);
                    }
                }
                else
                {
                    //Debug.WriteLine("No courses found.");
                }

                //beje tash SINKRONIZIMIN
                //per cdo feed ne liste bej sinkronizim
                foreach (var feed in listaFeeds)
                {
                    //kontrollo a eshte feed-i ne liste
                    var c = db.Feeds.Where(f => f.id.Equals(feed.id)).SingleOrDefault();
                    if (c == null)
                    {
                        try
                        {
                            db.Feeds.InsertOnSubmit(feed);
                            db.SubmitChanges();
                        }
                        catch (Exception ex) { }
                    }
                }
            }
            catch (Exception ex) { }
        }

        #endregion

        #region 4. Course Stats SYNC

        //sync course STATS
        public bool SyncGenerateALLCoursesStats()
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            //get all term courses from DB and generate stats one by one
            var courses = db.Kursis.Where(k => k.termid.Equals(termid)).ToList();
            foreach (var c in courses)
            {
                SyncGenerateCourseStats(c.id);
            }
            return true;
        }

        //sync one course stats
        public void SyncGenerateCourseStats(string id)
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];
            //get course details
            var courseRq = clservice.Courses.Get(id);

            try
            {
                Course course = courseRq.Execute();

                //krijohet nje instance statistike
                Stat newCourseStat = new Stat();
                newCourseStat.courseId = course.Id; newCourseStat.userId = course.OwnerId;

                //kontrollo a ka syllabus ne ABOUT tabin ose ne RESOURCE
                //RESOURCE = "driveFile" "youTubeVideo" "link" "form"
                if (course.CourseMaterialSets != null)
                {
                    newCourseStat.hasResources = true;
                    foreach (var matset in course.CourseMaterialSets)
                    {
                        newCourseStat.nrResources += matset.Materials.Count;  //shto nr e materialeve ne Resources
                        //1. ne emrin e matsetit
                        string matsetName = matset.Title.ToLower();
                        if (matsetName.Contains("sillab") || matsetName.Contains("syllab") ||
                            matsetName.Contains("silab") || matsetName.Contains("sylab") ||
                            matsetName.Contains("plan") || matsetName.Contains("program"))
                            newCourseStat.hasSyllabus = true;
                        //2. ne cdo Drive fajll te matsetit
                        foreach (var mat in matset.Materials)
                        {
                            if (mat.DriveFile != null)
                                if (mat.DriveFile.Title != null)
                                {
                                    //var driveFileRq = drvservice.Files.List();
                                    //var files = driveFileRq.Execute();

                                    string fileName = mat.DriveFile.Title.ToLower();
                                    if (fileName.Contains("sillab") || fileName.Contains("syllab") ||
                                        fileName.Contains("silab") || fileName.Contains("sylab") ||
                                        fileName.Contains("plan") || fileName.Contains("program"))
                                        newCourseStat.hasSyllabus = true;

                                }
                        }
                    }
                }

                ////kontrollo a ka sillabus ne STREAM tabin si announcement
                //var courseAnnouncements = clservice.Courses.List(course.Id);

                //ASSIGNMENTS = ASSIGNMENT SHORT_ANSWER_QUESTION MULTIPLE_CHOICE_QUESTION COURSE_WORK_TYPE_UNSPECIFIED
                var courseWorksRq = clservice.Courses.CourseWork.List(course.Id);
                ListCourseWorkResponse courseWorksRs = courseWorksRq.Execute();
                if (courseWorksRs.CourseWork != null)
                {
                    newCourseStat.hasAssignments = true;
                    foreach (CourseWork cw in courseWorksRs.CourseWork)
                    {
                        newCourseStat.nrAssignments += 1; //shto 1 ne Assignments
                    }
                    //set hasDiffAssignmentTypes
                    if (newCourseStat.nrAssignments > 1)
                    {
                        string[] assignTypes = new string[20]; int i = 0;
                        foreach (CourseWork cw in courseWorksRs.CourseWork)
                        {
                            assignTypes[i] = cw.WorkType;
                        }
                        int nrOfDiffTypesAssignments = assignTypes.Distinct().Count();
                        if (nrOfDiffTypesAssignments > 1)
                            newCourseStat.hasDiffAssignmentTypes = true;
                    }
                }

                //Course Level
                //0 -> bosh, 1 -> hasSyllabus = true + nrResources(+12), 2.1 -> L1 + nrAssignments (1), 2.2 -> L2.1 + nrAssignments(>=2 different)
                if (newCourseStat.hasSyllabus && newCourseStat.nrResources > 11)
                {
                    newCourseStat.courseLevel = 1;
                    if (newCourseStat.nrAssignments > 0)
                    {
                        newCourseStat.courseLevel = 21;
                        if (newCourseStat.nrAssignments > 1 && newCourseStat.hasDiffAssignmentTypes)
                            newCourseStat.courseLevel = 22;
                    }
                }


                //get number of students
                var studentsRq = clservice.Courses.Students.List(id);
                ListStudentsResponse studentsLs = studentsRq.Execute();
                if (studentsLs.Students != null)
                    newCourseStat.nrStudents = studentsLs.Students.Count();

                //store into DB
                var newDBCstats = new CourseStat()
                {
                    id = Guid.NewGuid(),
                    courseId = newCourseStat.courseId,
                    courseLevel = newCourseStat.courseLevel,
                    hasAssignments = newCourseStat.hasAssignments,
                    hasResources = newCourseStat.hasResources,
                    hasSyllabus = newCourseStat.hasSyllabus,
                    nrResources = newCourseStat.nrResources,
                    nrAssignments = newCourseStat.nrAssignments,
                    nrStreams = newCourseStat.nrStreams,
                    nrStudents = newCourseStat.nrStudents,
                    userid = newCourseStat.userId
                };
                //check if course stats exists if so update data else add new course stats
                CourseStat courseStatsExists = db.CourseStats.Where(cs => cs.courseId.Equals(newDBCstats.courseId)).SingleOrDefault();
                if (courseStatsExists != null)
                {   //update stats
                    courseStatsExists.courseLevel = newCourseStat.courseLevel;
                    courseStatsExists.hasAssignments = newCourseStat.hasAssignments;
                    courseStatsExists.hasResources = newCourseStat.hasResources;
                    courseStatsExists.hasSyllabus = newCourseStat.hasSyllabus;
                    courseStatsExists.nrResources = newCourseStat.nrResources;
                    courseStatsExists.nrAssignments = newCourseStat.nrAssignments;
                    courseStatsExists.nrStreams = newCourseStat.nrStreams;
                    courseStatsExists.nrStudents = newCourseStat.nrStudents;
                    db.SubmitChanges();
                }
                else //insert new row
                {
                    db.CourseStats.InsertOnSubmit(newDBCstats);
                    db.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                //throw e;
            }
            //return PartialView("_Stats", newCourseStat);
        }


        #endregion

        #region 5. Reportings SYNC

        public bool SyncGenerateReports()
        {
            var all = SyncGenerateReportsALLDEPS();
            //Thread.Sleep(10000);
            var cst = SyncGenerateReportsDepartment("C");
            //Thread.Sleep(10000);
            var soc = SyncGenerateReportsDepartment("S");
            //Thread.Sleep(10000);
            var ark = SyncGenerateReportsDepartment("N");
            //Thread.Sleep(10000);
            var teknolo = SyncGenerateReportsDepartment("K");
            //Thread.Sleep(10000);
            var tek = SyncGenerateReportsDepartment("T");

            if (all && cst && soc && tek && teknolo && ark)
                return true;
            else
                return false;
        }

        public bool SyncGenerateReportsALLDEPS()
        {
            //string seeutermid = ConfigurationManager.AppSettings["SEEUCurrentTerm"];
            bool result = false;
            //regjistro sesionin e userit
            if (Session["user"] == null)
            {
                RegisterSession();
            }
            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            // Define request parameters.
            //CoursesResource.ListRequest request = clservice.Courses.List();
            //request.PageSize = 5;
            //request.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];
            // List courses.
            //ListCoursesResponse response = request.Execute();

            Reporting model = new Reporting
            {
                nrLendeMeSyllPaMat = 0,
                perqindjaKurseveMeMateriale = 0,
                totalDetyra = 0,
                totalDetyraJavaFundit = 0,
                totalLende = 0,
                totalLendePaMateriale = 0,
                totalMateriale = 0,
                totalMaterialeJavaFundit = 0
            };

            KursiMeMateriale nrKurseveMeMaterialePerMuaj = new KursiMeMateriale
            {
                jan = 0,
                feb = 0,
                mar = 0,
                apr = 0,
                may = 0,
                jul = 0,
                jun = 0,
                aug = 0,
                sep = 0,
                oct = 0,
                nov = 0,
                dec = 0
            };

            int nrKurseveMeMateriale = 0; int nrKurseveUserit = 0;
            string courseDepartment;
            //if (response.Courses != null && response.Courses.Count > 0)
            //{   //i merr te gjitha kurset e semestrit aktual

            foreach (var kurs in db.Kursis.Where(k => k.termid == termid))
            {
                try
                {
                    courseDepartment = kurs.depid;

                    nrKurseveUserit++;

                    var request = clservice.Courses.Get(kurs.id);
                    Course course = request.Execute();

                    //shto file-at, form-at dhe linqet
                    if (course.CourseMaterialSets != null)
                        model.totalMateriale += course.CourseMaterialSets.Count();

                    //shto assignments multiple choice questions
                    var materialeRq = clservice.Courses.CourseWork.List(course.Id);
                    ListCourseWorkResponse materialet = materialeRq.Execute();
                    if (course.CourseMaterialSets != null || materialet.CourseWork != null)
                        nrKurseveMeMateriale += 1;
                    if (materialet.CourseWork != null)
                    {
                        model.totalMateriale += materialet.CourseWork.Count();
                        //get the date of the first material published
                        var dataeFirstMat = materialet.CourseWork.Min(m => m.UpdateTime);
                        DateTime konvert = DateTime.Parse(dataeFirstMat);
                        var muaji = konvert.Month;
                        switch (muaji)
                        {
                            case 1: nrKurseveMeMaterialePerMuaj.jan += 1; break;
                            case 2: nrKurseveMeMaterialePerMuaj.feb += 1; break;
                            case 3: nrKurseveMeMaterialePerMuaj.mar += 1; break;
                            case 4: nrKurseveMeMaterialePerMuaj.apr += 1; break;
                            case 5: nrKurseveMeMaterialePerMuaj.may += 1; break;
                            case 6: nrKurseveMeMaterialePerMuaj.jun += 1; break;
                            case 7: nrKurseveMeMaterialePerMuaj.jul += 1; break;
                            case 8: nrKurseveMeMaterialePerMuaj.aug += 1; break;
                            case 9: nrKurseveMeMaterialePerMuaj.sep += 1; break;
                            case 10: nrKurseveMeMaterialePerMuaj.oct += 1; break;
                            case 11: nrKurseveMeMaterialePerMuaj.nov += 1; break;
                            case 12: nrKurseveMeMaterialePerMuaj.dec += 1; break;
                        }

                        //statistika per assignments 
                        foreach (var work in materialet.CourseWork.OrderByDescending(w => w.UpdateTime))
                        {
                            DateTime tani = DateTime.Now;
                            DateTime njeJavePara = tani.AddDays(-7);
                            if (DateTime.Parse(work.UpdateTime) > njeJavePara)
                            {
                                model.totalMaterialeJavaFundit += 1;
                                if (work.WorkType == "ASSIGNMENT")
                                    model.totalDetyraJavaFundit += 1;
                            }
                            if (work.WorkType == "ASSIGNMENT")
                                model.totalDetyra += 1;
                        }
                    }
                }
                catch (Exception e)
                { }
            }//perfundon perpunimi i te dhenave nga kurset


            int perqindjaKurseveMe = nrKurseveMeMateriale * 100 / nrKurseveUserit;
            model.perqindjaKurseveMeMateriale = perqindjaKurseveMe;
            //model.nrKursetMeMaterialePerMuaj = nrKurseveMeMaterialePerMuaj;
            model.totalLende = nrKurseveUserit;
            model.totalLendePaMateriale = nrKurseveUserit - nrKurseveMeMateriale;
            model.department = Session["dep"].ToString();
            model.timeStamp = DateTime.Now;
            model.termid = termid;

            model.id = Guid.NewGuid();
            try
            {
                db.Reportings.InsertOnSubmit(model);
                db.SubmitChanges();
                nrKurseveMeMaterialePerMuaj.id = Guid.NewGuid();
                nrKurseveMeMaterialePerMuaj.reportingid = model.id;
                db.KursiMeMateriales.InsertOnSubmit(nrKurseveMeMaterialePerMuaj);
                db.SubmitChanges();
                result = true;
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public bool SyncGenerateReportsDepartment(string departmentID)
        {
            //string seeutermid = ConfigurationManager.AppSettings["SEEUCurrentTerm"];
            bool result = false;
            //regjistro sesionin e userit
            if (Session["user"] == null)
            {
                RegisterSession();
            }
            //autorizim per Google token
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            // Define request parameters.
            //CoursesResource.ListRequest request = clservice.Courses.List();
            //request.PageSize = 5;
            //request.QuotaUser = ConfigurationManager.AppSettings["AdminUserAccount"];
            // List courses.
            //ListCoursesResponse response = request.Execute();

            Reporting model = new Reporting()
            {
                nrLendeMeSyllPaMat = 0,
                perqindjaKurseveMeMateriale = 0,
                totalDetyra = 0,
                totalDetyraJavaFundit = 0,
                totalLende = 0,
                totalLendePaMateriale = 0,
                totalMateriale = 0,
                totalMaterialeJavaFundit = 0
            };

            KursiMeMateriale nrKurseveMeMaterialePerMuaj = new KursiMeMateriale()
            {
                jan = 0,
                feb = 0,
                mar = 0,
                apr = 0,
                may = 0,
                jul = 0,
                jun = 0,
                aug = 0,
                sep = 0,
                oct = 0,
                nov = 0,
                dec = 0
            };

            int nrKurseveMeMateriale = 0; int nrKurseveUserit = 0;
            string courseDepartment;
            //if (response.Courses != null && response.Courses.Count > 0)
            //{   //i merr te gjitha kurset e semestrit aktual
            foreach (var kursi in db.Kursis.Where(k => k.termid == termid))
            {
                try
                {
                    courseDepartment = kursi.depid;
                    //kaperce kurset qe nuk i perkasin departamentit
                    if (courseDepartment == departmentID)
                    {
                        nrKurseveUserit++;
                        var request = clservice.Courses.Get(kursi.id);
                        Course course = request.Execute();
                        //shto file-at, form-at dhe linqet
                        if (course.CourseMaterialSets != null)
                            model.totalMateriale += course.CourseMaterialSets.Count();

                        //shto assignments multiple choice questions
                        var materialeRq = clservice.Courses.CourseWork.List(course.Id);
                        ListCourseWorkResponse materialet = materialeRq.Execute();
                        if (course.CourseMaterialSets != null || materialet.CourseWork != null)
                            nrKurseveMeMateriale += 1;
                        if (materialet.CourseWork != null)
                        {
                            model.totalMateriale += materialet.CourseWork.Count();
                            //get the date of the first material published
                            var dataeFirstMat = materialet.CourseWork.Min(m => m.UpdateTime);
                            DateTime konvert = DateTime.Parse(dataeFirstMat);
                            var muaji = konvert.Month;
                            switch (muaji)
                            {
                                case 1: nrKurseveMeMaterialePerMuaj.jan += 1; break;
                                case 2: nrKurseveMeMaterialePerMuaj.feb += 1; break;
                                case 3: nrKurseveMeMaterialePerMuaj.mar += 1; break;
                                case 4: nrKurseveMeMaterialePerMuaj.apr += 1; break;
                                case 5: nrKurseveMeMaterialePerMuaj.may += 1; break;
                                case 6: nrKurseveMeMaterialePerMuaj.jun += 1; break;
                                case 7: nrKurseveMeMaterialePerMuaj.jul += 1; break;
                                case 8: nrKurseveMeMaterialePerMuaj.aug += 1; break;
                                case 9: nrKurseveMeMaterialePerMuaj.sep += 1; break;
                                case 10: nrKurseveMeMaterialePerMuaj.oct += 1; break;
                                case 11: nrKurseveMeMaterialePerMuaj.nov += 1; break;
                                case 12: nrKurseveMeMaterialePerMuaj.dec += 1; break;
                            }

                            //statistika per assignments 
                            foreach (var work in materialet.CourseWork.OrderByDescending(w => w.UpdateTime))
                            {
                                DateTime tani = DateTime.Now;
                                DateTime njeJavePara = tani.AddDays(-7);
                                if (DateTime.Parse(work.UpdateTime) > njeJavePara)
                                {
                                    model.totalMaterialeJavaFundit += 1;
                                    if (work.WorkType == "ASSIGNMENT")
                                        model.totalDetyraJavaFundit += 1;
                                }
                                if (work.WorkType == "ASSIGNMENT")
                                    model.totalDetyra += 1;
                            }
                        }
                    }
                }
                catch (Exception ex) { }
            }//perfundon perpunimi i te dhenave nga kurset

            model.id = Guid.NewGuid();
            int perqindjaKurseveMe = nrKurseveMeMateriale * 100 / nrKurseveUserit;
            model.perqindjaKurseveMeMateriale = perqindjaKurseveMe;
            //model.nrKursetMeMaterialePerMuaj = nrKurseveMeMaterialePerMuaj;
            model.totalLende = nrKurseveUserit;
            model.totalLendePaMateriale = nrKurseveUserit - nrKurseveMeMateriale;
            model.department = departmentID;
            model.timeStamp = DateTime.Now;
            model.termid = termid;

            //db insert
            try
            {
                db.Reportings.InsertOnSubmit(model);
                db.SubmitChanges();
                nrKurseveMeMaterialePerMuaj.id = Guid.NewGuid();
                nrKurseveMeMaterialePerMuaj.reportingid = model.id;
                db.KursiMeMateriales.InsertOnSubmit(nrKurseveMeMaterialePerMuaj);
                db.SubmitChanges();
                result = true;
            }
            catch (Exception ex) { }
            return result;
        }

        #endregion

        #endregion

        #region AddAnnouncements

        //shto nje announcement ne Stream ne Classroom
        public bool addAnnouncementOnEveryCourse()
        {
            //per current term
            string termid = ConfigurationManager.AppSettings["CurrentTerm"];
            ViewBag.termid = termid;

            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();

            List<CourseViewModel> listaKurseveResult = new List<CourseViewModel>();
            List<Kursi> listaKurseve = db.Kursis.Where(kursi => kursi.termid == termid).ToList();

            //fillon filtrimi i dashboardit sipas rolit te userit
            if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
            {
                foreach (Kursi course in listaKurseve)
                {
                    addAnnouncementToOneCourse(course.id);
                }
            }
            return true;
        }

        //shto nje announcement ne Classroom
        public void addAnnouncementToOneCourse(string courseid)
        {
            //regjistro sesionin nese nuk eshte aktiv
            if (Session["user"] == null) RegisterSession();
            else
            {
                RegisterClassroomSvc();
                ClassroomService clservice = (ClassroomService)Session["clservice"];

                Kursi kursi = db.Kursis.Where(k => k.id == courseid).SingleOrDefault();

                //fillon filtrimi i dashboardit sipas rolit te userit
                if (Session["role"].ToString() == "libriadmin" || Session["role"].ToString() == "authorized")
                {

                    var owner = clservice.UserProfiles.Get(kursi.ownerid).Execute();
                    var instructorName = owner.Name.FullName;
                    var courseName = kursi.title;
                    var linkTitle = "Reminder: Course Evaluation Feedback";
                    //var ownerUsername = new MailAddress(ownerEmail).User;
                    Google.Apis.Classroom.v1.Data.TimeOfDay dueTime = new TimeOfDay()
                    {
                        Hours = 0,
                        Minutes = 0,
                        Seconds = 0
                    };
                    Google.Apis.Classroom.v1.Data.Date dueDate = new Date()
                    {
                        Day = 3,
                        Month = 6,
                        Year = 2019
                    };
                    try
                    {

                        List<Material> linkuFormes = new List<Material>()
                    {
                        new Material()
                        {
                            Link = new Link { 
                                Url = "https://libri3.seeu.edu.mk/CourseEvaluation/Evaluation.htm?entry.1200489450=" + courseName +
                                "&entry.1908394128=" + instructorName + 
                                "&entry.1483706407=" + kursi.coursecode,
                                Title = linkTitle }
                        }
                    };

                        //krijo courseWork resurs
                        CourseWork res = new CourseWork()
                        {
                            CourseId = kursi.id,
                            State = "PUBLISHED",
                            WorkType = "ASSIGNMENT",
                            Title = linkTitle,
                            Description = "Të nderuar studentë, \nJu lutem, të bëni vlerësimin e lëndëve tuaja deri më 3 Qershor, duke u kyçur në Google Classroom. Ky aktivitet është i menaxhuar nga e-Learning Center duke përdorur formular nga Google dhe është plotësisht anonim. Qëllimi  i këtij vlerësimi është të na ofroj informata në lidhje me lëndën dhe mësimdhënësin si dhe të identifikohen fushat që mund të përmirësohen. Ju sugjerojmë ta plotësoni formularin prej në kompjuter, laptop, android ose iPhone (në iPhone formulari do të hapet vetëm nëse e hapni përmes browserit) me qëllim që ta keni më të lehtë për ta plotësuar. \nNe do t’u jemi mirënjohës nëse mund ta realizoni këtë." +
                            "\n--\n Почитувани студенти, \nВе потсетуваме дека имате можност анонимно да ги евалуирате предметите до почнувањето на испитната сесија преку Google Classroom. Оваа активност  има за цел да ни обезбеди информации за предметот и предавачот и можни области за подобрување и затоа ќе ви бидеме многу благодарни ако ја направите евалуацијата. \nЗа полесен пристап, ве молиме да користите персонален или деск компјутер, Андроид или I Phone преку browser." +
                            "\n--\n Dear Students, \nPlease be reminded that you can anonymously evaluate your courses till the start of the exam session in June through Google Classroom. The aim of this evaluation is to provide information about the course and the instructor and possible areas for improvement and we will be very grateful if you could do it. \nFor more user friendly experience, you can access the form via Laptop/PC, Android and I Phone (through browser). ",
                            Materials = linkuFormes,
                            DueTime = dueTime,
                            DueDate = dueDate
                        };
                        // Define request parameters.
                        var request = clservice.Courses.CourseWork.Create(res, kursi.id);
                        request.QuotaUser = ConfigurationManager.AppSettings["userAccount"];

                        // List courses.
                        var response = request.Execute();

                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                //return true;
            }
        }

        #endregion

        #region EXPORTS

        public ActionResult ExportSyllabusToExcel(bool mepa, string termid)
        {
            var grid = new System.Web.UI.WebControls.GridView();

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

            grid.DataSource = listaKurseve.courses;
            grid.DataBind();

            Response.ClearContent();
            string exportFileName = "attachment; filename=" + (mepa == true ? "With" : "Without") + "_Syllabus_" + DateTime.Now.ToShortDateString().Replace(@"/", "") + ".xls";
            Response.AddHeader("content-disposition", exportFileName);
            Response.ContentType = "application/vnd.ms-excel";
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            grid.RenderControl(htw);

            Response.Write(sw.ToString());

            Response.End();

            return null;
        }

        public ActionResult ExportAllCoursesStats(string termid)
        {

            //response = null;
            List<AllCourseStats> listaStatistikave = new List<AllCourseStats>();
            List<Kursi> allTermCourses = new List<Kursi>();
            if (Session["dep"].ToString() == "A" || Session["dep"].ToString() == "R")
                allTermCourses = db.Kursis.Where(k => k.termid.Equals(termid)).ToList();
            else
                allTermCourses = db.Kursis.Where(k => k.termid.Equals(termid) && k.depid.Equals(Session["dep"].ToString())).ToList();
            foreach (Kursi course in allTermCourses)
            {
                //krijohet nje instance statistike
                AllCourseStats newCourseStat = new AllCourseStats();
                newCourseStat.Course = course.title;
                //newCourseStat.LastUpdated = course.updatetime;
                newCourseStat.CourseCode = course.coursecode;

                //var owner = db.GoogleUsers.Where(u => u.GoogleID.Equals(course.ownerid)).SingleOrDefault();
                //newCourseStat.Instructor = owner.Fullname;

                List<LibriTeacher> courseInstr = GetCourseInstructors(course.id);
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
                newCourseStat.Instructor = instructors;

                var kursStats = db.CourseStats.Where(g => g.courseId.Equals(course.id)).SingleOrDefault();
                if (kursStats != null)
                {
                    newCourseStat.Resources = (kursStats.nrResources == null ? 0 : Int32.Parse(kursStats.nrResources.ToString()));
                    newCourseStat.Level = (kursStats.courseLevel == null ? 0 : Int32.Parse(kursStats.courseLevel.ToString()));
                    newCourseStat.Students = (kursStats.nrStudents == null ? 0 : Int32.Parse(kursStats.nrStudents.ToString()));
                    newCourseStat.Assignments = (kursStats.nrAssignments == null ? 0 : Int32.Parse(kursStats.nrAssignments.ToString()));
                    newCourseStat.Streams = (kursStats.nrStreams == null ? 0 : Int32.Parse(kursStats.nrStreams.ToString()));
                    newCourseStat.hasSyllabus = (kursStats.hasSyllabus == null ? 0 : Convert.ToInt32(kursStats.hasSyllabus));
                }

                //department spec
                switch (course.depid)
                {
                    case "E": newCourseStat.Department = "ELC"; break;
                    case "C": newCourseStat.Department = "CST"; break;
                    case "T": newCourseStat.Department = "LCC"; break;
                    case "L": newCourseStat.Department = "LAW"; break;
                    case "P": newCourseStat.Department = "PAPS"; break;
                    case "B": newCourseStat.Department = "BA"; break;
                    case "Q": newCourseStat.Department = "LC"; break;
                    default: newCourseStat.Department = "N/A"; break;
                }

                listaStatistikave.Add(newCourseStat);
            }

            var grid = new System.Web.UI.WebControls.GridView();
            grid.DataSource = listaStatistikave;
            grid.DataBind();

            Response.ClearContent();
            string exportFileName = "attachment; filename=" + "AllCoursesStats" + DateTime.Now.ToShortDateString().Replace(@"/", "") + ".xls";
            Response.AddHeader("content-disposition", exportFileName);
            Response.ContentType = "application/vnd.ms-excel";
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            grid.RenderControl(htw);

            Response.Write(sw.ToString());

            Response.End();

            return null;
        }

        #endregion

        #region Helpers

        #region Register Classroom Service

        //UNT - classroom account
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
                            //DriveService.Scope.Drive, 
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

        //UNT - classroom account
        private void RegisterUNTClassroomSvc()
        {
            //autorizim per Google token
            ClassroomService clservice;
            if (Session["clservice"] == null)
            {
                //var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
                //    AuthorizeAsync(cancellationToken);

                String untServiceAccountEmail = ConfigurationManager.AppSettings["untServiceAccount"];//serviceAccount
                String untUserAccountEmail = ConfigurationManager.AppSettings["untUserAccount"];//userAccount

                var certificate = new X509Certificate2(Server.MapPath(@"~/resources/untkey.p12"), "notasecret", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);//elckey

                ServiceAccountCredential credential = new ServiceAccountCredential(
                   new ServiceAccountCredential.Initializer(untServiceAccountEmail)
                   {
                       User = untUserAccountEmail,
                       Scopes = new[] { 
                            //DriveService.Scope.Drive, 
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
                    ApplicationName = "UNT Classroom App"
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

        
        private ClassroomService RegisterUNTAdminClassroomSvc()
        {
            //autorizim per Google token
            ClassroomService clservice;

            String ServiceAccountEmail = ConfigurationManager.AppSettings["ServiceAccount"];//serviceAccount
            String UserAccountEmail = ConfigurationManager.AppSettings["UserAccount"];//userAccount

            var certificate = new X509Certificate2(Server.MapPath(@"~/resources/untkey.p12"), "notasecret", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);//elckey

            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(ServiceAccountEmail)
               {
                   User = ServiceAccountEmail,
                   Scopes = new[] { 
                            //DriveService.Scope.Drive, 
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
                ApplicationName = "UNT Admin Classroom App"
            });
            return clservice;
        }

        private ClassroomService RegisterELCClassroomSvc()
        {
            //autorizim per Google token
            ClassroomService elcclservice;
            //if (Session["elcclservice"] == null)
            //{
            //var result = await new AuthorizationCodeMvcApp(this, new AppFlowMetadata()).
            //    AuthorizeAsync(cancellationToken);

            String serviceAccountEmail = ConfigurationManager.AppSettings["serviceAccount"];//serviceAccount
            String userAccountEmail = ConfigurationManager.AppSettings["userAccount"];//userAccount

            var certificate = new X509Certificate2(Server.MapPath(@"~/resources/elckey.p12"), "notasecret", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);//elckey

            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(serviceAccountEmail)
               {
                   User = userAccountEmail,
                   Scopes = new[] { 
                            //DriveService.Scope.Drive, 
                            ClassroomService.Scope.ClassroomCourses,
                            ClassroomService.Scope.ClassroomCourseworkMe,
                            //ClassroomService.Scope.ClassroomAnnouncements,
                            ClassroomService.Scope.ClassroomCourseworkStudents,
                            ClassroomService.Scope.ClassroomProfileEmails,
                            ClassroomService.Scope.ClassroomProfilePhotos,
                            ClassroomService.Scope.ClassroomRosters
                       }
               }.FromCertificate(certificate));

            //if (result.Credential != null)
            //{
            // Create Classroom API service.
            elcclservice = new ClassroomService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Libri Classroom"
            });

            //regjistro sesionin per aksione ne Classroom
            Session["elcclservice"] = elcclservice;
            //}
            //else
            //{
            //    return new RedirectResult(result.RedirectUri);
            //}
            return elcclservice;
            //}
            //else
            //{
            //    clservice = (ClassroomService)Session["elcclservice"];
            //}
        }

        public List<LibriTeacher> GetCourseInstructors(string courseId)
        {
            //merri teachers of the course
            var courseTeachers = db.CourseDelegations
                .Join(db.GoogleUsers,
                    del => del.username,
                    guser => guser.Username,
                    (del, guser) => new { delegation = del, GUser = guser })
                .Where(cc => cc.delegation.kursiid == courseId).ToList();

            List<LibriTeacher> courseTeachersResult = new List<LibriTeacher>();

            foreach (var teacher in courseTeachers)
            {
                LibriTeacher newt = new LibriTeacher
                {
                    GoogleId = teacher.delegation.userid,
                    Username = teacher.delegation.username,
                    TeacherName = teacher.GUser.Fullname,
                    TeacherEmail = teacher.GUser.Username + "@" + ConfigurationManager.AppSettings["Domain"]
                };

                courseTeachersResult.Add(newt);
            }
            return courseTeachersResult;
        }

        #endregion

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
            //        //nese seshte aktiv sesioni i rolit
            //        if (Session["role"] == null)
            //        {
            //            //regjgistro rolin nese eshte priv.user
            //            var privUser = db.LibriDepDeans.Where(u => u.username == username).SingleOrDefault();
            //            Session["dep"] = privUser.code;     //E, C, T, L, P, B, Q, R & A, while O-none
            //            if (privUser.code == "A")
            //                Session["role"] = "libriadmin";Session["dep"] = "A";
            //            else if (privUser.code == "R")
            //                Session["role"] = "rectorate";
            //            else Session["role"] = "authorized";     //authorized -> high mngmnt and deans/directors 
            //            //libriadmin -> admins

            //            //Faculty of Contemporary Sciences and Technologies - N-CST -> C
            //            //Faculty of Languages, Cultures and Communication - N-TT -> T
            //            //Faculty of Law - N-LAW -> L
            //            //Faculty of Public Administration and Political Sciences - N-PA -> P
            //            //Faculty of Business and Economics - N-BA -> B
            //            //Language Centre - LC -> Q
            //            //E-Learning Centre - ELC -> E
            //            //Administrators -> A
            //            //Rectorate -> R
            //        }
            //    }
            //}
            Session["role"] = "libriadmin"; Session["dep"] = "A"; Session["user"] = "admin";
        }

        #endregion

        #region Export Current Term Courses to Excel

        public ActionResult ExportCoursesToExcel()
        {
            string termid = ConfigurationManager.AppSettings["CurrentTerm"];
            RegisterClassroomSvc();
            ClassroomService clservice = (ClassroomService)Session["clservice"];

            var grid = new System.Web.UI.WebControls.GridView();

            //merri kurset
            CoursesResource.ListRequest request = clservice.Courses.List();
            //request.PageSize = 50;

            // List courses.
            ListCoursesResponse response = request.Execute();
            List<CourseStatsVM> listaKurseve = new List<CourseStatsVM>();
            //Console.WriteLine("Courses:");
            if (response.Courses != null && response.Courses.Count > 0)
            {   //i merr te gjitha kurset
                foreach (Course course in response.Courses.Where(k => GetCourseTerm(k.Section) == termid))
                {
                    CourseStatsVM c = new CourseStatsVM
                    {
                        Id = course.Id,
                        Title = course.Name,
                        Section = course.Section,
                        CreationTime = DateTime.Parse(course.CreationTime),
                        UpdateTime = DateTime.Parse(course.UpdateTime)
                    };

                    //merri teachers of the course
                    var teachersRq = clservice.Courses.Teachers.List(course.Id);
                    ListTeachersResponse teachersLs = teachersRq.Execute();
                    List<string> courseTeachers = new List<string>();
                    string lsTeachers = ""; int nr = 0;
                    foreach (var teacher in teachersLs.Teachers)
                    {
                        //LibriTeacher newt = new LibriTeacher
                        //{
                        //    UserId = teacher.UserId,
                        //    TeacherName = teacher.Profile.Name.FullName,
                        //    TeacherEmail = teacher.Profile.EmailAddress
                        //};
                        //courseTeachers.Add(teacher.Profile.Name.FullName);
                        if (!teacher.Profile.Name.FullName.Equals("E-Learning Centre SEEU"))
                        {
                            lsTeachers = lsTeachers + teacher.Profile.Name.FullName;
                            nr++;
                            if (nr >= teachersLs.Teachers.Count())
                                lsTeachers += ", ";
                        }
                    }
                    c.InvitedTeachers = lsTeachers;
                    var materileRq = clservice.Courses.CourseWork.List(course.Id);
                    ListCourseWorkResponse materialet = materileRq.Execute();
                    //mbush kurset bosh
                    c.IsCourseEmpty = true;
                    if (materialet.CourseWork != null)
                    {
                        if (materialet.CourseWork.Count() > 0)
                        {
                            c.IsCourseEmpty = false;
                        }
                    }

                    if (course.CourseMaterialSets != null)
                    {
                        if (course.CourseMaterialSets.Count() > 0)
                        {
                            c.IsCourseEmpty = false;
                        }
                    }
                    listaKurseve.Add(c);
                }
            }

            grid.DataSource = listaKurseve;
            grid.DataBind();

            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment; filename=Course_Results.xls");
            Response.ContentType = "application/vnd.ms-excel";
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            grid.RenderControl(htw);

            Response.Write(sw.ToString());

            Response.End();

            return null;
        }

        #endregion

        public string GetCourseTerm(string course)
        {
            if (course != null)
            {
                if (3 >= course.Length) return course;
                return course.Substring(1, 2);
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

        #endregion

    }
}
