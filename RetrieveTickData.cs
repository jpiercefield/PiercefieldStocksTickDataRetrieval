using System.Data.SqlClient;

namespace PiercefieldStocksTickDataRetrieval;
public class RetrieveTickData {
    private static readonly HttpClient _httpClient = new();
    private static readonly CancellationTokenSource _cancellationTokenSource = new();

    public static async Task<int> BeginTickRetrieval(string APIKey, string Symbol, DateTime BeginDate, DateTime EndDate, List<DateTime> holidays, string DatabaseConnection) {
        int NumberOfRequests = 0;
        TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        //Need to start at EndDate and work backwards.
        DateTime currentDate = EndDate.Date;
        while(currentDate >= BeginDate.Date) {
            if(currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday && !holidays.Contains(currentDate)) {
                DateTime beginningOfPeriod = currentDate.Date.AddDays(-119).Date.AddHours(8); // 8 AM 119 days before
                DateTime endOfPeriod = currentDate.Date.AddHours(17); // end time at 5 PM

                if(beginningOfPeriod.Date < BeginDate.Date) {
                    beginningOfPeriod = BeginDate.Date;
                    beginningOfPeriod.Date.AddHours(8);
                }

                DateTime parameterBeginTime = TimeZoneInfo.ConvertTimeToUtc(beginningOfPeriod, easternZone);
                DateTime parameterEndTime = TimeZoneInfo.ConvertTimeToUtc(endOfPeriod, easternZone);
                long fromUnixTime = ((DateTimeOffset)parameterBeginTime).ToUnixTimeSeconds();
                long toUnixTime = ((DateTimeOffset)parameterEndTime).ToUnixTimeSeconds();

                string apiUrl = "https://eodhistoricaldata.com/api/intraday/" + Symbol + ".US?api_token=" + APIKey + "interval=1m&from=" + fromUnixTime.ToString() + "&to=" + toUnixTime.ToString();

                int responseCode = await DownloadAndInsertCsvFromAPIAsync(apiUrl, DatabaseConnection);
                if(responseCode == - 1) {
                    Console.WriteLine("There was an issue with your request, please see the output above.");
                    Console.WriteLine("Press 1 to continue, otherwise type anything else and the program will exit.");
                    string? input = Console.ReadLine();
                    if(input != null && input == "1") {
                        Console.WriteLine("Program is continuing..");
                    } else {
                        Console.WriteLine("Program exiting in 5 seconds..");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    }
                } else if(responseCode == 1) {
                    return NumberOfRequests++;
                }

                currentDate = parameterBeginTime.Date;
                NumberOfRequests++;
            }
       
            currentDate = currentDate.AddDays(-1); // move to the next day before
        }

        return NumberOfRequests;
    }

    private static async Task<int> DownloadAndInsertCsvFromAPIAsync(string url, string DatabaseConnection) {
        int maxAttempts = 20;
        int currentAttempt = 0;

        while(currentAttempt < maxAttempts) {
            try {
                if(!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) {
                    Console.WriteLine("No internet connection, retrying in 30 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    continue;
                }

                HttpResponseMessage response = await _httpClient.GetAsync(url, _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                string csvData = await response.Content.ReadAsStringAsync();
                if(csvData.Trim().Equals("Value", StringComparison.OrdinalIgnoreCase)) {
                    Console.WriteLine("No data returned");
                    return 1;
                }

                File.WriteAllText("\\\\servername\\sharename\\data.csv", csvData); //ToDo: Once Synology NAS arrive's I'll configure directories

                // Insert the CSV data into the database
                using(var connection = new SqlConnection(DatabaseConnection)) {
                    await connection.OpenAsync();
                    //ToDo: Once Synology NAS arrive's I'll configure directories
                    using var command = new SqlCommand("INSERT INTO yourtablename SELECT * FROM OPENROWSET('MSDASQL', 'Driver={Microsoft Access Text Driver (*.txt, *.csv)};DefaultDir=\\\\servername\\sharename;Extensions=csv;HDR=YES;CharacterSet=65001;', 'SELECT * FROM [" + "data.csv" + "]')", connection); 
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if(rowsAffected == -1) {
                        Console.WriteLine("Data was not inserted correctly");
                        return -1;
                    }
                }

                Console.WriteLine("Data was inserted successfully");
                //ToDo: Once Synology NAS arrive's I'll configure directories
                File.Delete("\\\\servername\\sharename\\data.csv"); // Delete the file from the server

                return 0;
            } catch(HttpRequestException ex) {
                Console.WriteLine($"Error: {ex.Message}");

                if(ex.InnerException is System.Net.Sockets.SocketException) {
                    Console.WriteLine("Lost internet connection, retrying in 30 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                } else {
                    Console.WriteLine($"Failed to connect to {url}, retrying in 30 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }

                currentAttempt++;
            } catch(TaskCanceledException) {
                Console.WriteLine($"Download of {url} cancelled");
                return -1;
            } catch(UnauthorizedAccessException ex) {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Exception occurred, most likely an error regarding unauthorized access to write to the server.");
                return -1;
            } catch(Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        Console.WriteLine($"Failed to download {url} after {maxAttempts} attempts");
        return -1;
    }
}