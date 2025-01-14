using PublicUtilitiesFunction.models;
using PuppeteerSharp;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace PublicUtilitiesFunction.Services
{

    public interface IScrapingService
    {
        Task<Oomapasc> WebScrapingOomapascAsync(string accountNumber);
        Task<Cfe> WebScrapingCfecAsync(string serviceNumber);

        //Task<bool> DownloadChromeAsync();
    }

    public class ScrapingService : IScrapingService
    {

        //public async Task<bool> DownloadChromeAsync()
        //{
        //    try
        //    {
        //        // Obtener la raíz del proyecto
        //        string projectRoot = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        //        // Ruta para guardar el archivo ZIP descargado
        //        string localPath = Path.Combine(projectRoot, "chrome.zip");

        //        // Ruta de extracción
        //        string extractPath = Path.Combine(projectRoot, "ExtractedFiles");

        //        // Ruta del binario extraído
        //        string pathToChrome = Path.Combine(extractPath, "Chrome", "Win64-130.0.6723.69", "chrome-win64");

        //        // Verificar si Chrome ya está extraído
        //        if (Directory.Exists(pathToChrome))
        //        {
        //            return true; // Ya existe, no es necesario descargar
        //        }

        //        // Crear carpeta de extracción si no existe
        //        if (!Directory.Exists(extractPath))
        //        {
        //            Directory.CreateDirectory(extractPath);
        //        }

        //        // Nombre del contenedor y archivo
        //        string containerName = "binaries";
        //        string blobName = "Resources.zip";

        //        // Obtener la cadena de conexión desde las variables de entorno
        //        string connectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
        //        if (string.IsNullOrEmpty(connectionString))
        //        {
        //            Console.WriteLine("Cadena de conexión no encontrada en las variables de entorno.");
        //            return false;
        //        }

        //        // Crear el cliente del blob
        //        var blobClient = new BlobClient(connectionString, containerName, blobName);

        //        // Descargar el archivo desde Blob Storage
        //        using (var blobStream = await blobClient.OpenReadAsync())
        //        using (var fileStream = File.OpenWrite(localPath))
        //        {
        //            await blobStream.CopyToAsync(fileStream);
        //        }

        //        // Descomprimir el archivo ZIP
        //        ZipFile.ExtractToDirectory(localPath, extractPath);

        //        // Validar que la extracción fue exitosa
        //        if (Directory.Exists(pathToChrome))
        //        {
        //            return true; // Descarga y extracción exitosas
        //        }
        //        else
        //        {
        //            Console.WriteLine("La extracción no generó la carpeta esperada.");
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log del error (puedes cambiarlo a una herramienta de logging como Serilog)
        //        Console.WriteLine($"Error al descargar o extraer Chrome: {ex.Message}");
        //        return false; // Indica que no fue exitoso
        //    }
        //}


        public async Task<Cfe> WebScrapingCfecAsync(string serviceNumber)
        {
            try
            {
                //// Descarga el navegador si es necesario
                //await new BrowserFetcher().DownloadAsync();

                var pathToChrome = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Chrome", "Win64-130.0.6723.69", "chrome-win64", "chrome.exe");

                if (!File.Exists(pathToChrome))
                {
                    throw new FileNotFoundException($"No se encontró chrome.exe en la ruta: {pathToChrome}");
                }

                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = pathToChrome
                });


                // Inicia el navegador
                //using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                using var page = await browser.NewPageAsync();

                // Navega a la página de inicio de sesión
                await page.GoToAsync("https://app.cfe.mx/Aplicaciones/CCFE/MiEspacio/Login.aspx");

                //var user = "ocdamian";
                //var password = "ocampoelectro"; 
                var user = Environment.GetEnvironmentVariable("CFE_USER");
                var password = Environment.GetEnvironmentVariable("CFE_PASSWORD");

                // Completar el formulario de inicio de sesión
                await page.TypeAsync("#ctl00_MainContent_txtUsuario", user);
                await page.TypeAsync("#ctl00_MainContent_txtPassword", password);
                await page.ClickAsync("#ctl00_MainContent_btnIngresar");

                // Esperar la navegación posterior al inicio de sesión
                await page.WaitForNavigationAsync();

                // Seleccionar un servicio
                await page.WaitForSelectorAsync("#ctl00_MainContent_ddlServicios");
                await page.SelectAsync("#ctl00_MainContent_ddlServicios", serviceNumber);

                // Esperar la carga de los datos
                await page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });

                // Extraer los datos necesarios
                var total = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ctl00_MainContent_lblMonto')?.textContent?.trim() || ''");
                var period = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ctl00_MainContent_lblPeriodoConsumo')?.textContent?.trim() || ''");
                var payBefore = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ctl00_MainContent_lblFechaLimite')?.textContent?.trim() || ''");
                var address = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ctl00_MainContent_lblDireccionCliente')?.textContent?.trim() || ''");
                var status = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ctl00_MainContent_lblEstadoRecibo')?.textContent?.trim() || ''");
                var userName = await page.EvaluateFunctionAsync<string>("() => document.querySelector('.col-lg-6:nth-of-type(1) h3')?.childNodes[0]?.textContent?.trim() || ''");

                // Cerrar el navegador
                await browser.CloseAsync();

                var info = new Cfe
                {
                    ServiceNumber = serviceNumber,
                    Period = period,
                    UserName = userName,
                    PayBefore = payBefore,
                    Total = total,
                    Address = address,
                    Status = status
                };

                return info;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during scraping: {ex.Message}", ex);
            }
        }

        public async Task<Oomapasc> WebScrapingOomapascAsync(string accountNumber)
        {

            // Descargar Chromium si no está disponible
            //await new BrowserFetcher().DownloadAsync();

            string pathToChrome = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Chrome", "Win64-130.0.6723.69", "chrome-win64");

            // Verificar si Chrome ya está extraído
            if (!Directory.Exists(pathToChrome))
            {
                //await DownloadChromeAsync();
            }

            var pathChrome = Path.Combine(pathToChrome, "chrome.exe");

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = pathChrome
            });

            // Lanzar el navegador en modo headless
            //using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

            // Navegar a la página de inicio de sesión
            //Console.WriteLine("Navegando a la página de inicio de sesión...");
            await page.GoToAsync("https://pagos.oomapasc.gob.mx/Panel/CapturaTarjeta.aspx");

            var captcha = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#textoCaptcha').textContent.trim()");

            // Resolver la operación del captcha
            var result = ResolverOperacionCaptcha(captcha);

            // Completar el formulario de inicio de sesión
            Console.WriteLine("Ingresando credenciales...");
            await page.TypeAsync("#ContentPlaceHolder1_txtNumCta", accountNumber); // Cambia '#username' por el selector correcto
            await page.TypeAsync("#captchaAnswer", result.ToString()); // Cambia '#password' por el selector correcto
            await page.ClickAsync("#ContentPlaceHolder1_btnConsultar"); // Cambia '#loginButton' por el selector del botón de inicio de sesión

            // Esperar la navegación a la página posterior al inicio de sesión
            await page.WaitForNavigationAsync();

            // Extraer datos de la página después del inicio de sesión
            //Console.WriteLine("Extrayendo datos después del inicio de sesión...");

            //Console.WriteLine("Extrayendo el texto del elemento...");
            var userName = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ContentPlaceHolder1_lblNombreUsuario').textContent.trim()");
            var expiredMonths = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ContentPlaceHolder1_lblMesesVencidos').textContent.trim()");
            var payBefore = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ContentPlaceHolder1_lblPagueseAntesDe').textContent.trim()");
            var total = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ContentPlaceHolder1_lblTotalAPagar').textContent.trim()");
            var suburb = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ContentPlaceHolder1_lblColonia').textContent.trim()");
            var address = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ContentPlaceHolder1_lblDomicilio').textContent.trim()");
            var betweenStreets = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ContentPlaceHolder1_lblEntreCalles').textContent.trim()");
            var description = await page.EvaluateFunctionAsync<string>("() => document.querySelector('#ContentPlaceHolder1_lblDescripcion').textContent.trim()");

            await browser.CloseAsync();
            var oomapasc = new Oomapasc
            {
                AccountNumber = accountNumber,
                UserName = userName,
                PayBefore = payBefore,
                ExpiredMonths = expiredMonths,
                Total = total,
                Suburb = suburb,
                Address = address,
                BetweenStreets = betweenStreets,
                Description = description
            };
            return oomapasc;
        }

        private int ResolverOperacionCaptcha(string texto)
        {
            // Extraer la operación del texto (por ejemplo, "¿1 + 5?")
            var partes = texto.Replace("Resuelva,", "").Replace("¿", "").Replace("?", "").Trim().Split(' ');
            if (partes.Length == 3 && int.TryParse(partes[0], out int num1) && int.TryParse(partes[2], out int num2))
            {
                var operacion = partes[1];
                return operacion switch
                {
                    "+" => num1 + num2,
                    "-" => num1 - num2,
                    "*" => num1 * num2,
                    "/" => num2 != 0 ? num1 / num2 : 0,
                    _ => throw new InvalidOperationException("Operación desconocida")
                };
            }
            throw new FormatException("El formato del captcha no es válido");
        }

    }
}







//---------------------------------------

//var pathToChrome = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Extracted", "Resources", "Chrome", "Win64-130.0.6723.69", "chrome-win64", "chrome.exe");



//var pathResource = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
//var zipFilePath = Path.Combine(pathResource, "Resources.zip");
//var extractPath = Path.Combine(pathResource, "Extracted");

//if (!Directory.Exists(extractPath))
//{
//    Directory.CreateDirectory(extractPath); // Crea la carpeta de destino para extraer

//    if (File.Exists(zipFilePath))
//    {
//        ZipFile.ExtractToDirectory(zipFilePath, extractPath);
//    }
//}

////var pathToChrome = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Chrome", "Win64-130.0.6723.69", "chrome-win64", "chrome.exe");

//if (!File.Exists(pathToChrome))
//{
//    throw new FileNotFoundException($"No se encontró chrome.exe en la ruta: {pathToChrome}");
//}
//---------------------------------------

//var pathChrome = Path.Combine(pathToChrome, "chrome.exe");

//var browser = await Puppeteer.LaunchAsync(new LaunchOptions
//{
//    Headless = true,
//    ExecutablePath = pathChrome
//});
