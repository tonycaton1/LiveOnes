//#define FULL_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

/*
 * PROGRAMMING PROBLEM:
 *
 * Description:
 *	Given a list of people with their birth and end years (all between 1900 and 2000), find the year with the most number of people alive.
 *
 * Code:
 *	Solve using a language of your choice and dataset of your own creation.
 *
 *Submission:
 *	Please upload your code, dataset, and example of the program’s output to Bit Bucket or Github. Please include any graphs or charts created by your program.
 *
 */


namespace LiveOnes {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow:Window {

		// member variables

		int mostLiveOnes = 0;										// what is the highest number of people alive at the same time
		List<LifeEvent> lifeEventsList = null;						// list of all birth/death events which will be sorted by date after all events have been added from file drop
		List<int[]> mostLiveOnesYearsList = null;					// list of all year ranges when the highest number of people were alive at the same time

		// member methods

		// constructor
		public MainWindow() 
		{
			InitializeComponent();
			ResetData();
		}

		// handle csv files containing birth/death information by name being dropped into main window
		//	1) reads files and shows names/dates
		//	2) creates a sorted list of names by birth/death date so the list can be walked
		//	3) determines year(s) when most people are alive and outputs the results
		private void MainWindow_Drop(object sender, DragEventArgs dragEventArg)
		{
			if (dragEventArg.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] droppedFiles = dragEventArg.Data.GetData(DataFormats.FileDrop, true) as string[];

				if (droppedFiles.Any())
				{
					ResetData();

					bool wasAnyInfoFound = false;
					foreach (string droppedFile in droppedFiles)
					{
						wasAnyInfoFound |= ReadBirthDeathInfo(droppedFile);
					}

					// now use the above-read information to determine the most people alive at the same time and when that was
					if ( VERIFY (wasAnyInfoFound && lifeEventsList.Any()))				// we should have had at least one valid name with at least one valid birth year
					{
						DEBUG("Total life events: " + lifeEventsList.Count().ToString());

						// sorting life events by year then birth first so we can walk determine the most people were alive and when
						// note: events themselves handle sorting order so lower years come first, then births before deaths
						lifeEventsList.Sort();

						// process the sorted life events, incrementing count for each birth, decrementing for each death to determine when most were alive
						ASSERT(mostLiveOnes == 0);					// should have been reset in resetdata (above)
						ProcessLifeEventsList();
						ASSERT(mostLiveOnes > 0);					// we should have found at least 1 birth record so there should be at least 1 person alive sometime!
					}

					// handle no data found error message
					else
					{
						Output("ERROR: No valid data found so no dates were generated. Please be sure to supply a properly-formatted csv file with name, birthyear, [deathyear].");
					}
				}
			}
		}

		// allow drag/drop functionality
		private void RichTextBox_Drag(object sender, DragEventArgs dragEventArg)
		{
			dragEventArg.Handled = true;
		}

		// reset all data and clear output to reset at startup or when new files are dropped
		private void ResetData()
		{
			ClearOutput();

			mostLiveOnes = 0;
			lifeEventsList = new List<LifeEvent>();
			mostLiveOnesYearsList = new List<int[]>();
		}

		// reads one csv file and adds birth/death info to life events list
		private bool ReadBirthDeathInfo(string csvFileName)
		{
			bool wasInfoFound = false;

			Output("Data from: " + csvFileName);

			string[] lines = System.IO.File.ReadAllLines(csvFileName);
		    foreach (string line in lines)
			{
				if (line != "")
				{
					var fields = line.Split(',');
					if ( VERIFY (fields.Length >= 2))
					{
						var name = fields[0].Trim();
						int year;
						if (LifeEvent.ParseYear(fields[1], out year))
						{
							// remember we found a record so we can return success
							wasInfoFound = true;

							// add the birth event from the current line of the csv file
							var birthEvent = new LifeEvent(name, year);
							lifeEventsList.Add(birthEvent);
							DEBUG("Birth: " + birthEvent.ToString(), 2);

							bool wasDeathYearFound = false;

							// check for death field (if death field is missing then person is still alive)
							if (fields.Length >= 3)
							{
								var deathYearStr = fields[2].Trim();

								// if death field is left blank then person is still alive
								if (deathYearStr != "")
								{
									if (LifeEvent.ParseYear(deathYearStr, out year))
									{
										var deathEvent = new LifeEvent(name, year, false);
										ASSERT(deathEvent.year >= birthEvent.year);	// we should have been born BEFORE we died, no?!
										lifeEventsList.Add(deathEvent);
										Output(name + " (" + birthEvent.year.ToString() + " - " + deathEvent.year.ToString() + ")", 1);
										wasDeathYearFound = true;

										DEBUG("Death: " + deathEvent.ToString(), 2);
									}
								}
							}

							// if no death year was found then display name with 'birth - present' format
							if (! wasDeathYearFound)
							{
								Output(name + " (" + birthEvent.year.ToString() + " - present)", 1);
							}
						}
					}
				}
		    }

			// handle error condition: no names in file
			if (! wasInfoFound)
			{
				ASSERT(false);										// failure: no names and valid dates found in file
				Output("ERROR: No properly-formatted names and dates found in file.", 1);
			}

			// skip a line after showing all names in csv file
			Output();

			return wasInfoFound;
		}

