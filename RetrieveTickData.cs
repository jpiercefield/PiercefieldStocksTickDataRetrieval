using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiercefieldStocksTickDataRetrieval;
public class RetrieveTickData {
    private string _apiUrl;
    private string _outputDirectory;
    private string _databaseConnectionString;

    public static async Task<int> BeginTickRetrieval(string Symbol, DateTime BeginDate, DateTime EndDate, List<DateTime> holidays) {
        int NumberOfRequests = 0;
        TimeSpan timeSpanDifference = BeginDate.Date - EndDate.Date;
        TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        DateTime currentDate = EndDate.Date;
        while(currentDate <= EndDate.Date) {
            if(currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday && !holidays.Contains(currentDate)) {
                DateTime startDateTime = currentDate.AddHours(8); // start time at 8 AM
                DateTime endDateTime = currentDate.AddDays(119).AddHours(9).AddMinutes(59).AddSeconds(59); // end time at 5 PM (9 hours later than the start time)

                if(endDateTime > EndDate) {
                    endDateTime = EndDate; // set the end time to 5 PM on the end date
                }

                // Do something with startDateTime and endDateTime
                Console.WriteLine("Start time: " + startDateTime.ToString());
                Console.WriteLine("End time: " + endDateTime.ToString());
            }

            currentDate = currentDate.AddDays(1); // move to the next day
        }

        await Task.Delay(1000);

        return NumberOfRequests;
    }
}