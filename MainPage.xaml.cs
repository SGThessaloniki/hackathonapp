using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace hackathonapp
{
    public partial class MainPage : ContentPage
    {
        public List<String> listpaths = new List<String>();  // list of paths	
        public List<String> listid = new List<String>();    // list of id returned
        public List<String> listnames = new List<String>();  // list of names
        public Stream strempath;
        public String sub_key = "f21c3b7544e74d608a249fafe5a7f96c";
        public String FLid = "hackathonms";
        public String endpoint = "https://westeurope.api.cognitive.microsoft.com/face/v1.0/detect";
        public MainPage()
        {
            InitializeComponent();
            my_photo.Source = "https://scontent.fath6-1.fna.fbcdn.net/v/t1.15752-9/47580109_1723840854394455_5243873541899681792_n.jpg?_nc_cat=111&_nc_ht=scontent.fath6-1.fna&oh=fb7d0d40592771c9df218a5634942dbe&oe=5C6F812D";

        }

        private async void Photo_but_Clicked(object sender, EventArgs e)
        {
            var photo = await Plugin.Media.CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions() { });
            strempath = photo.GetStream();
            

            if (photo != null)
            {
                my_photo.Source = ImageSource.FromStream(() => { return photo.GetStream(); });

                

            }
            else {  }
        }

        private async void Analyze_but_Clicked(object sender, EventArgs e)
        {
            byte[] b = ReadFully(strempath);
            List<FaceDetectResponse> detect_response = new List<FaceDetectResponse>();
            detect_response = await DetectFace(b);


            List<FaceFindSimilarResponse> similar_faces_response = new List<FaceFindSimilarResponse>();
            similar_faces_response = await FindSimilar(detect_response[0].FaceId, FLid, 1);

            int index = listid.FindIndex(x => x.StartsWith(similar_faces_response[0].PersistedFaceId));
            athlete_photo.Source = listpaths[index];
            welcome_label.Text = "You look like " + listnames[index] + " with confidence " + similar_faces_response[0].Confidence.ToString();
        }

        public static async Task<List<FaceDetectResponse>> DetectFace(byte [] image)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "f21c3b7544e74d608a249fafe5a7f96c");

                var uri = "https://westeurope.api.cognitive.microsoft.com/face/v1.0/detect";
                var content = new ByteArrayContent(image);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = await client.PostAsync(uri, content);

                if(response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<FaceDetectResponse>>(responseBody);
                    return result;
                }
                else
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<FaceApiErrorResponse>(errorText);
                    throw new FaceApiException(errorResponse.Error.Code, errorResponse.Error.Message);
                }
            }
        }

        public static async Task<List<FaceFindSimilarResponse>> FindSimilar(string faceId, string faceListId, int count)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "f21c3b7544e74d608a249fafe5a7f96c");

                var uri = "https://westeurope.api.cognitive.microsoft.com/face/v1.0/findsimilars";

                var body = new FaceFindSimilarRequest()
                {
                    FaceId = faceId,
                    FaceListId = faceListId,
                    MaxNumOfCandidatesReturned = count,
                    Mode = FaceFindSimilarRequestMode.MatchFace
                };
                var bodyText = JsonConvert.SerializeObject(body);

                var httpContent = new StringContent(bodyText, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(uri, httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<FaceFindSimilarResponse>>(responseBody);
                    return result;
                }
                else
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonConvert.DeserializeObject<FaceApiErrorResponse>(errorText);
                    throw new FaceApiException(errorResponse.Error.Code, errorResponse.Error.Message);
                }
            }
        }


        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public class FaceDetectResponse
        {
            public string FaceId { get; set; }

            public Rectangle FaceRectangle { get; set; }

            public FaceLandmarks FaceLandmarks { get; set; }

            public FaceAttributes FaceAttributes { get; set; }
        }

        public class FaceApiErrorResponse
        {
            public FaceApiError Error { get; set; }
        }

        public class FaceApiException : Exception
        {
            public string Code { get; private set; }

            public FaceApiException(string code, string message) : base(message)
            {
                Code = code;
            }
        }

        public class FaceFindSimilarRequest
        {
            public string FaceId { get; set; }
            public string FaceListId { get; set; }
            public List<string> FaceIds { get; set; }
            public int MaxNumOfCandidatesReturned { get; set; }
            public string Mode { get; set; }
        }

        public class FaceFindSimilarResponse
        {
            public string PersistedFaceId { get; set; }
            public string FaceId { get; set; }
            public double Confidence { get; set; }
        }

        public class FaceFindSimilarRequestMode
        {
            public const string MatchPerson = "matchPerson";
            public const string MatchFace = "matchFace";
        }
        public class FaceApiError
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }
    }
}
