namespace TenisDeMasa.Worker.Application.TenisDeMasaForumScrapper;

using System.Text.RegularExpressions;

public class Scraper(ILogger<Scraper> logger)
{
    // Common Romanian locations for tournaments
    private static readonly string[] CommonLocations =
   [
        "Suceava", "Cicarlau", "București", "Focșani", "Vaslui",
        "Iași", "Voinești", "Miroslava", "Sântămăria", "Hunedoara",
        "Ditrău", "Craiova", "Arad", "Topoloveni", "Târgu Mureș", "Timișoara",
        "Ocna Sibiului", "Cluj", "Constanța", "Brașov", "Bacău", "Botoșani"
    ];

    // Romanian month names for date parsing
    private static readonly Dictionary<string, int> RomanianMonths = new()
    {
        { "ianuarie", 1 }, { "februarie", 2 }, { "martie", 3 }, { "aprilie", 4 },
        { "mai", 5 }, { "iunie", 6 }, { "iulie", 7 }, { "august", 8 },
        { "septembrie", 9 }, { "octombrie", 10 }, { "noiembrie", 11 }, { "decembrie", 12 }
    };

    public async Task<List<Tournament>> ScrapeAndExtractInfo()
    {
        string BaseUrl = "https://www.tenisdemasa.ro/forum/node/25?filter_sort=created&filter_time=time_today";

        var extractedTournaments = new List<Tournament>();

        // Initialize Playwright
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

        var page = await browser.NewPageAsync();

        // Load from URL
        logger.LogInformation("Navigating to the forum...");
        await page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Wait for the topic list to load
        await page.WaitForSelectorAsync("tr.topic-item");

        // Get all topic items
        var topicItems = await page.QuerySelectorAllAsync("tr.topic-item");
        logger.LogInformation("Found {topicItemsCount} topic items", topicItems.Count);

        foreach (var item in topicItems)
        {
            try
            {
                var tournamentInfo = new Tournament();

                // Extract title
                var titleElement = await item.QuerySelectorAsync("a.topic-title");
                if (titleElement != null)
                {
                    tournamentInfo.Title = await titleElement.TextContentAsync();

                    var href = await titleElement.GetAttributeAsync("href");
                    tournamentInfo.Url = href;

                    try
                    {
                        var url = new Uri(href);
                        var id = int.Parse(url.Segments.Last());
                        tournamentInfo.Id = id;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error parsing URL {href}", href);
                    }

                    // Extract location from title
                    tournamentInfo.Location = ExtractLocation(tournamentInfo.Title);

                    // Extract date from title
                    var dateInfo = ExtractDateAndTime(tournamentInfo.Title);
                    tournamentInfo.Date = dateInfo.date;
                    tournamentInfo.Time = dateInfo.time;
                }

                // Extract category
                var categoryElement = await item.QuerySelectorAsync("a.js-topic-prefix");
                if (categoryElement != null)
                {
                    tournamentInfo.Category = await categoryElement.TextContentAsync();
                    // Clean up category text if needed
                    tournamentInfo.Category = Regex.Replace(tournamentInfo.Category, @"AmaTur:\s*", "").Trim();
                }

                // Extract author
                var authorElement = await item.QuerySelectorAsync("div.topic-info a");
                if (authorElement != null)
                {
                    tournamentInfo.Author = await authorElement.TextContentAsync();
                }

                // Extract created date
                var dateElement = await item.QuerySelectorAsync("div.topic-info span.date");
                if (dateElement != null)
                {
                    string dateText = await dateElement.TextContentAsync();
                    if (ParseRomanianDate(dateText.Trim(), out DateTime createdDate))
                    {
                        tournamentInfo.CreatedDate = createdDate;
                    }
                }

                // Extract replies
                var repliesElement = await item.QuerySelectorAsync("div.posts-count");
                if (repliesElement != null)
                {
                    string repliesText = await repliesElement.TextContentAsync();
                    var repliesCount = repliesText.Split(' ')[0];
                    tournamentInfo.Replies = int.Parse(repliesCount);
                }

                // Extract last post info
                var lastPostByElement = await item.QuerySelectorAsync("div.lastpost-by a");
                if (lastPostByElement != null)
                {
                    tournamentInfo.LastPostBy = await lastPostByElement.TextContentAsync();
                }

                // Extract last post date
                var lastPostDateElement = await item.QuerySelectorAsync("span.post-date");
                if (lastPostDateElement != null)
                {
                    string lastPostDateText = await lastPostDateElement.TextContentAsync();
                    if (ParseRomanianDate(lastPostDateText.Trim(), out DateTime lastPostDate))
                    {
                        tournamentInfo.LastPostDate = lastPostDate;
                    }
                }

                extractedTournaments.Add(tournamentInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing topic item");
            }
        }

        return extractedTournaments;
    }

    // Extract location from title using the common locations list
    private string ExtractLocation(string title)
    {
        if (string.IsNullOrEmpty(title))
            return null;

        // Check for direct matches of known locations
        foreach (string location in CommonLocations)
        {
            if (title.Contains(location, StringComparison.OrdinalIgnoreCase))
            {
                return location;
            }
        }

        // Try to find locations in parentheses - common format
        var locationInParenthesesMatch = Regex.Match(title, @"\(([^)]+)\)");
        if (locationInParenthesesMatch.Success)
        {
            string potentialLocation = locationInParenthesesMatch.Groups[1].Value;
            // Check if it contains any common Romanian county codes
            if (potentialLocation.Contains("MM") || potentialLocation.Contains("SV") ||
                potentialLocation.Contains("IS") || potentialLocation.Contains("BT") ||
                potentialLocation.Contains("HD") || potentialLocation.EndsWith("jud."))
            {
                return potentialLocation;
            }
        }

        return null;
    }

    // Extract date and time from title
    private (string date, string time) ExtractDateAndTime(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return (null, null);
        }

        string date = null;
        string time = null;

        // Check for numeric date formats (DD.MM.YYYY or DD/MM/YYYY)
        var numericDateMatch = Regex.Match(title, @"(\d{1,2})[\/\.\-](\d{1,2})[\/\.\-](\d{2,4})");
        if (numericDateMatch.Success)
        {
            date = numericDateMatch.Value;

            // Look for time near the date
            var timeMatch = Regex.Match(title, @"(\d{1,2}):(\d{2})");
            if (timeMatch.Success)
            {
                time = timeMatch.Value;
            }

            return (date, time);
        }

        // Check for date with month name (e.g., "22 martie 2025")
        foreach (var month in RomanianMonths.Keys)
        {
            if (title.Contains(month, StringComparison.OrdinalIgnoreCase))
            {
                var romanianDateMatch = Regex.Match(
                    title,
                    $@"(\d{{1,2}})\s+{month}\s+(\d{{4}})",
                    RegexOptions.IgnoreCase
                );

                if (romanianDateMatch.Success)
                {
                    date = romanianDateMatch.Value;
                    break;
                }
            }
        }

        return (date, time);
    }

