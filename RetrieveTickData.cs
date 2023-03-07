using System.Data.SqlClient;
using System.Globalization;
using CsvHelper;
using System.Data;

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

                beginningOfPeriod = DateTime.SpecifyKind(beginningOfPeriod, DateTimeKind.Unspecified);
                endOfPeriod = DateTime.SpecifyKind(endOfPeriod, DateTimeKind.Unspecified);

                DateTime parameterBeginTime = TimeZoneInfo.ConvertTimeToUtc(beginningOfPeriod, easternZone);
                DateTime parameterEndTime = TimeZoneInfo.ConvertTimeToUtc(endOfPeriod, easternZone);
                long fromUnixTime = ((DateTimeOffset)parameterBeginTime).ToUnixTimeSeconds();
                long toUnixTime = ((DateTimeOffset)parameterEndTime).ToUnixTimeSeconds();

                string apiUrl = "https://eodhistoricaldata.com/api/intraday/" + Symbol + ".US?api_token=" + APIKey + "&interval=1m&from=" + fromUnixTime.ToString() + "&to=" + toUnixTime.ToString();

                int responseCode = await DownloadAndInsertCsvFromAPIAsync(apiUrl, DatabaseConnection, Symbol);
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

    private static async Task<int> DownloadAndInsertCsvFromAPIAsync(string url, string DatabaseConnection, string Symbol) {
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

                using(var stream = await response.Content.ReadAsStreamAsync()) {
                    if(stream.Length == 0) {
                        Console.WriteLine("No data returned");
                        return 1;
                    }

                    using var reader = new StreamReader(stream);
                    using(var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) {

                        // Check if there is any data
                        if(reader.Peek() == -1) {
                            Console.WriteLine("No data returned");
                            return 1;
                        }

                        DataTable dataTable = new();
                        if(csv.Read()) {
                            csv.ReadHeader();
                            if(csv.HeaderRecord == null || csv.HeaderRecord.Length <= 1 || csv.HeaderRecord[0].ToUpper() == "VALUE") {
                                Console.WriteLine("End of Symbol from VALUE finished for: " + Symbol);
                                return 1;
                            }

                            foreach(var header in csv.HeaderRecord) {
                                dataTable.Columns.Add(header);                              
                            }

                            dataTable.Columns.Add("Symbol", typeof(string)); // Add Symbol column                           

                        } else {
                            Console.WriteLine("No data returned");
                            return 1;
                        }

                        while(csv.Read()) {
                            var row = dataTable.NewRow();
                            for(var i = 0; i < dataTable.Columns.Count - 1; i++) {
                                row[i] = csv.GetField(i);
                            }

                            if(row.IsNull("Volume")) {
                                row["Volume"] = 0;
                            } else {
                                string? volumeStr = row["Volume"].ToString();
                                if(string.IsNullOrEmpty(volumeStr)) {
                                    row["Volume"] = 0;
                                } else {
                                    if(!long.TryParse(volumeStr, out long volume)) {
                                        row["Volume"] = 0;
                                    }
                                }
                            }

                            row["Symbol"] = Symbol; // Set Symbol value
                            dataTable.Rows.Add(row);
                        }

                        using var connection = new SqlConnection(DatabaseConnection);
                        await connection.OpenAsync();
                        using var transaction = connection.BeginTransaction();
                        try {
                            using(var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)) {
                                bulkCopy.DestinationTableName = "dbo.IntradayData";
                                bulkCopy.BatchSize = 10000;
                                bulkCopy.ColumnMappings.Add("Symbol", "Symbol"); // Add Symbol mapping
                                bulkCopy.ColumnMappings.Add("Timestamp", "Timestamp");
                                bulkCopy.ColumnMappings.Add("Gmtoffset", "Gmtoffset");
                                bulkCopy.ColumnMappings.Add("Datetime", "Datetime");
                                bulkCopy.ColumnMappings.Add("Open", "Open");
                                bulkCopy.ColumnMappings.Add("High", "High");
                                bulkCopy.ColumnMappings.Add("Low", "Low");
                                bulkCopy.ColumnMappings.Add("Close", "Close");
                                bulkCopy.ColumnMappings.Add("Volume", "Volume");

                                try {
                                    bulkCopy.WriteToServer(dataTable);
                                } catch(Exception ex) {
                                    Console.WriteLine($"Error copying data: {ex.Message}");
                                    return -1;
                                }
                            }

                            transaction.Commit();
                        } catch(Exception ex) {
                            transaction.Rollback();
                            connection.Close();
                            Console.WriteLine("Bulk insert failed. Transaction rolled back.");
                            Console.WriteLine(ex.ToString());
                            return -1;
                        }

                        connection.Close();
                    }
                }
                           
                Console.WriteLine("Data was inserted successfully");

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
                currentAttempt++;
            } catch(UnauthorizedAccessException ex) {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Exception occurred, most likely an error regarding unauthorized access to write to the server.");
                currentAttempt++;
            } catch(Exception ex) {
                Console.WriteLine($"Error: {ex.Message}");
                currentAttempt++;
            }
        }

        Console.WriteLine($"Failed to download {url} after {maxAttempts} attempts");
        return -1;
    }
}