		// processes all life events to determine the total number of people alive at once and when
		// note: the 'when' could be one year, a range of years, or a list of ranges of years, and could include people still alive (with death field left blank)
		private void ProcessLifeEventsList()
		{
			ASSERT(mostLiveOnes == 0);								// should have been reset--note: this could be done now but wasteful unless needed
			ASSERT(! mostLiveOnesYearsList.Any());					// should have been reset--note: this could be done now but wasteful unless needed

			var numLiveOnes = 0;
			int mostRecentBirthYear = 0;

			// iterate through the life events to determine most living people and when
			// note: there may be multiple ranges when the most people were alive so all times that qualify are added to the list of dates
			foreach(var e in lifeEventsList)
			{
				DEBUG("Processing event: " + e.ToString(), 2);
				if (e.isBirthEvent)
				{
					++ numLiveOnes;
					mostRecentBirthYear = e.year;					// remember the starting year for the current range
				}
				else
				{
					UpdateLiveOnesForDeathYear(numLiveOnes, mostRecentBirthYear, e.year);
					-- numLiveOnes;
				}
			}

			// handle most alive at end of run (if last life event was a birth which makes the most people alive)
			UpdateLiveOnesForDeathYear(numLiveOnes, mostRecentBirthYear);	// note: this allows for all those people who are still alive at the end of the search
			ASSERT(mostLiveOnes > 0);
			ASSERT(mostLiveOnesYearsList.Any());

			// show years when the above total number of people were alive at the same time
			if (! mostLiveOnesYearsList.Any())
			{
				ASSERT(false);										// did not find any years for people being alive
				Output("ERROR: No years found for people being alive at the same time.");
			}
			else
			{
				// format number alive
				// note: this is obviously slower than doing it inline below, but appears cleaner to me and, as it is not in the main computational loop, but done once per execution, I feel the tradeoff is worth it
				var aliveStr = "Most number of people alive at the same time was " + mostLiveOnes.ToString();

				// if we have only 1 range then format as a single line with result
				if (mostLiveOnesYearsList.Count() == 1)
				{
					var years = mostLiveOnesYearsList[0];
					ASSERT(years.Length == 2);

					// if only 1 year (not a range) then format it as a single value
					if (years[0] == years[1])
					{
						ASSERT(years[0] != int.MaxValue);
						Output(aliveStr + " in the year " + years[0].ToString() + ".");
					}

					// if result is a range of years then format output as such, including the possibility of handling through 'present'
					else
					{
						Output(aliveStr + " between the years of " + years[0].ToString() + " and " + (years[1] == int.MaxValue ? "present" : years[1].ToString()) + ".");
					}
				}

				// if there was more than 1 time when the max number of people alive at the same time were alive (i.e. 1979-1985 AND 1990-present)
				else
				{
					Output(aliveStr + " in the following years: ");

					// iterate through all year ranges (i.e. 1979-1985)
					foreach(var years in mostLiveOnesYearsList)
					{
						// if single year then just show that year (don't do 1985-1985)
						if (years[0] == years[1])
						{
							ASSERT(years[0] != int.MaxValue);
							Output(years[0].ToString(), 1);
						}
						
						// if range of years then format it correctly, including the possibility of handling through 'present'
						else
						{
							Output(years[0].ToString() + "-" + (years[1] == int.MaxValue ? "present" : years[1].ToString()), 1);
						}
					}
				}
			}
		}

		// when a death year is found in the events list (or at end to handle those alive through present time), update the most live ones as well as the list of years they were alive (if necessary)
		private void UpdateLiveOnesForDeathYear(int numLiveOnes, int mostRecentBirthYear, int deathYear = int.MaxValue)
		{
			ASSERT(mostRecentBirthYear <= deathYear);

			if (numLiveOnes > mostLiveOnes)
			{
				mostLiveOnesYearsList.Clear();
				mostLiveOnes = numLiveOnes;
			}
			if (numLiveOnes >= mostLiveOnes)
			{
				ASSERT(mostRecentBirthYear <= deathYear);
				mostLiveOnesYearsList.Add(new int[] { mostRecentBirthYear, deathYear } );
			}
		}

		// clear the output window
		private void ClearOutput()
		{
			richTextBoxParagraph.Inlines.Clear();
		}

		// add a line of text to the output window (indenting it, if required)
		private void Output(string s = "", int indentation = 0)
		{
			ASSERT(indentation >= 0);
			richTextBoxParagraph.Inlines.Add(new Run(new string('\t', indentation) + s + "\n"));
		}

		// debugging tools

		// output a line of debugging text to the output window if the conditional flag is set
		[System.Diagnostics.Conditional("FULL_DEBUG")]
		private void DEBUG(string s = "", int indentation = 0)
		{
			Output(s,indentation);	
		}

		// assert a value is true (simply calls debug assert but easier to type)
		[System.Diagnostics.Conditional("DEBUG")]
		private void ASSERT(bool b)
		{
			System.Diagnostics.Debug.Assert(b);
		}

		// asserts value is true (as above) but also returns result so conditional instruction can use the value as well
		private bool VERIFY(bool b)
		{
			System.Diagnostics.Debug.Assert(b);						// pops up a trace window in debug mode only (release does nothing)
			return b;
		}
	}
}
