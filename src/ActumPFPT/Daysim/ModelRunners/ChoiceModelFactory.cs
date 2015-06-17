// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;
using Daysim.ShadowPricing;
using Ninject;

namespace Daysim.ModelRunners {
	public static class ChoiceModelFactory {
		private static Type _type;

        public static int TotalTimesHouseholdModelSuiteRun { get; set; }

		public static int TotalTimesPersonModelSuiteRun { get; set; }
		
		public static int TotalTimesHouseholdDayModelSuiteRun { get; set; }

		public static int TotalTimesJointHalfTourGenerationModelSuiteRun { get; set; }

		public static int TotalTimesJointTourGenerationModelSuiteRun { get; set; }

		public static int TotalTimesPersonDayMandatoryModelSuiteRun { get; set; }
		
		public static int TotalTimesPersonDayModelSuiteRun { get; set; }
		
		public static int TotalTimesPartialJointHalfTourModelSuiteRun { get; set; }

		public static int TotalTimesFullJointHalfTourModelSuiteRun { get; set; }

		public static int TotalTimesMandatoryTourModelSuiteRun { get; set; }
		
		public static int TotalTimesJointTourModelSuiteRun { get; set; }

		public static int TotalTimesNonMandatoryTourModelSuiteRun { get; set; }
		
		public static int TotalTimesTourModelSuiteRun { get; set; }
		
		public static int TotalTimesTourTripModelsRun { get; set; }
		
		public static int TotalTimesTourSubtourModelsRun { get; set; }
		
		public static int TotalTimesProcessHalfToursRun { get; set; }
		
		public static int TotalTimesTourSubtourModelSuiteRun { get; set; }
		
		public static int TotalTimesSubtourTripModelsRun { get; set; }
		
		public static int TotalTimesProcessHalfSubtoursRun { get; set; }
		
		public static int TotalTimesAutoOwnershipModelRun { get; set; }
		
		public static int TotalTimesWorkLocationModelRun { get; set; }
		
		public static int TotalTimesSchoolLocationModelRun { get; set; }
		
		public static int TotalTimesPaidParkingAtWorkplaceModelRun { get; set; }
		
		public static int TotalTimesWorkUsualModeAndScheduleModelRun { get; set; }

		public static int TotalTimesTransitPassOwnershipModelRun { get; set; }

		public static int TotalTimesMandatoryTourGenerationModelRun { get; set; }

		public static int TotalTimesMandatoryStopPresenceModelRun { get; set; }

		public static int TotalTimesJointHalfTourGenerationModelRun { get; set; }
		
		public static int TotalTimesFullJointHalfTourParticipationModelRun { get; set; }
		
		public static int TotalTimesPartialJointHalfTourParticipationAndChauffeurModelsRun { get; set; }

		public static int TotalTimesJointTourGenerationModelRun { get; set; }

		public static int TotalTimesJointTourParticipationModelRun { get; set; }

		public static int TotalTimesPersonDayPatternModelRun { get; set; }

		public static int TotalTimesPersonTourGenerationModelRun { get; set; }
		
		public static int TotalTimesPersonExactNumberOfToursModelRun { get; set; }
		
		public static int TotalTimesWorkTourDestinationModelRun { get; set; }
		
		public static int TotalTimesTourDestinationModelRun { get; set; }
		
		public static int TotalTimesOtherTourDestinationModelRun { get; set; }
		
		public static int TotalTimesWorkBasedSubtourGenerationModelRun { get; set; }
		
		public static int TotalTimesTourModeTimeModelRun { get; set; }

		public static int TotalTimesWorkTourModeModelRun { get; set; }
		
		public static int TotalTimesWorkTourTimeModelRun { get; set; }
		
		public static int TotalTimesSchoolTourModeModelRun { get; set; }
		
		public static int TotalTimesSchoolTourTimeModelRun { get; set; }
		
		public static int TotalTimesEscortTourModeModelRun { get; set; }
		
		public static int TotalTimesOtherHomeBasedTourModeModelRun { get; set; }
		
		public static int TotalTimesOtherHomeBasedTourTimeModelRun { get; set; }
		
		public static int TotalTimesWorkSubtourDestinationModelRun { get; set; }
		
		public static int TotalTimesBusinessSubtourDestinationModelRun { get; set; }
		
		public static int TotalTimesOtherSubtourDestinationModelRun { get; set; }
		
