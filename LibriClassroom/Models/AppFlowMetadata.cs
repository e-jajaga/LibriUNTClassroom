using System;
using System.Web.Mvc;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Mvc;
using Google.Apis.Drive.v2;
using Google.Apis.Util.Store;
using Google.Apis.Classroom.v1;
using System.Configuration;
using LibriClassroom.Models;

namespace LibriClassroom.Models
{
    public class AppFlowMetadata : FlowMetadata
    {
        private static readonly String conexion = ConfigurationManager.ConnectionStrings["Google"].ConnectionString;

        private static readonly IAuthorizationCodeFlow flow =
            new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = "497103680084-18h1j868aisevoi1k1gpsrbv9b7glriu.apps.googleusercontent.com",
                    ClientSecret = "h1Fij6MavdeZtkJR3nGn0O29"
                },
                Scopes = new[] { 
                    DriveService.Scope.Drive, 
                    ClassroomService.Scope.ClassroomCourses,
                    ClassroomService.Scope.ClassroomCourseworkMe,
                    ClassroomService.Scope.ClassroomCourseworkStudents,
                    ClassroomService.Scope.ClassroomProfileEmails,
                    ClassroomService.Scope.ClassroomProfilePhotos,
                    ClassroomService.Scope.ClassroomRosters
                },
                //DataStore = new FileDataStore(@"D:\personal\documents\visual studio 2013\Projects\GClassMVCoAuth2\GClassMVCoAuth2", fullPath:true)
                DataStore = new DbDataStore(conexion)
            });

        public override string GetUserId(Controller controller)
        {
            // In this sample we use the session to store the user identifiers.
            // That's not the best practice, because you should have a logic to identify
            // a user. You might want to use "OpenID Connect".
            // You can read more about the protocol in the following link:
            // https://developers.google.com/accounts/docs/OAuth2Login.
            var user = controller.Session["user"];
            //if-i meposhtem duhet te fshihet ne fund ngase do ta marrim nga SSO username-in dhe ate do ta perdorim si Session["user"]
            //if (user == null)
            //{
            //    user = "Session expired";
            //    controller.Session["user"] = user;
            //}
            return user.ToString();

        }

        public override IAuthorizationCodeFlow Flow
        {
            get { return flow; }
        }
    }
}