    // Parse Romanian date formats
    private bool ParseRomanianDate(string dateText, out DateTime result)
    {
        result = DateTime.Now;

        try
        {
            // Handle special cases like "Astăzi, 19:20" (Today)
            if (dateText.StartsWith("Astăzi", StringComparison.OrdinalIgnoreCase))
            {
                string time = dateText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
                string[] timeParts = time.Split(':');

                result = DateTime.Today.AddHours(int.Parse(timeParts[0])).AddMinutes(int.Parse(timeParts[1]));
                return true;
            }
            // Handle "Ieri, 20:43" (Yesterday)
            else if (dateText.StartsWith("Ieri", StringComparison.OrdinalIgnoreCase))
            {
                string time = dateText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
                string[] timeParts = time.Split(':');

                result = DateTime.Today.AddDays(-1).AddHours(int.Parse(timeParts[0]))
                    .AddMinutes(int.Parse(timeParts[1]));
                return true;
            }
            // Handle regular dates like "15.mar.2025, 00:03"
            else
            {
                // Create dictionary for Romanian month abbreviations
                var romanianMonths = new Dictionary<string, int>
                {
                    { "ian", 1 }, { "feb", 2 }, { "mar", 3 }, { "apr", 4 }, { "mai", 5 }, { "iun", 6 },
                    { "iul", 7 }, { "aug", 8 }, { "sep", 9 }, { "oct", 10 }, { "noi", 11 }, { "dec", 12 }
                };

                var parts = dateText.Split(new[] { ',', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 4)
                {
                    int day = int.Parse(parts[0]);

                    int month = 1;
                    var monthText = parts[1].ToLower();
                    if (romanianMonths.ContainsKey(monthText))
                    {
                        month = romanianMonths[monthText];
                    }

                    int year = int.Parse(parts[2]);

                    string[] timeParts = parts[3].Split(':');
                    int hour = int.Parse(timeParts[0]);
                    int minute = int.Parse(timeParts[1]);

                    result = new DateTime(year, month, day, hour, minute, 0);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation($"Error parsing date {dateText}: {ex.Message}");
        }

        return false;
    }
}
