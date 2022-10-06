using System.Net;
using System.Text;
using System.Web;

namespace DokuServer
{
    public class WebServer
    {
        private const string HttpFolder = "WebFiles";
        private HttpListener listener = new HttpListener();

        public WebServer(){
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine ("OS is not supported!");
                throw new PlatformNotSupportedException("OS is not supported!");
            }
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            listener.Prefixes.Add($"http://*:8080/");
        }

        private string[] getFilePrefixes() {
            var currentDirectory = Directory.GetCurrentDirectory();
            var paths = Directory.GetFiles(currentDirectory + HttpFolder);
            return paths.Select(path => {
                return path.Replace(currentDirectory + HttpFolder, "");
            }).ToArray();
        }

        public void run() {
            Console.WriteLine("Listening....");
            listener.Start();
            try
            {
                while(true) 
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    var resultBytes = request.HttpMethod switch {
                        "GET" => handleGet( context),
                        _ => handleError(context)
                    };
                    // Obtain a response object.
                    HttpListenerResponse response = context.Response;
                    response.ContentLength64 = resultBytes.Length;
                    response.OutputStream.Write(resultBytes);
                    response.OutputStream.Close();
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private byte[] handleGet(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.RawUrl == null)
            {
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return new byte[] {};
            }
            var url = request.RawUrl;
            if (url[url.Length-1] == '/'){
                url += "index.html";
            }
            var path = $"{Directory.GetCurrentDirectory()}/{HttpFolder}{url}";
            if (File.Exists(path)){
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = extensionToMimeType(Path.GetExtension(path));
                return File.ReadAllBytes(path);
            }

            if (request?.Url?.Segments[1] == "fetch") {
                var parameter = request.QueryString.Keys.Cast<string>().ToDictionary(k => k, v => context.Request.QueryString[v]);
                var requestUrl = parameter["url"];
                if (requestUrl == null) 
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "text/plain";
                    return Encoding.UTF8.GetBytes("url parameter is needed");
                }
                // http://localhost:8080/fetch?url=https%3A%2F%2Fgoogle.de
                if (new Uri(requestUrl).Host.Contains("google")){
                    var client = new HttpClient();
                    var byteArray = client.GetByteArrayAsync(requestUrl).Result;
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = extensionToMimeType(Path.GetExtension(new Uri(requestUrl).LocalPath));
                    return byteArray;
                }
            }

            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "text/plain";
            return Encoding.UTF8.GetBytes("404 Not Found");
        }

        private byte[] handleError(HttpListenerContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "text/plain";
            return Encoding.UTF8.GetBytes("Not supported");
        }

        private string extensionToMimeType(string extension) => extension switch {
            "html" => "text/html",
            "css" => "text/css",
            "json" => "application/json",
            _ => "text"
        };
    }
}