		public static int TotalTimesWorkBasedSubtourModeModelRun { get; set; }
		
		public static int TotalTimesWorkBasedSubtourTimeModelRun { get; set; }
		
		public static int TotalTimesTripModelSuiteRun { get; set; }
		
		public static int TotalTimesIntermediateStopGenerationModelRun { get; set; }
		
		public static int TotalTimesIntermediateStopGenerated { get; set; }
		
		public static int TotalTimesChangeModeStopGenerated { get; set; }
		
		public static int TotalTimesChangeModeLocationSet { get; set; }
		
		public static int TotalTimesChangeModeTransitModeSet { get; set; }
		
		public static int TotalTimesTripIsToTourOrigin { get; set; }
		
		public static int TotalTimesNextTripIsNull { get; set; }
		
		public static int TotalTimesIntermediateStopLocationModelRun { get; set; }

        public static int TotalTimesTripModeTimeModelRun { get; set; }

        public static int TotalTimesTripModeModelRun { get; set; }
		
		public static int TotalTimesTripTimeModelRun { get; set; }

		public static int TotalTimesActumPrimaryPriorityTimeModelRun { get; set; }

		public static int TotalTimesHouseholdDayPatternTypeModelRun { get; set; }

		public static int TotalTimesPersonDayPatternTypeModelRun { get; set; }

		public static int TotalTimesWorkAtHomeModelRun { get; set; }

		public static int HouseholdFileRecordsWritten { get; set; }

		public static int PersonFileRecordsWritten { get; set; }

		public static int HouseholdDayFileRecordsWritten { get; set; }

		public static int PersonDayFileRecordsWritten { get; set; }

		public static int TourFileRecordsWritten { get; set; }

		public static int TripFileRecordsWritten { get; set; }

		public static int JointTourFileRecordsWritten { get; set; }

		public static int PartialHalfTourFileRecordsWritten { get; set; }

		public static int FullHalfTourFileRecordsWritten { get; set; }

		public static Int64 HouseholdVehiclesOwnedCheckSum { get; set; }

		public static Int64 PersonUsualWorkParcelCheckSum { get; set; }

  	public static Int64 PersonUsualSchoolParcelCheckSum { get; set; }

		public static Int64 PersonTransitPassOwnershipCheckSum { get; set; }

  	public static Int64 PersonPaidParkingAtWorkCheckSum { get; set; }

  	public static Int64 PersonDayHomeBasedToursCheckSum { get; set; }

  	public static Int64 PersonDayWorkBasedToursCheckSum { get; set; }

  	public static Int64 TourMainDestinationPurposeCheckSum { get; set; }

  	public static Int64 TourMainDestinationParcelCheckSum { get; set; }

  	public static Int64 TourMainModeTypeCheckSum { get; set; }

  	public static Int64 TourOriginDepartureTimeCheckSum { get; set; }

  	public static Int64 TourDestinationArrivalTimeCheckSum { get; set; }

  	public static Int64 TourDestinationDepartureTimeCheckSum { get; set; }

  	public static Int64 TourOriginArrivalTimeCheckSum { get; set; }

  	public static Int64 TripHalfTourCheckSum { get; set; }

		public static Int64 TripCheckSum { get; set; }

		public static Int64 TripDestinationPurposeCheckSum { get; set; }

		public static Int64 TripDestinationParcelCheckSum { get; set; }

		public static Int64 TripModeCheckSum { get; set; }

		public static Int64 TripPathTypeCheckSum { get; set; }

		public static Int64 TripDepartureTimeCheckSum { get; set; }

		public static Int64 TripArrivalTimeCheckSum { get; set; }


		public static ThreadQueue ThreadQueue { get; private set; }

		public static ExporterFactory ExporterFactory { get; private set; }

		public static Dictionary<int, CondensedParcel> Parcels { get; set; }

		//public static Dictionary<int, ZoneTotals> ZoneTotals { get; private set; }

		public static Dictionary<int, int> ZoneKeys { get; private set; }

		public static int SmallPeriodDuration { get; private set; }

		public static TDMTripListExporter TDMTripListExporter { get; private set; }

		public static int TotalPersonDays { get; set; }

		public static int TotalHouseholdDays { get; set; }

		public static int TotalInvalidAttempts { get; set; }

		public static ParkAndRideNodeDao ParkAndRideNodeDao { get; private set; }

