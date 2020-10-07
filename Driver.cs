using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SeleniumDriver
{
    public class Driver
    {
        // Driver Component
        private IWebDriver driver;
        /// <summary>
        /// The last navigated URL by method GoToUrl saved here.
        /// You can compare current URL with this field to see if driver changed page after your manipulations
        /// </summary>
        public string NavigatedUrl { get; private set; }

        public static readonly int MAX_WAIT_COUNT = 60;
        public static readonly int REFRESH_PAGE_COUNT = 30;
        public static readonly int TIME_WAIT_TO_SEARCH_CSS = 200;
        public static readonly int TIME_WAIT_AFTER_REFRESH = 1000;
        public static readonly int TIME_WAIT_TO_SEND_KEYS = 300;
        public static readonly int TIME_WAIT_BEFORE_RETURN_KEY = 600;

        public delegate void NotificationDelegate(string text);

        public enum DriverType
        {
            Chrome
        }

        public Driver(DriverType type, 
            NotificationDelegate notificationHandler,
            bool startMaximized = false,
            bool headless = true,
            bool hidePrompt = true,
            bool showExceptions = false,
            string driverName = "chromedriver.exe")
        {
            switch (type)
            {
                case DriverType.Chrome:
                    CreateChromeDriver(notificationHandler, startMaximized, headless, hidePrompt, showExceptions, driverName);
                    break;
            }
        }

        private void CreateChromeDriver(
            NotificationDelegate notificationHandler,
            bool startMaximized = false,
            bool headless = true,
            bool hidePrompt = true,
            bool showExceptions = false,
            string driverName = "chromedriver.exe")
        {
            ChromeDriverService serv;
            ChromeOptions opts = new ChromeOptions();

            if (startMaximized)
                opts.AddArgument("start-maximized");

            if (headless)
                opts.AddArgument("headless");

            List<string> directoryNames = new List<string>()
            {
                "chromedriver84/",
                "chromedriver83/",
                "chromedriver81/"
            };

            foreach (var dir in directoryNames)
            {
                try
                {
                    serv = ChromeDriverService.CreateDefaultService(dir);
                    serv.HideCommandPromptWindow = hidePrompt;
                    driver = new ChromeDriver(serv, opts);
                }
                catch (Exception ex)
                {
                    if (showExceptions)
                        notificationHandler($"[{dir + driverName}]\n" + ex.Message);
                }
            }

            notificationHandler("Обновите ChromeDriver до версии 81, 83 или 84");
            throw new Exception("Неудалось создать экземпляр ChromeDriver.\n" + "Обновите ChromeDriver до версии 81, 83 или 84");
        }

        /// <summary>
        /// This method is safe clone of method GoToUrl in OpenQA.Selenium
        /// </summary>
        /// <param name="url">Url</param>
        /// <param name="title">Full or cutted title in any case</param>
        public void GoToUrl(String url, String title = "")
        {
            Thread.Sleep(700);
            driver.Navigate().GoToUrl(url);
            NavigatedUrl = url;

            var counter = 0;
            while (title.Length > 0 && !driver.Title.ToLower().Trim().Contains(title.ToLower().Trim()))
            {
                counter++;
                if (counter % REFRESH_PAGE_COUNT == 0)
                {
                    driver.Navigate().Refresh(); Thread.Sleep(TIME_WAIT_AFTER_REFRESH);
                }

                Thread.Sleep(TIME_WAIT_TO_SEARCH_CSS);
            }
        }

        public string GetCurrentUrl() => driver.Url;

        /// <summary>
        /// Safe clone witch accept null result of IWebElement and could search elements in parents
        /// </summary>
        /// <param name="selector">CSS Selector of searchable element</param>
        /// <param name="targetElement">Parent IWebElement</param>
        /// <param name="isNullAcceptable">Could be result of search equals null</param>
        /// <returns>Returns search result by selector in parent(targetElement) or on web page</returns>
        public IWebElement FindCss(string selector, IWebElement targetElement = null, bool isNullAcceptable = false)
        {
            int counter = 0;
            while (true)
            {
                counter++;
                // Is refresh time comes
                if (counter % REFRESH_PAGE_COUNT == 0)
                {
                    driver.Navigate().Refresh();
                    Thread.Sleep(TIME_WAIT_AFTER_REFRESH);
                }

                // If result of method could be Null
                if (isNullAcceptable && counter == MAX_WAIT_COUNT) return null;

                // Trying to get webelement
                try
                {
                    IWebElement result = null;
                    if (targetElement == null)
                        result = driver.FindElement(By.CssSelector(selector));

                    if (result != null) return result;
                    else Thread.Sleep(TIME_WAIT_TO_SEARCH_CSS);
                }
                catch { Thread.Sleep(TIME_WAIT_TO_SEARCH_CSS); }
            }
        }
        public List<IWebElement> FindCssList(string selector, IWebElement targetElement = null, bool isNullAcceptable = false)
        {
            int counter = 0;
            while (true)
            {
                counter++;
                // Is refresh time comes
                if (counter % REFRESH_PAGE_COUNT == 0)
                {
                    driver.Navigate().Refresh();
                    Thread.Sleep(TIME_WAIT_AFTER_REFRESH);
                }

                // If result of method could be Null
                if (isNullAcceptable && counter == MAX_WAIT_COUNT) return null;

                // Trying to get webelement
                try
                {
                    List<IWebElement> result = null;
                    if (targetElement == null)
                        result = driver.FindElements(By.CssSelector(selector)).ToList();

                    if (result != null) return result;
                    else Thread.Sleep(TIME_WAIT_TO_SEARCH_CSS);
                }
                catch { Thread.Sleep(TIME_WAIT_TO_SEARCH_CSS); }
            }
        }
        /// !!!!! TODO
        /// <summary>
        /// !!!!! TODO
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookies"></param>
        public void SetupCookies(string url, List<Cookie> cookies)
        {
            GoToUrl(url);
            foreach (var c in cookies)
                driver.Manage().Cookies.AddCookie(c);

            Thread.Sleep(1000);
            driver.Navigate().Refresh(); Thread.Sleep(1000);
        }

        /// <summary>
        /// Safe method to send keys via OpenQA SendKeys method with options
        /// </summary>
        /// <param name="targetElement">Your IWebElement to send keys to</param>
        /// <param name="text">Keys you want to send</param>
        /// <param name="sendReturnKey">If you want to send return key after text then set this parameter as TRUE</param>
        /// <param name="allowException">If you want this method to throwing exceptions then set this parater as TRUE</param>
        public void KeySend(IWebElement targetElement, String text, bool sendReturnKey = false, bool allowException = false)
        {
            while (true)
            {
                try
                {
                    targetElement.SendKeys(text);
                    if (sendReturnKey)
                    {
                        Thread.Sleep(TIME_WAIT_BEFORE_RETURN_KEY);
                        targetElement.SendKeys(OpenQA.Selenium.Keys.Return);
                    }
                    break;
                }
                catch(Exception ex)
                {
                    if (allowException) throw ex;
                    Thread.Sleep(TIME_WAIT_TO_SEND_KEYS);
                }
            }
        }

        /// <summary>
        /// Those methods checks is driver still running
        /// </summary>
        /// <param name="dr">Driver object</param>
        /// <returns>
        /// TRUE - if driver still running
        /// FALSE - if driver stopped
        /// </returns>
        public static Boolean Alive(Driver dr) => dr.driver == null ? false : true;
        public Boolean Alive() => driver == null ? false : true;

        public void StopDriver()
        {
            if (this.Alive())
            {
                driver.Quit();
                driver = null;
            }
        }

        public string GetPageSource() => driver.PageSource;
    }
}
