// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Linq;
using Daysim.ChoiceModels;
using Daysim.DomainModels;
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Roster;
using Ninject;
using System.Collections.Generic;
using System.Collections;

using Daysim.ChoiceModels.H;
using Daysim.Interfaces;

namespace Daysim.ModelRunners {
	public sealed class HouseholdChoiceModelRunner : IChoiceModelRunner {
		private readonly HouseholdWrapper _household;
		private static object _lock = new object();

		public HouseholdChoiceModelRunner(IHousehold household) {
			_household = (HouseholdWrapper) Global.Kernel.Get<HouseholdWrapperFactory>().HouseholdWrapperCreator.CreateWrapper(household);
		}

		public void SetRandomSeed(int randomSeed) {
			_household.RandomUtility.ResetHouseholdSynchronization(randomSeed);
			_household.RandomUtility.ResetUniform01(randomSeed);
			_household.Init();
		}

		public void RunChoiceModels() {
			RunPersonModels();
			RunHouseholdModels();
			RunHouseholdDayModels();

			UpdateHousehold();

			if (ChoiceModelFactory.ThreadQueue != null) {
				ChoiceModelFactory.ThreadQueue.Add(this);
			}
		}

		private void RunHouseholdModels() {
			if (!Global.Configuration.ShouldRunHouseholdModels) {
				return;
			}

#if RELEASE
			try {
#endif
			lock (_lock) {
				ChoiceModelFactory.TotalTimesHouseholdModelSuiteRun++;
			}
			RunHouseholdModelSuite(_household);
#if RELEASE
			}
			catch (Exception e) {
				throw new HouseholdModelException(string.Format("Error running household models for {0}.", _household), e);
			}
#endif
		}

		private void RunPersonModels() {
			if (!Global.Configuration.ShouldRunPersonModels) {
				return;
			}

			foreach (var person in _household.Persons) {
#if RELEASE
				try {
#endif
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonModelSuiteRun++;
				}
				RunPersonModelSuite(person);
#if RELEASE
				}
				catch (Exception e) {
					throw new PersonModelException(string.Format("Error running person models for {0}.", person), e);
				}
#endif
			}
		}

		private void RunHouseholdDayModels() {
			if (!Global.Configuration.ShouldRunPersonDayModels) {
				return;
			}

			foreach (HouseholdDayWrapper householdDay in _household.HouseholdDays) {
#if RELEASE
				try {
#endif
				lock (_lock) {
					ChoiceModelFactory.TotalHouseholdDays++;  //TODO:  John M.  This replaces TotalPersonDays, but TotalPersonDays is used in Engine, so that code probably needs to be patched
				}
				var simulatedAnInvalidHouseholdDay = false;

				while (!householdDay.IsValid && (!Global.Configuration.IsInEstimationMode || !simulatedAnInvalidHouseholdDay)) { //don't retry household in estimation mode

					if (Global.Configuration.InvalidAttemptsBeforeContinue > 0 && householdDay.AttemptedSimulations > Global.Configuration.InvalidAttemptsBeforeContinue) {
						Global.PrintFile.WriteLine("***** Household day for household {0} invalid after {1} attempts", householdDay.Household.Id, householdDay.AttemptedSimulations);
						break;
					}
					else

						householdDay.IsValid = true;
					foreach (var personDay in householdDay.PersonDays) {
						personDay.IsValid = true;
					}

					//mbtrace
					Global.TraceResults = (Global.Configuration.TraceModelResultValidity && householdDay.AttemptedSimulations >= Global.Configuration.InvalidAttemptsBeforeTrace);
					//mbtrace
					if (Global.TraceResults) Global.PrintFile.WriteLine("> RunHouseholdDayModels for household {0}, attempt {1}", householdDay.Household.Id, householdDay.AttemptedSimulations);

					lock (_lock) {
						ChoiceModelFactory.TotalTimesHouseholdDayModelSuiteRun++;
					}
					RunHouseholdDayModelSuite((HouseholdDayWrapper) householdDay);

					// householdDay is invalid if any person day is invalid
					foreach (var personDay in householdDay.PersonDays) {
						if (personDay.IsValid == false) {
							householdDay.IsValid = false;
						}
					}

					// exits the loop if the household's day is valid
					if (householdDay.IsValid) {
						// after updating park and ride lot loads
						foreach (var personDay in householdDay.PersonDays) {
							if (!Global.Configuration.IsInEstimationMode && personDay.Tours != null) {
								foreach (var tour in personDay.Tours.Where(tour => tour.Mode == Constants.Mode.PARK_AND_RIDE)) {
									tour.SetParkAndRideStay();
								}
							}
						}

						break;
					}


					householdDay.AttemptedSimulations++;
					foreach (var personDay in householdDay.PersonDays) {
						personDay.AttemptedSimulations++;
					}

					if (!simulatedAnInvalidHouseholdDay) {
						simulatedAnInvalidHouseholdDay = true;

						// counts unique instances where a household's day is invalid
						lock (_lock) {
							ChoiceModelFactory.TotalInvalidAttempts++;
						}
					}

					if (householdDay.Household.Id == 2033) { //3556) { // 35552) {
						bool testbreak = true;
					}

					householdDay.Reset();
				}

#if RELEASE
				}
				catch (Exception e) {
					throw new HouseholdDayModelException(string.Format("Error running household day models for {0}.", _household), e);
				}
#endif


			}
		}

		private static void RunTourTripModels(TourWrapper tour, PersonDayWrapper personDay, HouseholdDayWrapper householdDay, int firstDirection, int lastDirection) {
			if (!Global.Configuration.ShouldRunTourTripModels) {
				return;
			}

			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > RunTourTripModels Person {0} Tour {1} Direction {2} to {3}",
				 personDay.Person.Sequence, tour.Sequence, firstDirection, lastDirection);

			lock (_lock) {
				ChoiceModelFactory.TotalTimesProcessHalfToursRun++;
			}
			ProcessHalfTours(tour, personDay, householdDay, firstDirection, lastDirection);

			if (!tour.PersonDay.IsValid) {
				return;
			}

			tour.SetOriginTimes();
		}

		private static void RunSubtourModels(TourWrapper tour, PersonDayWrapper personDay, HouseholdDayWrapper householdDay) {
			if (!Global.Configuration.ShouldRunSubtourModels) {
				return;
			}
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > RunSubtourModels Person {0} Tour {1}", personDay.Person.Sequence, tour.Sequence);

			foreach (var subtour in tour.Subtours) {
#if RELEASE
				try {
#endif
				lock (_lock) {
					ChoiceModelFactory.TotalTimesTourSubtourModelSuiteRun++;
				}
				RunSubtourModelSuite(subtour, householdDay);

				if (!subtour.PersonDay.IsValid) {
					return;
				}

				lock (_lock) {
					ChoiceModelFactory.TotalTimesSubtourTripModelsRun++;
				}
				RunSubtourTripModels(subtour, personDay, householdDay, Constants.TourDirection.ORIGIN_TO_DESTINATION, Constants.TourDirection.DESTINATION_TO_ORIGIN);

				if (!subtour.PersonDay.IsValid) {
					return;
				}
#if RELEASE
				}
				catch (Exception e) {
					throw new SubtourModelException(string.Format("Error running subtour models for {0}.", subtour), e);
				}
#endif
			}
		}

		private static void RunSubtourTripModels(ITourWrapper subtour, IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, int firstDirection, int lastDirection) {
			if (!Global.Configuration.ShouldRunSubtourTripModels) {
				return;
			}
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > RunSubtourTripModels Subtour {0}", subtour.Sequence);

			lock (_lock) {
				ChoiceModelFactory.TotalTimesProcessHalfSubtoursRun++;
			}
			ProcessHalfTours(subtour, personDay, householdDay, firstDirection, lastDirection);

			if (!subtour.PersonDay.IsValid) {
				return;
			}

			subtour.SetOriginTimes();
		}

		private static void RunHouseholdModelSuite(HouseholdWrapper household) {
			if (!Global.Configuration.ShouldRunAutoOwnershipModel) {
				return;
			}

			// sets number of vehicles in household
			lock (_lock) {
				ChoiceModelFactory.TotalTimesAutoOwnershipModelRun++;
			}
			HAutoOwnershipModel.Run(household);
		}

		private void RunPersonModelSuite(IPersonWrapper person) {
			if (Global.Configuration.ShouldRunWorkLocationModel && person.IsFullOrPartTimeWorker) {
				if (Global.Configuration.IsInEstimationMode || person.Household.RandomUtility.Uniform01() > _household.FractionWorkersWithJobsOutsideRegion) {
					// sets a person's usual work location
					// for full or part-time workers
					lock (_lock) {
						ChoiceModelFactory.TotalTimesWorkLocationModelRun++;
					}
					HWorkLocationModel.Run(person, Global.Configuration.WorkLocationModelSampleSize);

				}
				else {
					if (!Global.Configuration.IsInEstimationMode) {
						person.UsualWorkParcelId = Constants.OUT_OF_REGION_PARCEL_ID;
					}
				}
			}

			if (Global.Configuration.ShouldRunSchoolLocationModel && person.IsStudent) {
				// sets a person's school location
				lock (_lock) {
					ChoiceModelFactory.TotalTimesSchoolLocationModelRun++;
				}
				HSchoolLocationModel.Run(person, Global.Configuration.SchoolLocationModelSampleSize);
			}

			if (Global.Configuration.ShouldRunWorkLocationModel && person.IsWorker && person.IsNotFullOrPartTimeWorker) {
				if (Global.Configuration.IsInEstimationMode || person.Household.RandomUtility.Uniform01() > _household.FractionWorkersWithJobsOutsideRegion) {
					// sets a person's usual work location
					// for other workers in relation to a person's school location
					lock (_lock) {
						ChoiceModelFactory.TotalTimesWorkLocationModelRun++;
					}
					HWorkLocationModel.Run(person, Global.Configuration.WorkLocationModelSampleSize);
				}
				else {
					if (!Global.Configuration.IsInEstimationMode) {
						person.UsualWorkParcelId = Constants.OUT_OF_REGION_PARCEL_ID;
					}
				}
			}
			if (person.IsWorker && person.UsualWorkParcel != null // && person.UsualWorkParcel.ParkingOffStreetPaidDailySpacesBuffer2 > 0 
				  && Global.Configuration.IncludePayToParkAtWorkplaceModel) {
				if (Global.Configuration.ShouldRunPayToParkAtWorkplaceModel) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesPaidParkingAtWorkplaceModelRun++;
					}
					HPayToParkAtWorkplaceModel.Run(person);
				}
			}
			else {
				person.PayToParkAtWorkplaceFlag = 1; // by default, people pay the parcel parking price
			}

			if (person.IsWorker && person.UsualWorkParcel != null) {
				if (Global.Configuration.ShouldRunWorkUsualModeAndScheduleModel) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesWorkUsualModeAndScheduleModelRun++;
					}
					HWorkUsualModeAndScheduleModel.Run(person);
				}
			}

			if (!Global.Configuration.IsInEstimationMode && Global.Configuration.Policy_UniversalTransitPassOwnership) {
				person.TransitPassOwnershipFlag = 1; //policy to turn on transit pass ownership
			}
			else if (!person.IsChildUnder5 && Global.Configuration.IncludeTransitPassOwnershipModel && Global.Configuration.ShouldRunTransitPassOwnershipModel) {
				lock (_lock) {
					ChoiceModelFactory.TotalTimesTransitPassOwnershipModelRun++;
				}
				HTransitPassOwnershipModel.Run(person);
			}
			else {
				person.TransitPassOwnershipFlag = 0; // by default, people don't own a transit pass
			}
		}

		private static void RunHouseholdDayModelSuite(HouseholdDayWrapper householdDay) {

			if (householdDay.Household.Id == 15454) {
				bool testbreak = true;
			}
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > RunHouseholdDayModelSuite for household {0}", householdDay.Household.Id);

			if (Global.Configuration.ShouldRunHouseholdDayPatternTypeModel) {
				// determines if household day includes primary priority time
				lock (_lock) {
					ChoiceModelFactory.TotalTimesHouseholdDayPatternTypeModelRun++;
				}
				HHouseholdDayPatternTypeModel.Run(householdDay);
				IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.Person.HouseholdDayPatternParticipationPriority).ToList().Cast<PersonDayWrapper>();
				int i = 0;
				foreach (PersonDayWrapper personDay in orderedPersonDays) {
					i++;
					if (i > 5 || (i > 4 && Global.Configuration.IsInEstimationMode && Global.Configuration.EstimationModel == "HPersonDayPatternTypeModel")) {
						lock (_lock) {
							ChoiceModelFactory.TotalTimesPersonDayPatternTypeModelRun++;
						}
						HPersonDayPatternTypeModel.Run(personDay, householdDay);
					}
					//mbtrace
					if (Global.TraceResults) Global.PrintFile.WriteLine("> > > Predicted pattern type for person {0} is {1}", personDay.Person.Sequence, personDay.PatternType);
				}


			}
			if (householdDay.IsValid == false) {
				return;
			}

			foreach (var personDay in householdDay.PersonDays) {

				//TODO:  replace the following reset of personDay.IsValid when the logic for invalid person days is enhanced to handle householdDays
				//personDay.IsValid = true;

				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonDayMandatoryModelSuiteRun++;
				}
				RunPersonDayMandatoryModelSuite(personDay, householdDay);

				if (personDay.IsValid == false) {
					return;
				}
			}

			lock (_lock) {
				ChoiceModelFactory.TotalTimesJointHalfTourGenerationModelSuiteRun++;
			}
			RunJointHalfTourGenerationModelSuite(householdDay);

			if (householdDay.IsValid == false) {
				return;
			}

			lock (_lock) {
				ChoiceModelFactory.TotalTimesJointTourGenerationModelSuiteRun++;
			}
			RunJointTourGenerationModelSuite((HouseholdDayWrapper) householdDay);

			if (householdDay.IsValid == false) {
				return;
			}

			foreach (var personDay in householdDay.PersonDays) {
#if RELEASE
				try {
#endif
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonDayModelSuiteRun++;
				}
				RunPersonDayModelSuite(personDay, householdDay);

				if (personDay.IsValid == false) {
					return;
				}
				if (!Global.Configuration.IsInEstimationMode) {
					personDay.SetHomeBasedNonMandatoryTours();
				}


#if RELEASE
				}
				catch (Exception e) {
					throw new PersonDayModelException(string.Format("Error running person-day models for {0}.", personDay), e);
				}