		public static void Initialize(string name) {
			_type = Type.GetType("Daysim.ModelRunners." + name);

			if (_type == null) {
				throw new Exception(string.Format(@"Unable to determine choice model runner type from name ""{0}"".", name));
			}

			if (!Global.Configuration.IsInEstimationMode) {
				ThreadQueue = new ThreadQueue();
			}

			ExporterFactory = Global.Kernel.Get<ExporterFactory>();

			// e.g. 30 minutes between each minute span
			SmallPeriodDuration = DayPeriod.SmallDayPeriods.First().Duration;

			if (Global.Configuration.ShouldOutputTDMTripList) {
				TDMTripListExporter = new TDMTripListExporter(Global.GetOutputPath(Global.Configuration.OutputTDMTripListPath), Global.Configuration.TDMTripListDelimiter);
			}

			LoadData();
		}

		public static void LoadData() {
			var parcelReader = Global.Kernel.Get<Reader<Parcel>>();

			Parcels = new Dictionary<int, CondensedParcel>(parcelReader.Count);

			var zoneReader = Global.Kernel.Get<Reader<Zone>>();

//			ZoneTotals = new Dictionary<int, ZoneTotals>(zoneReader.Count);
			ZoneKeys = new Dictionary<int, int>(zoneReader.Count);

			var zones = new Dictionary<int, Zone>();

			foreach (var zone in zoneReader) {
				ZoneKeys.Add(zone.Id, zone.Key);
				zones.Add(zone.Id, zone);
			}

			var shadowPrices = ShadowPriceReader.ReadShadowPrices();

			foreach (var parcel in parcelReader) {
				var condensedParcel = parcel.GetCondensedParcel();
				Parcels.Add(parcel.Id, condensedParcel);

				condensedParcel.SetShadowPricing(zones, shadowPrices);

//				ZoneTotals zoneTotals;
//
//				if (!ZoneTotals.TryGetValue(parcel.ZoneId, out zoneTotals)) {
//					zoneTotals = new ZoneTotals();
//
//					ZoneTotals.Add(parcel.ZoneId, zoneTotals);
//				}
//
//				zoneTotals.SumTotals(parcel);
			}
			
			if (Global.ParkAndRideNodeIsEnabled) {
				ParkAndRideNodeDao = new ParkAndRideNodeDao();
			}
		}
        
