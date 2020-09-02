using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebChecker.Model
{
    public class WebItem
    {
        public string Name { get; set; }
        public IEnumerable<WebStep> Steps { get; set; }

        public async Task Run()
        {
            var webLogger = NLog.LogManager.GetLogger(Name);
            if (Steps?.Any() == true)
            {
                webLogger.Info($"{Name} start.");
                Random rnd = new Random();
                using var client = new HttpClient(new HttpClientHandler() { CookieContainer = new CookieContainer() });
                for (int stepCount = 0; stepCount < Steps.Count(); stepCount++)
                {
                    bool stepStatus = false;
                    var step = Steps.ElementAt(stepCount);
                    step.OnSuccess += (_, responseString) => {
                        webLogger.Info($"{Name} step {stepCount + 1} cheking url {step.URL} OK.");
                        if (Steps.Count() > (stepCount + 1))
                        {
                            var nextStep = Steps.ElementAt(stepCount + 1);
                            if (nextStep.IsWebForm)
                            {
                                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                                doc.LoadHtml(responseString);
                                var forms = doc.DocumentNode.SelectNodes("//form[@action]");
                                if (forms?.Any(x => x.GetAttributeValue("action", string.Empty).Contains(nextStep.URL, StringComparison.OrdinalIgnoreCase)) == true)
                                {
                                    var form = forms.FirstOrDefault(x => x.GetAttributeValue("action", string.Empty).Contains(nextStep.URL, StringComparison.OrdinalIgnoreCase));
                                    var inputs = form.SelectNodes("//input");
                                    if (inputs?.Any(x => x.GetAttributeValue("type", string.Empty).Contains("hidden", StringComparison.OrdinalIgnoreCase)) == true)
                                    {
                                        var hiddenParams = new List<WebParameter>();
                                        foreach (var input in inputs.Where(x => x.GetAttributeValue("type", string.Empty).Contains("hidden", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            var param = new WebParameter()
                                            {
                                                Name = input.GetAttributeValue("name", string.Empty),
                                                Value = input.GetAttributeValue("value", string.Empty)
                                            };
                                            if (!string.IsNullOrEmpty(param.Name))
                                            {
                                                hiddenParams.Add(param);
                                            }
                                        }
                                        if (hiddenParams.Count > 0)
                                        {
                                            nextStep.Parameters = nextStep.Parameters.Concat(hiddenParams);
                                        }
                                    }
                                }
                            }
                        }
                        stepStatus = true;
                    };
                    step.OnFail += (response, responseString) => {
                        webLogger.Warn($"{Name} step {stepCount + 1} cheking url {step.URL} Failed with Status code {response.StatusCode}, Reason {response.ReasonPhrase} and Response {responseString}.");
                        stepStatus = false;
                    };
                    webLogger.Info($"{Name} cheking url {step.URL}.");
                    try
                    {
                        var delay = rnd.Next(3, 11);
                        webLogger.Info($"{Name} step {stepCount + 1} cheking url {step.URL} delay {delay} seconds.");
                        await Task.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);
                        await step.Run(client).ConfigureAwait(false);
                        if (!stepStatus)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        webLogger.Error(ex, $"{Name} step {stepCount + 1} cheking url {step.URL}  error.");
                        break;
                    }
                }

                webLogger.Info($"{Name} end.");
                client.Dispose();
            }
            else
            {
                webLogger.Info($"{Name} has no steps.");
            }
        }
    }
}
