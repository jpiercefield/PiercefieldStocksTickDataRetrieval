using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Reflection;

namespace PiercefieldStocksTickDataRetrieval;
public class RetrieveSymbols {
    public static List<string> BeginSymbolRetrieval() { //Auto bot controls, #SoPunkRock... 
        var symbols = new List<string>();
        var options = new ChromeOptions();
        options.AddArgument("start-maximized");
        options.AddArgument("disable-infobars");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-setuid-sandbox");
        options.AddArgument("--disable-sandbox");
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
        options.AddUserProfilePreference("profile.default_content_settings.popups", 0);
        options.AddUserProfilePreference("download.default_directory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads")); //Will download CSV files to project's directory in a Downloads folder
        options.AddUserProfilePreference("profile.content_settings.exceptions.automatic_downloads.*.setting", 1);
        options.AddUserProfilePreference("download.prompt_for_download", false);
        options.AddUserProfilePreference("safebrowsing.enabled", true);
        options.AddUserProfilePreference("safebrowsing.disable_download_protection", true);
        options.AddUserProfilePreference("disable-popup-blocking", "true");
        options.AddUserProfilePreference("download.directory_upgrade", true);

        using(var driver = new ChromeDriver(GetChromeDriverDirectory(), options)) {
            driver.Navigate().GoToUrl("https://www.nasdaq.com/market-activity/stocks/screener");
          
            driver.FindElement(By.Id("onetrust-accept-btn-handler")).Click(); // Find and click the "Accept All Cookies" button

            driver.FindElement(By.Id("radioItemNASDAQ")).Click(); // Select NASDAQ exchange
            driver.FindElement(By.Id("radioItemNGS")).Click(); // Select NGS subExchange

            // Select market cap filters
            Thread.Sleep(100);
            driver.FindElement(By.CssSelector("label[for='checkboxItemmega'] span.checkBox")).Click();
            Thread.Sleep(100);
            driver.FindElement(By.CssSelector("label[for='checkboxItemlarge'] span.checkBox")).Click();
            Thread.Sleep(100);
            driver.FindElement(By.CssSelector("label[for='checkboxItemmid'] span.checkBox")).Click();
            Thread.Sleep(100); 

            driver.FindElement(By.CssSelector("label[for='checkboxItemunited_states'] span.checkBox")).Click(); // Select location filter
            Thread.Sleep(100);

            driver.FindElement(By.CssSelector("button.nasdaq-screener__form-button--apply")).Click(); //Apply filtered selections

            Thread.Sleep(3500); //Allow for reload
            driver.ExecuteScript("window.scrollTo(0, 0)"); //Scroll to top                 
            Thread.Sleep(1000);

            driver.FindElement(By.CssSelector(".nasdaq-screener__form-button--download")).Click(); // Download CSV     
            Thread.Sleep(5000); // Wait for the file to download
            ReadFile(ref symbols); //Read file into symbols

            driver.Navigate().Refresh(); //The website does not act well with asking for multiple files without refreshing.
            Thread.Sleep(2000);

            driver.FindElement(By.Id("radioItemNYSE")).Click(); // Select NYSE exchange

            // Select market cap filters
            Thread.Sleep(100);
            driver.FindElement(By.CssSelector("label[for='checkboxItemmega'] span.checkBox")).Click();
            Thread.Sleep(100);
            driver.FindElement(By.CssSelector("label[for='checkboxItemlarge'] span.checkBox")).Click();
            Thread.Sleep(100);
            driver.FindElement(By.CssSelector("label[for='checkboxItemmid'] span.checkBox")).Click();
            Thread.Sleep(100);

            driver.FindElement(By.CssSelector("label[for='checkboxItemunited_states'] span.checkBox")).Click(); // Select location filter
            Thread.Sleep(100);

            driver.FindElement(By.CssSelector("button.nasdaq-screener__form-button--apply")).Click(); //Apply filtered selections

            Thread.Sleep(3500); //Allow for reload
            driver.ExecuteScript("window.scrollTo(0, 0)"); //Scroll to top                 
            Thread.Sleep(1000);

            driver.FindElement(By.CssSelector(".nasdaq-screener__form-button--download")).Click(); // Download CSV     
            Thread.Sleep(5000); // Wait for the file to download
            ReadFile(ref symbols); //Read file into symbols
        }

        return symbols;
    }

    public static void ReadFile(ref List<string> symbols) {
        string downloadsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
        string? csvFileName = Directory.GetFiles(downloadsPath, "*.csv").FirstOrDefault();

        if(string.IsNullOrEmpty(csvFileName)) {
            Console.WriteLine("Error: No File found in the directory.");
        } else {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", csvFileName);
            symbols.AddRange(File.ReadAllLines(filePath).Skip(1).Select(line => line.Split(',').First()).ToList());          
            File.Delete(filePath); // Remove file
        }
    }

    private static string GetChromeDriverDirectory() {      
        string? assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // Get the directory containing the current assembly

        if(string.IsNullOrEmpty(assemblyDirectory)) {
            throw new Exception("Failed to get directory.");
        } else {
            // Construct the path to the directory containing the chromedriver.exe file
            return Path.Combine(assemblyDirectory, "chromedriver_win32");
        }
    }
}