		public static void WriteCounters() {
			Global.PrintFile.WriteLine("Person day statistics:");

			Global.PrintFile.IncrementIndent();
			if (TotalHouseholdDays > 0) {
				Global.PrintFile.WriteLine("TotalHouseholdDays = {0} ", TotalHouseholdDays);
			}
			else {
				Global.PrintFile.WriteLine("TotalPersonDays = {0} ", TotalPersonDays);
			}
			Global.PrintFile.WriteLine("TotalInvalidAttempts = {0} ", TotalInvalidAttempts);
			Global.PrintFile.DecrementIndent();

			Global.PrintFile.WriteLine("Counters:");

			Global.PrintFile.IncrementIndent();
			Global.PrintFile.WriteLine("TotalTimesHouseholdModelSuiteRun = {0} ", TotalTimesHouseholdModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesPersonModelSuiteRun = {0} ", TotalTimesPersonModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesHouseholdDayModelSuiteRun = {0} ", TotalTimesHouseholdDayModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesActumPriorityTimeModelRun = {0} ", TotalTimesActumPrimaryPriorityTimeModelRun);
			Global.PrintFile.WriteLine("TotalTimesHouseholdDayPatternTypeModelRun = {0} ", TotalTimesHouseholdDayPatternTypeModelRun);
			Global.PrintFile.WriteLine("TotalTimesPersonDayPatternTypeModelRun = {0} ", TotalTimesPersonDayPatternTypeModelRun);
			Global.PrintFile.WriteLine("TotalTimesJointHalfTourGenerationModelSuiteRun = {0} ", TotalTimesJointHalfTourGenerationModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesJointHalfTourGenerationModelRun = {0} ", TotalTimesJointHalfTourGenerationModelRun);
			Global.PrintFile.WriteLine("TotalTimesFullJointHalfTourParticipationModelRun = {0} ", TotalTimesFullJointHalfTourParticipationModelRun);
			Global.PrintFile.WriteLine("TotalTimesPartialJointHalfTourParticipationAndChauffeurModelsRun = {0} ", TotalTimesPartialJointHalfTourParticipationAndChauffeurModelsRun);
			Global.PrintFile.WriteLine("TotalTimesJointTourGenerationModelSuiteRun = {0} ", TotalTimesJointTourGenerationModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesJointTourGenerationModelRun = {0} ", TotalTimesJointTourGenerationModelRun);
			Global.PrintFile.WriteLine("TotalTimesJointTourParticipationModelRun = {0} ", TotalTimesJointTourParticipationModelRun);
			Global.PrintFile.WriteLine("TotalTimesPersonDayMandatoryModelSuiteRun = {0} ", TotalTimesPersonDayMandatoryModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesPersonDayModelSuiteRun = {0} ", TotalTimesPersonDayModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesPartialJointHalfTourModelSuiteRun = {0} ", TotalTimesPartialJointHalfTourModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesFullJointHalfTourModelSuiteRun = {0} ", TotalTimesFullJointHalfTourModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesMandatoryTourModelSuiteRun = {0} ", TotalTimesMandatoryTourModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesJointTourModelSuiteRun = {0} ", TotalTimesJointTourModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesNonMandatoryTourModelSuiteRun = {0} ", TotalTimesNonMandatoryTourModelSuiteRun);
		
			Global.PrintFile.WriteLine("TotalTimesTourModelSuiteRun = {0} ", TotalTimesTourModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesTourTripModelsRun = {0} ", TotalTimesTourTripModelsRun);
			Global.PrintFile.WriteLine("TotalTimesTourSubtourModelsRun = {0} ", TotalTimesTourSubtourModelsRun);
			Global.PrintFile.WriteLine("TotalTimesProcessHalfToursRun = {0} ", TotalTimesProcessHalfToursRun);
			Global.PrintFile.WriteLine("TotalTimesTourSubtourModelSuiteRun = {0} ", TotalTimesTourSubtourModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesSubtourTripModelsRun = {0} ", TotalTimesSubtourTripModelsRun);
			Global.PrintFile.WriteLine("TotalTimesProcessHalfSubtoursRun = {0} ", TotalTimesProcessHalfSubtoursRun);
			Global.PrintFile.WriteLine("TotalTimesAutoOwnershipModelRun = {0} ", TotalTimesAutoOwnershipModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkLocationModelRun = {0} ", TotalTimesWorkLocationModelRun);
			Global.PrintFile.WriteLine("TotalTimesSchoolLocationModelRun = {0} ", TotalTimesSchoolLocationModelRun);
			Global.PrintFile.WriteLine("TotalTimesPaidParkingAtWorkplaceModelRun = {0} ", TotalTimesPaidParkingAtWorkplaceModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkUsualModeAndScheduleModelRun = {0} ", TotalTimesWorkUsualModeAndScheduleModelRun);
			Global.PrintFile.WriteLine("TotalTimesTransitPassOwnershipModelRun = {0} ", TotalTimesTransitPassOwnershipModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkAtHomeModelRun = {0} ", TotalTimesWorkAtHomeModelRun);
			Global.PrintFile.WriteLine("TotalTimesMandatoryTourGenerationModelRun = {0} ", TotalTimesMandatoryTourGenerationModelRun);
			Global.PrintFile.WriteLine("TotalTimesMandatoryStopPresenceModelRun = {0} ", TotalTimesMandatoryStopPresenceModelRun);
			Global.PrintFile.WriteLine("TotalTimesPersonDayPatternModelRun = {0} ", TotalTimesPersonDayPatternModelRun);
			Global.PrintFile.WriteLine("TotalTimesPersonTourGenerationModelRun = {0} ", TotalTimesPersonTourGenerationModelRun);
			Global.PrintFile.WriteLine("TotalTimesPersonExactNumberOfToursModelRun = {0} ", TotalTimesPersonExactNumberOfToursModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkTourDestinationModelRun = {0} ", TotalTimesWorkTourDestinationModelRun);
			Global.PrintFile.WriteLine("TotalTimesTourDestinationModelRun = {0} ", TotalTimesTourDestinationModelRun);
			Global.PrintFile.WriteLine("TotalTimesOtherTourDestinationModelRun = {0} ", TotalTimesOtherTourDestinationModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkBasedSubtourGenerationModelRun = {0} ", TotalTimesWorkBasedSubtourGenerationModelRun);
			Global.PrintFile.WriteLine("TotalTimesTourModeTimeModelRun = {0} ", TotalTimesTourModeTimeModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkTourModeModelRun = {0} ", TotalTimesWorkTourModeModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkTourTimeModelRun = {0} ", TotalTimesWorkTourTimeModelRun);
			Global.PrintFile.WriteLine("TotalTimesSchoolTourModeModelRun = {0} ", TotalTimesSchoolTourModeModelRun);
			Global.PrintFile.WriteLine("TotalTimesSchoolTourTimeModelRun = {0} ", TotalTimesSchoolTourTimeModelRun);
			Global.PrintFile.WriteLine("TotalTimesEscortTourModeModelRun = {0} ", TotalTimesEscortTourModeModelRun);
			Global.PrintFile.WriteLine("TotalTimesOtherHomeBasedTourModeModelRun = {0} ", TotalTimesOtherHomeBasedTourModeModelRun);
			Global.PrintFile.WriteLine("TotalTimesOtherHomeBasedTourTimeModelRun = {0} ", TotalTimesOtherHomeBasedTourTimeModelRun);
			Global.PrintFile.WriteLine("TotalTimesWork(Sub)TourDestinationModelRun = {0} ", TotalTimesWorkSubtourDestinationModelRun);
			Global.PrintFile.WriteLine("TotalTimesBusiness(Sub)TourDestinationModelRun = {0} ", TotalTimesBusinessSubtourDestinationModelRun);
			Global.PrintFile.WriteLine("TotalTimesOther(Sub)TourDestinationModelRun = {0} ", TotalTimesOtherSubtourDestinationModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkBasedSubtourModeModelRun = {0} ", TotalTimesWorkBasedSubtourModeModelRun);
			Global.PrintFile.WriteLine("TotalTimesWorkBasedSubtourTimeModelRun = {0} ", TotalTimesWorkBasedSubtourTimeModelRun);
			Global.PrintFile.WriteLine("TotalTimesTripModelSuiteRun = {0} ", TotalTimesTripModelSuiteRun);
			Global.PrintFile.WriteLine("TotalTimesIntermediateStopGenerationModelRun = {0} ", TotalTimesIntermediateStopGenerationModelRun);
			Global.PrintFile.WriteLine("TotalTimesIntermediateStopGenerated = {0} ", TotalTimesIntermediateStopGenerated);
			Global.PrintFile.WriteLine("TotalTimesChangeModeStopGenerated = {0} ", TotalTimesChangeModeStopGenerated);
			Global.PrintFile.WriteLine("TotalTimesChangeModeLocationSet = {0} ", TotalTimesChangeModeLocationSet);
			Global.PrintFile.WriteLine("TotalTimesChangeModeTransitModeSet = {0} ", TotalTimesChangeModeTransitModeSet);
			Global.PrintFile.WriteLine("TotalTimesTripIsToTourOrigin = {0} ", TotalTimesTripIsToTourOrigin);
			Global.PrintFile.WriteLine("TotalTimesNextTripIsNull = {0} ", TotalTimesNextTripIsNull);
			Global.PrintFile.WriteLine("TotalTimesIntermediateStopLocationModelRun = {0} ", TotalTimesIntermediateStopLocationModelRun);
			Global.PrintFile.WriteLine("TotalTimesTripModeModelRun = {0} ", TotalTimesTripModeModelRun);
			Global.PrintFile.WriteLine("TotalTimesTripTimeModelRun = {0} ", TotalTimesTripTimeModelRun);
            Global.PrintFile.WriteLine("TotalTimesTripModeTimeModelRun = {0} ", TotalTimesTripModeTimeModelRun);
            Global.PrintFile.WriteLine();
			Global.PrintFile.WriteLine("HouseholdFileRecordsWritten      = {0} ", HouseholdFileRecordsWritten);
			Global.PrintFile.WriteLine("PersonFileRecordsWritten         = {0} ", PersonFileRecordsWritten);
			Global.PrintFile.WriteLine("HouseholdDayFileRecordsWritten   = {0} ", HouseholdDayFileRecordsWritten);
			Global.PrintFile.WriteLine("PersonDayFileRecordsWritten      = {0} ", PersonDayFileRecordsWritten);
			Global.PrintFile.WriteLine("TourFileRecordsWritten           = {0} ", TourFileRecordsWritten);
			Global.PrintFile.WriteLine("TripFileRecordsWritten           = {0} ", TripFileRecordsWritten);
			Global.PrintFile.WriteLine("JointTourFileRecordsWritten      = {0} ", JointTourFileRecordsWritten);
			Global.PrintFile.WriteLine("PartialHalfTourFileRecordsWritten= {0} ", PartialHalfTourFileRecordsWritten);
			Global.PrintFile.WriteLine("FullHalfTourFileRecordsWritten   = {0} ", FullHalfTourFileRecordsWritten);
			Global.PrintFile.WriteLine();
			Global.PrintFile.WriteLine("HouseholdVehiclesOwnedCheckSum      = {0} ", HouseholdVehiclesOwnedCheckSum);
			Global.PrintFile.WriteLine("PersonUsualWorkParcelCheckSum       = {0} ", PersonUsualWorkParcelCheckSum);
			Global.PrintFile.WriteLine("PersonUsualSchoolParcelCheckSum     = {0} ", PersonUsualSchoolParcelCheckSum);
			Global.PrintFile.WriteLine("PersonTransitPassOwnershipCheckSum  = {0} ", PersonTransitPassOwnershipCheckSum);
			Global.PrintFile.WriteLine("PersonPaidParkingAtWorkCheckSum     = {0} ", PersonPaidParkingAtWorkCheckSum);
			Global.PrintFile.WriteLine("PersonDayHomeBasedToursCheckSum     = {0} ", PersonDayHomeBasedToursCheckSum);
			Global.PrintFile.WriteLine("PersonDayWorkBasedToursCheckSum     = {0} ", PersonDayWorkBasedToursCheckSum);
			Global.PrintFile.WriteLine("TourMainDestinationPurposeCheckSum  = {0} ", TourMainDestinationPurposeCheckSum);
			Global.PrintFile.WriteLine("TourMainDestinationParcelCheckSum   = {0} ", TourMainDestinationParcelCheckSum);
			Global.PrintFile.WriteLine("TourMainModeTypeCheckSum            = {0} ", TourMainModeTypeCheckSum);
			Global.PrintFile.WriteLine("TourOriginDepartureTimeCheckSum     = {0} ", TourOriginDepartureTimeCheckSum);
			Global.PrintFile.WriteLine("TourDestinationArrivalTimeCheckSum  = {0} ", TourDestinationArrivalTimeCheckSum);
			Global.PrintFile.WriteLine("TourDestinationDepartureTimeCheckSum= {0} ", TourDestinationDepartureTimeCheckSum);
			Global.PrintFile.WriteLine("TourOriginArrivalTimeCheckSum       = {0} ", TourOriginArrivalTimeCheckSum);
			Global.PrintFile.WriteLine("TripHalfTourCheckSum                = {0} ", TripHalfTourCheckSum);
			Global.PrintFile.WriteLine("TripDestinationPurposeCheckSum      = {0} ", TripDestinationPurposeCheckSum);
			Global.PrintFile.WriteLine("TripDestinationParcelCheckSum       = {0} ", TripDestinationParcelCheckSum);
			Global.PrintFile.WriteLine("TripModeCheckSum                    = {0} ", TripModeCheckSum);
			Global.PrintFile.WriteLine("TripPathTypeCheckSum                = {0} ", TripPathTypeCheckSum);
			Global.PrintFile.WriteLine("TripDepartureTimeCheckSum           = {0} ", TripDepartureTimeCheckSum);
			Global.PrintFile.WriteLine("TripArrivalTimeCheckSum             = {0} ", TripArrivalTimeCheckSum);
      Global.PrintFile.DecrementIndent();
			Global.PrintFile.WriteLine();
			Global.PrintFile.WriteLine("Run completed at ", DateTime.Now.ToString(CultureInfo.InvariantCulture));
		}

		public static void SignalShutdown() {
			if (ThreadQueue == null) {
				return;
			}

			ThreadQueue.Shutdown();

			HouseholdWrapper.Close();
			PersonWrapper.Close();
			HouseholdDayWrapper.Close();
			JointTourWrapper.Close();
			FullHalfTourWrapper.Close();
			PartialHalfTourWrapper.Close();
			PersonDayWrapper.Close();
			TourWrapper.Close();
			TripWrapper.Close();
		}

		public static IChoiceModelRunner Get(IHousehold household, int randomSeed) {
			var runner = (IChoiceModelRunner) Activator.CreateInstance(_type, household);
			runner.SetRandomSeed(randomSeed);
			return runner;
		}
	}
}