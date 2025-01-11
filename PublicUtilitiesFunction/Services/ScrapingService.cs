using PublicUtilitiesFunction.models;
using PuppeteerSharp;
using System;
using System.Threading.Tasks;

namespace PublicUtilitiesFunction.Services
{

    public interface IScrapingService
    {
        Task<Oomapasc> WebScrapingOomapascAsync(string accountNumber);
        Task<Cfe> WebScrapingCfecAsync(string serviceNumber);
    }

    public class ScrapingService : IScrapingService
    {
        public async Task<Cfe> WebScrapingCfecAsync(string serviceNumber)
        {
            try
            {
                // Descarga el navegador si es necesario
                await new BrowserFetcher().DownloadAsync();

                // Inicia el navegador
                using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
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
            await new BrowserFetcher().DownloadAsync();

            // Lanzar el navegador en modo headless
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
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
