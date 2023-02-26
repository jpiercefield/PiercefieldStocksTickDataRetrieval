using System;

namespace PiercefieldStocksTickDataRetrieval;

class Program
{
	static async Task Main(string[] args)
	{
        Console.WriteLine("Welcome to Piercefield Stocks Tick Data Retrieval.");
        Console.WriteLine("WARNING: This can amount in a very large amount of data being transferred to your SQL Server.");
        Console.WriteLine("WARNING: You must have a valid EOD Historical Data API key for this to work and must be placed within your computer's enviornment variables named as: MyAPIToken");
        Console.WriteLine("WARNING: You are solely responsible for what you do with this program, the developer accepts no responsibility for your actions.");
        Console.WriteLine("WARNING: Ensure your database has been set-up correctly. The table you're inserting to will need to align with the CSV file's columns. You can always change them later.");
        Console.WriteLine("WARNING: By default, this program is set-up to insert nearly 2.2 BILLION rows of data into your SQL server, so ensure if you're using these default values that you have adequate storage.");
        Console.WriteLine("WARNING: Do your own math to determine exactly how much storage space you'll need. Default could need around 5-6 TB for redundancies and speed, depends on how you set-up your DB table though.");
        Console.WriteLine("WARNING: I reccomend removing any indexes on your table prior to inserting this data, this will help with processing speeds.");
        string apiKey = Environment.GetEnvironmentVariable("MyAPIToken") ?? ""; //Inserting this into computer's enviornment variables, keeps it safe. 
        if(apiKey == string.Empty) {
            Console.WriteLine("Your API Key is empty.");
        } else {
            List<string> symbols = RetrieveSymbols.BeginSymbolRetrieval(); //I personally am only looking for an updated list of Mega, Large, and Medium Cap US Stocks from NASDAQ (NSM/Global Select) and NYSE. You can modify the code to meet your needs.
            if(symbols == null || symbols.Count == 0) {
                Console.WriteLine("No Symbols were retrieved from NASDAQ.com. Check your connection and parameters.");
            } else {
                Console.WriteLine("There were " + symbols.Count + " symbols returned from NASDAQ.com.");

                Console.Write("Enter Begin Date (e.g. 10/22/1987): ");
                string? strDate = Console.ReadLine();
                DateTime dtmBeginDate = new();
                DateTime dtmEndDate = new();
                bool Error = true;
                if(string.IsNullOrEmpty(strDate)) {
                    Console.WriteLine("No Begin Date Entered. January 1st, of 12 years ago will be the default begin date and today will be the end date.");              
                } else if(DateTime.TryParse(strDate, out dtmBeginDate)) {
                    Console.Write("Enter Begin Date (e.g. 10/22/1987): ");
                    strDate = Console.ReadLine();
                    if(string.IsNullOrEmpty(strDate) ) {
                        Console.WriteLine("No End Date Entered. January 1st, of 12 years ago will be the default begin date and today will be the end date.");
                    } else if(DateTime.TryParse(strDate, out dtmEndDate)) {
                        if(dtmBeginDate >= dtmEndDate) {
                            Console.WriteLine("End Date must be after the Begin Date. January 1st, of 12 years ago will be the default begin date and today will be the end date.");
                        } else {
                            Error = false;
                        }
                    } else {
                        Console.WriteLine("Incorrect End Date Format Entered. January 1st, of 12 years ago will be the default begin date and today will be the end date.");
                    }
                } else {
                    Console.WriteLine("Incorrect Begin Date Format Entered. January 1st, of 12 years ago will be the default begin date and today will be the end date.");
                }

                if(Error) {
                    Console.WriteLine("If you do not agree to the outcome above, press 1 or exit the program now. Otherwise, 12 years will default.");
                    string? input = Console.ReadLine();
                    if(!string.IsNullOrEmpty(input) && input.Equals("1")) {
                        Environment.Exit(0);
                    } else {
                        dtmBeginDate = new DateTime(DateTime.Now.Year - 12, 1, 1);
                        dtmEndDate = DateTime.Today;
                    }
                }

                int beginIndex = 0;
                TimeSpan differenceOfDate = dtmBeginDate - dtmEndDate;
                int numberOfDays = differenceOfDate.Days;
                int numberOfMaxRequestsPerSymbol = (int)Math.Ceiling((double)numberOfDays / 120); // 120 Days is the Max Amount of Tick Data you can receive at a time per symbol.
                if(numberOfMaxRequestsPerSymbol * symbols.Count > 20000 ) { //100,000 API calls allowed per day, each request counts as 5 API calls, so 20,000 API calls allowed/per day
                    Console.WriteLine("Warning: The number of requests that this calculation will take, exceed the number of maximum allowed requests from the API per day. ");
                    Console.WriteLine("The program will request as much data as possible today, then it will output the last totally retrieved Symbol to the Console and a Text File (named PiercefieldSymbolLast########.txt) to your desktop.");
                    Console.WriteLine("This program is smart enough to not begin work on retrieving data for a new Symbol unless the full amount of data can be requested before running out of daily API calls.");
                    Console.WriteLine("Once restarting the program the following day, you will be given an opportunity to insert a symbol name from where the program previously left off at.");
                    Console.WriteLine("The program will then begin retrieving the day for the next symbol in the list from the symbol given.");
                    Console.Write("\n\n Do you have a Symbol you'd like to enter for this reasoning? If so, enter the symbol now, otherwise enter 1 to skip: ");
                    string? input = Console.ReadLine();
                    if(string.IsNullOrEmpty(input)) {
                        Console.WriteLine("No valid input received, exiting program in 5 seconds.");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    } else {
                        if(!input.Equals("1")) {
                            beginIndex = symbols.IndexOf(input);
                            if(beginIndex == -1) {
                                Console.WriteLine("The index you entered does not exist in the list of symbols. Exiting the program in 5 seconds.");
                                Thread.Sleep(5000);
                                Environment.Exit(0);
                            }
                        }
                    }
                }

                
                dtmBeginDate = dtmBeginDate.Date + TimeSpan.FromHours(8); // 8 AM 
                dtmEndDate = dtmBeginDate.Date + TimeSpan.FromHours(17); // 5 PM

                List<DateTime> holidays = Helper.GetUSStockMarketHolidays(dtmBeginDate.Date, dtmEndDate.Date);

                Console.WriteLine("Data Retrieval and Insertion is about to commence. Multi-threading or various synchro approaches could be implemented here. I'm using async programming as I only want to do this once.");
                Console.WriteLine("Independent benchmarking should be determined if you feel as if you can implement that for your system.");
                Console.WriteLine("Additionally, various speed adjustments can be made within the tick retrieval code.");
                Thread.Sleep(1000);
                Console.WriteLine("5...");
                Thread.Sleep(1000);
                Console.WriteLine("4...");
                Thread.Sleep(1000);
                Console.WriteLine("3...");
                Thread.Sleep(1000);
                Console.WriteLine("2...");
                Thread.Sleep(1000);
                Console.WriteLine("Houston, we have lift off...");

                int numberOfRequests = 0;
                while(20000 - numberOfRequests >= numberOfMaxRequestsPerSymbol && beginIndex < symbols.Count) { //Don't start a new symbol if we don't have the available requests or if all symbols are done.
                    numberOfRequests += await RetrieveTickData.BeginTickRetrieval(symbols[beginIndex], dtmBeginDate, dtmEndDate, holidays);                  
                    beginIndex++;
                }

                if(beginIndex == symbols.Count) {
                    Console.WriteLine("The program has finished fully, congratulations!");
                } else {
                    Console.WriteLine("You've reached your maximum API calls for the day, the last successfully retrieved Symbol was: " + symbols[beginIndex-1]);
                    Console.WriteLine("Type this symbol in tomorrow to begin where you left off.");
                    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PiercefieldSymbolLast" + DateTime.Now.ToString("MMddyyyy")+ ".txt");
                    string content = "Last symbol on " + DateTime.Now.ToShortDateString() + " was: " + symbols[beginIndex - 1] + ". Type this symbol in tomorrow to begin where you left off.";
                    File.WriteAllText(filePath, content);
                }
            }
        }

        Console.WriteLine("Program has completed. Press any key to exit...");
        Console.ReadLine();
    }
}