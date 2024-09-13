
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using IronOcr;
using Patagames.Ocr;
namespace ManipurHighCourt
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize the Chrome WebDriver
            using (IWebDriver driver = new ChromeDriver())
            {
                string url = "https://hcservices.ecourts.gov.in/ecourtindiaHC/cases/s_orderdate.php?state_cd=25&dist_cd=1&court_code=1&stateNm=Manipur"; // Replace with your URL
                int maxRetries = 3;
                int retryCount = 0;
                bool success = false;

                // Retry navigation in case of failure
                while (retryCount < maxRetries && !success)
                {
                    try
                    {
                        driver.Navigate().GoToUrl(url);
                        success = true;
                    }
                    catch (WebDriverException ex)
                    {
                        Console.WriteLine($"Error navigating to URL: {ex.Message}");
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            Console.WriteLine("Retrying...");
                            await Task.Delay(3000); // Wait for 3 seconds before retrying
                        }
                        else
                        {
                            Console.WriteLine("Max retries reached. Exiting...");
                        }
                    }
                }

                if (success)
                {
                    DateTime today = DateTime.Today;
                    DateTime oneMonthAgo = today.AddMonths(-1);
                    string fromDate = oneMonthAgo.ToString("dd-MM-yyyy");
                    string toDate = today.ToString("dd-MM-yyyy");

                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    // Fill the 'from_date' field
                    IWebElement fromDateElement = wait.Until(d => d.FindElement(By.Id("from_date")));
                    fromDateElement.SendKeys(fromDate);

                    // Fill the 'to_date' field
                    IWebElement toDateElement = wait.Until(d => d.FindElement(By.Id("to_date")));
                    toDateElement.SendKeys(toDate);

                    // Capture the CAPTCHA image element
                    IWebElement captchaImageElement = wait.Until(d => d.FindElement(By.Id("captcha_image")));

                    // Capture a screenshot of the entire page
                    Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();

                    string captchaImagePath = string.Empty;
                    using (var fullImage = new System.Drawing.Bitmap(new MemoryStream(screenshot.AsByteArray)))
                    {
                        // Get the location and size of the CAPTCHA element
                        var elementLocation = captchaImageElement.Location;
                        var elementSize = captchaImageElement.Size;

                        // Create a new bitmap with the size of the CAPTCHA image
                        using (var elementScreenshot = new System.Drawing.Bitmap(elementSize.Width, elementSize.Height))
                        {
                            using (var graphics = System.Drawing.Graphics.FromImage(elementScreenshot))
                            {
                                // Draw the portion of the screenshot that corresponds to the CAPTCHA element
                                graphics.DrawImage(fullImage, new System.Drawing.Rectangle(0, 0, elementSize.Width, elementSize.Height),
                                    new System.Drawing.Rectangle(elementLocation.X, elementLocation.Y, elementSize.Width, elementSize.Height),
                                    System.Drawing.GraphicsUnit.Pixel);
                            }

                            // Save the CAPTCHA image to a file
                            string projectDirectory = Directory.GetCurrentDirectory();
                            string imgFolderPath = Path.Combine(projectDirectory, "img");

                            if (!Directory.Exists(imgFolderPath))
                            {
                                Directory.CreateDirectory(imgFolderPath);
                            }

                            captchaImagePath = Path.Combine(imgFolderPath, "captcha.png");
                            elementScreenshot.Save(captchaImagePath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }

                    // Process the CAPTCHA image to get the text
                    string captchaText = ScanTextFromImage(captchaImagePath);

                    // Fill the CAPTCHA input field
                    IWebElement captchaInputElement = wait.Until(d => d.FindElement(By.Id("captcha")));
                    captchaInputElement.SendKeys(captchaText);

                    // Submit the form or click the submit button
                    IWebElement submitButton = wait.Until(d => d.FindElement(By.Id("submit_button_id"))); // Replace 'submit_button_id' with the actual ID of the submit button
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", submitButton);
                    submitButton.Click();

                    // Optionally, wait for the next page to load
                    await Task.Delay(5000);
                }
            }
        }




        private static string ScanTextFromImage(string imagePath)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                Console.WriteLine("Image file not found.");
                return string.Empty;
            }

            try
            {
                using (var objOcr = OcrApi.Create())
                {
                    // Initialize the OCR engine with the desired language
                    objOcr.Init(Patagames.Ocr.Enums.Languages.English);

                    // Extract and return the text from the image
                    string plainText = objOcr.GetTextFromImage(imagePath);
                    return plainText;
                }
            }
            catch (Exception ex)
            {
                // Handle any potential exceptions and display an error message
                Console.WriteLine($"An error occurred: {ex.Message}");
                return string.Empty;
            }
        }






        // Method to download the image
        private static async Task DownloadImageAsync(string imageUrl, string savePath)
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(imageUrl))
            {
                response.EnsureSuccessStatusCode();
                using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
        }



       
    }
}
