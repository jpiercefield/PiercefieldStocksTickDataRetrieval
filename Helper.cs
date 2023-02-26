using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiercefieldStocksTickDataRetrieval;
public class Helper {
    public static List<DateTime> GetUSStockMarketHolidays(DateTime startDate, DateTime endDate) {
        Calendar usCalendar = new CultureInfo("en-US").Calendar; // Create an instance of the US calendar

        List<DateTime> holidays = new(); // Create a list to store the holiday dates

        // Loop through the dates between the start and end dates
        for(DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1)) {
            if(date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) { // Check if the date is a weekend
                holidays.Add(date);
                continue;
            }

            if(usCalendar.IsLeapYear(date.Year)) { // Check if the date is a federal holiday
                // If the year is a leap year, add the leap day to the list of holidays
                if(date.Month == 2 && date.Day == 29) {
                    holidays.Add(date);
                    continue;
                }
            }

            if(date.Month == 1) {
                if(date.Day == 1 || (date.DayOfWeek == DayOfWeek.Monday && date.Day >= 15 && date.Day <= 21)) {
                    holidays.Add(date);
                    continue;
                }
            } else if(date.Month == 2) {
                if(date.DayOfWeek == DayOfWeek.Monday && date.Day >= 15 && date.Day <= 21) {
                    holidays.Add(date);
                    continue;
                }
            } else if(date.Month == 5) {
                if(date.DayOfWeek == DayOfWeek.Monday && date.Day >= 22 && date.Day <= 28) {
                    holidays.Add(date);
                    continue;
                }
            } else if(date.Month == 7) {
                if(date.Day == 4 || (date.DayOfWeek == DayOfWeek.Monday && date.Day >= 1 && date.Day <= 7)) {
                    holidays.Add(date);
                    continue;
                }
            } else if(date.Month == 9) {
                if(date.DayOfWeek == DayOfWeek.Monday && date.Day == 1) {
                    holidays.Add(date);
                    continue;
                }

                if(date.DayOfWeek == DayOfWeek.Monday && date.Day >= 15 && date.Day <= 21) {
                    holidays.Add(date);
                    continue;
                }
            } else if(date.Month == 10) {
                if(date.DayOfWeek == DayOfWeek.Monday && date.Day >= 8 && date.Day <= 14) {
                    holidays.Add(date);
                    continue;
                }

                if(date.DayOfWeek == DayOfWeek.Monday && date.Day == 31) {
                    holidays.Add(date);
                    continue;
                }
            } else if(date.Month == 11) {
                if(date.DayOfWeek == DayOfWeek.Thursday && date.Day == 22) {
                    holidays.Add(date);
                    continue;
                }
            } else if(date.Month == 12) {
                if(date.Day == 25 || (date.DayOfWeek == DayOfWeek.Tuesday && date.Day == 24)) {
                    holidays.Add(date);
                    continue;
                }
            }
        }

        return holidays;
    }
}