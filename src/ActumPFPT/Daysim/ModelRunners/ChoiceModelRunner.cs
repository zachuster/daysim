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
using Daysim.Interfaces;
using Ninject;

namespace Daysim.ModelRunners {
	public sealed class ChoiceModelRunner : IChoiceModelRunner {
		private readonly IHouseholdWrapper _household;
		private static object _lock = new object();

		public ChoiceModelRunner(IHousehold household) {
			_household = Global.Kernel.Get<HouseholdWrapperFactory>().HouseholdWrapperCreator.CreateWrapper(household);

		}

		public void SetRandomSeed(int randomSeed) {
			_household.RandomUtility.ResetHouseholdSynchronization(randomSeed);
			_household.RandomUtility.ResetUniform01(randomSeed);
			_household.Init();
		}

		public void RunChoiceModels() {
			RunPersonModels();
			RunHouseholdModels();
			RunPersonDayModels();

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

		private void RunPersonDayModels() {
			if (!Global.Configuration.ShouldRunPersonDayModels) {
				return;
			}

			foreach (var personDay in _household.HouseholdDays.SelectMany(householdDay => householdDay.PersonDays)) {
#if RELEASE
				try {
#endif
				lock (_lock) {
					ChoiceModelFactory.TotalPersonDays++;
				}
				var simulatedAnInvalidPersonDay = false;

				while (!personDay.IsValid) {
					personDay.IsValid = true;

					lock (_lock) {
						ChoiceModelFactory.TotalTimesPersonDayModelSuiteRun++;
					}
					RunPersonDayModelSuite(personDay);
					RunTourModels(personDay);

					// exits the loop if the person's day is valid
					if (personDay.IsValid) {
						// after updating park and ride lot loads
						if (!Global.Configuration.IsInEstimationMode && personDay.Tours != null) {
							foreach (var tour in personDay.Tours.Where(tour => tour.Mode == Constants.Mode.PARK_AND_RIDE)) {
								tour.SetParkAndRideStay();
							}
						}

						break;
					}

					personDay.AttemptedSimulations++;

					if (!simulatedAnInvalidPersonDay) {
						simulatedAnInvalidPersonDay = true;

						// counts unique instances where a person's day is invalid
						lock (_lock) {
							ChoiceModelFactory.TotalInvalidAttempts++;
						}
					}

					personDay.Reset();
				}
#if RELEASE
				}
				catch (Exception e) {
					throw new PersonDayModelException(string.Format("Error running person-day models for {0}.", personDay), e);
				}
#endif
			}
		}

		private static void RunTourModels(IPersonDayWrapper personDay) {
			if (!Global.Configuration.ShouldRunTourModels) {
				return;
			}

			// creates or adds tours to a person's day based on application or estimation mode
			// tours are created by purpose
			personDay.SetTours();

			foreach (var tour in personDay.Tours) {
#if RELEASE
				try {
#endif
				lock (_lock) {
					ChoiceModelFactory.TotalTimesTourModelSuiteRun++;
				}
				RunTourModelSuite(tour);

				if (!personDay.IsValid) {
					if (Global.Configuration.IsInEstimationMode && Global.Configuration.EstimationModel == "IntermediateStopLocationModel") {
						Global.PrintFile.WriteEstimationRecordExclusionMessage("ChoiceModelRunner", "RunTourModels", tour.Household.Id, tour.Person.Sequence, -1, tour.Sequence, -1, -1, tour.HalfTour1Trips + tour.HalfTour2Trips);
					}

					return;
				}

				lock (_lock) {
					ChoiceModelFactory.TotalTimesTourTripModelsRun++;
				}
				RunTourTripModels(tour);

				if (!personDay.IsValid) {
					return;
				}

				lock (_lock) {
					ChoiceModelFactory.TotalTimesTourSubtourModelsRun++;
				}
				RunSubtourModels(tour);

				if (!personDay.IsValid) {
					return;
				}
#if RELEASE
				}
				catch (Exception e) {
					throw new TourModelException(string.Format("Error running tour models for {0}.", tour), e);
				}
#endif
			}
		}

		private static void RunTourTripModels(ITourWrapper tour) {
			if (!Global.Configuration.ShouldRunTourTripModels) {
				return;
			}

			lock (_lock) {
				ChoiceModelFactory.TotalTimesProcessHalfToursRun++;
			}
			ProcessHalfTours(tour);

			if (!tour.PersonDay.IsValid) {
				return;
			}

			tour.SetOriginTimes();
		}

		private static void RunSubtourModels(ITourWrapper tour) {
			if (!Global.Configuration.ShouldRunSubtourModels) {
				return;
			}

			foreach (var subtour in tour.Subtours) {
#if RELEASE
				try {
#endif
				lock (_lock) {
					ChoiceModelFactory.TotalTimesTourSubtourModelSuiteRun++;
				}
				RunSubtourModelSuite(subtour);

				if (!tour.PersonDay.IsValid) {
					return;
				}

				lock (_lock) {
					ChoiceModelFactory.TotalTimesSubtourTripModelsRun++;
				}
				RunSubtourTripModels(subtour);

				if (!tour.PersonDay.IsValid) {
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

		private static void RunSubtourTripModels(ITourWrapper subtour) {
			if (!Global.Configuration.ShouldRunSubtourTripModels) {
				return;
			}

			lock (_lock) {
				ChoiceModelFactory.TotalTimesProcessHalfSubtoursRun++;
			}
			ProcessHalfTours(subtour);

			if (!subtour.PersonDay.IsValid) {
				return;
			}

			subtour.SetOriginTimes();
		}

		private static void RunHouseholdModelSuite(IHouseholdWrapper household) {
			if (!Global.Configuration.ShouldRunAutoOwnershipModel) {
				return;
			}

			// sets number of vehicles in household
			lock (_lock) {
				ChoiceModelFactory.TotalTimesAutoOwnershipModelRun++;
			}
			(Global.ChoiceModelDictionary.Get("AutoOwnershipModel") as AutoOwnershipModel).Run(household);
		}

		private void RunPersonModelSuite(IPersonWrapper person) {
			if (Global.Configuration.ShouldRunWorkLocationModel && person.IsFullOrPartTimeWorker) {
				if (Global.Configuration.IsInEstimationMode || person.Household.RandomUtility.Uniform01() > _household.FractionWorkersWithJobsOutsideRegion) {
					// sets a person's usual work location
					// for full or part-time workers
					lock (_lock) {
						ChoiceModelFactory.TotalTimesWorkLocationModelRun++;
					}
					(Global.ChoiceModelDictionary.Get("WorkLocationModel") as WorkLocationModel).Run(person, Global.Configuration.WorkLocationModelSampleSize);
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
				(Global.ChoiceModelDictionary.Get("SchoolLocationModel") as SchoolLocationModel).Run(person, Global.Configuration.SchoolLocationModelSampleSize);
			}

			if (Global.Configuration.ShouldRunWorkLocationModel && person.IsWorker && person.IsNotFullOrPartTimeWorker) {
				if (Global.Configuration.IsInEstimationMode || person.Household.RandomUtility.Uniform01() > _household.FractionWorkersWithJobsOutsideRegion) {
					// sets a person's usual work location
					// for other workers in relation to a person's school location
					lock (_lock) {
						ChoiceModelFactory.TotalTimesWorkLocationModelRun++;
					}
					(Global.ChoiceModelDictionary.Get("WorkLocationModel") as WorkLocationModel).Run(person, Global.Configuration.WorkLocationModelSampleSize);
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
					(Global.ChoiceModelDictionary.Get("PayToParkAtWorkplaceModel") as PayToParkAtWorkplaceModel).Run(person);
				}
			}
			else {
				person.PayToParkAtWorkplaceFlag = 1; // by default, people pay the parcel parking price
			}

            if (!Global.Configuration.IsInEstimationMode && Global.Configuration.Policy_UniversalTransitPassOwnership)
            {
                person.TransitPassOwnershipFlag = 1; //policy to turn on transit pass ownership
            }
            else if (!person.IsChildUnder5 && Global.Configuration.IncludeTransitPassOwnershipModel && Global.Configuration.ShouldRunTransitPassOwnershipModel)
            {
                lock (_lock)
                {
                    ChoiceModelFactory.TotalTimesTransitPassOwnershipModelRun++;
                }
                HTransitPassOwnershipModel.Run(person);
            }
            else
            {
                person.TransitPassOwnershipFlag = 0; // by default, people don't own a transit pass
            }
		}

		private static void RunPersonDayModelSuite(IPersonDayWrapper personDay) {
			if (Global.Configuration.ShouldRunIndividualPersonDayPatternModel) {
				// determines if there are tours for a person's day
				// sets number of stops for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonDayPatternModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("IndividualPersonDayPatternModel") as IndividualPersonDayPatternModel).Run(personDay);
			}

			if (!Global.Configuration.ShouldRunPersonExactNumberOfToursModel) {
				return;
			}

			if (personDay.WorkTours > 0) {
				// sets number of work tours for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonExactNumberOfToursModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("PersonExactNumberOfToursModel") as PersonExactNumberOfToursModel).Run(personDay, Constants.Purpose.WORK);
			}

			if (personDay.SchoolTours > 0) {
				// sets number of school tours for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonExactNumberOfToursModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("PersonExactNumberOfToursModel") as PersonExactNumberOfToursModel).Run(personDay, Constants.Purpose.SCHOOL);
			}

			if (personDay.EscortTours > 0) {
				// sets number of escort tours for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonExactNumberOfToursModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("PersonExactNumberOfToursModel") as PersonExactNumberOfToursModel).Run(personDay, Constants.Purpose.ESCORT);
			}

			if (personDay.PersonalBusinessTours > 0) {
				// sets number of personal business tours for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonExactNumberOfToursModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("PersonExactNumberOfToursModel") as PersonExactNumberOfToursModel).Run(personDay, Constants.Purpose.PERSONAL_BUSINESS);
			}

			if (personDay.ShoppingTours > 0) {
				// sets number of shopping tours for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonExactNumberOfToursModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("PersonExactNumberOfToursModel") as PersonExactNumberOfToursModel).Run(personDay, Constants.Purpose.SHOPPING);
			}

			if (personDay.MealTours > 0) {
				// sets number of meal tours for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonExactNumberOfToursModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("PersonExactNumberOfToursModel") as PersonExactNumberOfToursModel).Run(personDay, Constants.Purpose.MEAL);
			}

			if (personDay.SocialTours > 0) {
				// sets number of social tours for a person's day
				lock (_lock) {
					ChoiceModelFactory.TotalTimesPersonExactNumberOfToursModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("PersonExactNumberOfToursModel") as PersonExactNumberOfToursModel).Run(personDay, Constants.Purpose.SOCIAL);
			}
		}

		private static void RunTourModelSuite(ITourWrapper tour) {
			tour.SetHomeBasedIsSimulated();

			SetTourDestination(tour);

			if (!tour.PersonDay.IsValid) {
				return;
			}

			GenerateSubtours(tour);

			if (!tour.PersonDay.IsValid) {
				return;
			}
			SetTourModeAndTime(tour);

			if (!tour.PersonDay.IsValid) {
				return;
			}

			if (!Global.Configuration.IsInEstimationMode && tour.DestinationArrivalTime > tour.DestinationDepartureTime) {
				Global.PrintFile.WriteArrivalTimeGreaterThanDepartureTimeWarning("ChoiceModelRunner", "RunTourModels", tour.PersonDay.Id, tour.DestinationArrivalTime, tour.DestinationDepartureTime);
				tour.PersonDay.IsValid = false;

				return;
			}

			// # = busy :(
			// - = available :)

			// carves out a person's availability for the day in relation to the tour
			// person day availabilty [----###########----]
			tour.PersonDay.TimeWindow.SetBusyMinutes(tour.DestinationArrivalTime, tour.DestinationDepartureTime);

			if (tour.Subtours.Count == 0) {
				return;
			}

			// sets the availabilty for a tour's subtours 
			// tour availabilty [####-----------####]
			tour.TimeWindow.SetBusyMinutes(1, tour.DestinationArrivalTime);
			tour.TimeWindow.SetBusyMinutes(tour.DestinationDepartureTime, Constants.Time.MINUTES_IN_A_DAY);
		}

		private static void SetTourDestination(ITourWrapper tour) {
			switch (tour.DestinationPurpose) {
				case Constants.Purpose.WORK:
					if (Global.Configuration.ShouldRunWorkTourDestinationModel) {
						// sets the destination for the work tour
						// the usual work location or some another work location
						lock (_lock) {
							ChoiceModelFactory.TotalTimesWorkTourDestinationModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("WorkTourDestinationModel") as WorkTourDestinationModel).Run(tour, Global.Configuration.WorkTourDestinationModelSampleSize);
					}

					return;
				case Constants.Purpose.SCHOOL:
					if (!Global.Configuration.IsInEstimationMode) {
						// sets the destination for the school tour
						tour.DestinationParcelId = tour.Person.UsualSchoolParcelId;
						tour.DestinationParcel = tour.Person.UsualSchoolParcel;
						tour.DestinationZoneKey = tour.Person.UsualSchoolZoneKey;
						tour.DestinationAddressType = Constants.AddressType.USUAL_SCHOOL;
					}

					return;
				default:
					if (Global.Configuration.ShouldRunOtherTourDestinationModel) {
						// sets the destination for the work tour
						// the usual work location or some another work location
						lock (_lock) {
							ChoiceModelFactory.TotalTimesOtherTourDestinationModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("OtherTourDestinationModel") as OtherTourDestinationModel).Run(tour, Global.Configuration.OtherTourDestinationModelSampleSize);
					}

					return;
			}
		}

		private static void GenerateSubtours(ITourWrapper tour) {
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
					(Global.ChoiceModelDictionary.Get("WorkBasedSubtourGenerationModel") as WorkBasedSubtourGenerationModel).Run(tour, nCallsForTour, subtour.DestinationPurpose);
				}
				nCallsForTour++;
				lock (_lock) {
					ChoiceModelFactory.TotalTimesWorkBasedSubtourGenerationModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("WorkBasedSubtourGenerationModel") as WorkBasedSubtourGenerationModel).Run(tour, nCallsForTour);
			}
			else {
				// creates the subtours for work tour 
				var nCallsForTour = 0;
				while (tour.Subtours.Count < 4) {
					// -- in application mode --
					// sets the destination purpose of the subtour
					lock (_lock) {
						ChoiceModelFactory.TotalTimesWorkBasedSubtourGenerationModelRun++;
					}
					nCallsForTour++;
					var destinationPurposeForSubtour = (Global.ChoiceModelDictionary.Get("WorkBasedSubtourGenerationModel") as WorkBasedSubtourGenerationModel).Run(tour, nCallsForTour);

					if (destinationPurposeForSubtour == Constants.Purpose.NONE_OR_HOME) {
						break;
					}
					// the subtour is added to the tour's Subtours collection when the subtour's purpose is not NONE_OR_HOME
					tour.Subtours.Add(tour.CreateSubtour(tour.DestinationAddressType, tour.DestinationParcelId, tour.DestinationZoneKey, destinationPurposeForSubtour));
				}

				tour.PersonDay.WorkBasedTours += tour.Subtours.Count;
			}
		}

		private static void SetTourModeAndTime(ITourWrapper tour) {
			switch (tour.DestinationPurpose) {
				case Constants.Purpose.WORK:
					if (Global.Configuration.ShouldRunWorkTourModeModel) {
						// sets the work tour's mode of travel to the destination
						lock (_lock) {
							ChoiceModelFactory.TotalTimesWorkTourModeModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("WorkTourModeModel") as WorkTourModeModel).Run(tour);
					}

					if (Global.Configuration.ShouldRunWorkTourTimeModel) {
						// sets the work tour's destination arrival and departure times
						lock (_lock) {
							ChoiceModelFactory.TotalTimesWorkTourTimeModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("WorkTourTimeModel") as WorkTourTimeModel).Run(tour);
					}

					return;
				case Constants.Purpose.SCHOOL:
					if (Global.Configuration.ShouldRunSchoolTourModeModel) {
						// sets the school tour's mode of travel to the destination
						lock (_lock) {
							ChoiceModelFactory.TotalTimesSchoolTourModeModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("SchoolTourModeModel") as SchoolTourModeModel).Run(tour);
					}

					if (Global.Configuration.ShouldRunSchoolTourTimeModel) {
						// sets the school tour's destination arrival and departure times
						lock (_lock) {
							ChoiceModelFactory.TotalTimesSchoolTourTimeModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("SchoolTourTimeModel") as SchoolTourTimeModel).Run(tour);
					}

					return;
				case Constants.Purpose.ESCORT:
					if (Global.Configuration.ShouldRunEscortTourModeModel) {
						// sets the escort tour's mode of travel to the destination
						lock (_lock) {
							ChoiceModelFactory.TotalTimesEscortTourModeModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("EscortTourModeModel") as EscortTourModeModel).Run(tour);
					}

					if (Global.Configuration.ShouldRunOtherHomeBasedTourTimeModel) {
						// sets the escort tour's destination arrival and departure times
						lock (_lock) {
							ChoiceModelFactory.TotalTimesOtherHomeBasedTourTimeModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("OtherHomeBasedTourTimeModel") as OtherHomeBasedTourTimeModel).Run(tour);
					}

					return;
				default:
					if (Global.Configuration.ShouldRunOtherHomeBasedTourModeModel) {
						// sets the tour's mode of travel to the destination with the purposes personal business, shopping, meal, social
						lock (_lock) {
							ChoiceModelFactory.TotalTimesOtherHomeBasedTourModeModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("OtherHomeBasedTourModeModel") as OtherHomeBasedTourModeModel).Run(tour);
					}

					if (Global.Configuration.ShouldRunOtherHomeBasedTourTimeModel) {
						// sets the tour's destination arrival and departure times with the purposes personal business, shopping, meal, social
						lock (_lock) {
							ChoiceModelFactory.TotalTimesOtherHomeBasedTourTimeModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("OtherHomeBasedTourTimeModel") as OtherHomeBasedTourTimeModel).Run(tour);
					}

					return;
			}
		}

		private static void RunSubtourModelSuite(ITourWrapper subtour) {
			subtour.SetWorkBasedIsSimulated();

			SetSubtourDestination(subtour);

			if (!subtour.PersonDay.IsValid) {
				return;
			}

			SetSubtourModeAndTime(subtour);

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
			subtour.ParentTour.TimeWindow.SetBusyMinutes(subtour.DestinationArrivalTime, subtour.DestinationDepartureTime);
		}

		private static void SetSubtourDestination(ITourWrapper subtour) {
			switch (subtour.DestinationPurpose) {
				case Constants.Purpose.WORK:
					if (Global.Configuration.ShouldRunWorkTourDestinationModel) {
						// sets the destination for the work tour
						// the usual work location or some another work location
						lock (_lock) {
							ChoiceModelFactory.TotalTimesWorkSubtourDestinationModelRun++;
						}
						(Global.ChoiceModelDictionary.Get("WorkTourDestinationModel") as WorkTourDestinationModel).Run(subtour, Global.Configuration.WorkTourDestinationModelSampleSize);
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
						(Global.ChoiceModelDictionary.Get("OtherTourDestinationModel") as OtherTourDestinationModel).Run(subtour, Global.Configuration.OtherTourDestinationModelSampleSize);
					}

					return;
			}
		}

		private static void SetSubtourModeAndTime(ITourWrapper subtour) {
			if (Global.Configuration.ShouldRunWorkBasedSubtourModeModel) {
				// sets the subtour's mode of travel to the destination
				lock (_lock) {
					ChoiceModelFactory.TotalTimesWorkBasedSubtourModeModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("WorkBasedSubtourModeModel") as WorkBasedSubtourModeModel).Run(subtour);
			}

			if (Global.Configuration.ShouldRunWorkBasedSubtourTimeModel) {
				// sets subtour's destination arrival and departure times
				lock (_lock) {
					ChoiceModelFactory.TotalTimesWorkBasedSubtourTimeModelRun++;
				}
				(Global.ChoiceModelDictionary.Get("WorkBasedSubtourTimeModel") as WorkBasedSubtourTimeModel).Run(subtour);
			}
		}

		private static void ProcessHalfTours(ITourWrapper tour) {
			// goes in two directions, from origin to destination and destination to origin
			for (var direction = Constants.TourDirection.ORIGIN_TO_DESTINATION; direction <= Constants.TourDirection.TOTAL_TOUR_DIRECTIONS; direction++) {
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
					RunTripModelSuite(tour, halfTour, trip);

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

		private static void RunTripModelSuite(ITourWrapper tour, IHalfTour halfTour, ITripWrapper trip) {
			var nextTrip = GenerateIntermediateStop(halfTour, trip);

			SetIntermediateStopDestination(trip, nextTrip);
			SetTripModeAndTime(tour, trip);

			if (!trip.PersonDay.IsValid) {
				return;
			}

			// retrieves window based on whether or not the trip's tour is home-based or work-based
			var timeWindow = tour.IsHomeBasedTour ? tour.PersonDay.TimeWindow : tour.ParentTour.TimeWindow;

			if (trip.IsHalfTourFromOrigin && trip.Sequence == 1) {
				// occupies minutes in window between destination and stop
				timeWindow.SetBusyMinutes(tour.DestinationArrivalTime, trip.ArrivalTime);
			}
			else if (!trip.IsHalfTourFromOrigin && trip.Sequence == 1) {
				// occupies minutes in window between destination and stop
				timeWindow.SetBusyMinutes(tour.DestinationDepartureTime, trip.ArrivalTime);
			}
			else {
				// occupies minutes in window from previous stop to stop
				timeWindow.SetBusyMinutes(trip.PreviousTrip.DepartureTime, trip.ArrivalTime);
			}
		}

		private static ITripWrapper GenerateIntermediateStop(IHalfTour halfTour, ITripWrapper trip) {
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
				}
				if (trip.PersonDay.TotalStops > 0) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesIntermediateStopGenerationModelRun++;
					}
					(Global.ChoiceModelDictionary.Get("IntermediateStopGenerationModel") as IntermediateStopGenerationModel).Run(trip, intermediateStopPurpose);
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
					intermediateStopPurpose = (Global.ChoiceModelDictionary.Get("IntermediateStopGenerationModel") as IntermediateStopGenerationModel).Run(trip);
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

		private static void SetIntermediateStopDestination(ITripWrapper trip, ITripWrapper nextTrip) {
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
				(Global.ChoiceModelDictionary.Get("IntermediateStopLocationModel") as IntermediateStopLocationModel).Run(trip, Global.Configuration.IntermediateStopLocationModelSampleSize);
			}
			if (Global.Configuration.IsInEstimationMode) {
				return;
			}

			nextTrip.OriginParcelId = trip.DestinationParcelId;
			nextTrip.OriginParcel = trip.DestinationParcel;
			nextTrip.OriginZoneKey = trip.DestinationZoneKey;
			nextTrip.SetOriginAddressType(trip.DestinationAddressType);
		}

		private static void SetTripModeAndTime(ITourWrapper tour, ITripWrapper trip) {
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
					(Global.ChoiceModelDictionary.Get("TripModeModel") as TripModeModel).Run(trip);
				}
				if (!trip.PersonDay.IsValid) {
					return;
				}
			}

			// sets the trip's destination arrival and departure times
			if (trip.Sequence == 1) {
				if (!Global.Configuration.IsInEstimationMode) {
					trip.DepartureTime = trip.IsHalfTourFromOrigin ? tour.DestinationArrivalTime : tour.DestinationDepartureTime;
					trip.UpdateTripValues();
				}
			}
			else if (trip.OriginPurpose == Constants.Purpose.CHANGE_MODE) {
				//stay at park and ride lot assumed to be 3 minutes
				if (!Global.Configuration.IsInEstimationMode) {
					int endpoint;

					if (trip.IsHalfTourFromOrigin) {
						trip.DepartureTime = trip.PreviousTrip.ArrivalTime - 3;
						endpoint = trip.DepartureTime + 1;
					}
					else {
						trip.DepartureTime = trip.PreviousTrip.ArrivalTime + 3;
						endpoint = trip.DepartureTime - 1;
					}
					if (trip.DepartureTime >= 1 && trip.DepartureTime <= Constants.Time.MINUTES_IN_A_DAY && trip.PersonDay.TimeWindow.EntireSpanIsAvailable(endpoint, trip.DepartureTime)) {
						trip.UpdateTripValues();
					}
					else {
						trip.PersonDay.IsValid = false;
					}
				}
			}
			else {
				if (Global.Configuration.ShouldRunTripTimeModel) {
					lock (_lock) {
						ChoiceModelFactory.TotalTimesTripTimeModelRun++;
					}

					(Global.ChoiceModelDictionary.Get("TripTimeModel") as TripTimeModel).Run(trip);
				}
			}
		}

		private void UpdateHousehold() {
			foreach (var person in _household.Persons) {
				person.UpdatePersonValues();
			}

			foreach (var tour in _household.HouseholdDays.SelectMany(householdDay => householdDay.PersonDays.Where(personDay => personDay.Tours != null)).SelectMany(personDay => personDay.Tours)) {
				tour.UpdateTourValues();

				foreach (var subtour in tour.Subtours) {
					subtour.UpdateTourValues();
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

				if (Global.UseJointTours) {
					foreach (var jointTour in householdDay.JointToursList) {
						jointTour.Export();
					}

					foreach (var fullHalfTour in householdDay.FullHalfToursList) {
						fullHalfTour.Export();
					}

					foreach (var fullHalfTour in householdDay.FullHalfToursList) {
						fullHalfTour.Export();
					}

					foreach (var partialHalfTour in householdDay.PartialHalfToursList) {
						partialHalfTour.Export();
					}
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