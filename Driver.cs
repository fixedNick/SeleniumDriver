﻿using System;
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
        private Int32 ProcessID = -1;
        private static List<Int32> DriverProcessesId = new List<Int32>();
        private static List<Thread> DriverAdvertMonitorProcessesID = new List<Thread>();
        private Thread MonitorThread = null;
        public static Driver Empty = new Driver();

        /// <summary>
        /// The last navigated URL by method GoToUrl saved here.
        /// You can compare current URL with this field to see if driver changed page after your manipulations
        /// </summary>
        public string NavigatedUrl { get; private set; }

        private static readonly int MAX_WAIT_COUNT = 25;
        private static readonly int MAX_WAIT_COUNT_TO_SEND_KEYS = 6;
        private static readonly int REFRESH_PAGE_COUNT = 12;
        private static readonly int TIME_WAIT_TO_SEARCH_CSS = 200;
        private static readonly int TIME_WAIT_AFTER_REFRESH = 1000;
        private static readonly int TIME_WAIT_TO_SEND_KEYS = 300;
        private static readonly int TIME_WAIT_BEFORE_RETURN_KEY = 600;
        private static readonly int TIME_WAIT_TO_NAVIGATE = 3000;
        private static readonly int TIME_WAIT_AFTER_ACTION = 1000;


        public delegate void NotificationDelegate(string text);
        public NotificationDelegate NotificationHandler;

        public enum DriverType
        {
            Chrome
        }

        public Driver() { }

        public Driver(DriverType type,
            NotificationDelegate notificationHandler,
            bool startMaximized = false,
            bool headless = true,
            bool hidePrompt = true,
            bool showExceptions = false,
            string directoryPath = "",
            string driverName = "chromedriver.exe")
        {
            NotificationHandler = notificationHandler;
            // Добавим все активные процессы наших драйверов, дабы различать какой процесс был запущен, а какой запускается
            // При условии, что еще ни один драйвер не был запущен и его процесс не был добавлен в пул активных потоков
            if (DriverProcessesId.Count <= 0)
            {
                foreach (var activeProcess in System.Diagnostics.Process.GetProcessesByName(driverName))
                    DriverProcessesId.Add(activeProcess.Id);
            }

            switch (type)
            {
                case DriverType.Chrome:
                    CreateChromeDriver(notificationHandler, startMaximized, headless, hidePrompt, showExceptions, directoryPath, driverName);
                    break;
            }
        }

        private void CreateChromeDriver(
            NotificationDelegate notificationHandler,
            bool startMaximized = false,
            bool headless = true,
            bool hidePrompt = true,
            bool showExceptions = false,
            string directoryPath = "",
            string driverName = "chromedriver.exe")
        {
            ChromeDriverService serv;
            ChromeOptions opts = new ChromeOptions();


            if (startMaximized)
                opts.AddArgument("start-maximized");

            if (headless)
                opts.AddArgument("headless");

            string[] driverDirectories = System.IO.Directory.GetDirectories(directoryPath);

            foreach (var dir in driverDirectories)
            {
                var pathToSingleDriver = $"{dir}\\{driverName}";
                try
                {
                    notificationHandler($"starting driver: '{pathToSingleDriver}'");
                    serv = ChromeDriverService.CreateDefaultService(dir, driverName);
                    serv.HideCommandPromptWindow = hidePrompt;
                    driver = new ChromeDriver(serv, opts);

                    // Определяем запущенный процесс, обозначаем его в экземпляре и добавляем в список запущенных потоков.

                    var activeProcesses = System.Diagnostics.Process.GetProcessesByName(driverName.Split('.')[0]);
                    foreach(var activeProcess in activeProcesses)
                    {
                        if (DriverProcessesId.Contains(activeProcess.Id))
                            continue;
                        ProcessID = activeProcess.Id;
                        DriverProcessesId.Add(activeProcess.Id);
                        break;
                    }

                    // Успешный запуск браузера, запускаем процесс, который будет отслеживать появление рекламы на страницах и закрывать их
                    //Thread advertiseThread = new Thread(AdvertiseMonitor);
                    //DriverAdvertMonitorProcessesID.Add(advertiseThread);
                    //MonitorThread = advertiseThread;
                    //advertiseThread.Start();

                    return;
                }
                catch (Exception ex)
                {
                    // Не удалось запустить драйвер, убиваем поток консоли
                    var activeProcesses = System.Diagnostics.Process.GetProcessesByName(driverName.Split('.')[0]);
                    foreach (var activeProcess in activeProcesses)
                    {
                        if (DriverProcessesId.Contains(activeProcess.Id))
                            continue;
                        activeProcess.Kill();
                    }

                    if (showExceptions)
                        notificationHandler($"[{pathToSingleDriver}]\n" + ex.Message);
                }
            }

            notificationHandler("Обновите ChromeDriver до новой версии");
            throw new Exception("Неудалось создать экземпляр ChromeDriver.\n" + "Обновите ChromeDriver до новой версии");
        }
        /// <summary>
        /// Данный метод работает в отдельном потоке и постоянно проверяет - не появилось ли уведомление, рекламаи тп
        /// Закидываем список селекторов кнопок, которые закрывают различные уведомления, чаты и прочую ересь
        /// Проверяем, есть ли таковые на странице 
        /// Если есть - закрываем их и продолжаем действия по кругу в бесконечном цикле
        /// 
        /// Если метод не сработает - возможное решение -
        /// при ошибке в методах FindCss и подобных ему - будет запускать поиск рекламы и закрывать ее, а далее перевызывать метод, в котором произошел сбой
        /// </summary>
        /// <param name="o"></param>
        //private void AdvertiseMonitor(object o)
        //{
        //    var closeSelectors = System.IO.File.ReadAllLines("css_btn_close.txt");

        //    while(true)
        //    {
        //        foreach (var selector in closeSelectors)
        //        {
        //            var result = FindCss(selector, isNullAcceptable: true, useFastSearch: true, refreshPage: false);
        //            if(result != null)
        //                Click(result, allowException: false);
        //        }
        //    }
        //}


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
                    RefreshPage();

                Thread.Sleep(TIME_WAIT_TO_SEARCH_CSS);
            }

            Thread.Sleep(1500);
        }

        public string GetCurrentUrl() => driver.Url;
        public void RefreshPage() {
            driver.Navigate().Refresh();
            Thread.Sleep(TIME_WAIT_AFTER_REFRESH);
        }

        /// <summary>
        /// Safe clone witch accept null result of IWebElement and could search elements in parents
        /// Only when isNullAcceptable: If time left and element didnt found - returns NULL
        /// </summary>
        /// <param name="selector">CSS Selector of searchable element</param>
        /// <param name="targetElement">Parent IWebElement</param>
        /// <param name="isNullAcceptable">Could be result of search equals null</param>
        /// <returns>Returns search result by selector in parent(targetElement) or on web page</returns>
        public IWebElement FindCss(string selector, IWebElement targetElement = null, bool isNullAcceptable = false, bool useFastSearch = false, bool refreshPage = true, bool showExceptions = true)
        {
            int counter = 0;
            while (true)
            {
                counter++;
                // Is refresh time comes
                if (refreshPage == true && counter % (useFastSearch == false ? REFRESH_PAGE_COUNT : 4) == 0)
                {
                    RefreshPage();
                }

                // If result of method could be Null
                if (isNullAcceptable == true && counter == (useFastSearch == false ? MAX_WAIT_COUNT : 8)) return null;

                // Trying to get webelement
                try
                {
                    IWebElement result = null;
                    if (targetElement == null)
                    {
                        try
                        {
                            result = driver.FindElement(By.CssSelector(selector));
                        }
                        catch { } // TODO логирование

                    }
                    else
                    {
                        try
                        {
                            result = targetElement.FindElement(By.CssSelector(selector));
                        }
                        catch { } // TODO логирование
                    }

                    if (result != null) 
                        return result;
                    
                    Thread.Sleep(TIME_WAIT_TO_SEARCH_CSS);
                }
                catch { Thread.Sleep(TIME_WAIT_TO_SEARCH_CSS); }
            }
        }

        ///TODO: Добавить все фичи одиночного поиска
        public List<IWebElement> FindCssList(string selector, IWebElement targetElement = null, bool isNullAcceptable = false)
        {
            int counter = 0;
            while (true)
            {
                counter++;
                // Is refresh time comes
                if (counter % REFRESH_PAGE_COUNT == 0)
                    RefreshPage();

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

            Thread.Sleep(1000); RefreshPage();

        }

        /// <summary>
        /// Safe method to send keys via OpenQA SendKeys method with options
        /// </summary>
        /// <param name="targetElement">Your IWebElement to send keys to</param>
        /// <param name="text">Keys you want to send</param>
        /// <param name="sendReturnKey">If you want to send return key after text then set this parameter as TRUE</param>
        /// <param name="allowException">If you want this method to throwing exceptions then set this parater as TRUE</param>
        /// <returns> 
        /// TRUE - OK
        /// FALSE - Thrown an Exception, keys didnt send
        /// </returns>
        public bool KeySend(IWebElement targetElement, String text, bool sendReturnKey = false, bool allowException = false)
        {
            int counter = 0;
            Exception thrownException = null;
            while (counter < MAX_WAIT_COUNT_TO_SEND_KEYS)
            {
                try
                {
                    targetElement.SendKeys(text);
                    if (sendReturnKey)
                    {
                        Thread.Sleep(TIME_WAIT_BEFORE_RETURN_KEY);
                        targetElement.SendKeys(OpenQA.Selenium.Keys.Return);
                    }
                    return true;
                }
                catch(Exception ex)
                {
                    thrownException = ex;
                    Thread.Sleep(TIME_WAIT_TO_SEND_KEYS);
                    counter++;
                }
            }

            if (allowException && thrownException != null)
                throw thrownException;
            return false;
        }

        /// <summary>
        /// Click witch accepts selector of any element on page.
        /// Firstly this method using other method FindCss to find element on page.
        /// Then makes click on it
        /// </summary>
        /// <returns>
        ///     true - clicked successfully
        ///     false - element unreacheble
        /// </returns>
        public bool Click(string elementSelector, bool allowException = true, IWebElement targetElement = null, bool isNullAcceptable = false, bool useFastSearch = false, bool refreshPage = true)
        {
            var foundElement = FindCss(elementSelector, targetElement, isNullAcceptable, useFastSearch, refreshPage, allowException);
            if (isNullAcceptable == true && foundElement == null)
                return false;
            return Click(foundElement, allowException);
        }

        /// <summary>
        /// The same method as KeysSend, but sending a click to any element you choose
        /// </summary>
        /// <param name="elementToClick">Your IWebElement to send keys to</param>
        /// <param name="allowException">If you want this method to throwing exceptions then set this parater as TRUE</param>
        /// <returns> 
        /// TRUE - OK
        /// FALSE - Thrown an Exception, keys didnt send
        /// </returns>
        public bool Click(IWebElement elementToClick, bool allowException = true)
        {
            int counter = 0;
            Exception thrownException = null;
            while (counter < MAX_WAIT_COUNT_TO_SEND_KEYS)
            {
                try
                {
                    elementToClick.Click();
                    Thread.Sleep(TIME_WAIT_AFTER_ACTION);
                    return true;
                }
                catch(Exception ex)
                {
                    thrownException = ex;
                    Thread.Sleep(TIME_WAIT_TO_SEND_KEYS);
                    counter++;
                }
            }

            if (allowException && thrownException != null) 
                throw thrownException;
            return false;
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

        /// <summary>
        /// Метод для остановки работы текущего драйвера
        /// Пытается закрыть драйвер, так же пытается убить его процесс, если тот не был убит при закрытии драйвера и удаляет id потока из пула активных
        /// </summary>
        public void StopDriver()
        {
            if (this.Alive())
            {
                try
                {
                    driver.Quit();
                    driver = null;
                } catch { }

                try
                {
                    DriverProcessesId.Remove(this.ProcessID);
                    System.Diagnostics.Process.GetProcessById(this.ProcessID).Kill();
                }
                catch { }

                try
                {
                    if (MonitorThread?.IsAlive == true)
                        MonitorThread.Abort();
                }
                catch { }
            }
        }

        public string GetPageSource() => driver.PageSource;

        public Boolean IsURLChangedAfterNavigate()
        {
            int waitor = 0;
            int circleAmount = TIME_WAIT_TO_NAVIGATE / 10;

            while(true)
            {
                if (waitor >= TIME_WAIT_TO_NAVIGATE) return false;
                if (NavigatedUrl == driver.Url)
                {
                    waitor += circleAmount;
                    Thread.Sleep(circleAmount);
                    continue;
                }

                return true;
            }
        }

        public void ExecuteScript(string script)
        {
            IJavaScriptExecutor IJS = driver as IJavaScriptExecutor;
            IJS.ExecuteScript(script);
        }
    }
}


//// TODO
/// Возможность выборочного запуска процесса мониторинга