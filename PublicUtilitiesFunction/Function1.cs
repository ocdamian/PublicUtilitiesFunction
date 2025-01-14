using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PublicUtilitiesFunction.Services;

namespace PublicUtilitiesFunction
{
    public class Function1
    {

        private readonly IScrapingService _scrapingService;
        public Function1(IScrapingService scrapingService)
        {
            _scrapingService = scrapingService ?? throw new ArgumentNullException(nameof(scrapingService));
        }


        [FunctionName("Oomapasc")]
        public async Task<IActionResult> GetOomapascInfo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {

            string accountNumber = req.Query["accountNumber"];

            string basePath = Environment.CurrentDirectory;

            // Construir el path hacia la carpeta Resources
            string resourcesPath = Path.Combine(basePath, "Resources", "Chrome", "Win64-130.0.6723.69", "chrome-win64");

            if (!Directory.Exists(resourcesPath)) 
            {
                return new BadRequestObjectResult("No existe el path "+   resourcesPath);
            }

            if (string.IsNullOrEmpty(accountNumber))
            {
                log.LogInformation("Account number is required.");
                return new BadRequestObjectResult("Account number is required.");
            }
            var oomapasc = await _scrapingService.WebScrapingOomapascAsync(accountNumber);

            return new OkObjectResult(oomapasc);
        }


        [FunctionName("Cfe")]
        public async Task<IActionResult> GetCfeInfo(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
        {

            string serviceNumber = req.Query["serviceNumber"];

            if (string.IsNullOrEmpty(serviceNumber))
            {
                log.LogInformation("Account number is required.");
                return new BadRequestObjectResult("Account number is required.");
            }
            var cfe = await _scrapingService.WebScrapingCfecAsync(serviceNumber);

            return new OkObjectResult(cfe);
        }

        [FunctionName("Chrome")]
        public async Task<IActionResult> DownloadAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log)
        {

            var isDonwloaded = await _scrapingService.DownloadChromeAsync();

            return new OkObjectResult(isDonwloaded);
        }


    }
}

