using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace BootstrapperUI
{
    public class HttpClient
    {
        private HttpWebRequest Client { get; set; }
        private readonly string _uri;
        public HttpClient(string uri)
        {
            _uri = uri;
        }
        private void InitializeClient()
        {
            Client = (HttpWebRequest)WebRequest.Create(new Uri(_uri));
            Client.ContentType = "application/json";
            Client.Method = "GET";
        }

        public T Get<T>()
        {
            T response = default(T);
            try
            {
                InitializeClient();
                var httpResponse = (HttpWebResponse)Client.GetResponse();
                using (var responseStream = httpResponse.GetResponseStream())
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        response = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return response;
        }
    }
}