#endif
			}


			if (!Global.Configuration.ShouldRunTourModels) {
				return;
			}

			// This section sets missign data flags needed to drop observations if necessary for model estimation
			if (Global.Configuration.IsInEstimationMode) {
				foreach (var personDay in householdDay.PersonDays) {
					if (personDay.Tours != null) { ///was getting an exception here
						foreach (var tour in personDay.Tours) {
							if (tour.DestinationParcelId <= 0) { tour.IsMissingData = true; }
							//if (tour.DestinationPurpose == Constants.Purpose.SCHOOL && !(tour.DestinationParcelId == tour.Person.UsualSchoolParcelId)) { tour.IsMissingData = true; }

							if (tour.IsMissingData) { personDay.IsMissingData = true; }
						}
					}
					if (personDay.IsMissingData) { householdDay.IsMissingData = true; }
					if (personDay.Household.Id == 14724) { householdDay.IsMissingData = true; }  //this PSRC HH has inconsistent data or data prep misinterpreted?
					if (personDay.Household.Id == 41121) { householdDay.IsMissingData = true; }  //this PSRC HH has inconsistent data
					if (personDay.Household.Id == 45702) { householdDay.IsMissingData = true; }  //this PSRC HH has inconsistent data
				}
			}


			foreach (var partialJointHalfTour in householdDay.PartialHalfToursList) {
				if (householdDay.IsMissingData) {
					break;
				}
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPartialJointHalfTourModelSuiteRun++;
				}
				RunPartialJointHalfTourModelSuite((HouseholdDayWrapper) householdDay, (PartialHalfTourWrapper) partialJointHalfTour);

				if (householdDay.IsValid == false) {
					return;
				}
			}

			foreach (var fullJointHalfTour in householdDay.FullHalfToursList) {
				lock (_lock) {
					ChoiceModelFactory.TotalTimesFullJointHalfTourModelSuiteRun++;
				}
				RunFullJointHalfTourModelSuite((HouseholdDayWrapper) householdDay, (FullHalfTourWrapper) fullJointHalfTour);

				if (householdDay.IsValid == false) {
					return;
				}
			}

			foreach (var personDay in householdDay.PersonDays) {
				foreach (var tour in personDay.Tours) {
					if (tour.DestinationPurpose == Constants.Purpose.WORK || tour.DestinationPurpose == Constants.Purpose.SCHOOL) {
						lock (_lock) {
							ChoiceModelFactory.TotalTimesMandatoryTourModelSuiteRun++;
						}

						if (householdDay.Household.Id == 1267500 && personDay.Person.Sequence == 5 
							&& householdDay.AttemptedSimulations == 2) {
							bool testbreak = true;
						}

		
						
						RunMandatoryTourModelSuite((TourWrapper) tour, (PersonDayWrapper) personDay, (HouseholdDayWrapper) householdDay);

						if (personDay.IsValid == false) {
							return;
						}
					}
				}
			}

			foreach (var jointTour in householdDay.JointToursList) {
				lock (_lock) {
					ChoiceModelFactory.TotalTimesJointTourModelSuiteRun++;
				}
				RunJointTourModelSuite((HouseholdDayWrapper) householdDay, (JointTourWrapper) jointTour);

				if (householdDay.IsValid == false) {
					return;
				}
			}

			foreach (var personDay in householdDay.PersonDays) {

				// creates and adds individual tours to person's day (if not in estimation mode)
				// (in estimation mode they were already created)
				// tours are created by purpose
				//if (!Global.Configuration.IsInEstimationMode) {
				//	personDay.GetIndividualTourSimulatedData(personDay, personDay.Tours);
				//}

				foreach (var tour in personDay.Tours) {
					if (tour.Household.Id == 2015 && tour.Person.Sequence == 1 && tour.Sequence == 1) {
						bool testbreak = true;
					}
					if (!(tour.DestinationPurpose == Constants.Purpose.WORK || tour.DestinationPurpose == Constants.Purpose.SCHOOL)) {
						lock (_lock) {
							ChoiceModelFactory.TotalTimesNonMandatoryTourModelSuiteRun++;
						}
						RunNonMandatoryTourModelSuite((TourWrapper) tour, (PersonDayWrapper) personDay, (HouseholdDayWrapper) householdDay);

						if (personDay.IsValid == false) {
							return;
						}
					}
				}
			}



			//			foreach (var personDay in householdDay.PersonDays) {
			//#if RELEASE
			//				try {
			//#endif
			//				if (personDay.IsValid) {
			//					// update park and ride lot loads
			//					if (!Global.Configuration.IsInEstimationMode && personDay.Tours != null) {
			//						foreach (var tour in personDay.Tours.Where(tour => tour.Mode == Constants.Mode.PARK_AND_RIDE)) {
			//							tour.SetParkAndRideStay();
			//						}
			//					}
			//				}
			//
			//#if RELEASE
			//				}
			//				catch (Exception e) {
			//					throw new PersonDayModelException(string.Format("Error running person-day models for {0}.", personDay), e);
			//				}
			//#endif
			//			}

			// TODO:  add logic that deals with invalid person days.  Probably need to flag invalid householdDays instead.
		}


		private static void RunPersonDayMandatoryModelSuite(IPersonDayWrapper personDay, HouseholdDayWrapper householdDay) {

			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > RunPersonDayMandatoryModelSuite for household {0} person {1}", householdDay.Household.Id, personDay.Person.Sequence);

			if (Global.Configuration.ShouldRunWorkAtHomeModel) {
				// determines if full or part time worker works at home during day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesWorkAtHomeModelRun++;
				}
				HWorkAtHomeModel.Run(personDay, householdDay);
				//mbtrace
				if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > HWorkAtHomeModel predicts WorkAtHomeDuration {0}", personDay.WorkAtHomeDuration);
			}

			if (Global.Configuration.ShouldRunMandatoryTourGenerationModel) {

				// creates tour list; full for estimation mode, empty for application mode
				personDay.InitializeTours();

				if (personDay.PatternType == 1 && (personDay.Person.IsWorker || personDay.Person.IsStudent)) {
					int[] totalMandatoryTours = new int[4];
					int[] simulatedMandatoryTours = new int[4];
					int choice;
					var ncallsfortour = 0;
					if (Global.Configuration.IsInEstimationMode) {
						totalMandatoryTours[1] = personDay.UsualWorkplaceTours;
						totalMandatoryTours[2] = personDay.WorkTours - totalMandatoryTours[1];
						totalMandatoryTours[3] = personDay.SchoolTours;
						totalMandatoryTours[0] = totalMandatoryTours[1] + totalMandatoryTours[2] + totalMandatoryTours[3];
						if (personDay.UsualWorkplaceTours + personDay.SchoolTours > 0) {
							personDay.HasMandatoryTourToUsualLocation = true;
						}
						for (var i = 0; i <= totalMandatoryTours[0]; i++) {
							if (i < totalMandatoryTours[1]) { choice = 1; }
							else if (i < totalMandatoryTours[1] + totalMandatoryTours[2]) { choice = 2; }
							else if (i < totalMandatoryTours[0]) { choice = 3; }
							else { choice = 0; }
							lock (_lock) {
								ChoiceModelFactory.TotalTimesMandatoryTourGenerationModelRun++;
							}
							ncallsfortour++;
							HMandatoryTourGenerationModel.Run(personDay, householdDay, ncallsfortour, simulatedMandatoryTours, choice);
							if (choice > 0) {
								simulatedMandatoryTours[choice]++;
								simulatedMandatoryTours[0]++;
							}
						}
					}
					else {
						int maxNumberOfMandatoryTours = 4;

						for (var i = 0; i <= totalMandatoryTours[0]; i++) {
							lock (_lock) {
								ChoiceModelFactory.TotalTimesMandatoryTourGenerationModelRun++;
							}
							ncallsfortour++;
							var tourPurpose = HMandatoryTourGenerationModel.Run(personDay, householdDay, ncallsfortour, simulatedMandatoryTours);
							if (tourPurpose > 0) {
								simulatedMandatoryTours[tourPurpose]++;
								simulatedMandatoryTours[0]++;
								totalMandatoryTours[0]++;
								if (tourPurpose == 1 || tourPurpose == 3) {
									personDay.HasMandatoryTourToUsualLocation = true;
								}
							}
							if (totalMandatoryTours[0] >= maxNumberOfMandatoryTours) { break; }
						}
						// create the mandatory tours and add them to the person day's list of tours
						personDay.UsualWorkplaceTours = simulatedMandatoryTours[1];
						personDay.WorkTours = simulatedMandatoryTours[1] + simulatedMandatoryTours[2];
						personDay.SchoolTours = simulatedMandatoryTours[3];
						personDay.GetMandatoryTourSimulatedData(personDay, personDay.Tours);
						//mbtrace
						if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > Generated mandatory tours: Usual work {0} Other work {1} School {2}",
							 simulatedMandatoryTours[1], simulatedMandatoryTours[2], simulatedMandatoryTours[3]);

					}

					personDay.CreatedWorkTours = personDay.WorkTours;
					personDay.CreatedSchoolTours = personDay.SchoolTours;

					// determine presence of mandatory stops
					lock (_lock) {
						ChoiceModelFactory.TotalTimesMandatoryStopPresenceModelRun++;
					}
					HMandatoryStopPresenceModel.Run(personDay, householdDay);
					//mbtrace
					if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > Generated mandatory stops 0/1+: Work {0} School {1}",
						 personDay.WorkStops, personDay.SchoolStops);

				}
			}

		}



		private static void RunJointHalfTourGenerationModelSuite(HouseholdDayWrapper householdDay) {

			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > RunJointHalfTourGenerationModelSuite for Household {0}", householdDay.Household.Id);

			if (householdDay.Household.Id == 3601) { //3913) { //3556 35552 2033 2495{
				bool testbreak = true;
			}

			int maxNumberParticipants = 5;

			if (!Global.Configuration.ShouldRunJointHalfTourGenerationModels) {
				return;
			}

			IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointHalfTourParticipationPriority).ToList().Cast<PersonDayWrapper>();

			// initialize availability for all HH members and for household
			//    This logic doesn't depend on survey data (ie, it works with synthetic pop too)
			//bool[] adult = new bool[9];
			bool[,] hTAvailable = new bool[3, 9]; //[subtype (0--paired, 1--outbound, 2--return), personDay.Person.Sequence]
			bool[] fHTAvailable = new bool[9];
			bool[] pHTAvailable = new bool[9];
			int[] fHTMandatoryAdultsAvailable = new int[3]; //[subtype]
			int[] fHTMandatoryPersonsAvailable = new int[3]; //[subtype]
			int[] fHTNonMandatoryAdultsAvailable = new int[3]; //[subtype]
			int[] pHTAdultsAvailable = new int[3]; //[subtype]
			int[] pHTPersonsAvailable = new int[3]; //[subtype]
			//int pDay = 0;
			int count = 0;
			foreach (PersonDayWrapper personDay in orderedPersonDays) {
				//pDay++;
				//if (personDay.Person.IsAdult) {
				//	adult[pDay] = true;
				//}
				count++;
				if (count <= maxNumberParticipants) {
					if (personDay.HasMandatoryTourToUsualLocation) {
						for (var i = 0; i < 3; i++) {
							fHTMandatoryPersonsAvailable[i]++;
							pHTPersonsAvailable[i]++;
							hTAvailable[i, count] = true;
						}
						fHTAvailable[count] = true;
						pHTAvailable[count] = true;
						//if (personDay.Person.IsAdult) {
						if (personDay.Person.IsDrivingAge) {
							for (var i = 0; i < 3; i++) {
								fHTMandatoryAdultsAvailable[i]++;
								pHTAdultsAvailable[i]++;
							}
						}
					}
					else if (!(personDay.PatternType == Constants.PatternType.HOME)
						//&& personDay.Person.IsAdult) {
						 && personDay.Person.IsDrivingAge) {
						fHTAvailable[count] = true;
						for (var i = 0; i < 3; i++) {
							fHTNonMandatoryAdultsAvailable[i]++;
							hTAvailable[i, count] = true;
						}
					}
				}
			}

			int[] jHTId = new int[8];  //Id of the first (if paired) pht or fht survey record corresponding to the observed joint halftour or halftour pair
			int[] jHTType = new int[8];  //1--full; 2--partial
			int[] jHTSubType = new int[8]; //0--paired; 1--outbound halftour; 2--return halftour 
			int[] jHTChauffeurSequence = new int[8];  //person.Sequence of PartialJointHalfTour chauffeur
			bool[,] jHTParticipation = new bool[8, Global.MaximumHouseholdSize]; //jhtour x personDay.Sequence
			int jHTCount = 0;
			bool[] paired = new bool[9];

			if (Global.Configuration.IsInEstimationMode) {
				// Derive choice variables and identify participants for all joint half tours
				// Update personDay EscortFullJointHalfTour property
				//   This logic depends on survey data (must be used only in estimation mode)
				int i1 = 0;
				foreach (var hT1 in householdDay.FullHalfToursList) {
					i1++;
					int i2 = 0;
					foreach (var hT2 in householdDay.FullHalfToursList) {
						i2++;
						if (!(hT1.Equals(hT2))
							 && (i2 > i1)
							 && (paired[i1] == false)
							 && (paired[i2] == false)
							 && !(hT1.Direction == hT2.Direction)
							 && (hT1.PersonSequence1 == hT2.PersonSequence1)
							 && (hT1.PersonSequence2 == hT2.PersonSequence2)
							 && (hT1.PersonSequence3 == hT2.PersonSequence3)
							 && (hT1.PersonSequence4 == hT2.PersonSequence4)
							 && (hT1.PersonSequence5 == hT2.PersonSequence5)
							 && (hT1.PersonSequence6 == hT2.PersonSequence6)
							 && (hT1.PersonSequence7 == hT2.PersonSequence7)
							 && (hT1.PersonSequence8 == hT2.PersonSequence8)
							 ) {
							paired[i1] = hT1.Paired = true;
							paired[i2] = hT2.Paired = true;
							jHTCount++;
							jHTType[jHTCount] = 1;  //full
							jHTSubType[jHTCount] = 0; //paired
							jHTId[jHTCount] = hT1.Id;
							if (hT1.PersonSequence1 > 0 && hT1.PersonSequence1 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence1] = true; }
							if (hT1.PersonSequence2 > 0 && hT1.PersonSequence2 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence2] = true; }
							if (hT1.PersonSequence3 > 0 && hT1.PersonSequence3 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence3] = true; }
							if (hT1.PersonSequence4 > 0 && hT1.PersonSequence4 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence4] = true; }
							if (hT1.PersonSequence5 > 0 && hT1.PersonSequence5 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence5] = true; }
							if (hT1.PersonSequence6 > 0 && hT1.PersonSequence6 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence6] = true; }
							if (hT1.PersonSequence7 > 0 && hT1.PersonSequence7 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence7] = true; }
							if (hT1.PersonSequence8 > 0 && hT1.PersonSequence8 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence8] = true; }
						}
					}
				}
				i1 = 0;
				foreach (var hT1 in householdDay.FullHalfToursList) {
					i1++;
					if (paired[i1] == false) {
						jHTCount++;
						jHTType[jHTCount] = 1;  //full
						jHTSubType[jHTCount] = hT1.Direction; //direction
						jHTId[jHTCount] = hT1.Id;
						if (hT1.PersonSequence1 > 0 && hT1.PersonSequence1 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence1] = true; }
						if (hT1.PersonSequence2 > 0 && hT1.PersonSequence2 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence2] = true; }
						if (hT1.PersonSequence3 > 0 && hT1.PersonSequence3 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence3] = true; }
						if (hT1.PersonSequence4 > 0 && hT1.PersonSequence4 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence4] = true; }
						if (hT1.PersonSequence5 > 0 && hT1.PersonSequence5 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence5] = true; }
						if (hT1.PersonSequence6 > 0 && hT1.PersonSequence6 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence6] = true; }
						if (hT1.PersonSequence7 > 0 && hT1.PersonSequence7 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence7] = true; }
						if (hT1.PersonSequence8 > 0 && hT1.PersonSequence8 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence8] = true; }
					}
				}

				for (var i = 0; i < 9; i++) { paired[i] = false; }
				i1 = 0;
				foreach (var hT1 in householdDay.PartialHalfToursList) {
					i1++;
					int i2 = 0;
					foreach (var hT2 in householdDay.PartialHalfToursList) {
						i2++;
						if (!(ReferenceEquals(hT1, hT2))
							 && (i2 > i1)
							 && (paired[i1] == false)
							 && (paired[i2] == false)
							 && !(hT1.Direction == hT2.Direction)
							 && (hT1.PersonSequence1 == hT2.PersonSequence1)
							 && (hT1.PersonSequence2 == hT2.PersonSequence2)
							 && (hT1.PersonSequence3 == hT2.PersonSequence3)
							 && (hT1.PersonSequence4 == hT2.PersonSequence4)
							 && (hT1.PersonSequence5 == hT2.PersonSequence5)
							 && (hT1.PersonSequence6 == hT2.PersonSequence6)
							 && (hT1.PersonSequence7 == hT2.PersonSequence7)
							 && (hT1.PersonSequence8 == hT2.PersonSequence8)
							 ) {
							paired[i1] = hT1.Paired = true;
							paired[i2] = hT2.Paired = true;
							jHTCount++;
							jHTType[jHTCount] = 2;  //partial
							jHTSubType[jHTCount] = 0; //paired
							jHTChauffeurSequence[jHTCount] = hT1.PersonSequence1;
							jHTId[jHTCount] = hT1.Id;
							if (hT1.PersonSequence1 > 0 && hT1.PersonSequence1 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence1] = true; }
							if (hT1.PersonSequence2 > 0 && hT1.PersonSequence2 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence2] = true; }
							if (hT1.PersonSequence3 > 0 && hT1.PersonSequence3 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence3] = true; }
							if (hT1.PersonSequence4 > 0 && hT1.PersonSequence4 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence4] = true; }
							if (hT1.PersonSequence5 > 0 && hT1.PersonSequence5 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence5] = true; }
							if (hT1.PersonSequence6 > 0 && hT1.PersonSequence6 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence6] = true; }
							if (hT1.PersonSequence7 > 0 && hT1.PersonSequence7 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence7] = true; }
							if (hT1.PersonSequence8 > 0 && hT1.PersonSequence8 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence8] = true; }
						}
					}
				}
				i1 = 0;
				foreach (var hT1 in householdDay.PartialHalfToursList) {
					i1++;
					if (paired[i1] == false) {
						jHTCount++;
						jHTType[jHTCount] = 2;  //partial
						jHTSubType[jHTCount] = hT1.Direction; //direction
						jHTChauffeurSequence[jHTCount] = hT1.PersonSequence1;
						jHTId[jHTCount] = hT1.Id;
						if (hT1.PersonSequence1 > 0 && hT1.PersonSequence1 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence1] = true; }
						if (hT1.PersonSequence2 > 0 && hT1.PersonSequence2 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence2] = true; }
						if (hT1.PersonSequence3 > 0 && hT1.PersonSequence3 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence3] = true; }
						if (hT1.PersonSequence4 > 0 && hT1.PersonSequence4 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence4] = true; }
						if (hT1.PersonSequence5 > 0 && hT1.PersonSequence5 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence5] = true; }
						if (hT1.PersonSequence6 > 0 && hT1.PersonSequence6 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence6] = true; }
						if (hT1.PersonSequence7 > 0 && hT1.PersonSequence7 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence7] = true; }
						if (hT1.PersonSequence8 > 0 && hT1.PersonSequence8 <= Global.MaximumHouseholdSize) { jHTParticipation[jHTCount, hT1.PersonSequence8] = true; }
					}
				}
				int count2 = 0;
				foreach (var personDay in orderedPersonDays) {
					count2++;
					foreach (var tour in personDay.Tours) {
						if (count2 <= maxNumberParticipants
							&& (tour.FullHalfTour1Sequence > 0 || tour.FullHalfTour2Sequence > 0)
							&& tour.DestinationPurpose == Constants.Purpose.ESCORT) {
							personDay.EscortFullHalfTours++;
							//personDay.CreatedEscortTours++;
						}
					}
				}
			} // end choice variable evaluation for estimation mode

			// run generation and participation models

			int type = 0;
			int subType = 0;
			int jHTSimulated = 0;
			bool[] jHTAvailable = new bool[7];  //alternatives
			var nCallsForTour = 0;
			int maxChoice = 0;
			int maxJointHalfTours = 5;
			for (var i = 0; i <= jHTSimulated; i++) {

				//determine availability of the seven choice alternatives for the generation model
				for (int j = 0; j < 7; j++) {
					if (j == 0) { jHTAvailable[j] = true; }
					else if (j < 4) {
						//						if ((fHTMandatoryAdultsAvailable[j - 1] >= 1 || fHTNonMandatoryAdultsAvailable[j - 1] >= 1)
						//							 && (fHTMandatoryPersonsAvailable[j - 1] >= 2)
						if ((fHTMandatoryAdultsAvailable[j - 1] >= 1 && fHTMandatoryPersonsAvailable[j - 1] >= 2)
							|| (fHTNonMandatoryAdultsAvailable[j - 1] >= 1 && fHTMandatoryPersonsAvailable[j - 1] >= 1)
							 ) {
							jHTAvailable[j] = true;
						}
						else jHTAvailable[j] = false;
					}
					else {
						if (pHTAdultsAvailable[j - 4] >= 1 && pHTPersonsAvailable[j - 4] >= 2) {
							jHTAvailable[j] = true;
						}
						else jHTAvailable[j] = false;
					}
				}
				//Availability restrictions arising from sequence of data
				if (maxChoice >= 2) {
					jHTAvailable[1] = false;
				}
				if (maxChoice >= 4) {
					jHTAvailable[2] = false;
					jHTAvailable[3] = false;
				}
				if (maxChoice >= 5) {
					jHTAvailable[4] = false;
				}
				//user suppression of partial joint half tours
				if (Global.Configuration.ShouldSuppressPartiallyJointHalfTours) {
					jHTAvailable[4] = false;
					jHTAvailable[5] = false;
					jHTAvailable[6] = false;
				}

				//Set choice variables type and subType
				if (Global.Configuration.IsInEstimationMode) {
					if (jHTSimulated == jHTCount) {
						type = 0;
						subType = 0;
					}
					else {
						type = jHTType[i + 1];
						subType = jHTSubType[i + 1];
					}
				}

				lock (_lock) {
					ChoiceModelFactory.TotalTimesJointHalfTourGenerationModelRun++;
				}
				nCallsForTour++;
				int genChoice = HJointHalfTourGenerationModel.Run(householdDay, nCallsForTour, jHTAvailable, type, subType);

				int[] participants = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
				if (genChoice > 0) {
					jHTSimulated++;
					//mbtrace
					if (Global.TraceResults) Global.PrintFile.WriteLine("> > > Generated joint half tour {0}, type {1}", jHTSimulated, genChoice);

				}

				if (genChoice > maxChoice) { maxChoice = genChoice; }  // used to limit choice set due to ordering of data:  fht first, and paired first within that

				if (genChoice > 0 && genChoice <= 3) {
					// run full half tour participation model
					for (int p = 0; p < 8; p++) {
						participants[p] = 0;
					}
					lock (_lock) {
						ChoiceModelFactory.TotalTimesFullJointHalfTourParticipationModelRun++;
					}
					//					int[] participants = HFullJointHalfTourParticipationModel.Run(householdDay, jHTSimulated, genChoice, hTAvailable, fHTAvailable, jHTParticipation);
					participants = HFullJointHalfTourParticipationModel.Run(householdDay, jHTSimulated, genChoice, hTAvailable, fHTAvailable, jHTParticipation);

					if (Global.Configuration.IsInEstimationMode) {
						int numberAdults = 0;
						int count2 = 0;
						foreach (PersonDayWrapper personDay in orderedPersonDays) {
							count2++;
							if (count2 <= maxNumberParticipants) {
								//if (participants[count2] == 1 && personDay.Person.IsAdult) {
								if (participants[count2] == 1 && personDay.Person.IsDrivingAge) {
									numberAdults++;
								}
							}
						}
						if (genChoice == 1 || genChoice == 2) {  //half tour to mandatory
							//var fullHalfTour = householdDay.CreateFullHalfTour(householdDay, orderedPersonDays, participants, 1);
							//if (genChoice == 1) {
							//	fullHalfTour.Paired = true;
							//}
							int count3 = 0;
							foreach (PersonDayWrapper personDay in orderedPersonDays) {
								count3++;
								if (count3 <= maxNumberParticipants) {
									if (participants[count3] == 1 &&
										 (!(personDay.PatternType == Constants.PatternType.MANDATORY)
										 || (personDay.Person.IsDrivingAge && numberAdults == 1))) { //assume that if only adult on tour it is an escort for them even if they have mandatory pattern
										personDay.CreatedEscortTours++;
									}
								}
							}
						}
						if (genChoice == 1 || genChoice == 3) { //half tour from mandatory
							// if nonmandatory escorts both directions we assume that they make two tours to do it
							int count3 = 0;
							foreach (PersonDayWrapper personDay in orderedPersonDays) {
								count3++;
								if (count3 <= maxNumberParticipants) {
									if (participants[count3] == 1 &&
										 (!(personDay.PatternType == Constants.PatternType.MANDATORY)
										 || (personDay.Person.IsDrivingAge && numberAdults == 1))) { //assume that if only adult on tour it is an escort for them even if they have mandatory pattern
										personDay.CreatedEscortTours++;
									}
								}
							}
						}
					}

					else {   //ie (!Global.Configuration.IsInEstimationMode) 
						int numberAdults = 0;
						int count2 = 0;
						foreach (PersonDayWrapper personDay in orderedPersonDays) {
							count2++;
							if (count2 <= maxNumberParticipants) {
								//if (participants[count2] == 1 && personDay.Person.IsAdult) {
								if (participants[count2] == 1 && personDay.Person.IsDrivingAge) {
									numberAdults++;
								}
							}
						}
						if (genChoice == 1 || genChoice == 2) {  //half tour to mandatory
							var fullHalfTour = householdDay.CreateFullHalfTour(householdDay, orderedPersonDays, participants, 1);
							if (genChoice == 1) {
								fullHalfTour.Paired = true;
							}
							if (fullHalfTour.Id == 24951) {
								bool testbreak = true;
							}

							int count3 = 0;
							foreach (PersonDayWrapper personDay in orderedPersonDays) {
								count3++;
								if (count3 <= maxNumberParticipants) {
									if ((participants[count3] == 1)
										&& ((!personDay.HasMandatoryTourToUsualLocation)
										//|| (personDay.Person.IsAdult && numberAdults == 1))) { //assume that if only adult on tour it is an escort for them even if they have mandatory pattern
										 || (personDay.Person.IsDrivingAge && numberAdults == 1))) { //assume that if only adult on tour it is an escort for them even if they have mandatory pattern
										// create nonmandatory tour for person and associate it with fullJointHalfTour
										TourWrapper tour = (TourWrapper) personDay.GetEscortTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey);
										tour.FullHalfTour1Sequence = fullHalfTour.Sequence;
										fullHalfTour.SetParticipantTourSequence(tour);
										personDay.EscortFullHalfTours++;
										personDay.CreatedEscortTours++;
									}
									//update mandatory tour of mandatory participants with FullJointHalfTour index
									else if ((participants[count3] == 1) && (personDay.HasMandatoryTourToUsualLocation)) {
										if (personDay.Person.IsFullOrPartTimeWorker) {
											var tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											//var tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											if (!(tour == null)) {
												tour.FullHalfTour1Sequence = fullHalfTour.Sequence;
												fullHalfTour.SetParticipantTourSequence(tour);
											}
											else {
												tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
												//tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsSchoolPurpose == true);
												if (!(tour == null)) {
													tour.FullHalfTour1Sequence = fullHalfTour.Sequence;
													fullHalfTour.SetParticipantTourSequence(tour);
												}
											}
										}
										else {
											var tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
											//var tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsSchoolPurpose == true);
											if (!(tour == null)) {
												tour.FullHalfTour1Sequence = fullHalfTour.Sequence;
												fullHalfTour.SetParticipantTourSequence(tour);
											}
											else {
												tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
												//tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
												if (!(tour == null)) {
													tour.FullHalfTour1Sequence = fullHalfTour.Sequence;
													fullHalfTour.SetParticipantTourSequence(tour);
												}
											}
										}
									}
								}
							}
						}
						if (genChoice == 1 || genChoice == 3) { //half tour from mandatory
							// if nonmandatory escorts both directions we assume that they make two tours to do it
							var fullHalfTour = householdDay.CreateFullHalfTour(householdDay, orderedPersonDays, participants, 2);
							if (genChoice == 1) {
								fullHalfTour.Paired = true;
							}
							int count3 = 0;
							foreach (PersonDayWrapper personDay in orderedPersonDays) {
								count3++;
								if (count3 <= maxNumberParticipants) {

									if ((participants[count3] == 1)
										&& ((!personDay.HasMandatoryTourToUsualLocation)
										//|| (personDay.Person.IsAdult && numberAdults == 1))) { //assume that if only adult on tour it is an escort for them even if they have mandatory pattern
										 || (personDay.Person.IsDrivingAge && numberAdults == 1))) { //assume that if only adult on tour it is an escort for them even if they have mandatory pattern
										// create nonmandatory tour for person and associate it with fullJointHalfTour
										TourWrapper tour = (TourWrapper) personDay.GetEscortTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey);
										tour.FullHalfTour2Sequence = fullHalfTour.Sequence;
										fullHalfTour.SetParticipantTourSequence(tour);
										personDay.EscortFullHalfTours++;
										personDay.CreatedEscortTours++;
									}
									//update mandatory tour of mandatory participants with FullJointHalfTour index
									else if ((participants[count3] == 1) && (personDay.HasMandatoryTourToUsualLocation)) {
										if (personDay.Person.IsFullOrPartTimeWorker) {
											var tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											//var tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											if (!(tour == null)) {
												tour.FullHalfTour2Sequence = fullHalfTour.Sequence;
												fullHalfTour.SetParticipantTourSequence(tour);
											}
											else {
												tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
												//tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsSchoolPurpose == true);
												if (!(tour == null)) {
													tour.FullHalfTour2Sequence = fullHalfTour.Sequence;
													fullHalfTour.SetParticipantTourSequence(tour);
												}
												//TODO:  may need to reject tour or household day if person with mandatory day has no work or school tours to usual location.
												//  But the logic for running the tour generation and participation should not let this happen
											}
										}
										else {
											var tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
											//var tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsSchoolPurpose == true);
											if (!(tour == null)) {
												tour.FullHalfTour2Sequence = fullHalfTour.Sequence;
												fullHalfTour.SetParticipantTourSequence(tour);
											}
											else {
												tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
												//tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
												if (!(tour == null)) {
													tour.FullHalfTour2Sequence = fullHalfTour.Sequence;
													fullHalfTour.SetParticipantTourSequence(tour);
												}
												//TODO:  may need to reject tour or household day if person with mandatory day has no work or school tours to usual location.
												//  But the logic for running the tour generation and participation should not let this happen
											}
										}
									}
								}
							}
						}
					}
				}
				else if (genChoice > 0) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesPartialJointHalfTourParticipationAndChauffeurModelsRun++;
					}
					if (householdDay.Household.Id == 2071) {
						bool testbreak = true;
					}
					for (int p = 0; p < 8; p++) {
						participants[p] = 0;
					}
					//					int[] participants = HPartialJointHalfTourParticipationModel.Run(householdDay, jHTSimulated, genChoice - 3, hTAvailable, pHTAvailable, jHTParticipation);
					participants = HPartialJointHalfTourParticipationModel.Run(householdDay, jHTSimulated, genChoice - 3, hTAvailable, pHTAvailable, jHTParticipation);
					int chauffeurSequence = HPartialJointHalfTourChauffeurModel.Run(householdDay, jHTSimulated, genChoice - 3, participants, jHTChauffeurSequence);
					if (!Global.Configuration.IsInEstimationMode) {
						// determine purpose and order of all participants
						int[] purpose = new int[] { 0, 0, 0, 0, 0, 0 };   //purpose[n] is destinationPurpose of person with person.Sequence = n
						double[] distanceFromChauffeur = new double[] { 999, 999, 999, 999, 999, 999 }; //after sorting distanceFromChauffeur[n] is distance from chauffeur of pickup (drop off) number n+1 (chauffeur's distanceFromChauffeur is -1)
						ICondensedParcel[] destinationParcel = new CondensedParcel[] { null, null, null, null, null, null };
						int[] pickOrder = new int[] { 0, 1, 2, 3, 4, 5 };// after sorting, pickOrder[n] is the orderedPerson order of pickup (drop off) number n+1 (chauffeur's pickOrder = 1)
						ICondensedParcel chauffeurParcel = null;
						int count2 = 0;
						foreach (PersonDayWrapper personDay in orderedPersonDays) {
							count2++;
							if (count2 <= maxNumberParticipants) {
								//determine tour purposes of participants
								if (participants[count2] == 1) {
									if (personDay.Person.IsFullOrPartTimeWorker) {
										var tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
										if (!(tour == null)) {
											purpose[count2] = Constants.Purpose.WORK;
											destinationParcel[count2] = personDay.Person.UsualWorkParcel;
										}
										else {
											tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
											if (!(tour == null)) {
												purpose[count2] = Constants.Purpose.SCHOOL;
												destinationParcel[count2] = personDay.Person.UsualSchoolParcel;
											}
										}
									}
									else {
										var tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
										if (!(tour == null)) {
											purpose[count2] = Constants.Purpose.SCHOOL;
											destinationParcel[count2] = personDay.Person.UsualSchoolParcel;
										}
										else {
											tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											if (!(tour == null)) {
												purpose[count2] = Constants.Purpose.WORK;
												destinationParcel[count2] = personDay.Person.UsualWorkParcel;
											}
										}
									}
									if (personDay.Person.Sequence == chauffeurSequence) {
										chauffeurParcel = destinationParcel[count2];
									}

								}
							}
						}

						//determine chauffeur's order in the orderedPersonDays
						int chauffeurOrder = 0;
						int ct = 0;
						foreach (PersonDayWrapper personDay in orderedPersonDays) {
							ct++;
							if (ct <= maxNumberParticipants) {
								if (personDay.Person.Sequence == chauffeurSequence) {
									chauffeurOrder = ct;
								}
							}
						}
						//Loop on the participant array index and calculate distance from the chauffeurParcel to each participant's destinationParcelID
						for (int j = 1; j <= maxNumberParticipants; j++) {
							if (j == chauffeurOrder) {
								distanceFromChauffeur[j] = -1;  // forces chauffeur to be first in pick sequence
							}
							else if (destinationParcel[j] != null) {
								var circuityDistance = Global.Configuration.UseShortDistanceNodeToNodeMeasures
											  ? chauffeurParcel.NodeToNodeDistance(destinationParcel[j])
											  : (Global.Configuration.UseShortDistanceCircuityMeasures)
														? chauffeurParcel.CircuityDistance(destinationParcel[j])
														: Constants.DEFAULT_VALUE;

								distanceFromChauffeur[j] = ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, 1, chauffeurParcel, destinationParcel[j], circuityDistance).Variable;
							}
						}
						// Sort to find distance rank and set pickOrder in increasing distance from chauffeur parcelID.
						Array.Sort(distanceFromChauffeur, pickOrder);

						if (genChoice == 4 || genChoice == 5) {  //half tour to mandatory

							// Create the partial half tour, put the persons in pick sequence in it, and associate the persons' tours with it.
							var partialHalfTour = householdDay.CreatePartialHalfTour(householdDay, orderedPersonDays, participants, pickOrder, distanceFromChauffeur, 1);
							if (genChoice == 4) {
								partialHalfTour.Paired = true;
							}
							// associate persons' tours with partialHalfTour
							int count3 = 0;
							foreach (PersonDayWrapper personDay in orderedPersonDays) {
								count3++;
								if (count3 <= maxNumberParticipants) {
									//update mandatory tour of mandatory participants with FullJointHalfTour index
									if (participants[count3] == 1) {
										if (personDay.Person.IsFullOrPartTimeWorker) {
											var tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											//var tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											if (!(tour == null)) {
												tour.PartialHalfTour1Sequence = partialHalfTour.Sequence;
												partialHalfTour.SetParticipantTourSequence(tour);
											}
											else {
												tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
												//tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsSchoolPurpose == true);
												if (!(tour == null)) {
													tour.PartialHalfTour1Sequence = partialHalfTour.Sequence;
													partialHalfTour.SetParticipantTourSequence(tour);
												}
											}
										}
										else {
											var tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
											//var tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsSchoolPurpose == true);
											if (!(tour == null)) {
												tour.PartialHalfTour1Sequence = partialHalfTour.Sequence;
												partialHalfTour.SetParticipantTourSequence(tour);
											}
											else {
												tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
												//tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
												if (!(tour == null)) {
													tour.PartialHalfTour1Sequence = partialHalfTour.Sequence;
													partialHalfTour.SetParticipantTourSequence(tour);
												}
											}
										}
									}
								}
							}
						}

						if (genChoice == 4 || genChoice == 6) {  //half tour from mandatory

							// Create the partial half tour, put the persons in pick sequence in it, and associate the persons' tours with it.
							var partialHalfTour = householdDay.CreatePartialHalfTour(householdDay, orderedPersonDays, participants, pickOrder, distanceFromChauffeur, 2);
							if (genChoice == 4) {
								partialHalfTour.Paired = true;
							}
							// associate persons' tours with partialHalfTour
							int count3 = 0;
							foreach (PersonDayWrapper personDay in orderedPersonDays) {
								count3++;
								if (count3 <= maxNumberParticipants) {
									//update mandatory tour of mandatory participants with FullJointHalfTour index
									if (participants[count3] == 1) {
										if (personDay.Person.IsFullOrPartTimeWorker) {
											var tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											//var tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
											if (!(tour == null)) {
												tour.PartialHalfTour2Sequence = partialHalfTour.Sequence;
												partialHalfTour.SetParticipantTourSequence(tour);
											}
											else {
												tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
												//tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsSchoolPurpose == true);
												if (!(tour == null)) {
													tour.PartialHalfTour2Sequence = partialHalfTour.Sequence;
													partialHalfTour.SetParticipantTourSequence(tour);
												}
											}
										}
										else {
											var tour = personDay.Tours.FirstOrDefault(x => x.IsSchoolPurpose == true);
											//var tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsSchoolPurpose == true);
											if (!(tour == null)) {
												tour.PartialHalfTour2Sequence = partialHalfTour.Sequence;
												partialHalfTour.SetParticipantTourSequence(tour);
											}
											else {
												tour = personDay.Tours.FirstOrDefault(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
												//tour = personDay.Tours.DefaultIfEmpty(null).First(x => x.IsWorkPurpose == true && x.DestinationParcelId == personDay.Person.UsualWorkParcelId);
												if (!(tour == null)) {
													tour.PartialHalfTour2Sequence = partialHalfTour.Sequence;
													partialHalfTour.SetParticipantTourSequence(tour);
												}
											}
										}
									}
								}
							}
						}
						// TODO:  create trip records for all trips of all persons on the partialHalfTour
						//        using information about pickSequence and each person's destination parcel and purpose
						//     OR wait and create trip records in the course of the personDay simulation


					}
				}

				// update availability for all participants and for household
				//   based on results of this iteration of the generation and participation models
				//   Logic assumes that a person can have only one joitn half tour in each direction per day
				//     A partial joint halftour and a full joint halftour in the same direction are not both allowed
				int count4 = 0;
				foreach (PersonDayWrapper personDay in orderedPersonDays) {
					count4++;
					if (count4 <= maxNumberParticipants) {
						for (var j = 1; j <= 3; j++) {
							//							if ((genChoice == j || genChoice == j + 3) && jHTParticipation[jHTSimulated, personDay.Person.Sequence] == true) {
							if ((genChoice == j || genChoice == j + 3) && participants[count4] == 1) {
								// paired alternatives become unavailable if any joint alternative was chosen
								hTAvailable[0, count4] = false;
								if (personDay.HasMandatoryTourToUsualLocation) {
									fHTMandatoryPersonsAvailable[0]--;
									pHTPersonsAvailable[0]--;
									//if (personDay.Person.IsAdult) {
									if (personDay.Person.IsDrivingAge) {
										fHTMandatoryAdultsAvailable[0]--;
										pHTAdultsAvailable[0]--;
									}
								}
								else if (!(personDay.PatternType == Constants.PatternType.HOME)) {
									fHTNonMandatoryAdultsAvailable[0]--;
								}
								if (j != 1) {
									//if one direction was chosen, same direction alternatives also become unavailable
									hTAvailable[j - 1, count4] = false;
									if (personDay.HasMandatoryTourToUsualLocation) {
										fHTMandatoryPersonsAvailable[j - 1]--;
										pHTPersonsAvailable[j - 1]--;
										//if (personDay.Person.IsAdult) {
										if (personDay.Person.IsDrivingAge) {
											fHTMandatoryAdultsAvailable[j - 1]--;
											pHTAdultsAvailable[j - 1]--;
										}
									}
									else if (!(personDay.PatternType == Constants.PatternType.HOME)) {
										fHTNonMandatoryAdultsAvailable[j - 1]--;
									}
								}
								//if pair was chosen, both one-direction alternatives also become unavailable
								if (j == 1) {
									hTAvailable[j, count4] = false;
									hTAvailable[j + 1, count4] = false;
									if (personDay.HasMandatoryTourToUsualLocation) {
										fHTMandatoryPersonsAvailable[j]--;
										pHTPersonsAvailable[j]--;
										fHTMandatoryPersonsAvailable[j + 1]--;
										pHTPersonsAvailable[j + 1]--;
										//if (personDay.Person.IsAdult) {
										if (personDay.Person.IsDrivingAge) {
											fHTMandatoryAdultsAvailable[j]--;
											pHTAdultsAvailable[j]--;
											fHTMandatoryAdultsAvailable[j + 1]--;
											pHTAdultsAvailable[j + 1]--;
										}
									}
									else if (!(personDay.PatternType == Constants.PatternType.HOME)) {
										fHTNonMandatoryAdultsAvailable[j]--;
										fHTNonMandatoryAdultsAvailable[j + 1]--;
									}
								}
							}
						}
					}
				}
				if (jHTSimulated >= maxJointHalfTours && !Global.Configuration.IsInEstimationMode) { break; }
			} //end generation and participation loops
		}






		private static void RunJointTourGenerationModelSuite(HouseholdDayWrapper householdDay) {
			//don't run this model if no more than one person in household has a travel day
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > RunJointTourGenerationModelSuite for Household {0}", householdDay.Household.Id);

			int maxNumberParticipants = 5;

			IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<PersonDayWrapper>();

			int numberOfEligiblePersonsInHousehold = 0;
			foreach (var personDay in orderedPersonDays) {
				if (personDay.JointTourParticipationPriority < 9) {
					numberOfEligiblePersonsInHousehold++;
				}
			}
			if (numberOfEligiblePersonsInHousehold <= 1 || !Global.Configuration.ShouldRunJointTourGenerationModel) {
				return;
			}


			int[] purpose = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			bool[,] jTParticipation = new bool[8, Global.MaximumHouseholdSize]; //jointTour x personDay.Sequence
			int jt = 0;
			var totalJointTours = 0;
			if (Global.Configuration.IsInEstimationMode) {
				//set joint tour choice outcome variable
				foreach (var jointTour in householdDay.JointToursList) {
					jt++;
					if (jt <= 8) {
						purpose[jt] = jointTour.MainPurpose;
						if (jointTour.PersonSequence1 > 0 && jointTour.PersonSequence1 <= Global.MaximumHouseholdSize) { jTParticipation[jt, jointTour.PersonSequence1] = true; }
						if (jointTour.PersonSequence2 > 0 && jointTour.PersonSequence2 <= Global.MaximumHouseholdSize) { jTParticipation[jt, jointTour.PersonSequence2] = true; }
						if (jointTour.PersonSequence3 > 0 && jointTour.PersonSequence3 <= Global.MaximumHouseholdSize) { jTParticipation[jt, jointTour.PersonSequence3] = true; }
						if (jointTour.PersonSequence4 > 0 && jointTour.PersonSequence4 <= Global.MaximumHouseholdSize) { jTParticipation[jt, jointTour.PersonSequence4] = true; }
						if (jointTour.PersonSequence5 > 0 && jointTour.PersonSequence5 <= Global.MaximumHouseholdSize) { jTParticipation[jt, jointTour.PersonSequence5] = true; }
						if (jointTour.PersonSequence6 > 0 && jointTour.PersonSequence6 <= Global.MaximumHouseholdSize) { jTParticipation[jt, jointTour.PersonSequence6] = true; }
						if (jointTour.PersonSequence7 > 0 && jointTour.PersonSequence7 <= Global.MaximumHouseholdSize) { jTParticipation[jt, jointTour.PersonSequence7] = true; }
						if (jointTour.PersonSequence8 > 0 && jointTour.PersonSequence8 <= Global.MaximumHouseholdSize) { jTParticipation[jt, jointTour.PersonSequence8] = true; }
						totalJointTours = jt;
					}
				}
				// update counts of person's joint tours by purpose
				int count = 0;
				foreach (var personDay in orderedPersonDays) {
					count++;
					foreach (var tour in personDay.Tours) {
						if (count <= maxNumberParticipants && tour.JointTourSequence > 0) {  //only count as joint tour participant if in top five priority persons in HH
							//TODO:  In generating individual tours, the tours recorded as joint for persons below priority five must be counted as individual.
							//var jointTourPurpose = householdDay.JointToursList.First(jT => jT.Sequence == tour.JointTourSequence).MainPurpose;
							personDay.JointTours++;
							switch (tour.DestinationPurpose) {
								case Constants.Purpose.ESCORT:
									personDay.EscortJointTours++;
									break;
								case Constants.Purpose.PERSONAL_BUSINESS:
									personDay.PersonalBusinessJointTours++;
									break;
								case Constants.Purpose.SHOPPING:
									personDay.ShoppingJointTours++;
									break;
								case Constants.Purpose.MEAL:
									personDay.MealJointTours++;
									break;
								case Constants.Purpose.SOCIAL:
									personDay.SocialJointTours++;
									break;
								case Constants.Purpose.RECREATION:
									personDay.RecreationJointTours++;
									break;
								case Constants.Purpose.MEDICAL:
									personDay.MedicalJointTours++;
									break;
							}
						}
					}
				}
			}

			var nCallsForTour = 0;
			for (var i = 0; i <= 8; i++) {
				lock (_lock) {
					ChoiceModelFactory.TotalTimesJointTourGenerationModelRun++;
				}
				nCallsForTour++;
				purpose[nCallsForTour] = HJointTourGenerationModel.Run(householdDay, nCallsForTour, purpose[nCallsForTour]);
				if (purpose[nCallsForTour] > Constants.Purpose.NONE_OR_HOME) {
					//mbtrace
					if (Global.TraceResults) Global.PrintFile.WriteLine("> > > Generated joint tour {0} with purpose {1}", nCallsForTour, purpose[nCallsForTour]);

					// run tour participation model and create tour
					lock (_lock) {
						ChoiceModelFactory.TotalTimesJointTourParticipationModelRun++;
					}
					int[] participants = HJointTourParticipationModel.Run(householdDay, nCallsForTour, purpose, jTParticipation);
					if (Global.Configuration.IsInEstimationMode) {
						int count = 0;
						foreach (PersonDayWrapper personDay in orderedPersonDays) {
							count++;
							if (count <= maxNumberParticipants) {
								if (participants[count] == 1) {
									//update the choices iteratively while the model runs in estimation
									switch (purpose[nCallsForTour]) {
										case Constants.Purpose.ESCORT:
											personDay.CreatedEscortTours++;
											break;
										case Constants.Purpose.PERSONAL_BUSINESS:
											personDay.CreatedPersonalBusinessTours++;
											break;
										case Constants.Purpose.SHOPPING:
											personDay.CreatedShoppingTours++;
											break;
										case Constants.Purpose.MEAL:
											personDay.CreatedMealTours++;
											break;
										case Constants.Purpose.SOCIAL:
											personDay.CreatedSocialTours++;
											break;
										case Constants.Purpose.RECREATION:
											personDay.CreatedRecreationTours++;
											break;
										case Constants.Purpose.MEDICAL:
											personDay.CreatedMedicalTours++;
											break;
									}
								}
							}
						}

					}

					else if (!Global.Configuration.IsInEstimationMode && !(purpose[nCallsForTour] == Constants.Purpose.NONE_OR_HOME)) {
						//create joint tour
						var jointTour = householdDay.CreateJointTour(householdDay, orderedPersonDays, participants, purpose[nCallsForTour]);
						int count = 0;
						foreach (PersonDayWrapper personDay in orderedPersonDays) {
							count++;
							if (count <= maxNumberParticipants) {
								if (participants[count] == 1) {
									// create nonmandatory tour for person and associate it with JointTour
									TourWrapper tour = (TourWrapper) personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, purpose[nCallsForTour]);
									tour.JointTourSequence = jointTour.Sequence;
									jointTour.SetParticipantTourSequence(tour);
									personDay.JointTours++;
									switch (jointTour.MainPurpose) {
										case Constants.Purpose.ESCORT:
											personDay.EscortJointTours++;
											personDay.CreatedEscortTours++;
											break;
										case Constants.Purpose.PERSONAL_BUSINESS:
											personDay.PersonalBusinessJointTours++;
											personDay.CreatedPersonalBusinessTours++;
											break;
										case Constants.Purpose.SHOPPING:
											personDay.ShoppingJointTours++;
											personDay.CreatedShoppingTours++;
											break;
										case Constants.Purpose.MEAL:
											personDay.MealJointTours++;
											personDay.CreatedMealTours++;
											break;
										case Constants.Purpose.SOCIAL:
											personDay.SocialJointTours++;
											personDay.CreatedSocialTours++;
											break;
										case Constants.Purpose.RECREATION:
											personDay.RecreationJointTours++;
											personDay.CreatedRecreationTours++;
											break;
										case Constants.Purpose.MEDICAL:
											personDay.MedicalJointTours++;
											personDay.CreatedMedicalTours++;
											break;
									}
								}
							}
						}
					}
				}
				//conditionally break the generation loop
				if (Global.Configuration.IsInEstimationMode && nCallsForTour == totalJointTours + 1) {
					return;
				}
				if (!Global.Configuration.IsInEstimationMode && (purpose[nCallsForTour] == Constants.Purpose.NONE_OR_HOME || nCallsForTour == 6)) {
					return;
				}

				//update orderedPersonDays to account for people who are no longer eligible
				orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<PersonDayWrapper>();

				numberOfEligiblePersonsInHousehold = 0;
				foreach (var personDay in orderedPersonDays) {
					if (personDay.JointTourParticipationPriority < 9) {
						numberOfEligiblePersonsInHousehold++;
					}
				}
				if (numberOfEligiblePersonsInHousehold <= 1 || !Global.Configuration.ShouldRunJointTourGenerationModel) {
					return;
				}
			}
		}


		private static void RunPersonDayModelSuite(IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay) {
			if (personDay.PatternType == Constants.PatternType.HOME) {
				return;
			}
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > RunPersonDayModelSuite for Household {0} Person {1}", householdDay.Household.Id, personDay.Person.Sequence);

			if (Global.Configuration.ShouldRunHouseholdPersonDayPatternModel) {
				// determines if there are tours for a person's day
				// sets number of stops for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonDayPatternModelRun++;
				}
				HPersonDayPatternModel.Run(personDay, householdDay);

				if (householdDay.IsValid == false || personDay.IsValid == false) {
					return;
				}
			}

			if (!Global.Configuration.ShouldRunPersonTourGenerationModel) {
				return;
			}

			int[] tourPurpose = new int[11];

			//set choice array tourPurpose for estimation
			if (Global.Configuration.IsInEstimationMode) {
				int count = 0;
				int createdEscortTours = personDay.CreatedEscortTours;
				int createdPersonalBusinessTours = personDay.CreatedPersonalBusinessTours;
				int createdShoppingTours = personDay.CreatedShoppingTours;
				int createdMealTours = personDay.CreatedMealTours;
				int createdSocialTours = personDay.CreatedSocialTours;
				int createdRecreationTours = personDay.CreatedRecreationTours;
				int createdMedicalTours = personDay.CreatedMedicalTours;
				foreach (var tour in personDay.Tours) {
					switch (tour.DestinationPurpose) {
						case Constants.Purpose.ESCORT:
							if (createdEscortTours < personDay.EscortTours) {
								count++;
								tourPurpose[count] = Constants.Purpose.ESCORT;
								createdEscortTours++;
							}
							break;
						case Constants.Purpose.PERSONAL_BUSINESS:
							if (createdPersonalBusinessTours < personDay.PersonalBusinessTours) {
								count++;
								tourPurpose[count] = Constants.Purpose.PERSONAL_BUSINESS;
								createdPersonalBusinessTours++;
							}
							break;
						case Constants.Purpose.SHOPPING:
							if (createdShoppingTours < personDay.ShoppingTours) {
								count++;
								tourPurpose[count] = Constants.Purpose.SHOPPING;
								createdShoppingTours++;
							}
							break;
						case Constants.Purpose.MEAL:
							if (createdMealTours < personDay.MealTours) {
								count++;
								tourPurpose[count] = Constants.Purpose.MEAL;
								createdMealTours++;
							}
							break;
						case Constants.Purpose.SOCIAL:
							if (createdSocialTours < personDay.SocialTours) {
								count++;
								tourPurpose[count] = Constants.Purpose.SOCIAL;
								createdSocialTours++;
							}
							break;
						case Constants.Purpose.RECREATION:
							if (createdRecreationTours < personDay.RecreationTours) {
								count++;
								tourPurpose[count] = Constants.Purpose.RECREATION;
								createdRecreationTours++;
							}
							break;
						case Constants.Purpose.MEDICAL:
							if (createdMedicalTours < personDay.MedicalTours) {
								count++;
								tourPurpose[count] = Constants.Purpose.MEDICAL;
								createdMedicalTours++;
							}
							break;
					}
				}
			}

			int maxPurpose = 2;
			for (var count = 1; count <= 10; count++) {
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonTourGenerationModelRun++;
				}
				tourPurpose[count] = HPersonTourGenerationModel.Run(personDay, householdDay, maxPurpose, tourPurpose[count]);
				if (tourPurpose[count] == Constants.Purpose.NONE_OR_HOME || personDay.TotalCreatedTours >= 10) {
					break;
				}
				maxPurpose = Math.Max(maxPurpose, tourPurpose[count]);
				switch (tourPurpose[count]) {
					case Constants.Purpose.ESCORT:
						personDay.CreatedEscortTours++;
						break;
					case Constants.Purpose.SHOPPING:
						personDay.CreatedShoppingTours++;
						break;
					case Constants.Purpose.MEAL:
						personDay.CreatedMealTours++;
						break;
					case Constants.Purpose.SOCIAL:
						personDay.CreatedSocialTours++;
						break;
					case Constants.Purpose.RECREATION:
						personDay.CreatedRecreationTours++;
						break;
					case Constants.Purpose.MEDICAL:
						personDay.CreatedMedicalTours++;
						break;
				}
				if (!Global.Configuration.IsInEstimationMode) {
					TourWrapper tour = (TourWrapper) personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, tourPurpose[count]);
					//mbtrace
					if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > Generated Individual Tour {0} for Purpose {1}", tour.Sequence, tour.DestinationPurpose);
				}
			}
		}

		private static void RunPartialJointHalfTourModelSuite(HouseholdDayWrapper householdDay, PartialHalfTourWrapper partialJointHalfTour) {

			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > RunPartialJointHalfTourModelSuite for Household {0} PartialHalfTour {1}", householdDay.Household.Id, partialJointHalfTour.Sequence);

			if (householdDay.Household.Id == 23982 && householdDay.AttemptedSimulations == 1) { //15454) { //2071) {
				bool testbreak = true;
			}

			// declare array variables 
			IPersonDayWrapper[] participantDay = new IPersonDayWrapper[9];
			ITourWrapper[] tour = new ITourWrapper[9];
			IHalfTour[] halfTour = new IHalfTour[9];

			//get partialJointHalfTour.TourSequence1 thru 8 tours into tour array
			participantDay[1] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == partialJointHalfTour.PersonSequence1);
			participantDay[2] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == partialJointHalfTour.PersonSequence2);
			participantDay[3] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == partialJointHalfTour.PersonSequence3);
			participantDay[4] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == partialJointHalfTour.PersonSequence4);
			participantDay[5] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == partialJointHalfTour.PersonSequence5);
			participantDay[6] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == partialJointHalfTour.PersonSequence6);
			participantDay[7] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == partialJointHalfTour.PersonSequence7);
			participantDay[8] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == partialJointHalfTour.PersonSequence8);
			//get partialJointHalfTour.PersonSequence1 thru 8 personDays into particpantDay array
			tour[1] = participantDay[1] == null ? null : participantDay[1].Tours.FirstOrDefault(t => t.Sequence == partialJointHalfTour.TourSequence1);
			tour[2] = participantDay[2] == null ? null : participantDay[2].Tours.FirstOrDefault(t => t.Sequence == partialJointHalfTour.TourSequence2);
			tour[3] = participantDay[3] == null ? null : participantDay[3].Tours.FirstOrDefault(t => t.Sequence == partialJointHalfTour.TourSequence3);
			tour[4] = participantDay[4] == null ? null : participantDay[4].Tours.FirstOrDefault(t => t.Sequence == partialJointHalfTour.TourSequence4);
			tour[5] = participantDay[5] == null ? null : participantDay[5].Tours.FirstOrDefault(t => t.Sequence == partialJointHalfTour.TourSequence5);
			tour[6] = participantDay[6] == null ? null : participantDay[6].Tours.FirstOrDefault(t => t.Sequence == partialJointHalfTour.TourSequence6);
			tour[7] = participantDay[7] == null ? null : participantDay[7].Tours.FirstOrDefault(t => t.Sequence == partialJointHalfTour.TourSequence7);
			tour[8] = participantDay[8] == null ? null : participantDay[8].Tours.FirstOrDefault(t => t.Sequence == partialJointHalfTour.TourSequence8);

			//			if (householdDay.Household.Id == 7426) {
			//				int ii = 0;
			//			}

			var direction = partialJointHalfTour.Direction;

			if (Global.Configuration.IsInEstimationMode) {
				//sort the participants in order by decreasing number of trips on half tour
				int[] sortOrder = new int[9];
				int[] sortOrderClone = new int[9];
				sortOrder[0] = 0;
				sortOrderClone[1] = sortOrder[1] = tour[1] == null ? 999 : direction == 1 ? 20 - tour[1].HalfTour1Trips : 20 - tour[1].HalfTour2Trips;
				sortOrderClone[2] = sortOrder[2] = tour[2] == null ? 999 : direction == 1 ? 20 - tour[2].HalfTour1Trips : 20 - tour[2].HalfTour2Trips;
				sortOrderClone[3] = sortOrder[3] = tour[3] == null ? 999 : direction == 1 ? 20 - tour[3].HalfTour1Trips : 20 - tour[3].HalfTour2Trips;
				sortOrderClone[4] = sortOrder[4] = tour[4] == null ? 999 : direction == 1 ? 20 - tour[4].HalfTour1Trips : 20 - tour[4].HalfTour2Trips;
				sortOrderClone[5] = sortOrder[5] = tour[5] == null ? 999 : direction == 1 ? 20 - tour[5].HalfTour1Trips : 20 - tour[5].HalfTour2Trips;
				sortOrderClone[6] = sortOrder[6] = tour[6] == null ? 999 : direction == 1 ? 20 - tour[6].HalfTour1Trips : 20 - tour[6].HalfTour2Trips;
				sortOrderClone[7] = sortOrder[7] = tour[7] == null ? 999 : direction == 1 ? 20 - tour[7].HalfTour1Trips : 20 - tour[7].HalfTour2Trips;
				sortOrderClone[8] = sortOrder[8] = tour[8] == null ? 999 : direction == 1 ? 20 - tour[8].HalfTour1Trips : 20 - tour[8].HalfTour2Trips;

				Array.Sort(sortOrder, participantDay);
				Array.Sort(sortOrderClone, tour);
			}

			int[] halfTourTrips = new int[9];
			ICondensedParcel[] destination = new ICondensedParcel[9];
			int[] destinationSequence = new int[9];
			int[] destinationParcelID = new int[9];
			int[] travelers = new int[9];
			int[] tourWithDestination = new int[9];
			//determine the sequence number of each person's destination, taking into consideration that people with same tour destination have the same sequence
			destinationSequence[1] = 1;
			for (var i = 1; i <= partialJointHalfTour.Participants; i++) {
				if (Global.Configuration.IsInEstimationMode) {
					destinationParcelID[i] = tour[i].DestinationParcelId;
				}
				else {
					destinationParcelID[i] = tour[i].DestinationPurpose == Constants.Purpose.WORK ? tour[i].Person.UsualWorkParcelId : tour[i].Person.UsualSchoolParcelId;
				}
				if (i == 1) {
					destinationSequence[i] = 1;
				}
				else {
					destinationSequence[i] = destinationSequence[i - 1];
					if (!(destinationParcelID[i] == destinationParcelID[i - 1])) {
						destinationSequence[i]++;
					}
				}
			}
			//determine the number of travelers for each segment of the halftour
			for (var i = 1; i <= partialJointHalfTour.Participants; i++) {
				for (var j = 1; j <= destinationSequence[partialJointHalfTour.Participants]; j++) {
					if (destinationSequence[i] <= j) {
						travelers[j]++;
					}
				}
			}

			//determine a tour with destination at each segment of the halftour
			for (var i = 1; i <= partialJointHalfTour.Participants; i++) {
				for (var j = 1; j <= destinationSequence[partialJointHalfTour.Participants]; j++) {
					if (destinationSequence[i] == j) {
						tourWithDestination[j] = i;
					}
				}
			}

			//if (!Global.Configuration.IsInEstimationMode) {
			//determine the number of trips in each participant's halfTour
			for (var i = 1; i <= partialJointHalfTour.Participants; i++) {
				halfTourTrips[i] = destinationSequence[partialJointHalfTour.Participants] - destinationSequence[i] + 1;
			}
			//determine the destination of each segment of the halfTour
			for (var j = 1; j <= destinationSequence[partialJointHalfTour.Participants]; j++) {
				for (var i = 1; i <= partialJointHalfTour.Participants; i++) {
					if (destinationSequence[i] == j) {
						destination[j] = tour[i].DestinationParcel;
						break;
					}
				}
			}
			//}   Note:  Use the above logic for estimation mode, which causes us to ignore trips other than drop-off/pickup trips on the halftour
			//else {
			//determine the number of trips in each participant's halfTour
			//	for (var i = 1; i <= partialJointHalfTour.Participants; i++) {
			//	halfTourTrips[i] = direction == 1 ? tour[i].HalfTour1Trips : tour[i].HalfTour2Trips;
			//}
			//determine the destination of each segment of the halfTour
			//for (var j = 1; j <= halfTourTrips[1]; j++) {
			//for (var i = 1; i <= partialJointHalfTour.Participants; i++) {
			//if (halfTourTrips[i] == halfTourTrips[1] - j + 1) {
			//destination[j] = tour[i].DestinationParcel;
			//break;
			//}
			//}
			//}

			//}

			for (var i = 2; i <= partialJointHalfTour.Participants; i++) {
				int destinationArrivalTime = 0;
				int destinationDepartureTime = 0;
				string impedanceVariable;
				int mode;
				int pathType;
				switch (tour[i - 1].Mode) {
					case Constants.Mode.WALK:
						impedanceVariable = "time";
						mode = Constants.Mode.WALK;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.BIKE:
						impedanceVariable = "time";
						mode = Constants.Mode.BIKE;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.SOV:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.SOV;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.HOV2:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.HOV2;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.HOV3:
					case Constants.Mode.SCHOOL_BUS:
					case Constants.Mode.OTHER:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.HOV3;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.TRANSIT:
					case Constants.Mode.PARK_AND_RIDE:
						impedanceVariable = "ivtime";   // TODO:  logic that uses this SHOULD also include initial wait time and transfer time
						mode = Constants.Mode.TRANSIT;
						pathType = Constants.PathType.LOCAL_BUS;
						break;
					default:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.HOV3;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
				}

				var circuityDistance = Global.Configuration.UseShortDistanceNodeToNodeMeasures
												? tour[i - 1].DestinationParcel.NodeToNodeDistance(tour[i].DestinationParcel)
												: (Global.Configuration.UseShortDistanceCircuityMeasures)
												? tour[i - 1].DestinationParcel.CircuityDistance(tour[i].DestinationParcel)
												: Constants.DEFAULT_VALUE;

				if (!tour[i].DestinationModeAndTimeHaveBeenSimulated) {
					if (!(Global.Configuration.IsInEstimationMode)) {
						tour[i].Mode = partialJointHalfTour.Participants > 2 ? Constants.Mode.HOV3 : Constants.Mode.HOV2;
						// need to model mode for Actum
					}

					if (i == 2) {
						// set tour TOD for i.  If i == 2 then arrival and departure time are modeled
						SetTourModeAndTime(householdDay, tour[i], tour[i].Mode, 0, 0);

						// set exact minute of tour destination arrival and departure times
						if (tour[i].HalfTourFromOrigin == null) {
							tour[i].SetHalfTours(1);
							if (!(Global.Configuration.IsInEstimationMode)) {
								var hT = tour[i].GetHalfTour(1);
								var trip = hT.Trips[0];
								trip.DestinationParcel = partialJointHalfTour.Participants > 2 ? tour[i + 1].DestinationParcel : tour[i].OriginParcel;
								trip.OriginParcel = trip.Tour.DestinationParcel;
								trip.Mode = tour[i].Mode;
								bool forTourTimesOnly = true;
								SetTripModeAndTime(householdDay, tour[i], trip, forTourTimesOnly);
								tour[i].DestinationArrivalTime = trip.DepartureTime;
							}
						}
						if (tour[i].HalfTourFromDestination == null) {
							tour[i].SetHalfTours(2);
							if (!(Global.Configuration.IsInEstimationMode)) {
								var hT = tour[i].GetHalfTour(2);
								var trip = hT.Trips[0];
								trip.DestinationParcel = partialJointHalfTour.Participants > 2 ? tour[i + 1].DestinationParcel : tour[i].OriginParcel;
								trip.OriginParcel = trip.Tour.DestinationParcel;
								trip.Mode = tour[i].Mode;
								bool forTourTimesOnly = true;
								SetTripModeAndTime(householdDay, tour[i], trip, forTourTimesOnly);
								tour[i].DestinationDepartureTime = trip.DepartureTime;
							}
						}
						// JLB 20130812  The following escape seems to hide a bug. The following note came from the Actum version,
						//  and a siilar bug occured 59% through a 25% run of PSRC synthetic pop. :  
						// In particular a case with DySim version 2199
						// household 30345.  Tour mode was 5. The above code modeled trip mode = 1 on first half tour
						// and a trip departure time (tour dest arrivel time) of 749...the last minute of the modeled destination arrival/departure period
						// and the second half tour trip departure time was modeled as 1.
						if (tour[i].DestinationArrivalTime < Constants.Time.FIVE_AM
							|| tour[i].DestinationDepartureTime > Constants.Time.EIGHT_PM
							|| tour[i].DestinationDepartureTime < tour[i].DestinationArrivalTime + 60) {
							householdDay.IsValid = false;
							return;
						}
					}
					else if (partialJointHalfTour.Paired) {
						if (!(Global.Configuration.IsInEstimationMode)) {
							//arrival and departure times are both determined (as below) rather than beign modeled.
							if (halfTourTrips[i] == halfTourTrips[i - 1]) {
								tour[i].DestinationArrivalTime = tour[i - 1].DestinationArrivalTime;
								tour[i].DestinationDepartureTime = tour[i - 1].DestinationDepartureTime;
							}
							else {
								tour[i].DestinationArrivalTime = tour[i - 1].DestinationArrivalTime
									 - (int) Math.Round(ImpedanceRoster.GetValue(impedanceVariable, mode, pathType, Math.Max(Global.Configuration.VotLowMedium + 1, 10.0), tour[i - 1].DestinationArrivalTime - 1, tour[i].DestinationParcel, tour[i - 1].DestinationParcel, circuityDistance).Variable)
									 - 3;
								tour[i].DestinationDepartureTime = tour[i - 1].DestinationDepartureTime
									 + (int) Math.Round(ImpedanceRoster.GetValue(impedanceVariable, mode, pathType, Math.Max(Global.Configuration.VotLowMedium + 1, 10.0), tour[i - 1].DestinationDepartureTime + 1, tour[i - 1].DestinationParcel, tour[i].DestinationParcel, circuityDistance).Variable)
									 + 3;
							}
							// Mode and times were already set, but other properties need to be set by SetTourModeAndTime()
							SetTourModeAndTime(householdDay, tour[i], tour[i].Mode, tour[i].DestinationArrivalTime, tour[i].DestinationDepartureTime);
						}
					}
					else if (direction == 1) {
						//arrival time is determined by i-1's arrival time and travel time by tour mode between i's destination and i-1's destination 
						if (halfTourTrips[i] == halfTourTrips[i - 1]) {
							destinationArrivalTime = tour[i - 1].DestinationArrivalTime;
						}
						else {
							destinationArrivalTime = tour[i - 1].DestinationArrivalTime
								 - (int) Math.Round(ImpedanceRoster.GetValue(impedanceVariable, mode, pathType, Math.Max(Global.Configuration.VotLowMedium + 1, 10.0), tour[i - 1].DestinationArrivalTime - 1, tour[i].DestinationParcel, tour[i - 1].DestinationParcel, circuityDistance).Variable)
								 - 3;
						}
						//departure time is modeled
						SetTourModeAndTime(householdDay, tour[i], 0, destinationArrivalTime, 0);
					}
					else if (direction == 2) {
						//departure time is determined by i-1's departure time and travel time by tour mode between i's destination and i-1's destination
						if (halfTourTrips[i] == halfTourTrips[i - 1]) {
							destinationDepartureTime = tour[i - 1].DestinationDepartureTime;
						}
						else {
							destinationDepartureTime = tour[i - 1].DestinationDepartureTime
								 + (int) Math.Round(ImpedanceRoster.GetValue(impedanceVariable, mode, pathType, Math.Max(Global.Configuration.VotLowMedium + 1, 10.0), tour[i - 1].DestinationDepartureTime + 1, tour[i - 1].DestinationParcel, tour[i].DestinationParcel, circuityDistance).Variable)
								 + 3;
						}
						//arrival time is modeled
						SetTourModeAndTime(householdDay, tour[i], 0, 0, destinationDepartureTime);
					}
					if (i > 2) {
						if (tour[i].HalfTourFromOrigin == null) {
							tour[i].SetHalfTours(1);
						}
						if (tour[i].HalfTourFromDestination == null) {
							tour[i].SetHalfTours(2);
						}
					}
					tour[i].DestinationModeAndTimeHaveBeenSimulated = true;
				}
				halfTour[i] = tour[i].GetHalfTour(direction);

				//mbtrace
				if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > Tour results for participant {0} Destination {1} Mode {2} ArrivalTime {3} DepartureTime {4}",
					 i, tour[i].DestinationParcelId, tour[i].Mode, tour[i].DestinationArrivalTime, tour[i].DestinationDepartureTime);

			}

			if (!tour[1].DestinationModeAndTimeHaveBeenSimulated) {

				string impedanceVariable;
				int mode;
				int pathType;
				switch (tour[1].Mode) {
					case Constants.Mode.WALK:
						impedanceVariable = "time";
						mode = Constants.Mode.WALK;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.BIKE:
						impedanceVariable = "time";
						mode = Constants.Mode.BIKE;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.SOV:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.SOV;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.HOV2:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.HOV2;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.HOV3:
					case Constants.Mode.SCHOOL_BUS:
					case Constants.Mode.OTHER:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.HOV3;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
					case Constants.Mode.TRANSIT:
					case Constants.Mode.PARK_AND_RIDE:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.TRANSIT;
						pathType = Constants.PathType.LOCAL_BUS;
						break;
					default:
						impedanceVariable = "ivtime";
						mode = Constants.Mode.HOV3;
						pathType = Constants.PathType.FULL_NETWORK;
						break;
				}

				if (!(Global.Configuration.IsInEstimationMode)) {
					tour[1].Mode = partialJointHalfTour.Participants > 2 ? Constants.Mode.HOV3 : Constants.Mode.HOV2;
				}
				if (partialJointHalfTour.Paired) {
					if (!(Global.Configuration.IsInEstimationMode)) {
						//arrival and departure times are both determined (as below) rather than beign modeled.
						if (halfTourTrips[1] == halfTourTrips[2]) {
							tour[1].DestinationArrivalTime = tour[2].DestinationArrivalTime;
							tour[1].DestinationDepartureTime = tour[2].DestinationDepartureTime;
						}
						else {
							tour[1].DestinationArrivalTime = tour[2].DestinationArrivalTime
								 + (int) Math.Round(ImpedanceRoster.GetValue(impedanceVariable, mode, pathType, Math.Max(Global.Configuration.VotLowMedium + 1, 10.0),
											Math.Min(tour[2].DestinationArrivalTime + 4, 1440), tour[2].DestinationParcel, tour[1].DestinationParcel).Variable)
								 + 3;
							tour[1].DestinationDepartureTime = tour[2].DestinationDepartureTime
								 - (int) Math.Round(ImpedanceRoster.GetValue(impedanceVariable, mode, pathType, Math.Max(Global.Configuration.VotLowMedium + 1, 10.0),
											Math.Max(tour[2].DestinationDepartureTime - 4, 1), tour[1].DestinationParcel, tour[2].DestinationParcel).Variable)
								 - 3;
						}
						// Mode and times were already set, but other properties need to be set by SetTourModeAndTime()
						SetTourModeAndTime(householdDay, tour[1], tour[1].Mode, tour[1].DestinationArrivalTime, tour[1].DestinationDepartureTime);
					}
				}

				else if (direction == 1) {
					int destinationArrivalTime = 0;
					//arrival time is determined by 2's arrival time and travel time by tour mode between 1's destination and 2's destination 
					if (halfTourTrips[1] == halfTourTrips[2]) {
						destinationArrivalTime = tour[2].DestinationArrivalTime;
					}
					else {
						//if (tour[2].DestinationParcel == null || tour[1].DestinationParcel == null) {
						//tour[2].DestinationParcel = tour[2].DestinationParcel;
						//}

						destinationArrivalTime = tour[2].DestinationArrivalTime
							 + (int) Math.Round(ImpedanceRoster.GetValue(impedanceVariable, mode, pathType, Math.Max(Global.Configuration.VotLowMedium + 1, 10.0), tour[2].DestinationArrivalTime + 4, tour[2].DestinationParcel, tour[1].DestinationParcel).Variable)
							 + 3;
					}
					//departure time is modeled
					SetTourModeAndTime(householdDay, tour[1], 0, destinationArrivalTime, 0);
				}
				else if (direction == 2) {
					int destinationDepartureTime = 0;
					//departure time is determined by 2's departure time and travel time by tour mode between 1's destination and 2's destination
					if (halfTourTrips[1] == halfTourTrips[2]) {
						destinationDepartureTime = tour[2].DestinationDepartureTime;
					}
					else {
						destinationDepartureTime = tour[2].DestinationDepartureTime
							 - (int) Math.Round(ImpedanceRoster.GetValue(impedanceVariable, mode, pathType, Math.Max(Global.Configuration.VotLowMedium + 1, 10.0), tour[2].DestinationDepartureTime - 4, tour[1].DestinationParcel, tour[2].DestinationParcel).Variable)
							 - 3;
					}
					//arrival time is modeled
					SetTourModeAndTime(householdDay, tour[1], 0, 0, destinationDepartureTime);
				}
				if (tour[1].HalfTourFromOrigin == null) {
					tour[1].SetHalfTours(1);
				}
				if (tour[1].HalfTourFromDestination == null) {
					tour[1].SetHalfTours(2);
				}
				tour[1].DestinationModeAndTimeHaveBeenSimulated = true;
			}
			halfTour[1] = tour[1].GetHalfTour(direction);

			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > Tour results for participant {0} Destination {1} Mode {2} ArrivalTime {3} DepartureTime {4}",
				 1, tour[1].DestinationParcelId, tour[1].Mode, tour[1].DestinationArrivalTime, tour[1].DestinationDepartureTime);

			//update tours' time windows
			//JLB 20130716 comment out the temporary updating of time window
			//for (var i = 1; i <= partialJointHalfTour.Participants; i++) {
			//	UpdateTimeWindowForTourDestinationTimes(tour[i]);
			//}

			// simulate halftour
			ICondensedParcel stopLocation = null;
			int stopPurpose = Constants.Purpose.NONE_OR_HOME;
			int tripMode = Constants.Mode.NONE;
			int tripDepartureTime = 0;
			for (var i = 1; i <= partialJointHalfTour.Participants; i++) {  // this loops on all participants
				for (var j = halfTourTrips[1] - halfTourTrips[i] + 1; j <= halfTourTrips[1]; j++) {  // this loops on the trips for the participant
					if (householdDay.Household.Id == 15454) {
						bool testbreak = true;
					}
					var trip = halfTour[i].Trips[j + halfTourTrips[i] - halfTourTrips[1] - 1];
					halfTour[i].SimulatedTrips++;
					if (!Global.Configuration.IsInEstimationMode) {
						if (trip.IsHalfTourFromOrigin) {
							tour[i].HalfTour1Trips++;
						}
						else {
							tour[i].HalfTour2Trips++;
						}
					}
					stopPurpose = j == halfTourTrips[1] ? Constants.Purpose.NONE_OR_HOME : Constants.Purpose.ESCORT;
					stopLocation = j == halfTourTrips[1] ? householdDay.Household.ResidenceParcel : destination[j + 1];
					tripMode = travelers[j] == 1 ? Constants.Mode.SOV : travelers[j] == 2 ? Constants.Mode.HOV2 : Constants.Mode.HOV3;
					if (direction == 1) {
						tripDepartureTime = tour[tourWithDestination[j]].DestinationArrivalTime;
					}
					else {
						tripDepartureTime = tour[tourWithDestination[j]].DestinationDepartureTime;
					}
					RunPartialHalfTourTripModelSuite(tour[i], halfTour[i], trip, stopPurpose, stopLocation, tripMode, tripDepartureTime);
				}
				if (direction == Constants.TourDirection.ORIGIN_TO_DESTINATION) {
					tour[i].HalfTour1HasBeenSimulated = true;
				}
				else {
					tour[i].HalfTour2HasBeenSimulated = true;
				}
				tour[i].SetOriginTimes(direction);
				if (tour[i].HalfTour1HasBeenSimulated && tour[i].HalfTour2HasBeenSimulated) {
					UpdateTimeWindowForTourDestinationTimes(tour[i]);
				}
			}
		}

		private static void RunFullJointHalfTourModelSuite(HouseholdDayWrapper householdDay, FullHalfTourWrapper fullJointHalfTour) {

			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > RunFullJointHalfTourModelSuite for Household {0} FullHalfTour {1}", householdDay.Household.Id, fullJointHalfTour.Sequence);

			// declare array variables 
			IPersonDayWrapper[] participantDay = new IPersonDayWrapper[9];
			ITourWrapper[] tour = new ITourWrapper[9];

			if (fullJointHalfTour.Id == 24951) {
				bool testbreak = true;
			}
			if (householdDay.Household.Id == 23982 && householdDay.AttemptedSimulations == 1) {
				bool testbreak = true;
			}

			//get fullJointHalfTour.TourSequence1 thru 8 tours into tour array
			participantDay[1] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == fullJointHalfTour.PersonSequence1);
			participantDay[2] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == fullJointHalfTour.PersonSequence2);
			participantDay[3] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == fullJointHalfTour.PersonSequence3);
			participantDay[4] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == fullJointHalfTour.PersonSequence4);
			participantDay[5] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == fullJointHalfTour.PersonSequence5);
			participantDay[6] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == fullJointHalfTour.PersonSequence6);
			participantDay[7] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == fullJointHalfTour.PersonSequence7);
			participantDay[8] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == fullJointHalfTour.PersonSequence8);
			//get fullJointHalfTour.PersonSequence1 thru 8 personDays into particpantDay array
			tour[1] = participantDay[1] == null ? null : participantDay[1].Tours.FirstOrDefault(t => t.Sequence == fullJointHalfTour.TourSequence1);
			tour[2] = participantDay[2] == null ? null : participantDay[2].Tours.FirstOrDefault(t => t.Sequence == fullJointHalfTour.TourSequence2);
			tour[3] = participantDay[3] == null ? null : participantDay[3].Tours.FirstOrDefault(t => t.Sequence == fullJointHalfTour.TourSequence3);
			tour[4] = participantDay[4] == null ? null : participantDay[4].Tours.FirstOrDefault(t => t.Sequence == fullJointHalfTour.TourSequence4);
			tour[5] = participantDay[5] == null ? null : participantDay[5].Tours.FirstOrDefault(t => t.Sequence == fullJointHalfTour.TourSequence5);
			tour[6] = participantDay[6] == null ? null : participantDay[6].Tours.FirstOrDefault(t => t.Sequence == fullJointHalfTour.TourSequence6);
			tour[7] = participantDay[7] == null ? null : participantDay[7].Tours.FirstOrDefault(t => t.Sequence == fullJointHalfTour.TourSequence7);
			tour[8] = participantDay[8] == null ? null : participantDay[8].Tours.FirstOrDefault(t => t.Sequence == fullJointHalfTour.TourSequence8);

			var direction = fullJointHalfTour.Direction;

			int oldestNonEscortPersonID = 0;
			int oldestNonEscortAge = 0;
			int defaultMode = 0;
			int defaultArrivalTime = 0;
			int defaultDepartureTime = 0;
			ICondensedParcel destinationParcel = null;

			bool modeAndTimeAlreadySet = false;
			for (var i = 1; i <= 8; i++) {
				// if a non-escort participant's tour mode and time have been set already, use it
				if (!modeAndTimeAlreadySet && (tour[i] != null) && (tour[i].DestinationPurpose != Constants.Purpose.ESCORT) && tour[i].DestinationModeAndTimeHaveBeenSimulated) {
					destinationParcel = tour[i].DestinationParcel;
					defaultMode = tour[i].Mode;
					defaultArrivalTime = tour[i].DestinationArrivalTime;
					defaultDepartureTime = tour[i].DestinationDepartureTime;
					modeAndTimeAlreadySet = true;
				}
			}
			// determine oldest nonEscort on halfTour
			var iOldest = 0;
			for (var i = 1; i <= 8; i++) {
				if (tour[i] != null && tour[i].DestinationPurpose != Constants.Purpose.ESCORT && participantDay[i].Person.Age >= oldestNonEscortAge) {
					oldestNonEscortAge = participantDay[i].Person.Age;
					oldestNonEscortPersonID = participantDay[i].Person.Id;
					iOldest = i;
				}
			}
			// if tour mode and time haven't already been set, then set them for oldest nonEscort
			if (!modeAndTimeAlreadySet) {
				for (var i = 1; i <= 8; i++) {
					if ((participantDay[i] != null) && (oldestNonEscortPersonID == participantDay[i].Person.Id)) {
						destinationParcel = tour[i].DestinationPurpose == Constants.Purpose.WORK ? tour[i].Person.UsualWorkParcel : tour[i].Person.UsualSchoolParcel;
						SetTourDestination(tour[i], householdDay, destinationParcel);
						// reset in case TourDestinationModel reset it in error situations
						destinationParcel = tour[i].DestinationPurpose == Constants.Purpose.WORK ? tour[i].Person.UsualWorkParcel : tour[i].Person.UsualSchoolParcel;
						SetTourModeAndTime(householdDay, tour[i], 0, 0, 0);
						if (householdDay.IsValid == false) {
							return;
						}
						defaultMode = tour[i].Mode;
						defaultArrivalTime = tour[i].DestinationArrivalTime;
						defaultDepartureTime = tour[i].DestinationDepartureTime;
						modeAndTimeAlreadySet = true;
						break;
					}
				}
			}
			// loop to set tour mode and time for all participants on the half tour.
			int mode = 0;
			int arrivalTime = 0;
			int departureTime = 0;
			for (var i = 1; i <= 8; i++) {
				if (!(tour[i] == null)) {
					if (!Global.Configuration.IsInEstimationMode) {
						//use constrained version of tourmodetimemodel
						mode = Math.Max(defaultMode, tour[i].Mode);
						if (tour[i].DestinationPurpose == Constants.Purpose.ESCORT) {
							if (direction == 1) {
								arrivalTime = defaultArrivalTime;
								departureTime = defaultArrivalTime + Constants.Time.MINIMUM_ACTIVITY_DURATION;
							}
							else {
								departureTime = defaultDepartureTime;
								arrivalTime = defaultDepartureTime - Constants.Time.MINIMUM_ACTIVITY_DURATION;
							}
						}
						else if (tour[i].DestinationModeAndTimeHaveBeenSimulated) {
							if (direction == 1) {
								arrivalTime = defaultArrivalTime;
								departureTime = Math.Max(tour[i].DestinationDepartureTime, arrivalTime + Constants.Time.MINIMUM_ACTIVITY_DURATION);
							}
							else {
								departureTime = defaultDepartureTime;
								arrivalTime = Math.Min(tour[i].DestinationArrivalTime, departureTime - Constants.Time.MINIMUM_ACTIVITY_DURATION);
							}
						}
						else {
							arrivalTime = defaultArrivalTime;
							departureTime = defaultDepartureTime;
						}
						SetTourDestination(tour[i], householdDay, destinationParcel);
						if (householdDay.IsValid == false) {
							return;
						}
						SetTourModeAndTime(householdDay, tour[i], mode, arrivalTime, departureTime);
						if (householdDay.IsValid == false) {
							return;
						}
					}
					tour[i].DestinationModeAndTimeHaveBeenSimulated = true;
				}
			}

			//update tours' time windows
			//JLB 20130716 comment out the temporary updating of time window
			//for (var i = 1; i <= fullJointHalfTour.Participants; i++) {
			//	if (tour[i] == null) {
			//		bool testbreak = true;
			//	}
			//	UpdateTimeWindowForTourDestinationTimes(tour[i]);
			//}

			// model halfTour for oldest nonEscortPerson
			ITourWrapper sourceTour = null;
			for (var i = 1; i <= 8; i++) {
				if (!(participantDay[i] == null) && oldestNonEscortPersonID == participantDay[i].Person.Id) {
					ProcessHalfTours(tour[i], participantDay[i], householdDay, fullJointHalfTour.Direction, fullJointHalfTour.Direction);

					sourceTour = tour[i];
					if (fullJointHalfTour.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION) {
						tour[i].HalfTour1HasBeenSimulated = true;
					}
					else {
						tour[i].HalfTour2HasBeenSimulated = true;
					}
					if (!Global.Configuration.IsInEstimationMode) {
						tour[i].SetOriginTimes(fullJointHalfTour.Direction);
					}
					if (tour[i].HalfTour1HasBeenSimulated && tour[i].HalfTour2HasBeenSimulated) {
						UpdateTimeWindowForTourDestinationTimes(tour[i]);
					}

					break;
				}
			}
			// clone halfTour for other participants
			for (var i = 1; i <= 8; i++) {
				if (!(participantDay[i] == null) && !(oldestNonEscortPersonID == participantDay[i].Person.Id)) {
					if (!Global.Configuration.IsInEstimationMode) {
						CloneHalfTours(sourceTour, tour[i], fullJointHalfTour.Direction, fullJointHalfTour.Direction);
						tour[i].SetOriginTimes(fullJointHalfTour.Direction);
					}
					if (fullJointHalfTour.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION) {
						tour[i].HalfTour1HasBeenSimulated = true;
					}
					else {
						tour[i].HalfTour2HasBeenSimulated = true;
					}
					if (tour[i].HalfTour1HasBeenSimulated && tour[i].HalfTour2HasBeenSimulated) {
						UpdateTimeWindowForTourDestinationTimes(tour[i]);
					}
				}
			}

		}

		private static void RunMandatoryTourModelSuite(TourWrapper tour, PersonDayWrapper personDay, HouseholdDayWrapper householdDay) {
			//needs to include running subtour models of work tours;  see RunTourModels
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > RunMandatoryTourModelSuite for Household {0} Person {1} Tour {2}", householdDay.Household.Id, personDay.Person.Sequence, tour.Sequence);

			if (householdDay.Household.Id == 464960 && personDay.Person.Sequence == 1) {
				bool testbreak = true;
			}

			tour.SetHomeBasedIsSimulated();

			if (!tour.DestinationModeAndTimeHaveBeenSimulated) {
				if (!(tour.DestinationParcelId > 0 && tour.DestinationParcelId == tour.Person.UsualWorkParcelId)) {
					SetTourDestination(tour, householdDay);
				}
				if (!tour.PersonDay.IsValid) {
					return;
				}
			}
			if (tour.DestinationPurpose == Constants.Purpose.WORK) {
				GenerateSubtours(tour, householdDay);
				if (!tour.PersonDay.IsValid) {
					return;
				}
			}
			if (!tour.DestinationModeAndTimeHaveBeenSimulated) {
				SetTourModeAndTime(householdDay, tour, 0, 0, 0);
				if (!tour.PersonDay.IsValid) {
					return;
				}
			}

			//update tour's time windows
			//JLB 20130716 comment out the temporary updating of time window
			//UpdateTimeWindowForTourDestinationTimes(tour);

			// model half tours that have not already been simulated as part of joint half tours
			if (!tour.HalfTour1HasBeenSimulated || !tour.HalfTour2HasBeenSimulated) {
				int firstDirection = Constants.TourDirection.ORIGIN_TO_DESTINATION;
				int lastDirection = Constants.TourDirection.DESTINATION_TO_ORIGIN;
				if (tour.HalfTour1HasBeenSimulated) {
					firstDirection = Constants.TourDirection.DESTINATION_TO_ORIGIN;
				}
				else if (tour.HalfTour2HasBeenSimulated) {
					lastDirection = Constants.TourDirection.ORIGIN_TO_DESTINATION;
				}
				lock (_lock) {
					ChoiceModelFactory.TotalTimesTourTripModelsRun++;
				}
				RunTourTripModels(tour, personDay, householdDay, firstDirection, lastDirection);
				if (!personDay.IsValid) {
					return;
				}
			}
			UpdateTimeWindowForTourDestinationTimes(tour);

			lock (_lock) {
				ChoiceModelFactory.TotalTimesTourSubtourModelsRun++;
			}
			RunSubtourModels(tour, personDay, householdDay);
			if (!personDay.IsValid) {
				return;
			}
		}

		private static void RunJointTourModelSuite(HouseholdDayWrapper householdDay, JointTourWrapper jointTour) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > RunJointTourModelSuite for Household {0} JointTour {1}", householdDay.Household.Id, jointTour.Sequence);

			// declare array variables 
			IPersonDayWrapper[] participantDay = new IPersonDayWrapper[9];
			ITourWrapper[] tour = new ITourWrapper[9];

			//get JointTour.TourSequence1 thru 8 tours into tour array
			participantDay[1] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == jointTour.PersonSequence1);
			participantDay[2] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == jointTour.PersonSequence2);
			participantDay[3] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == jointTour.PersonSequence3);
			participantDay[4] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == jointTour.PersonSequence4);
			participantDay[5] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == jointTour.PersonSequence5);
			participantDay[6] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == jointTour.PersonSequence6);
			participantDay[7] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == jointTour.PersonSequence7);
			participantDay[8] = householdDay.PersonDays.FirstOrDefault(pDay => pDay.Person.Sequence == jointTour.PersonSequence8);
			//get jointTour.PersonSequence1 thru 8 personDays into particpantDay array
			tour[1] = participantDay[1] == null ? null : participantDay[1].Tours.FirstOrDefault(t => t.Sequence == jointTour.TourSequence1);
			tour[2] = participantDay[2] == null ? null : participantDay[2].Tours.FirstOrDefault(t => t.Sequence == jointTour.TourSequence2);
			tour[3] = participantDay[3] == null ? null : participantDay[3].Tours.FirstOrDefault(t => t.Sequence == jointTour.TourSequence3);
			tour[4] = participantDay[4] == null ? null : participantDay[4].Tours.FirstOrDefault(t => t.Sequence == jointTour.TourSequence4);
			tour[5] = participantDay[5] == null ? null : participantDay[5].Tours.FirstOrDefault(t => t.Sequence == jointTour.TourSequence5);
			tour[6] = participantDay[6] == null ? null : participantDay[6].Tours.FirstOrDefault(t => t.Sequence == jointTour.TourSequence6);
			tour[7] = participantDay[7] == null ? null : participantDay[7].Tours.FirstOrDefault(t => t.Sequence == jointTour.TourSequence7);
			tour[8] = participantDay[8] == null ? null : participantDay[8].Tours.FirstOrDefault(t => t.Sequence == jointTour.TourSequence8);

			// set jointTour time window
			TimeWindow jointTourTimeWindow = new TimeWindow();
			for (var i = 1; i <= 8; i++) {
				if (!(tour[i] == null)) {
					jointTourTimeWindow.IncorporateAnotherTimeWindow(participantDay[i].TimeWindow);
				}
			}
			jointTour.TimeWindow = jointTourTimeWindow;

			int iOldest = 0;
			int oldestAge = 0;

			// set tour destination, mode and time for oldest participant
			// determine oldest
			for (var i = 1; i <= 8; i++) {
				if (!(tour[i] == null) && participantDay[i].Person.Age >= oldestAge) {
					oldestAge = participantDay[i].Person.Age;
					iOldest = i;
				}
			}
			SetTourDestination(tour[iOldest], householdDay);

			if (!participantDay[iOldest].IsValid) { return; }

			SetTourModeAndTime(householdDay, tour[iOldest], 0, 0, 0);

			if (!participantDay[iOldest].IsValid) { return; }

			// loop to set tour destination, mode and time for all participants on the tour.
			for (var i = 1; i <= 8; i++) {
				if (tour[i] != null) {
					if (i != iOldest) {
						if (!Global.Configuration.IsInEstimationMode) {
							SetTourDestination(tour[i], householdDay, tour[iOldest].DestinationParcel);
							SetTourModeAndTime(householdDay, tour[i], tour[iOldest].Mode, tour[iOldest].DestinationArrivalTime, tour[iOldest].DestinationDepartureTime);
						}
					}
					tour[i].DestinationModeAndTimeHaveBeenSimulated = true;
					//JLB 20130716 comment out the temporary updating of time window
					//UpdateTimeWindowForTourDestinationTimes(tour[i]);
				}
			}

			// model halfTours for oldest
			ProcessHalfTours(tour[iOldest], participantDay[iOldest], householdDay, Constants.TourDirection.ORIGIN_TO_DESTINATION, Constants.TourDirection.DESTINATION_TO_ORIGIN);

			if (!participantDay[iOldest].IsValid) { return; }

			tour[iOldest].HalfTour1HasBeenSimulated = true;
			tour[iOldest].HalfTour2HasBeenSimulated = true;
			UpdateTimeWindowForTourDestinationTimes(tour[iOldest]);

			if (!Global.Configuration.IsInEstimationMode) {
				tour[iOldest].SetOriginTimes();
			}

			// clone halfTours for other participants
			for (var i = 1; i <= 8; i++) {
				if (i != iOldest && participantDay[i] != null) {
					if (!Global.Configuration.IsInEstimationMode) {
						if (tour[i].Id == 21) {
							bool testbreak = true;
						}
						CloneHalfTours(tour[iOldest], tour[i], Constants.TourDirection.ORIGIN_TO_DESTINATION, Constants.TourDirection.DESTINATION_TO_ORIGIN);
						if (tour[i].PersonDay.IsValid) {
							tour[i].SetOriginTimes();
							tour[i].HalfTour1HasBeenSimulated = true;
							tour[i].HalfTour2HasBeenSimulated = true;
							UpdateTimeWindowForTourDestinationTimes(tour[i]);
						}
					}
				}
			}
		}

		private static void RunNonMandatoryTourModelSuite(TourWrapper tour, PersonDayWrapper personDay, HouseholdDayWrapper householdDay) {
#if RELEASE
				try {
#endif
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > RunNonMandatoryTourModelSuite for Household {0} Person {1} Tour {2}", householdDay.Household.Id, personDay.Person.Sequence, tour.Sequence);
			tour.SetHomeBasedIsSimulated();

			ICondensedParcel destinationParcel = null;
			int mode = 0;
			int destinationArrivalTime = 0;
			int destinationDepartureTime = 0;

			if (tour.FullHalfTour1Sequence > 0 || tour.FullHalfTour2Sequence > 0 || tour.JointTourSequence > 0 || tour.PartialHalfTour1Sequence > 0 || tour.PartialHalfTour2Sequence > 0) {
				destinationParcel = tour.DestinationParcel;
				mode = tour.Mode;
				destinationArrivalTime = tour.DestinationArrivalTime;
				destinationDepartureTime = tour.DestinationDepartureTime;
			}

			SetTourDestination(tour, householdDay, destinationParcel);

			if (!tour.PersonDay.IsValid) {
				return;
			}

			SetTourModeAndTime(householdDay, tour, mode, destinationArrivalTime, destinationDepartureTime);

			if (!tour.PersonDay.IsValid) {
				return;
			}

			//JLB 20130716 comment out the temporary updating of time window
			//UpdateTimeWindowForTourDestinationTimes(tour);

			int firstDirection = Constants.TourDirection.ORIGIN_TO_DESTINATION;
			int lastDirection = Constants.TourDirection.DESTINATION_TO_ORIGIN;

			//return if both half tours have already been simulated;
			if (tour.JointTourSequence > 0 || (tour.DestinationPurpose == Constants.Purpose.ESCORT && tour.FullHalfTour1Sequence > 0 && tour.FullHalfTour2Sequence > 0)) {
				return;
			}
			// only simulate escort half tours that have not already been simulated as part of fully joint half tours
			if (tour.DestinationPurpose == Constants.Purpose.ESCORT && tour.FullHalfTour1Sequence > 0) {
				firstDirection = Constants.TourDirection.DESTINATION_TO_ORIGIN;
			}
			else if (tour.DestinationPurpose == Constants.Purpose.ESCORT && tour.FullHalfTour2Sequence > 0) {
				lastDirection = Constants.TourDirection.ORIGIN_TO_DESTINATION;
			}
			lock (_lock) {
				ChoiceModelFactory.TotalTimesTourTripModelsRun++;
			}
			RunTourTripModels(tour, personDay, householdDay, firstDirection, lastDirection);
			if (!personDay.IsValid) {
				return;
			}
			UpdateTimeWindowForTourDestinationTimes(tour);

#if RELEASE
				}
				catch (Exception e) {
					throw new TourModelException(string.Format("Error running tour models for {0}.", tour), e);
				}
#endif


		}

		private static void UpdateTimeWindowForTourDestinationTimes(ITourWrapper tour) {

			//if (Global.Configuration.IsInEstimationMode) {
			//	return;
			//}

			if (!Global.Configuration.IsInEstimationMode && tour.DestinationArrivalTime > tour.DestinationDepartureTime) {
				Global.PrintFile.WriteArrivalTimeGreaterThanDepartureTimeWarning("HouseholdChoiceModelRunner", "UpdateTimeWindowForTourDestinationTimes", tour.PersonDay.Id, tour.DestinationArrivalTime, tour.DestinationDepartureTime);
				tour.PersonDay.IsValid = false;

				bool testbreak = true;

				return;
			}

			// # = busy :(
			// - = available :)

			// carves out a person's availability for the day in relation to the tour
			// person day availabilty [----###########----]
			tour.PersonDay.TimeWindow.SetBusyMinutes(tour.DestinationArrivalTime, tour.DestinationDepartureTime + 1);

			if (tour.Subtours.Count == 0) {
				return;
			}

			// sets the availabilty for a tour's subtours 
			// tour availabilty [####-----------####]
			tour.TimeWindow.SetBusyMinutes(1, tour.DestinationArrivalTime + 1);
			tour.TimeWindow.SetBusyMinutes(tour.DestinationDepartureTime, Constants.Time.MINUTES_IN_A_DAY + 1);
		}

		private static void SetTourDestination(ITourWrapper tour, IHouseholdDayWrapper householdDay, ICondensedParcel constrainedParcel = null) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > SetTourDestination for Tour {0}", tour.Sequence);

			//enhanced TourDestnationModel to handle constrainedParcel cases so that it can check for invalid data in estimation mode
			//if (constrainedParcel != null) {
			//	tour.DestinationParcel = constrainedParcel;
			//	tour.DestinationParcelId = constrainedParcel.Id;
			//	tour.DestinationZoneKey = constrainedParcel.ZoneId;
			//	return;
			//}

			switch (tour.DestinationPurpose) {
				case Constants.Purpose.WORK:
					if (Global.Configuration.ShouldRunWorkTourDestinationModel) {
						// sets the destination for the work tour
						// the usual work location or some another work location
						lock (_lock) {
							ChoiceModelFactory.TotalTimesWorkTourDestinationModelRun++;
						}
						HTourDestinationModel.Run(tour, householdDay, Global.Configuration.OtherTourDestinationModelSampleSize, constrainedParcel);
					}

					return;
				//JLB 20130706 embedded school result in HTourDestinationModel so it would be checked in estimation mode for bad survey data
				//case Constants.Purpose.SCHOOL:
				//	if (!Global.Configuration.IsInEstimationMode) {
				//		// sets the destination for the school tour
				//		tour.DestinationParcelId = tour.Person.UsualSchoolParcelId;
				//		tour.DestinationParcel = tour.Person.UsualSchoolParcel;
				//		tour.DestinationZoneKey = tour.Person.UsualSchoolZoneKey;
				//		tour.DestinationAddressType = Constants.AddressType.USUAL_SCHOOL;
				//	}

				//	return;
				default:
					if (Global.Configuration.ShouldRunOtherTourDestinationModel) {
						// sets the destination for the work tour
						// the usual work location or some another work location
						lock (_lock) {
							ChoiceModelFactory.TotalTimesOtherTourDestinationModelRun++;
						}
						HTourDestinationModel.Run(tour, householdDay, Global.Configuration.OtherTourDestinationModelSampleSize, constrainedParcel);
					}

					return;
			}
		}

		private static void GenerateSubtours(TourWrapper tour, HouseholdDayWrapper householdDay) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > GenerateSubtours for Tour {0}", tour.Sequence);

			// when the tour is to the usual work location then subtours for a work-based tour are created
			if (tour.Person.UsualWorkParcel == null || tour.DestinationParcel == null
				  || tour.DestinationParcel != tour.Person.UsualWorkParcel || !Global.Configuration.ShouldRunWorkBasedSubtourGenerationModel) {
				return;
			}

			if (Global.Configuration.IsInEstimationMode) {
				var nCallsForTour = 0;
				foreach (var subtour in tour.Subtours) {
					// -- in estimation mode --
					// sets the destination purpose of the subtour when in application mode
					lock (_lock) {
						ChoiceModelFactory.TotalTimesWorkBasedSubtourGenerationModelRun++;
					}
					nCallsForTour++;
					HWorkBasedSubtourGenerationModel.Run(tour, nCallsForTour, householdDay, subtour.DestinationPurpose);
				}
				nCallsForTour++;
				lock (_lock) {
					ChoiceModelFactory.TotalTimesWorkBasedSubtourGenerationModelRun++;
				}
				HWorkBasedSubtourGenerationModel.Run(tour, nCallsForTour, householdDay);
			}
			else {
				// creates the subtours for work tour 
				var nCallsForTour = 0;
				while (tour.Subtours.Count < 4 && tour.PersonDay.TotalCreatedTours < 10) {
					// -- in application mode --
					// sets the destination purpose of the subtour
					lock (_lock) {
						ChoiceModelFactory.TotalTimesWorkBasedSubtourGenerationModelRun++;
					}
					nCallsForTour++;
					var destinationPurposeForSubtour = HWorkBasedSubtourGenerationModel.Run(tour, nCallsForTour, householdDay);

					if (destinationPurposeForSubtour == Constants.Purpose.NONE_OR_HOME) {
						break;
					}

					// the subtour is added to the tour's Subtours collection when the subtour's purpose is not NONE_OR_HOME
					tour.Subtours.Add(tour.CreateSubtour(tour.DestinationAddressType, tour.DestinationParcelId, tour.DestinationZoneKey, destinationPurposeForSubtour));
					tour.PersonDay.CreatedWorkBasedTours++;
				}

				tour.PersonDay.WorkBasedTours += tour.Subtours.Count;
			}
		}


		private static void SetTourModeAndTime(IHouseholdDayWrapper householdDay, ITourWrapper tour,
			  int constrainedMode, int constrainedArrivalTime, int constrainedDepartureTime) {

			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > SetTourModeAndTime for Tour {0}", tour.Sequence);

			if (Global.Configuration.ShouldRunTourModeTimeModel) {
				if (tour.JointTourSequence > 0) {
					bool testbreak = true;
				}

				lock (_lock) {
					ChoiceModelFactory.TotalTimesTourModeTimeModelRun++;
				}
				HTourModeTimeModel.Run(householdDay, tour, constrainedMode, constrainedArrivalTime, constrainedDepartureTime);

				if (!tour.PersonDay.IsValid) {
					return;
				}

					//set additional tour variables based on HTourModeTime object for chosen alternative
				var choice = new HTourModeTime(tour.Mode, tour.DestinationArrivalTime, tour.DestinationDepartureTime);
				var timeWindow = (constrainedArrivalTime <= 0 && constrainedDepartureTime <= 0 && constrainedMode <= 0) ?
					tour.GetRelevantTimeWindow(householdDay) : new TimeWindow();
				HTourModeTime.SetImpedanceAndWindow(timeWindow, tour, choice);

				if (choice != null && choice.LongestFeasibleWindow != null) {
					tour.DestinationArrivalBigPeriod = choice.ArrivalPeriod;
					tour.DestinationDepartureBigPeriod = choice.DeparturePeriod;
					tour.EarliestOrignDepartureTime = choice.LongestFeasibleWindow.Start;
					tour.LatestOrignArrivalTime = choice.LongestFeasibleWindow.End;
					tour.IndicatedTravelTimeToDestination = choice.TravelTimeToDestination;
					tour.IndicatedTravelTimeFromDestination = choice.TravelTimeFromDestination;
				}
				else if (!Global.Configuration.IsInEstimationMode) {
					tour.PersonDay.IsValid = false;
					householdDay.IsValid = false;
					bool testbreak = true;
				}
				//mbtrace
				if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > Predicted Mode {0} ArrivalTime {1} DepartureTime {2} Valid {3}",
							  tour.Mode, tour.DestinationArrivalTime, tour.DestinationDepartureTime, tour.PersonDay.IsValid);
			}
		}

		private static void RunSubtourModelSuite(ITourWrapper subtour, IHouseholdDayWrapper householdDay) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > RunSubtourModelSuite for Subtour {0}", subtour.Sequence);

			subtour.SetWorkBasedIsSimulated();

			SetSubtourDestination(subtour, householdDay);

			if (!subtour.PersonDay.IsValid) {
				return;
			}

			SetSubtourModeAndTime(householdDay, subtour);

			if (!subtour.PersonDay.IsValid) {
				return;
			}

			if (!Global.Configuration.IsInEstimationMode) {
				if (subtour.DestinationArrivalTime > subtour.DestinationDepartureTime) {
					Global.PrintFile.WriteArrivalTimeGreaterThanDepartureTimeWarning("ChoiceModelRunner", "RunTourModels", subtour.PersonDay.Id, subtour.DestinationArrivalTime, subtour.DestinationDepartureTime);
					subtour.PersonDay.IsValid = false;

					return;
				}

				if (subtour.DestinationArrivalTime < subtour.ParentTour.DestinationArrivalTime || subtour.DestinationDepartureTime > subtour.ParentTour.DestinationDepartureTime) {
					Global.PrintFile.WriteSubtourArrivalAndDepartureTimesOutOfRangeWarning("ChoiceModelRunner", "RunTourModels", subtour.PersonDay.Id, subtour.DestinationArrivalTime, subtour.DestinationDepartureTime, subtour.ParentTour.DestinationArrivalTime, subtour.ParentTour.DestinationDepartureTime);
					subtour.PersonDay.IsValid = false;

					return;
				}
			}

			// # = busy :(
			// - = available :)

			// updates the parent tour's availabilty [----###########----]
			subtour.ParentTour.TimeWindow.SetBusyMinutes(subtour.DestinationArrivalTime, subtour.DestinationDepartureTime + 1);
		}

		private static void SetSubtourDestination(ITourWrapper subtour, IHouseholdDayWrapper householdDay) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > SetSubtourGeneration for Subtour {0}", subtour.Sequence);

			switch (subtour.DestinationPurpose) {
				case Constants.Purpose.WORK:
					if (Global.Configuration.ShouldRunWorkTourDestinationModel) {
						// sets the destination for the work tour
						// the usual work location or some another work location
						lock (_lock) {
							ChoiceModelFactory.TotalTimesWorkSubtourDestinationModelRun++;
						}
						HTourDestinationModel.Run(subtour, householdDay, Global.Configuration.OtherTourDestinationModelSampleSize);
					}

					return;
				case Constants.Purpose.SCHOOL:
					if (!Global.Configuration.IsInEstimationMode) {
						// sets the destination for the school subtour
						subtour.DestinationParcelId = subtour.Person.UsualSchoolParcelId;
						subtour.DestinationParcel = subtour.Person.UsualSchoolParcel;
						subtour.DestinationZoneKey = subtour.Person.UsualSchoolZoneKey;
						subtour.DestinationAddressType = Constants.AddressType.USUAL_SCHOOL;
					}

					return;
				default:
					if (Global.Configuration.ShouldRunOtherTourDestinationModel) {
						// sets the destination for the subtour
						lock (_lock) {
							ChoiceModelFactory.TotalTimesOtherSubtourDestinationModelRun++;
						}
						HTourDestinationModel.Run(subtour, householdDay, Global.Configuration.OtherTourDestinationModelSampleSize);
					}

					return;
			}
		}

		private static void SetSubtourModeAndTime(IHouseholdDayWrapper householdDay, ITourWrapper subtour) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > SetSubtourModeAndTime for Subtour {0}", subtour.Sequence);

			if (householdDay.Household.Id == 147768 && subtour.PersonDay.Person.Sequence == 1) {
				bool testbreak = true;
			}

			if (Global.Configuration.ShouldRunTourModeTimeModel) {
				TimeWindow timeWindow = new TimeWindow();
				if (subtour.JointTourSequence > 0) {
					foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
						var tInJoint = pDay.Tours.Find(t => t.JointTourSequence == subtour.JointTourSequence);
						if (!(tInJoint == null)) {
							// set jointTour time window
							timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						}
					}
				}
				else if (subtour.FullHalfTour1Sequence > 0) {
					foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
						var tInJoint = pDay.Tours.Find(t => t.FullHalfTour1Sequence == subtour.FullHalfTour1Sequence);
						if (!(tInJoint == null)) {
							// set jointTour time window
							timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						}
					}
				}
				else if (subtour.FullHalfTour2Sequence > 0) {
					foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
						var tInJoint = pDay.Tours.Find(t => t.FullHalfTour2Sequence == subtour.FullHalfTour2Sequence);
						if (!(tInJoint == null)) {
							// set jointTour time window
							timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						}
					}
				}
				else if (subtour.ParentTour == null) {
					timeWindow.IncorporateAnotherTimeWindow(subtour.PersonDay.TimeWindow);
				}
				else {
					timeWindow.IncorporateAnotherTimeWindow(subtour.ParentTour.TimeWindow);
				}

				// timeWindow.SetBusyMinutes(Constants.Time.END_OF_RELEVANT_WINDOW, Constants.Time.MINUTES_IN_A_DAY + 1);
				//JLB 20130811  commented out the above line to avoid cases where the only available time is after END_OF_RELEVANT_WINDOW
				//              causing zero available minutes.  Also added following test for no available time

				if (timeWindow.TotalAvailableMinutes(1,Constants.Time.MINUTES_IN_A_DAY) <= 0) {
					subtour.PersonDay.IsValid = false;
					return;
				}

				HTourModeTime.InitializeTourModeTimes();

				lock (_lock) {
					ChoiceModelFactory.TotalTimesTourModeTimeModelRun++;
				}
				HTourModeTimeModel.Run(householdDay, subtour, 0, 0, 0);
				if (subtour.PersonDay.IsValid == false){
					return;
				}

				//set additional tour variables based on HTourModeTime object for chosen alternative
				var choice = new HTourModeTime(subtour.Mode, subtour.DestinationArrivalTime, subtour.DestinationDepartureTime);
				if (choice != null && subtour.DestinationParcel != null && subtour.OriginParcel != null) {
					HTourModeTime.SetImpedanceAndWindow(timeWindow, subtour, choice);
				}
				if (choice != null && choice.LongestFeasibleWindow != null) {
					subtour.DestinationArrivalBigPeriod = choice.ArrivalPeriod;
					subtour.DestinationDepartureBigPeriod = choice.DeparturePeriod;
					subtour.EarliestOrignDepartureTime = choice.LongestFeasibleWindow.Start;
					subtour.LatestOrignArrivalTime = choice.LongestFeasibleWindow.End;
					subtour.IndicatedTravelTimeToDestination = choice.TravelTimeToDestination;
					subtour.IndicatedTravelTimeFromDestination = choice.TravelTimeFromDestination;
				}
				else {
					subtour.EarliestOrignDepartureTime = Constants.DEFAULT_VALUE;
					subtour.LatestOrignArrivalTime = Constants.DEFAULT_VALUE;
					subtour.IndicatedTravelTimeFromDestination = Constants.DEFAULT_VALUE;
					subtour.IndicatedTravelTimeToDestination = Constants.DEFAULT_VALUE;
				}

				//mbtrace
				if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > Predicted Mode {0} ArrivalTime {1} DepartureTime {2} Valid {3}",
							  subtour.Mode, subtour.DestinationArrivalTime, subtour.DestinationDepartureTime, subtour.PersonDay.IsValid);
			}
		}

		private static void RunPartialHalfTourTripModelSuite(ITourWrapper tour, IHalfTour halfTour, ITripWrapper trip, int stopPurpose, ICondensedParcel stopLocation, int mode, int departureTime) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > RunPartialHalfTourTripModelSuite for HalfTour {0} Direction {1} Trip {2}", tour.Sequence, trip.Direction, trip.Sequence);

			var nextTrip = GeneratePartialHalfTourIntermediateStop(halfTour, trip, stopPurpose, stopLocation, mode, departureTime);

			SetPartialHalfTourIntermediateStopDestination(trip, nextTrip, stopPurpose, stopLocation, mode, departureTime);
			SetPartialHalfTourTripModeAndTime(tour, trip, stopPurpose, stopLocation, mode, departureTime);

			if (!trip.PersonDay.IsValid) {
				return;
			}

			//JB 20130616 commented out the following code that updates time window 
			//           because window is updated above in SetPartiaHalfTourTripModeAndTime()
			// retrieves window based on whether or not the trip's tour is home-based or work-based
			//			var timeWindow = tour.IsHomeBasedTour ? tour.PersonDay.TimeWindow : tour.ParentTour.TimeWindow;

			//			if (trip.IsHalfTourFromOrigin) {
			//				if (trip.Sequence == 1) {
			//					// occupies minutes in window between destination and stop
			//					timeWindow.SetBusyMinutes(trip.ArrivalTime, tour.DestinationArrivalTime + 1);
			//				}
			//				else {
			//					// occupies minutes in window from previous stop to stop
			//					timeWindow.SetBusyMinutes(trip.ArrivalTime, trip.PreviousTrip.DepartureTime + 1);
			//				}
			//			}
			//			else {
			//				if (trip.Sequence == 1) {
			//					// occupies minutes in window between destination and stop
			//					timeWindow.SetBusyMinutes(tour.DestinationDepartureTime, trip.ArrivalTime + 1);
			//				}
			//				else {
			//					// occupies minutes in window from previous stop to stop
			//					timeWindow.SetBusyMinutes(trip.PreviousTrip.DepartureTime, trip.ArrivalTime + 1);
			//				}
			//			}
		}

		private static ITripWrapper GeneratePartialHalfTourIntermediateStop(IHalfTour halfTour, ITripWrapper trip, int stopPurpose, ICondensedParcel stopLocation, int mode, int departureTime) {
			if (!Global.Configuration.ShouldRunIntermediateStopGenerationModel) {
				return null;
			}
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > GeneratePartialHalfTourTripIntermediateStop Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			ITripWrapper nextTrip = null;

			if (Global.Configuration.IsInEstimationMode) {
				// -- in estimation mode --
				// sets the trip's destination purpose, determines whether or not a stop is generated in application mode
				// uses trip instead of nextTrip, deals with subtours with tour origin at work
				// need to set trip.IsToTourOrigin first
				//trip.IsToTourOrigin = trip.Sequence == trip.HalfTour.Trips.Count(); // last trip in half tour 
				//var intermediateStopPurpose = trip.IsToTourOrigin ? Constants.Purpose.NONE_OR_HOME : trip.DestinationPurpose;
				//nextTrip = trip.NextTrip;

				//if (intermediateStopPurpose != Constants.Purpose.NONE_OR_HOME) {
				//	ChoiceModelFactory.TotalTimesIntermediateStopGenerated++;
				//}
				// don't run generation model for partially joint half tour stops
				//if (trip.PersonDay.TotalStops > 0) {
				//	ChoiceModelFactory.TotalTimesIntermediateStopGenerationModelRun++;
				//	IntermediateStopGenerationModel.Run(trip, intermediateStopPurpose);
				//}
			}
			else {
				// -- in application mode --
				// sets the trip's destination purpose, determines whether or not a stop is generated

				// first, if it is the first trip on a park and ride half tour, then make it a change mode stop
				// TODO: this doesn't allow stops between the destination and the transit stop - can improve later
				//int intermediateStopPurpose = Constants.Purpose.NONE_OR_HOME;
				//don't need to deal with park and ride for partially joint half tours
				//if (trip.Sequence == 1 && trip.Tour.Mode == Constants.Mode.PARK_AND_RIDE) {
				//	intermediateStopPurpose = Constants.Purpose.CHANGE_MODE;
				//	ChoiceModelFactory.TotalTimesChangeModeStopGenerated++;
				//}
				//else 
				//if (trip.PersonDay.TotalStops == 0) {
				//	intermediateStopPurpose = Constants.Purpose.NONE_OR_HOME;
				//}
				//else {
				//	ChoiceModelFactory.TotalTimesIntermediateStopGenerationModelRun++;
				//	intermediateStopPurpose = IntermediateStopGenerationModel.Run(trip);
				//}

				if (stopPurpose != Constants.Purpose.NONE_OR_HOME) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesIntermediateStopGenerated++;
					}
					var destinationPurposeForNextTrip = trip.DestinationPurpose;

					// creates the next trip in the half-tour 
					// the next trip's destination is set to the current trip's destination
					nextTrip = halfTour.CreateNextTrip(trip, stopPurpose, destinationPurposeForNextTrip);

					halfTour.Trips.Add(nextTrip);

					trip.DestinationAddressType = Constants.AddressType.NONE;
					trip.DestinationPurpose = stopPurpose;
					trip.IsToTourOrigin = false;
				}
				else {
					trip.IsToTourOrigin = true;
				}
			}

			return nextTrip;
		}

		private static void SetPartialHalfTourIntermediateStopDestination(ITripWrapper trip, ITripWrapper nextTrip, int stopPurpose, ICondensedParcel stopLocation, int mode, int departureTime) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > SetPartialHalfTourTripIntermediateStopDestination Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			if (nextTrip == null || trip.IsToTourOrigin || !Global.Configuration.ShouldRunIntermediateStopLocationModel) {
				if (trip.IsToTourOrigin) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesTripIsToTourOrigin++;
					}
				}
				else if (nextTrip == null) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesNextTripIsNull++;
					}
				}

				if (trip.DestinationPurpose == Constants.Purpose.NONE_OR_HOME && Global.Configuration.IsInEstimationMode && Global.Configuration.EstimationModel == "IntermediateStopLocationModel") {
					Global.PrintFile.WriteEstimationRecordExclusionMessage("ChoiceModelRunner", "SetIntermediateStopDestination", trip.Household.Id, trip.Person.Sequence, trip.Day, trip.Tour.Sequence, trip.Direction, trip.Sequence, 1);
				}

				return;
			}

			// sets the new destination for the trip

			//if (trip.DestinationPurpose == Constants.Purpose.CHANGE_MODE) {
			// CHANGE_MODE location is always park and ride node for tour
			//var parkAndRideNode = ChoiceModelFactory.ParkAndRideNodeDao.Get(trip.Tour.ParkAndRideNodeId);

			//if (parkAndRideNode != null) {
			//trip.DestinationParcelId = parkAndRideNode.NearestParcelId;
			//trip.DestinationParcel = ChoiceModelFactory.Parcels[trip.DestinationParcelId];
			//trip.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[trip.DestinationParcel.ZoneId];
			//trip.DestinationAddressType = Constants.AddressType.OTHER;

			//ChoiceModelFactory.TotalTimesChangeModeLocationSet++;
			//}
			//}
			//else {
			//	ChoiceModelFactory.TotalTimesIntermediateStopLocationModelRun++;
			//	IntermediateStopLocationModel.Run(trip, Global.Configuration.IntermediateStopLocationModelSampleSize);
			//}
			if (Global.Configuration.IsInEstimationMode) {
				return;
			}

			var choice = stopLocation;

			trip.DestinationParcelId = choice.Id;
			trip.DestinationParcel = choice;
			trip.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[choice.ZoneId];
			trip.DestinationAddressType =
				 choice.Id == trip.Person.UsualWorkParcelId
					  ? Constants.AddressType.USUAL_WORKPLACE
					  : choice.Id == trip.Person.UsualSchoolParcelId ? Constants.AddressType.USUAL_SCHOOL : Constants.AddressType.OTHER;

			nextTrip.OriginParcelId = trip.DestinationParcelId;
			nextTrip.OriginParcel = trip.DestinationParcel;
			nextTrip.OriginZoneKey = trip.DestinationZoneKey;
			nextTrip.SetOriginAddressType(trip.DestinationAddressType);
		}

		private static void SetPartialHalfTourTripModeAndTime(ITourWrapper tour, ITripWrapper trip, int stopPurpose, ICondensedParcel stopLocation, int mode, int departureTime) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > SetPartialHalfTourTripModeAndTime Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			if (Global.Configuration.ShouldRunTripModeModel) {
				// sets the trip's mode of travel to the destination
				//if (trip.DestinationPurpose == Constants.Purpose.CHANGE_MODE) {
				// trips to change mode destination are always by transit
				//ChoiceModelFactory.TotalTimesChangeModeTransitModeSet++;
				//trip.Mode = Constants.Mode.TRANSIT;
				//}
				//else {
				//ChoiceModelFactory.TotalTimesTripModeModelRun++;
				//TripModeModel.Run(trip);
				if (!Global.Configuration.IsInEstimationMode) {
					trip.Mode = mode;
					trip.PathType = 0;
					//}
					//if (!trip.PersonDay.IsValid) {
					//	return;
				}
			}

			// sets the trip's destination arrival and departure times
			//if (trip.Sequence == 1) {
			//if (!Global.Configuration.IsInEstimationMode) {
			//trip.DepartureTime = trip.IsHalfTourFromOrigin ? tour.DestinationArrivalTime : tour.DestinationDepartureTime;
			//trip.UpdateTripValues();
			//}
			//}
			//else if (trip.OriginPurpose == Constants.Purpose.CHANGE_MODE) {
			//stay at park and ride lot assumed to be 3 minutes
			//if (!Global.Configuration.IsInEstimationMode) {
			//int endpoint;

			//if (trip.IsHalfTourFromOrigin) {
			//trip.DepartureTime = trip.PreviousTrip.ArrivalTime - 3;
			//endpoint = trip.DepartureTime + 1;
			//}
			//else {
			//trip.DepartureTime = trip.PreviousTrip.ArrivalTime + 3;
			//endpoint = trip.DepartureTime - 1;
			//}
			//if (trip.DepartureTime >= 1 && trip.DepartureTime <= Constants.Time.MINUTES_IN_A_DAY && trip.PersonDay.TimeWindow.EntireSpanIsAvailable(endpoint, trip.DepartureTime)) {
			//trip.UpdateTripValues();
			//}
			//else {
			//trip.PersonDay.IsValid = false;
			//}
			//}
			//}
			//else 
			if (!Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.ShouldRunTripTimeModel) {
					//ChoiceModelFactory.TotalTimesTripTimeModelRun++;
					//TripTimeModel.Run(trip);
					bool testbreak = true;
					trip.DepartureTime = departureTime;
					//the next 3 are set just to make HUpdateTripValues work ok for now - may need to change later.
					trip.ArrivalTimeLimit = trip.Direction == 1 ? 1 : Constants.Time.MINUTES_IN_A_DAY;
					trip.EarliestDepartureTime = 1;
					trip.LatestDepartureTime = Constants.Time.MINUTES_IN_A_DAY;

					if (departureTime >= 1 && departureTime <= Constants.Time.MINUTES_IN_A_DAY) {
						trip.HUpdateTripValues();
					}
					else {
						trip.PersonDay.IsValid = false;
						bool testbreak2 = true;
					}

				}
			}
		}



		private static void ProcessHalfTours(ITourWrapper tour, IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, int firstDirection, int lastDirection) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > ProcessHalfTours Person {0} Tour {1} Directions {2} to {3}",
					 personDay.Person.Sequence, tour.Sequence, firstDirection, lastDirection);

			// goes in two directions, from origin to destination and destination to origin
			for (var direction = firstDirection; direction <= lastDirection; direction++) {
				// creates origin and destination half-tours
				// creates or adds a trip to a tour based on application or estimation mode
				tour.SetHalfTours(direction);

				// the half-tour from the origin to destination or the half-tour from the destination to origin
				// depending on the direction
				var halfTour = tour.GetHalfTour(direction);

				// halfTour.Trips will dynamically grow, so keep this in a for loop
				for (var i = 0; i < halfTour.Trips.Count; i++) {
					var trip = halfTour.Trips[i];

#if RELEASE
					try {
#endif
					halfTour.SimulatedTrips++;

					if (trip.IsHalfTourFromOrigin) {
						tour.HalfTour1Trips++;
					}
					else {
						tour.HalfTour2Trips++;
					}

					lock (_lock) {
						ChoiceModelFactory.TotalTimesTripModelSuiteRun++;
					}
					RunTripModelSuite(householdDay, personDay, tour, halfTour, trip);

					if (!trip.PersonDay.IsValid) {
						return;
					}
#if RELEASE
					}
					catch (Exception e) {
						throw new TripModelException(string.Format("Error running trip models for {0}.", trip), e);
					}
#endif
				}
			}
		}

		private static void RunTripModelSuite(IHouseholdDayWrapper householdDay, IPersonDayWrapper personDay, ITourWrapper tour, IHalfTour halfTour, ITripWrapper trip) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > RunTripModelSuite Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);


			var nextTrip = GenerateIntermediateStop(halfTour, trip, householdDay);

			SetIntermediateStopDestination(trip, nextTrip, householdDay);
			if (!trip.PersonDay.IsValid) {
				return;
			}
			SetTripModeAndTime(householdDay, tour, trip);

			if (!trip.PersonDay.IsValid) {
				return;
			}

			// retrieves window based on whether or not the trip's tour is home-based or work-based
			var timeWindow = tour.IsHomeBasedTour ? tour.PersonDay.TimeWindow : tour.ParentTour.TimeWindow;
			int firstTime = 0;

			if (trip.IsHalfTourFromOrigin && trip.Sequence == 1) {
				// occupies minutes in window between destination and stop
				firstTime = tour.DestinationArrivalTime;
			}
			else if (!trip.IsHalfTourFromOrigin && trip.Sequence == 1) {
				// occupies minutes in window between destination and stop
				firstTime = tour.DestinationDepartureTime;
			}
			else {
				// occupies minutes in window from previous stop to stop
				firstTime = trip.PreviousTrip.DepartureTime;
			}
			if (trip.IsHalfTourFromOrigin) {
				timeWindow.SetBusyMinutes(trip.ArrivalTime, firstTime + 1);
				if (tour.JointTourSequence > 0) {
					// updates joint tour time window
					householdDay.JointToursList.FirstOrDefault(jTour => jTour.Sequence == tour.JointTourSequence).TimeWindow.SetBusyMinutes(trip.ArrivalTime, firstTime + 1);
				}
			}
			else {
				timeWindow.SetBusyMinutes(firstTime, trip.ArrivalTime + 1);
				if (tour.JointTourSequence > 0) {
					// updates joint tour time window
					householdDay.JointToursList.FirstOrDefault(jTour => jTour.Sequence == tour.JointTourSequence).TimeWindow.SetBusyMinutes(firstTime, trip.ArrivalTime + 1);
				}
			}
		}

		private static ITripWrapper GenerateIntermediateStop(IHalfTour halfTour, ITripWrapper trip, IHouseholdDayWrapper householdDay) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > GenerateIntermediateStops Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			if (!Global.Configuration.ShouldRunIntermediateStopGenerationModel) {
				return null;
			}

			ITripWrapper nextTrip = null;

			if (Global.Configuration.IsInEstimationMode) {
				// -- in estimation mode --
				// sets the trip's destination purpose, determines whether or not a stop is generated in application mode
				// uses trip instead of nextTrip, deals with subtours with tour origin at work
				// need to set trip.IsToTourOrigin first
				trip.IsToTourOrigin = trip.Sequence == trip.HalfTour.Trips.Count(); // last trip in half tour 
				var intermediateStopPurpose = trip.IsToTourOrigin ? Constants.Purpose.NONE_OR_HOME : trip.DestinationPurpose;
				nextTrip = trip.NextTrip;

				if (intermediateStopPurpose != Constants.Purpose.NONE_OR_HOME) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesIntermediateStopGenerated++;
					}
					trip.PersonDay.IncrementSimulatedStops(intermediateStopPurpose);
				}
				if (trip.PersonDay.TotalStops > 0) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesIntermediateStopGenerationModelRun++;
					}
					HIntermediateStopGenerationModel.Run(trip, householdDay, intermediateStopPurpose);
				}
			}
			else {
				// -- in application mode --
				// sets the trip's destination purpose, determines whether or not a stop is generated

				// first, if it is the first trip on a park and ride half tour, then make it a change mode stop
				// TODO: this doesn't allow stops between the destination and the transit stop - can improve later
				int intermediateStopPurpose;
				if (trip.Sequence == 1 && trip.Tour.Mode == Constants.Mode.PARK_AND_RIDE) {
					intermediateStopPurpose = Constants.Purpose.CHANGE_MODE;
					lock (_lock) {
						ChoiceModelFactory.TotalTimesChangeModeStopGenerated++;
					}
				}
				else if (trip.PersonDay.TotalStops == 0) {
					intermediateStopPurpose = Constants.Purpose.NONE_OR_HOME;
				}
				else {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesIntermediateStopGenerationModelRun++;
					}
					intermediateStopPurpose = HIntermediateStopGenerationModel.Run(trip, householdDay);
				}

				if (intermediateStopPurpose != Constants.Purpose.NONE_OR_HOME) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesIntermediateStopGenerated++;
					}
					var destinationPurposeForNextTrip = trip.DestinationPurpose;

					// creates the next trip in the half-tour 
					// the next trip's destination is set to the current trip's destination
					nextTrip = halfTour.CreateNextTrip(trip, intermediateStopPurpose, destinationPurposeForNextTrip);

					halfTour.Trips.Add(nextTrip);

					trip.DestinationAddressType = Constants.AddressType.NONE;
					trip.DestinationPurpose = intermediateStopPurpose;
					trip.IsToTourOrigin = false;
				}
				else {
					trip.IsToTourOrigin = true;
				}
			}

			return nextTrip;
		}

		private static void SetIntermediateStopDestination(ITripWrapper trip, ITripWrapper nextTrip, IHouseholdDayWrapper householdDay) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > SetIntermediateStopDestination Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			if (nextTrip == null || trip.IsToTourOrigin || !Global.Configuration.ShouldRunIntermediateStopLocationModel) {
				if (trip.IsToTourOrigin) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesTripIsToTourOrigin++;
					}
				}
				else if (nextTrip == null) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesNextTripIsNull++;
					}
				}

				if (trip.DestinationPurpose == Constants.Purpose.NONE_OR_HOME && Global.Configuration.IsInEstimationMode && Global.Configuration.EstimationModel == "IntermediateStopLocationModel") {
					Global.PrintFile.WriteEstimationRecordExclusionMessage("ChoiceModelRunner", "SetIntermediateStopDestination", trip.Household.Id, trip.Person.Sequence, trip.Day, trip.Tour.Sequence, trip.Direction, trip.Sequence, 1);
				}

				return;
			}

			// sets the new destination for the trip

			if (trip.DestinationPurpose == Constants.Purpose.CHANGE_MODE) {
				// CHANGE_MODE location is always park and ride node for tour
				var parkAndRideNode = ChoiceModelFactory.ParkAndRideNodeDao.Get(trip.Tour.ParkAndRideNodeId);

				if (parkAndRideNode != null) {
					trip.DestinationParcelId = parkAndRideNode.NearestParcelId;
					trip.DestinationParcel = ChoiceModelFactory.Parcels[trip.DestinationParcelId];
					//trip.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[trip.DestinationParcel.ZoneId];
					trip.DestinationZoneKey = parkAndRideNode.ZoneId;
					trip.DestinationAddressType = Constants.AddressType.OTHER;

					lock (_lock) {
						ChoiceModelFactory.TotalTimesChangeModeLocationSet++;
					}
				}
			}
			else {
				lock (_lock) {
					ChoiceModelFactory.TotalTimesIntermediateStopLocationModelRun++;
				}
				HIntermediateStopLocationModel.Run(trip, householdDay, Global.Configuration.IntermediateStopLocationModelSampleSize);
			}
			if (Global.Configuration.IsInEstimationMode) {
				return;
			}

			nextTrip.OriginParcelId = trip.DestinationParcelId;
			nextTrip.OriginParcel = trip.DestinationParcel;
			nextTrip.OriginZoneKey = trip.DestinationZoneKey;
			nextTrip.SetOriginAddressType(trip.DestinationAddressType);
		}

		private static void SetTripModeAndTime(IHouseholdDayWrapper householdDay, ITourWrapper tour, ITripWrapper trip, bool forTourTimesOnly = false) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > SetTripModeAndTime Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			if (Global.Configuration.IsInEstimationMode && (tour.DestinationArrivalBigPeriod == null || tour.DestinationDepartureBigPeriod == null)) {
				return;
			}

			//set bounds on departure time window as new trip properties

			//set time needed from intermediate stop to tour origin
			int minimumTimeForIntermediateStop = 0;
			if (!trip.IsToTourOrigin && trip.DestinationParcel != null && tour.OriginParcel != null) {

				var fastestTravelTime = ImpedanceRoster.GetValue("ivtime", Constants.Mode.HOV3, Constants.PathType.FULL_NETWORK, 20.0, 1,
					trip.IsHalfTourFromOrigin ? tour.OriginParcel : trip.DestinationParcel,
					trip.IsHalfTourFromOrigin ? trip.DestinationParcel : tour.OriginParcel,
					Constants.DEFAULT_VALUE).Variable;

				minimumTimeForIntermediateStop = (int) (fastestTravelTime + 0.5) + Constants.Time.MINIMUM_ACTIVITY_DURATION;
			}
			// HalfTour  Sequence   End         Equation
			//  1st         1st     Earliest    MAX(DestinationArrivalBigPeriod.Start , TourEarliestOriginDepartueTime + MinimumTimeForIntermediateStop)
			//  1st         1st     Latest      MIN(DestinationArrivalBigPeriod.End , TourLatestOriginArrivalTime - TourIndicatedTravelTimeFromDestination - MinimumActivityDuration)

			//  1st         Other   Earliest    TourEarliestOriginDepartueTime + MinimumTimeForIntermediateStop
			//  1st         Other   Latest      PreviousTripArrivalTime - MinimumActivityDuration

			//  2nd         1st     Earliest    MAX(DestinationDepartureBigPeriod.Start , DestinationArrivalTime + MinimumActivityDuration
			//  2nd         1st     Latest      MIN(DestinationDepartureBigPeriod.End , TourLatestOriginArrivalTime - MinimumTimeForIntermediateStop)

			//  2nd         Other   Earliest    PreviousTripArrivalTime + MinimumActivityDuration
			//  2nd         Other   Latest      LatestOriginArrivalTime - MinimumTimeForIntermediateStop 

			trip.EarliestDepartureTime = trip.Sequence == 1  // first simulated trip in half tour has to use tour big period
				 ? (trip.IsHalfTourFromOrigin
						  ? Math.Max(tour.DestinationArrivalBigPeriod.Start, tour.EarliestOrignDepartureTime + minimumTimeForIntermediateStop)
						  : Math.Max(tour.DestinationDepartureBigPeriod.Start, tour.DestinationArrivalTime + Constants.Time.MINIMUM_ACTIVITY_DURATION))
				 : (trip.IsHalfTourFromOrigin
						  ? tour.EarliestOrignDepartureTime + minimumTimeForIntermediateStop
						  : trip.PreviousTrip.ArrivalTime + Constants.Time.MINIMUM_ACTIVITY_DURATION);

			trip.LatestDepartureTime = trip.Sequence == 1 // first simulated trip in half tour has to use tour big period
					? (trip.IsHalfTourFromOrigin
							 ? Math.Min(tour.DestinationArrivalBigPeriod.End, tour.LatestOrignArrivalTime - (int) (tour.IndicatedTravelTimeFromDestination + 0.5) - Constants.Time.MINIMUM_ACTIVITY_DURATION)
							 : Math.Min(tour.DestinationDepartureBigPeriod.End, tour.LatestOrignArrivalTime - minimumTimeForIntermediateStop))
					 : (trip.IsHalfTourFromOrigin
							 ? trip.PreviousTrip.ArrivalTime - Constants.Time.MINIMUM_ACTIVITY_DURATION
							 : tour.LatestOrignArrivalTime - minimumTimeForIntermediateStop);

			trip.ArrivalTimeLimit = trip.IsHalfTourFromOrigin
				  ? tour.EarliestOrignDepartureTime + minimumTimeForIntermediateStop
				  : tour.LatestOrignArrivalTime - minimumTimeForIntermediateStop;


			if (Global.Configuration.ShouldRunTripModeModel) {
				// sets the trip's mode of travel to the destination
				if (trip.DestinationPurpose == Constants.Purpose.CHANGE_MODE) {
					// trips to change mode destination are always by transit
					lock (_lock) {
						ChoiceModelFactory.TotalTimesChangeModeTransitModeSet++;
					}
					trip.Mode = Constants.Mode.TRANSIT;
				}
				else {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesTripModeModelRun++;
					}
					HTripModeModel.Run(householdDay, trip);

					//mbtrace
					if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > Predicted Trip Mode {0} Valid {1}",
								  trip.Mode, trip.PersonDay.IsValid);

				}
				if (!trip.PersonDay.IsValid) {
					return;
				}
			}

			// sets the trip's destination arrival and departure times
			if (trip.OriginPurpose == Constants.Purpose.CHANGE_MODE) {
				//stay at park and ride lot assumed to be 3 minutes
				if (!Global.Configuration.IsInEstimationMode) {
					int endpoint;

					if (trip.IsHalfTourFromOrigin) {
						trip.DepartureTime = trip.PreviousTrip.ArrivalTime - Constants.Time.MINIMUM_ACTIVITY_DURATION;
						endpoint = trip.DepartureTime + 1;
					}
					else {
						trip.DepartureTime = trip.PreviousTrip.ArrivalTime + Constants.Time.MINIMUM_ACTIVITY_DURATION;
						endpoint = trip.DepartureTime - 1;
					}
					bool testbreak = true;
					if (trip.DepartureTime >= 1 && trip.DepartureTime <= Constants.Time.MINUTES_IN_A_DAY && trip.PersonDay.TimeWindow.EntireSpanIsAvailable(endpoint, trip.DepartureTime)) {
						if (!forTourTimesOnly) {
							trip.HUpdateTripValues();
						}
					}
					else {
						trip.PersonDay.IsValid = false;
						testbreak = true;
					}
				}
			}
			else {
				if (Global.Configuration.ShouldRunTripTimeModel) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesTripTimeModelRun++;
					}
					HTripTimeModel.Run(householdDay, trip);

					//mbtrace
					if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > Predicted Trip DepartureTime {0} Valid {1}",
								  trip.DepartureTime, trip.PersonDay.IsValid);

					bool testbreak = true;
					if (trip.PersonDay.IsValid) {
						if (!forTourTimesOnly) {
							trip.HUpdateTripValues();
						}
					}
				}
			}
		}


		private static void CloneHalfTours(ITourWrapper sourceTour, ITourWrapper tour, int firstDirection, int lastDirection) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > CloneHalfTours Tour {0} Directions {1} to {2}", tour.Sequence, firstDirection, lastDirection);

			// goes in two directions, from origin to destination and destination to origin
			for (var direction = firstDirection; direction <= lastDirection; direction++) {
				// creates origin and destination half-tours
				// creates or adds a trip to a tour based on application or estimation mode
				tour.SetHalfTours(direction);

				// the half-tour from the origin to destination or the half-tour from the destination to origin
				// depending on the direction
				var halfTour = tour.GetHalfTour(direction);
				var sourceHalfTour = sourceTour.GetHalfTour(direction);

				// halfTour.Trips will dynamically grow, so keep this in a for loop
				for (var i = 0; i < sourceHalfTour.Trips.Count; i++) {
					var trip = halfTour.Trips[i];
					var sourceTrip = sourceHalfTour.Trips[i];

#if RELEASE
					try {
#endif
					halfTour.SimulatedTrips++;

					if (trip.IsHalfTourFromOrigin) {
						tour.HalfTour1Trips++;
					}
					else {
						tour.HalfTour2Trips++;
					}

					lock (_lock) {
						ChoiceModelFactory.TotalTimesTripModelSuiteRun++;
					}

					if (!trip.PersonDay.IsValid) {
						bool testbreak = true;
					}

					RunTripCloneSuite(sourceTour, sourceHalfTour, sourceTrip, tour, halfTour, trip);

					if (!trip.PersonDay.IsValid) {
						return;
					}
#if RELEASE
					}
					catch (Exception e) {
						throw new TripModelException(string.Format("Error running trip models for {0}.", trip), e);
					}
#endif
				}
			}
		}

		private static void RunTripCloneSuite(ITourWrapper sourceTour, IHalfTour sourceHalfTour, ITripWrapper sourceTrip, ITourWrapper tour, IHalfTour halfTour, ITripWrapper trip) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > RunTripCloneSuite Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			var nextTrip = GenerateIntermediateStopClone(sourceHalfTour, sourceTrip, halfTour, trip);

			CloneIntermediateStopDestination(sourceTrip, trip, nextTrip);
			if (!trip.PersonDay.IsValid) {
				bool testbreak = true;
			}
			CloneTripModeAndTime(sourceTour, sourceTrip, tour, trip);
			if (!trip.PersonDay.IsValid) {
				bool testbreak = true;
			}

			if (!trip.PersonDay.IsValid) {
				return;
			}


			//JB 20130616 commented out the update of time window, which already occurred above in CloneTripModeAndTime()

			//			// retrieves window based on whether or not the trip's tour is home-based or work-based
			//			var timeWindow = tour.IsHomeBasedTour ? tour.PersonDay.TimeWindow : tour.ParentTour.TimeWindow;

			//			if (trip.IsHalfTourFromOrigin) {
			//				if (trip.Sequence == 1) {
			//					// occupies minutes in window between destination and stop
			//					timeWindow.SetBusyMinutes(trip.ArrivalTime, tour.DestinationArrivalTime + 1);
			//				}
			//				else {
			//					// occupies minutes in window from previous stop to stop
			//					timeWindow.SetBusyMinutes(trip.ArrivalTime, trip.PreviousTrip.DepartureTime + 1);
			//				}
			//			}
			//			else {
			//				if (trip.Sequence == 1) {
			//					// occupies minutes in window between destination and stop
			//					timeWindow.SetBusyMinutes(tour.DestinationDepartureTime, trip.ArrivalTime + 1);
			//				}
			//				else {
			//					// occupies minutes in window from previous stop to stop
			//					timeWindow.SetBusyMinutes(trip.PreviousTrip.DepartureTime, trip.ArrivalTime + 1);
			//				}
			//			}
		}

		private static ITripWrapper GenerateIntermediateStopClone(IHalfTour sourceHalfTour, ITripWrapper sourceTrip, IHalfTour halfTour, ITripWrapper trip) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > GenerateIntermediateStopClone Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			ITripWrapper nextTrip = null;

			var intermediateStopPurpose = sourceTrip.DestinationPurpose;

			if (intermediateStopPurpose != Constants.Purpose.NONE_OR_HOME) {
				lock (_lock) {
					ChoiceModelFactory.TotalTimesIntermediateStopGenerated++;
				}
				var destinationPurposeForNextTrip = trip.DestinationPurpose;

				// creates the next trip in the half-tour 
				// the next trip's destination is set to the current trip's destination
				nextTrip = halfTour.CreateNextTrip(trip, intermediateStopPurpose, destinationPurposeForNextTrip);

				halfTour.Trips.Add(nextTrip);

				trip.DestinationAddressType = Constants.AddressType.NONE;
				trip.DestinationPurpose = intermediateStopPurpose;
				trip.IsToTourOrigin = false;
			}
			else {
				trip.IsToTourOrigin = true;
			}
			return nextTrip;
		}

		private static void CloneIntermediateStopDestination(ITripWrapper sourceTrip, ITripWrapper trip, ITripWrapper nextTrip) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > CloneIntermediateStopDestination Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			if (nextTrip == null || trip.IsToTourOrigin || !Global.Configuration.ShouldRunIntermediateStopLocationModel) {
				if (trip.IsToTourOrigin) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesTripIsToTourOrigin++;
					}
				}
				else if (nextTrip == null) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesNextTripIsNull++;
					}
				}

				if (trip.DestinationPurpose == Constants.Purpose.NONE_OR_HOME && Global.Configuration.IsInEstimationMode && Global.Configuration.EstimationModel == "IntermediateStopLocationModel") {
					Global.PrintFile.WriteEstimationRecordExclusionMessage("ChoiceModelRunner", "SetIntermediateStopDestination", trip.Household.Id, trip.Person.Sequence, trip.Day, trip.Tour.Sequence, trip.Direction, trip.Sequence, 1);
				}

				return;
			}

			trip.DestinationParcelId = sourceTrip.DestinationParcelId;
			trip.DestinationParcel = sourceTrip.DestinationParcel;
			trip.DestinationZoneKey = sourceTrip.DestinationZoneKey;
			trip.DestinationAddressType = sourceTrip.DestinationAddressType;

			nextTrip.OriginParcelId = trip.DestinationParcelId;
			nextTrip.OriginParcel = trip.DestinationParcel;
			nextTrip.OriginZoneKey = trip.DestinationZoneKey;
			nextTrip.SetOriginAddressType(trip.DestinationAddressType);
		}

		private static void CloneTripModeAndTime(ITourWrapper sourceTour, ITripWrapper sourceTrip, ITourWrapper tour, ITripWrapper trip) {
			//mbtrace
			if (Global.TraceResults) Global.PrintFile.WriteLine("> > > > > > CloneTripModeAndTime Tour {0} Direction {1} Trip {2}", trip.Tour.Sequence, trip.Direction, trip.Sequence);

			trip.Mode = sourceTrip.Mode;
			trip.PathType = sourceTrip.PathType;

			trip.DepartureTime = sourceTrip.DepartureTime;
			trip.ArrivalTimeLimit = sourceTrip.ArrivalTimeLimit;
			trip.EarliestDepartureTime = sourceTrip.EarliestDepartureTime;
			trip.LatestDepartureTime = sourceTrip.LatestDepartureTime;
			bool testbreak = true;
			trip.HUpdateTripValues();
		}


		private void UpdateHousehold() {
			// TODO:  check on this with John M or Mark.  John B added this loop to prevent
			//        null reference exceptions in cases where housheoldDay simulation is stopped after a user specified number of atttempts
			foreach (var householdDay in _household.HouseholdDays) {
				if (householdDay.IsValid == false) {
					householdDay.Reset();
					return;
				}
			}

			foreach (var person in _household.Persons) {
				person.UpdatePersonValues();
			}

			if (Global.Configuration.ShouldRunTourModels) {

				foreach (var tour in _household.HouseholdDays.SelectMany(householdDay => householdDay.PersonDays.Where(personDay => personDay.Tours != null)).SelectMany(personDay => personDay.Tours)) {
					if (tour.DestinationParcel == null) {
						bool testbreak = true;
					}
					tour.UpdateTourValues();

					foreach (var subtour in tour.Subtours) {
						subtour.UpdateTourValues();
					}
				}
			}
		}

		public void Save() {
			_household.Export();

			foreach (var person in _household.Persons) {
				person.Export();
			}

			foreach (var householdDay in _household.HouseholdDays) {
				householdDay.Export();

				if (householdDay.JointToursList.Count + householdDay.FullHalfToursList.Count + householdDay.PartialHalfToursList.Count > 0) {
					bool testbreak = true;
				}

				foreach (var jointTour in householdDay.JointToursList) {
					jointTour.Export();
				}

				foreach (var fullHalfTour in householdDay.FullHalfToursList) {
					fullHalfTour.Export();
				}

				foreach (var partialHalfTour in householdDay.PartialHalfToursList) {
					partialHalfTour.Export();
				}

				foreach (var personDay in householdDay.PersonDays) {
					personDay.Export();

					if (personDay.Tours == null) {
						continue;
					}

					if (personDay.Tours.Count > 1) {
						// sorts tours chronologically
						personDay.Tours.Sort((tour1, tour2) => tour1.OriginDepartureTime.CompareTo(tour2.OriginDepartureTime));
					}

					foreach (var tour in personDay.Tours) {
						tour.Export();

						if (tour.HalfTourFromOrigin != null && tour.HalfTourFromDestination != null) {
							foreach (var trip in tour.HalfTourFromOrigin.Trips.Invert()) {
								trip.SetTourSequence(tour.Sequence);
								trip.SetTripValueOfTime();
								trip.Export();

								ChoiceModelUtility.WriteTripForTDM(trip, ChoiceModelFactory.TDMTripListExporter);
							}

							foreach (var trip in tour.HalfTourFromDestination.Trips) {
								trip.SetTourSequence(tour.Sequence);
								trip.SetTripValueOfTime();
								trip.Export();

								ChoiceModelUtility.WriteTripForTDM(trip, ChoiceModelFactory.TDMTripListExporter);
							}
						}

						if (tour.Subtours.Count > 1) {
							// sorts subtours chronologically
							tour.Subtours.Sort((tour1, tour2) => tour1.OriginDepartureTime.CompareTo(tour2.OriginDepartureTime));
						}

						foreach (var subtour in tour.Subtours) {
							subtour.SetParentTourSequence(tour.Sequence);
							subtour.Export();

							if (subtour.HalfTourFromOrigin == null || subtour.HalfTourFromDestination == null) {
								continue;
							}

							foreach (var trip in subtour.HalfTourFromOrigin.Trips.Invert()) {
								trip.SetTourSequence(subtour.Sequence);
								trip.Export();

								ChoiceModelUtility.WriteTripForTDM(trip, ChoiceModelFactory.TDMTripListExporter);
							}

							foreach (var trip in subtour.HalfTourFromDestination.Trips) {
								trip.SetTourSequence(subtour.Sequence);
								trip.Export();

								ChoiceModelUtility.WriteTripForTDM(trip, ChoiceModelFactory.TDMTripListExporter);
							}
						}
					}
				}
			}
		}
	}
}
