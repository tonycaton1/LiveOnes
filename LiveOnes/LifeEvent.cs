using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveOnes {

	// a single birth/death event
	// note: struct chosen to minimize memory allocations
	struct LifeEvent:IComparable<LifeEvent> 
	{
		public string name;
		public int year;
		public bool isBirthEvent;

		// create a life event based on name, year (as string so we don't have to handle parsing it every time a new life event is added) and a flag for birth/death
		public LifeEvent(string _name, int _year, bool _isBirthEvent = true)
		{
			name = _name;
			System.Diagnostics.Debug.Assert(name != null && name != "");		// check for missing or invalid name

			// parse the year string into a number (range 1900 to 2000)
			year = _year;
			System.Diagnostics.Debug.Assert(year >= 1900 && year <= 2000);		// sanity: problem did state expected range of years so we should verify input (though the algorith functions fine with any year data, including negatives [bc])

			isBirthEvent = _isBirthEvent;
		}

		// allow list sort to correctly sort our events list so years are in correct order (earlier first) and then births come before deaths in the same year
		public int CompareTo(LifeEvent other)
		{
			// first compare years
			var compareYear = year.CompareTo(other.year);
			if (compareYear != 0)
			{
				return compareYear;
			}

			// if years the same then put birth events first
			return other.isBirthEvent.CompareTo(isBirthEvent);		// note the reversed order so births will come before deaths
		}

		// parse the year and return true if it's valid
		static public bool ParseYear(string yearStr, out int outYear)
		{
			if (! int.TryParse(yearStr, out outYear))
			{
				System.Diagnostics.Debug.Assert(false);				// year was not parse-able
				return false;
			}

			// parse the year string into a number (range 1900 to 2000)
			outYear = int.Parse(yearStr);

			// the problem state that years should be in the range of 1900 to 2000 so, if 2-digit years are acceptable, we should bump them up to the correct range
			#if FORCE_TWO_DIGIT_YEAR
			if (outYear < 100)
			{
				outYear += 1900;
			}
			#endif
			System.Diagnostics.Debug.Assert(outYear >= 1900 && outYear <= 2000);		// sanity: problem did state expected range of years so we should verify input (though the algorith functions fine with any year data, including negatives [bc])

			return true;
		}

		// mostly for debugging
		public override string ToString()
		{
			return (isBirthEvent ? "BirthEvent: " : "DeathEvent: ") + name + " Year: " + year.ToString();
		}
	}
}
