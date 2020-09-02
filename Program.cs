using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebChecker.Model;

namespace WebChecker
{
    static class Program
    {

        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
            .Build();
            var webItems = config.GetSection("WebItems").Get<Model.WebItems>();
            if (webItems?.Webs?.Any(x => x?.Steps?.Any() == true) == true)
            {
                Task.WaitAll(webItems.Webs.Where(x => x?.Steps?.Any() == true).Select(x => x.Run()).ToArray());
            }
        }
    }
}
