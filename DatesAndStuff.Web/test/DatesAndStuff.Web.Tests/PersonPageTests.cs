using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

using System.Globalization;
using System.IO;
using System.Linq;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    [TestCase(0)]
    public void Person_SalaryIncrease_ShouldIncrease(double percentage)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));

        //var salaryLabelBefore = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        //var salaryBeforeSubmission = double.Parse(salaryLabelBefore.Text);
        var salaryBeforeSubmissionText = wait.Until(driver =>
        {
            var salaryLabelBefore = driver.FindElement(By.XPath("//*[@data-test='DisplayedSalary']"));
            if (salaryLabelBefore.Displayed)
            {
                return salaryLabelBefore.Text;
            }
            return null;
        });

        var salaryBeforeSubmission = double.Parse(salaryBeforeSubmissionText);
        var expectedSalary = salaryBeforeSubmission + salaryBeforeSubmission * percentage / 100;

        //var input = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));
        //input.Clear();
        //input.SendKeys(percentage.ToString());
        wait.Until(driver =>
        {
            var input = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));
            if (input.Displayed && input.Enabled)
            {
                input.Clear();
                input.SendKeys(percentage.ToString());
                return true;
            }
            return false;
        });

        // Act
        //var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        //submitButton.Click();
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']"))).Click();

        // Assert
        //var salaryLabel = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        //var salaryAfterSubmission = double.Parse(salaryLabel.Text);
        var salaryAfterSubmissionText = wait.Until(driver =>
        {
            var salaryLabel = driver.FindElement(By.XPath("//*[@data-test='DisplayedSalary']"));
            if (salaryLabel.Displayed)
            {
                return salaryLabel.Text;
            }
            return null;
        });

        var salaryAfterSubmission = double.Parse(salaryAfterSubmissionText);
        salaryAfterSubmission.Should().BeApproximately(expectedSalary, 0.001);
    }

    [TestCase(-10)]
    [TestCase(-15)]
    [TestCase(-50)]
    public void Person_SalaryIncrease_LessThanMinusTen_ShouldShowValidationErrors(double percentage)
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));

        wait.Until(driver =>
        {
            var input = driver.FindElement(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']"));
            if (input.Displayed && input.Enabled)
            {
                input.Clear();
                input.SendKeys(percentage.ToString());
                return true;
            }
            return false;
        });

        // Act
        wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']"))).Click();

        // Assert
        var errorTop = wait.Until(driver =>
        {
            var element = driver.FindElement(By.CssSelector("ul.validation-errors li.validation-message"));

            return element.Displayed;
        });
        var errorUnder = wait.Until(driver =>
        {
            var element = driver.FindElement(By.CssSelector("div.validation-message"));

            return element.Displayed;
        });

        errorTop.Should().BeTrue();
        errorUnder.Should().BeTrue();
    }

    [Test]
    public void BlazeDemo_MexicoToDublin_ShouldHaveAtLeastThreeFlights()
    {
        // Arrange
        double maxPrice = 230d;

        driver.Navigate().GoToUrl("https://blazedemo.com");

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));

        wait.Until(driver =>
        {
            var selectFrom = driver.FindElement(By.Name("fromPort"));
            var selectTo = driver.FindElement(By.Name("toPort"));

            if (selectFrom.Displayed && selectFrom.Enabled && selectTo.Displayed && selectTo.Enabled)
            {
                selectFrom.SendKeys("Mexico City");
                selectTo.SendKeys("Dublin");
                return true;
            }
            return false;
        });

        // Act
        wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("input[type='submit']"))).Click();

        // Assert
        var flightRows = wait.Until(driver =>
        {
            var rows = driver.FindElements(By.CssSelector("table tbody tr"));

            return rows.Count > 0 ? rows : null;
        });

        flightRows.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}