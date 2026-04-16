using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ASTDAT.Web.Infrastructure
{
	public static class DateTimeExtensions
	{
		public static List<Func<DateTime, bool>> IsUSAHolidays = new List<Func<DateTime, bool>>
		{
			//(1) New Year's is always January 1
			(d) => { return d.Month == 1 && d.Day == 1; },
			
			//(2) Christmas is always December 25
			(d) => { return d.Month == 12 && d.Day == 25; },
			
			//(3) Independence Day is always July 4
			(d) => { return d.Month == 7 && d.Day == 4; },
			
			//(4) Memorial Day - last Monday in May
			(d) => {
				if (d.Month != 5 || d.DayOfWeek != DayOfWeek.Monday) return false; // not Monday

				return d.AddDays(7).Month != d.Month; //next monday is next month
			},
			
			//(5) Labor Day is the first Monday in September
			(d) => {
				return d.Month == 9 && d.DayOfWeek == DayOfWeek.Monday && d.Day < 8; //is Monday and day < 8
			},

			//(6) Thanksgiving Day is the fourth Thursday in November
			(d) => {
				int nthWeekDay  = (int)(Math.Ceiling((double)d.Day / 7.0d));
				return d.Month == 11 && d.DayOfWeek == DayOfWeek.Thursday && nthWeekDay == 4;
			},
		};

		public static bool IsUSAHoliday(this DateTime dt)
		{
			foreach(var q in IsUSAHolidays)
			{
				if (q(dt))
				{
					return true;
				}
			}

			return false;
		}
	}
}