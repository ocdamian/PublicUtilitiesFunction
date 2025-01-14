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

           
            string functionAppDirectory = context.FunctionAppDirectory;

            // Construye la ruta hacia la carpeta Resources
            string pathToChrome = Path.Combine(functionAppDirectory, "Resources", "Chrome", "Win64-130.0.6723.69", "chrome-win64");
            
            //string pathToChrome = Path.Combine(" D:\\a\\r1\\a\\", "Resources", "Chrome", "Win64-130.0.6723.69", "chrome-win64");
            
            //if (!string.IsNullOrEmpty(pathToChrome))
            //{
            //    Console.WriteLine("El path " + pathToChrome  + "no existe");
            //    return new BadRequestObjectResult("El path " + pathToChrome + "no existe");
            //}

            if (string.IsNullOrEmpty(accountNumber))
            {
                log.LogInformation("Account number is required.");
                return new BadRequestObjectResult("Account number is required.");
            }
            var oomapasc = await _scrapingService.WebScrapingOomapascAsync(accountNumber, pathToChrome);

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


    }
}

