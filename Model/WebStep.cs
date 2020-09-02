using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebChecker.Model
{
    public enum WebStepMethod
    {
        GET,
        POST,
        PUT,
        PATCH,
        DELETE,
    }

    public class WebParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class WebStep
    {
        public string URL { get; set; }
        public IEnumerable<WebParameter> Parameters { get; set; }
        public WebStepMethod Method { get; set; } = WebStepMethod.GET;
        public bool IsWebForm { get; set; } = false;

        public event Action<HttpResponseMessage, string> OnSuccess;
        public event Action<HttpResponseMessage, string> OnFail;

        public async Task Run(HttpClient client)
        {
            HttpResponseMessage response = Method switch
            {
                Model.WebStepMethod.POST => await client.PostAsync(URL, new FormUrlEncodedContent(Parameters?.ToDictionary(x => x.Name, x => x.Value?.ToString()))).ConfigureAwait(false),
                Model.WebStepMethod.PUT => await client.PutAsync(URL, new FormUrlEncodedContent(Parameters?.ToDictionary(x => x.Name, x => x.Value?.ToString()))).ConfigureAwait(false),
                Model.WebStepMethod.PATCH => await client.PatchAsync(URL, new FormUrlEncodedContent(Parameters?.ToDictionary(x => x.Name, x => x.Value?.ToString()))).ConfigureAwait(false),
                Model.WebStepMethod.DELETE => await client.DeleteAsync(URL).ConfigureAwait(false),
                _ => await client.GetAsync(URL).ConfigureAwait(false),
            };
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                OnSuccess?.Invoke(response, responseString);
            }
            else
            {
                OnFail?.Invoke(response, responseString);
            }
        }
    }
}
