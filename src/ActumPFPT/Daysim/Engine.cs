// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Daysim.ChoiceModels;
using Daysim.ChoiceModels.Actum;
using Daysim.DomainModels;
using Daysim.DomainModels.Actum;
using Daysim.Factories;
using Daysim.Factories.Actum;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Framework.Roster;
using Daysim.Interfaces;
using Daysim.ModelRunners;
using Daysim.ParkAndRideShadowPricing;
using Daysim.ShadowPricing;
using HDF5DotNet;
using Ninject;

namespace Daysim {
	public static class Engine {
		public static int Start { get; set; }

		public static int End { get; set; }

		public static int Index { get; set; }

		public static void BeginTestMode()
		{
			RandomUtility randomUtility = new RandomUtility();
			randomUtility.ResetUniform01(Global.Configuration.RandomSeed);
			randomUtility.ResetHouseholdSynchronization(Global.Configuration.RandomSeed);


			
			BeginInitialize();

			RawConverter.RunTestMode();
		}

		public static void BeginProgram() {
			var timer = new Timer("Starting Daysim...");

			RandomUtility randomUtility = new RandomUtility();
			randomUtility.ResetUniform01(Global.Configuration.RandomSeed);
			randomUtility.ResetHouseholdSynchronization(Global.Configuration.RandomSeed);


			
			BeginInitialize();
			BeginRunRawConversion();
			BeginImportData();
			BeginBuildIndexes();

			BeginLoadNodeIndex();
			BeginLoadNodeDistances();
			
			BeginLoadRoster();

			BeginCalculateAggregateLogsums(randomUtility);
			BeginOutputAggregateLogsums();
			BeginCalculateSamplingWeights();
			BeginOutputSamplingWeights();
			
			BeginRunChoiceModels(randomUtility);
			BeginPerformHousekeeping();

			if (Start == -1 || End == -1 || Index == -1) {
				BeginUpdateShadowPricing();
			}

			timer.Stop("Total running time");
		}

		public static void BeginInitialize() {
			var timer = new Timer("Initializing...");

			Initialize();

			timer.Stop();
		}

		private static void Initialize() {
			if (Global.PrintFile != null) {
				Global.PrintFile.WriteLine("Application mode: {0}", Global.Configuration.IsInEstimationMode ? "Estimation" : "Simulation");

				if (Global.Configuration.IsInEstimationMode) {
					Global.PrintFile.WriteLine("Estimation model: {0}", Global.Configuration.EstimationModel);
				}
			}
			InitializeFactories();
			InitializeOutput();
			InitializeInput();
			InitializeWorking();

			if (Global.Configuration.ShouldOutputAggregateLogsums) {
				Global.GetOutputPath(Global.Configuration.OutputAggregateLogsumsPath).CreateDirectory();
			}

			if (Global.Configuration.ShouldOutputSamplingWeights) {
				Global.GetOutputPath(Global.Configuration.OutputSamplingWeightsPath).CreateDirectory();
			}

			if (Global.Configuration.ShouldOutputTDMTripList) {
				Global.GetOutputPath(Global.Configuration.OutputTDMTripListPath).CreateDirectory();
			}

			if (!Global.Configuration.IsInEstimationMode) {
				return;
			}

			if (Global.Configuration.ShouldOutputAlogitData) {
				Global.GetOutputPath(Global.Configuration.OutputAlogitDataPath).CreateDirectory();
			}

			Global.GetOutputPath(Global.Configuration.OutputAlogitControlPath).CreateDirectory();
		}

		private static void InitializeFactories()
		{
			Global.Kernel.Get<PersonPersistenceFactory>().Register("Default", new PersonPersister<Person>());
			Global.Kernel.Get<PersonPersistenceFactory>().Register("Actum", new PersonPersister<ActumPerson>());
			Global.Kernel.Get<PersonPersistenceFactory>().Initialize();

			Global.Kernel.Get<HouseholdPersistenceFactory>().Register("Default", new HouseholdPersister<Household>());
			Global.Kernel.Get<HouseholdPersistenceFactory>().Register("Actum", new HouseholdPersister<ActumHousehold>());
			Global.Kernel.Get<HouseholdPersistenceFactory>().Initialize();

			Global.Kernel.Get<HouseholdWrapperFactory>().Register("Default", new HouseholdWrapperCreator());
			Global.Kernel.Get<HouseholdWrapperFactory>().Register("Actum", new ActumHouseholdWrapperCreator());
			Global.Kernel.Get<HouseholdWrapperFactory>().Initialize();

			Global.Kernel.Get<PersonWrapperFactory>().Register("Default", new PersonWrapperCreator());
			Global.Kernel.Get<PersonWrapperFactory>().Register("Actum", new ActumPersonWrapperCreator());
			Global.Kernel.Get<PersonWrapperFactory>().Initialize();

			Global.Kernel.Get<TripPersistenceFactory>().Register("Default", new TripPersister<Trip>());
			Global.Kernel.Get<TripPersistenceFactory>().Register("Actum", new TripPersister<ActumTrip>());
			Global.Kernel.Get<TripPersistenceFactory>().Initialize();

			Global.Kernel.Get<TripWrapperFactory>().Register("Default", new TripWrapperCreator());
			Global.Kernel.Get<TripWrapperFactory>().Register("Actum", new ActumTripWrapperCreator());
			Global.Kernel.Get<TripWrapperFactory>().Initialize();

			Global.Kernel.Get<TourPersistenceFactory>().Register("Default", new TourPersister<Tour>());
			Global.Kernel.Get<TourPersistenceFactory>().Register("Actum", new TourPersister<Tour>());
			Global.Kernel.Get<TourPersistenceFactory>().Initialize();

			Global.Kernel.Get<TourWrapperFactory>().Register("Default", new TourWrapperCreator());
			Global.Kernel.Get<TourWrapperFactory>().Register("Actum", new ActumTourWrapperCreator());
			Global.Kernel.Get<TourWrapperFactory>().Initialize();

			Global.Kernel.Get<PersonDayPersistenceFactory>().Register("Default", new PersonDayPersister<PersonDay>());
			Global.Kernel.Get<PersonDayPersistenceFactory>().Register("Actum", new PersonDayPersister<ActumPersonDay>());
			Global.Kernel.Get<PersonDayPersistenceFactory>().Initialize();

			Global.Kernel.Get<PersonDayWrapperFactory>().Register("Default", new PersonDayWrapperCreator());
			Global.Kernel.Get<PersonDayWrapperFactory>().Register("Actum", new ActumPersonDayWrapperCreator());
			Global.Kernel.Get<PersonDayWrapperFactory>().Initialize();

			Global.Kernel.Get<HouseholdDayPersistenceFactory>().Register("Default", new HouseholdDayPersister<HouseholdDay>());
			Global.Kernel.Get<HouseholdDayPersistenceFactory>().Register("Actum", new HouseholdDayPersister<ActumHouseholdDay>());
			Global.Kernel.Get<HouseholdDayPersistenceFactory>().Initialize();

			Global.Kernel.Get<HouseholdDayWrapperFactory>().Register("Default", new HouseholdDayWrapperCreator());
			Global.Kernel.Get<HouseholdDayWrapperFactory>().Register("Actum", new ActumHouseholdDayWrapperCreator());
			Global.Kernel.Get<HouseholdDayWrapperFactory>().Initialize();

			Global.Kernel.Get<SkimFileReaderFactory>().Register("text_ij", new TextIJSkimFileReaderCreator());
			Global.Kernel.Get<SkimFileReaderFactory>().Register("bin", new BinarySkimFileReaderCreator());
			Global.Kernel.Get<SkimFileReaderFactory>().Register("emme", new EMMEReaderCreator());
			Global.Kernel.Get<SkimFileReaderFactory>().Register("hdf5", new HDF5ReaderCreator());
			Global.Kernel.Get<SkimFileReaderFactory>().Register("cube", new CubeReaderCreator());

			Global.Kernel.Get<SamplingWeightsSettingsFactory>().Register("SamplingWeightsSettings", new SamplingWeightsSettings());
			Global.Kernel.Get<SamplingWeightsSettingsFactory>().Register("SamplingWeightsSettingsSimple", new SamplingWeightsSettingsSimple());
			Global.Kernel.Get<SamplingWeightsSettingsFactory>().Initialize();

			Global.ChoiceModelDictionary = new ChoiceModelDictionary();

			Global.ChoiceModelDictionary.Register("AutoOwnershipModel", new AutoOwnershipModel());
			Global.ChoiceModelDictionary.Register("EscortTourModeModel", new EscortTourModeModel());
			Global.ChoiceModelDictionary.Register("IndividualPersonDayPatternModel", new IndividualPersonDayPatternModel());
			Global.ChoiceModelDictionary.Register("IntermediateStopGenerationModel", new IntermediateStopGenerationModel());
			Global.ChoiceModelDictionary.Register("IntermediateStopLocationModel", new IntermediateStopLocationModel());
			Global.ChoiceModelDictionary.Register("OtherHomeBasedTourModeModel", new OtherHomeBasedTourModeModel());
			Global.ChoiceModelDictionary.Register("OtherHomeBasedTourTimeModel", new OtherHomeBasedTourTimeModel());
			Global.ChoiceModelDictionary.Register("OtherTourDestinationModel", new OtherTourDestinationModel());
			Global.ChoiceModelDictionary.Register("PayToParkAtWorkplaceModel", new PayToParkAtWorkplaceModel());
			Global.ChoiceModelDictionary.Register("PersonExactNumberOfToursModel", new PersonExactNumberOfToursModel());
			Global.ChoiceModelDictionary.Register("SchoolLocationModel", new SchoolLocationModel());
			Global.ChoiceModelDictionary.Register("SchoolTourModeModel", new SchoolTourModeModel());
			Global.ChoiceModelDictionary.Register("SchoolTourTimeModel", new SchoolTourTimeModel());
			Global.ChoiceModelDictionary.Register("TransitPassOwnershipModel", new TransitPassOwnershipModel());
			Global.ChoiceModelDictionary.Register("TripModeModel", new TripModeModel());
			Global.ChoiceModelDictionary.Register("TripTimeModel", new TripTimeModel());
			Global.ChoiceModelDictionary.Register("WorkBasedSubtourGenerationModel", new WorkBasedSubtourGenerationModel());

			Global.ChoiceModelDictionary.Register("WorkBasedSubtourModeModel", new WorkBasedSubtourModeModel());
			Global.ChoiceModelDictionary.Register("WorkBasedSubtourTimeModel", new WorkBasedSubtourTimeModel());
			Global.ChoiceModelDictionary.Register("WorkLocationModel", new WorkLocationModel());
			Global.ChoiceModelDictionary.Register("WorkTourDestinationModel", new WorkTourDestinationModel());
			Global.ChoiceModelDictionary.Register("WorkTourModeModel", new WorkTourModeModel());
			Global.ChoiceModelDictionary.Register("WorkTourTimeModel", new WorkTourTimeModel());

			Global.ChoiceModelDictionary.Register("ActumEscortTourModeModel", new ActumEscortTourModeModel());
			Global.ChoiceModelDictionary.Register("ActumFullJointHalfTourParticipationModel", new ActumFullJointHalfTourParticipationModel());
			Global.ChoiceModelDictionary.Register("ActumHouseholdDayPatternTypeModel", new ActumHouseholdDayPatternTypeModel());
			Global.ChoiceModelDictionary.Register("ActumIntermediateStopGenerationModel", new ActumIntermediateStopGenerationModel());
			Global.ChoiceModelDictionary.Register("ActumIntermediateStopLocationModel", new ActumIntermediateStopLocationModel());
			Global.ChoiceModelDictionary.Register("ActumJointHalfTourGenerationModel", new ActumJointHalfTourGenerationModel());
			Global.ChoiceModelDictionary.Register("ActumJointTourGenerationModel", new ActumJointTourGenerationModel());
			Global.ChoiceModelDictionary.Register("ActumJointTourParticipationModel", new ActumJointTourParticipationModel());
			Global.ChoiceModelDictionary.Register("ActumMandatoryStopPresenceModel", new ActumMandatoryStopPresenceModel());
			Global.ChoiceModelDictionary.Register("ActumMandatoryTourGenerationModel", new ActumMandatoryTourGenerationModel());
			Global.ChoiceModelDictionary.Register("ActumOtherHomeBasedTourModeModel", new ActumOtherHomeBasedTourModeModel());
			Global.ChoiceModelDictionary.Register("ActumOtherHomeBasedTourTimeModel", new ActumOtherHomeBasedTourTimeModel());
			Global.ChoiceModelDictionary.Register("ActumPartialJointHalfTourChauffeurModel", new ActumPartialJointHalfTourChauffeurModel());
			Global.ChoiceModelDictionary.Register("ActumPartialJointHalfTourParticipationModel", new ActumPartialJointHalfTourParticipationModel());
			Global.ChoiceModelDictionary.Register("ActumPersonDayPatternModel", new ActumPersonDayPatternModel());
			Global.ChoiceModelDictionary.Register("ActumPersonDayPatternTypeModel", new ActumPersonDayPatternTypeModel());
			Global.ChoiceModelDictionary.Register("ActumPersonTourGenerationModel", new ActumPersonTourGenerationModel());
			Global.ChoiceModelDictionary.Register("ActumPrimaryPriorityTimeModel", new ActumPrimaryPriorityTimeModel());
			Global.ChoiceModelDictionary.Register("ActumPrimaryPriorityTimeScheduleModel", new ActumPrimaryPriorityTimeScheduleModel());
			Global.ChoiceModelDictionary.Register("ActumSchoolTourModeModel", new ActumSchoolTourModeModel());
			Global.ChoiceModelDictionary.Register("ActumSchoolTourTimeModel", new ActumSchoolTourTimeModel());
			Global.ChoiceModelDictionary.Register("ActumTourDestinationModel", new ActumTourDestinationModel());
			Global.ChoiceModelDictionary.Register("ActumTourModeTimeModel", new ActumTourModeTimeModel());
			Global.ChoiceModelDictionary.Register("ActumTripModeModel", new ActumTripModeModel());
			Global.ChoiceModelDictionary.Register("ActumTripTimeModel", new ActumTripTimeModel());
			Global.ChoiceModelDictionary.Register("ActumWorkAtHomeModel", new ActumWorkAtHomeModel());
			Global.ChoiceModelDictionary.Register("ActumWorkBasedSubtourGenerationModel", new ActumWorkBasedSubtourGenerationModel());
			Global.ChoiceModelDictionary.Register("ActumWorkBasedSubtourModeModel", new ActumWorkBasedSubtourModeModel());
			Global.ChoiceModelDictionary.Register("ActumWorkBasedSubtourTimeModel", new ActumWorkBasedSubtourTimeModel());
			Global.ChoiceModelDictionary.Register("ActumWorkTourModeModel", new ActumWorkTourModeModel());
			Global.ChoiceModelDictionary.Register("ActumWorkTourTimeModel", new ActumWorkTourTimeModel());
		}


		private static void InitializeOutput() {
			if (Start == -1 || End == -1 || Index == -1) {
				return;
			}
			
			Global.Configuration.OutputHouseholdPath = Global.GetOutputPath(Global.Configuration.OutputHouseholdPath).ToIndexedPath(Index);
			Global.Configuration.OutputPersonPath = Global.GetOutputPath(Global.Configuration.OutputPersonPath).ToIndexedPath(Index);
			Global.Configuration.OutputHouseholdDayPath = Global.GetOutputPath(Global.Configuration.OutputHouseholdDayPath).ToIndexedPath(Index);
			Global.Configuration.OutputJointTourPath = Global.GetOutputPath(Global.Configuration.OutputJointTourPath).ToIndexedPath(Index);
			Global.Configuration.OutputFullHalfTourPath = Global.GetOutputPath(Global.Configuration.OutputFullHalfTourPath).ToIndexedPath(Index);
			Global.Configuration.OutputPartialHalfTourPath = Global.GetOutputPath(Global.Configuration.OutputPartialHalfTourPath).ToIndexedPath(Index);
			Global.Configuration.OutputPersonDayPath = Global.GetOutputPath(Global.Configuration.OutputPersonDayPath).ToIndexedPath(Index);
			Global.Configuration.OutputTourPath = Global.GetOutputPath(Global.Configuration.OutputTourPath).ToIndexedPath(Index);
			Global.Configuration.OutputTripPath = Global.GetOutputPath(Global.Configuration.OutputTripPath).ToIndexedPath(Index);
			Global.Configuration.OutputTDMTripListPath = Global.GetOutputPath(Global.Configuration.OutputTDMTripListPath).ToIndexedPath(Index);
		}
		
		private static void InitializeInput() {
			if (string.IsNullOrEmpty(Global.Configuration.InputParkAndRideNodePath)) {
				Global.Configuration.InputParkAndRideNodePath = Global.DefaultInputParkAndRideNodePath;
				Global.Configuration.InputParkAndRideNodeDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputParcelNodePath)) {
				Global.Configuration.InputParcelNodePath = Global.DefaultInputParcelNodePath;
				Global.Configuration.InputParcelNodeDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputParcelPath)) {
				Global.Configuration.InputParcelPath = Global.DefaultInputParcelPath;
				Global.Configuration.InputParcelDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputParcelPath)) {
				Global.Configuration.InputParcelPath = Global.DefaultInputParcelPath;
				Global.Configuration.InputParcelDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputZonePath)) {
				Global.Configuration.InputZonePath = Global.DefaultInputZonePath;
				Global.Configuration.InputZoneDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputHouseholdPath)) {
				Global.Configuration.InputHouseholdPath = Global.DefaultInputHouseholdPath;
				Global.Configuration.InputHouseholdDelimiter = '\t';
			}
			
			if (string.IsNullOrEmpty(Global.Configuration.InputPersonPath)) {
				Global.Configuration.InputPersonPath = Global.DefaultInputPersonPath;
				Global.Configuration.InputPersonDelimiter = '\t';
			}
			
			if (string.IsNullOrEmpty(Global.Configuration.InputHouseholdDayPath)) {
				Global.Configuration.InputHouseholdDayPath = Global.DefaultInputHouseholdDayPath;
				Global.Configuration.InputHouseholdDayDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputJointTourPath)) {
				Global.Configuration.InputJointTourPath = Global.DefaultInputJointTourPath;
				Global.Configuration.InputJointTourDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputFullHalfTourPath)) {
				Global.Configuration.InputFullHalfTourPath = Global.DefaultInputFullHalfTourPath;
				Global.Configuration.InputFullHalfTourDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputPartialHalfTourPath)) {
				Global.Configuration.InputPartialHalfTourPath = Global.DefaultInputPartialHalfTourPath;
				Global.Configuration.InputPartialHalfTourDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputPersonDayPath)) {
				Global.Configuration.InputPersonDayPath = Global.DefaultInputPersonDayPath;
				Global.Configuration.InputPersonDayDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputTourPath)) {
				Global.Configuration.InputTourPath = Global.DefaultInputTourPath;
				Global.Configuration.InputTourDelimiter = '\t';
			}

			if (string.IsNullOrEmpty(Global.Configuration.InputTripPath)) {
				Global.Configuration.InputTripPath = Global.DefaultInputTripPath;
				Global.Configuration.InputTripDelimiter = '\t';
			}

			var inputParkAndRideNodeFile = Global.ParkAndRideNodeIsEnabled ? Global.GetInputPath(Global.Configuration.InputParkAndRideNodePath).ToFile() : null;
			var inputParcelNodeFile = Global.ParcelNodeIsEnabled ? Global.GetInputPath(Global.Configuration.InputParcelNodePath).ToFile() : null;
			var inputParcelFile = Global.GetInputPath(Global.Configuration.InputParcelPath).ToFile();
			var inputZoneFile = Global.GetInputPath(Global.Configuration.InputZonePath).ToFile();
			var inputHouseholdFile = Global.GetInputPath(Global.Configuration.InputHouseholdPath).ToFile();
			var inputPersonFile = Global.GetInputPath(Global.Configuration.InputPersonPath).ToFile();
			var inputHouseholdDayFile = Global.GetInputPath(Global.Configuration.InputHouseholdDayPath).ToFile();
			var inputJointTourFile = Global.GetInputPath(Global.Configuration.InputJointTourPath).ToFile();
			var inputFullHalfTourFile = Global.GetInputPath(Global.Configuration.InputFullHalfTourPath).ToFile();
			var inputPartialHalfTourFile = Global.GetInputPath(Global.Configuration.InputPartialHalfTourPath).ToFile();
			var inputPersonDayFile = Global.GetInputPath(Global.Configuration.InputPersonDayPath).ToFile();
			var inputTourFile = Global.GetInputPath(Global.Configuration.InputTourPath).ToFile();
			var inputTripFile = Global.GetInputPath(Global.Configuration.InputTripPath).ToFile();

			InitializeInputDirectories(inputParkAndRideNodeFile, inputParcelNodeFile, inputParcelFile, inputZoneFile, inputHouseholdFile, inputPersonFile, inputHouseholdDayFile, inputJointTourFile, inputFullHalfTourFile, inputPartialHalfTourFile, inputPersonDayFile, inputTourFile, inputTripFile);

			if (Global.PrintFile == null) {
				return;
			}

			Global.PrintFile.WriteLine("Input files:");
			Global.PrintFile.IncrementIndent();

			Global.PrintFile.WriteFileInfo(inputParkAndRideNodeFile, "Park-and-ride node is not enabled, input park-and-ride node file not set.");
			Global.PrintFile.WriteFileInfo(inputParcelNodeFile, "Parcel node is not enabled, input parcel node file not set.");
			Global.PrintFile.WriteFileInfo(inputParcelFile);
			Global.PrintFile.WriteFileInfo(inputZoneFile);
			Global.PrintFile.WriteFileInfo(inputHouseholdFile);
			Global.PrintFile.WriteFileInfo(inputPersonFile);

			if (Global.Configuration.IsInEstimationMode && !Global.Configuration.ShouldRunRawConversion) {
				Global.PrintFile.WriteFileInfo(inputHouseholdDayFile);
				Global.PrintFile.WriteFileInfo(inputJointTourFile);
				Global.PrintFile.WriteFileInfo(inputFullHalfTourFile);
				Global.PrintFile.WriteFileInfo(inputPartialHalfTourFile);
				Global.PrintFile.WriteFileInfo(inputPersonDayFile);
				Global.PrintFile.WriteFileInfo(inputTourFile);
				Global.PrintFile.WriteFileInfo(inputTripFile);
			}

			Global.PrintFile.DecrementIndent();
		}

		private static void InitializeInputDirectories(FileInfo inputParkAndRideNodeFile, FileInfo inputParcelNodeFile, FileInfo inputParcelFile, FileInfo inputZoneFile, FileInfo inputHouseholdFile, FileInfo inputPersonFile, FileInfo inputHouseholdDayFile, FileInfo inputJointTourFile, FileInfo inputFullHalfTourFile, FileInfo inputPartialHalfTourFile, FileInfo inputPersonDayFile, FileInfo inputTourFile, FileInfo inputTripFile) {
			if (inputParkAndRideNodeFile != null) {
				inputParkAndRideNodeFile.CreateDirectory();
			}

			if (inputParcelNodeFile != null) {
				inputParcelNodeFile.CreateDirectory();
			}

			inputParcelFile.CreateDirectory();
			inputZoneFile.CreateDirectory();
			
			inputHouseholdFile.CreateDirectory();
			Global.GetOutputPath(Global.Configuration.OutputHouseholdPath).CreateDirectory();
			
			inputPersonFile.CreateDirectory();
			Global.GetOutputPath(Global.Configuration.OutputPersonPath).CreateDirectory();
			
			var override1 = (inputParkAndRideNodeFile != null && !inputParkAndRideNodeFile.Exists) || (inputParcelNodeFile != null && !inputParcelNodeFile.Exists) || !inputParcelFile.Exists || !inputZoneFile.Exists || !inputHouseholdFile.Exists || !inputPersonFile.Exists;
			var override2 = false;

			if (Global.Configuration.IsInEstimationMode) {
				inputHouseholdDayFile.CreateDirectory();
				Global.GetOutputPath(Global.Configuration.OutputHouseholdDayPath).CreateDirectory();
				

				if (Global.UseJointTours)
				{
					inputJointTourFile.CreateDirectory();
					Global.GetOutputPath(Global.Configuration.OutputJointTourPath).CreateDirectory();

					inputFullHalfTourFile.CreateDirectory();
					Global.GetOutputPath(Global.Configuration.OutputFullHalfTourPath).CreateDirectory();

					inputPartialHalfTourFile.CreateDirectory();
					Global.GetOutputPath(Global.Configuration.OutputPartialHalfTourPath).CreateDirectory();
				}

				inputPersonDayFile.CreateDirectory();
				Global.GetOutputPath(Global.Configuration.OutputPersonDayPath).CreateDirectory();
				
				inputTourFile.CreateDirectory();
				Global.GetOutputPath(Global.Configuration.OutputTourPath).CreateDirectory();
				
				inputTripFile.CreateDirectory();
				Global.GetOutputPath(Global.Configuration.OutputTripPath).CreateDirectory();

				override2 = !inputHouseholdDayFile.Exists || !inputJointTourFile.Exists || !inputFullHalfTourFile.Exists || !inputPartialHalfTourFile.Exists || !inputPersonDayFile.Exists || !inputTourFile.Exists || !inputTripFile.Exists;
			}

			if (override1 || override2) {
				OverrideShouldRunRawConversion();
			}
		}
		
		private static void InitializeWorking() {
			var workingParkAndRideNodeFile = Global.ParkAndRideNodeIsEnabled ? Global.WorkingParkAndRideNodePath.ToFile() : null;
			var workingParcelNodeFile = Global.ParcelNodeIsEnabled ? Global.WorkingParcelNodePath.ToFile() : null;
			var workingParcelFile = Global.WorkingParcelPath.ToFile();
			var workingZoneFile = Global.WorkingZonePath.ToFile();
			var workingHouseholdFile = Global.WorkingHouseholdPath.ToFile();
			var workingHouseholdDayFile = Global.WorkingHouseholdDayPath.ToFile();
			var workingJointTourFile = Global.WorkingJointTourPath.ToFile();
			var workingFullHalfTourFile = Global.WorkingFullHalfTourPath.ToFile();
			var workingPartialHalfTourFile = Global.WorkingPartialHalfTourPath.ToFile();
			var workingPersonFile = Global.WorkingPersonPath.ToFile();
			var workingPersonDayFile = Global.WorkingPersonDayPath.ToFile();
			var workingTourFile = Global.WorkingTourPath.ToFile();
			var workingTripFile = Global.WorkingTripPath.ToFile();
			
			InitializeWorkingDirectory();

			InitializeWorkingImports(workingParkAndRideNodeFile, workingParcelNodeFile, workingParcelFile, workingZoneFile, workingHouseholdFile, workingPersonFile, workingHouseholdDayFile, workingJointTourFile, workingFullHalfTourFile, workingPartialHalfTourFile, workingPersonDayFile, workingTourFile, workingTripFile);

			if (Global.PrintFile == null) {
				return;
			}

			Global.PrintFile.WriteLine("Working files:");
			Global.PrintFile.IncrementIndent();

			Global.PrintFile.WriteFileInfo(workingParkAndRideNodeFile, "Park-and-ride node is not enabled, working park-and-ride node file not set.");
			Global.PrintFile.WriteFileInfo(workingParcelNodeFile, "Parcel node is not enabled, working parcel node file not set.");
			Global.PrintFile.WriteFileInfo(workingParcelFile);
			Global.PrintFile.WriteFileInfo(workingZoneFile);
			Global.PrintFile.WriteFileInfo(workingHouseholdFile);
			Global.PrintFile.WriteFileInfo(workingPersonFile);
			Global.PrintFile.WriteFileInfo(workingHouseholdDayFile);
			Global.PrintFile.WriteFileInfo(workingJointTourFile);
			Global.PrintFile.WriteFileInfo(workingFullHalfTourFile);
			Global.PrintFile.WriteFileInfo(workingPartialHalfTourFile);
			Global.PrintFile.WriteFileInfo(workingPersonDayFile);
			Global.PrintFile.WriteFileInfo(workingTourFile);
			Global.PrintFile.WriteFileInfo(workingTripFile);

			Global.PrintFile.DecrementIndent();
		}

		private static void InitializeWorkingDirectory() {
			var workingDirectory = new DirectoryInfo(Global.GetWorkingPath(""));
			
			if (Global.PrintFile != null) {
				Global.PrintFile.WriteLine("Working directory: {0}", workingDirectory);
			}

			if (workingDirectory.Exists) {
				return;
			}

			workingDirectory.CreateDirectory();
			OverrideShouldRunRawConversion();
		}

		private static void InitializeWorkingImports(FileInfo workingParkAndRideNodeFile, FileInfo workingParcelNodeFile, FileInfo workingParcelFile, FileInfo workingZoneFile, FileInfo workingHouseholdFile, FileInfo workingPersonFile, FileInfo workingHouseholdDayFile, FileInfo workingJointTourFile, FileInfo workingFullHalfTourFile, FileInfo workingPartialHalfTourFile, FileInfo workingPersonDayFile, FileInfo workingTourFile, FileInfo workingTripFile) {
			if (Global.Configuration.ShouldRunRawConversion || (workingParkAndRideNodeFile != null && !workingParkAndRideNodeFile.Exists) || (workingParcelNodeFile != null && !workingParcelNodeFile.Exists) || !workingParcelFile.Exists || !workingZoneFile.Exists || !workingHouseholdFile.Exists || !workingPersonFile.Exists) {
				if (workingParkAndRideNodeFile != null) {
					OverrideImport(Global.Configuration, x => x.ImportParkAndRideNodes);
				}

				if (workingParcelNodeFile != null) {
					OverrideImport(Global.Configuration, x => x.ImportParcelNodes);
				}

				OverrideImport(Global.Configuration, x => x.ImportParcels);
				OverrideImport(Global.Configuration, x => x.ImportZones);
				OverrideImport(Global.Configuration, x => x.ImportTransitStopAreas);
				OverrideImport(Global.Configuration, x => x.ImportHouseholds);
				OverrideImport(Global.Configuration, x => x.ImportPersons);
			}

			if (!Global.Configuration.IsInEstimationMode || (!Global.Configuration.ShouldRunRawConversion && workingHouseholdDayFile.Exists && workingJointTourFile.Exists && workingFullHalfTourFile.Exists && workingPartialHalfTourFile.Exists && workingPersonDayFile.Exists && workingTourFile.Exists && workingTripFile.Exists)) {
				return;
			}

			OverrideImport(Global.Configuration, x => x.ImportHouseholdDays);
			OverrideImport(Global.Configuration, x => x.ImportJointTours);
			OverrideImport(Global.Configuration, x => x.ImportFullHalfTours);
			OverrideImport(Global.Configuration, x => x.ImportPartialHalfTours);
			OverrideImport(Global.Configuration, x => x.ImportPersonDays);
			OverrideImport(Global.Configuration, x => x.ImportTours);
			OverrideImport(Global.Configuration, x => x.ImportTrips);
		}
		
		public static void BeginRunRawConversion() {
			if (!Global.Configuration.ShouldRunRawConversion) {
				return;
			}

			var timer = new Timer("Running raw conversion...");

			if (Global.PrintFile != null) {
				Global.PrintFile.IncrementIndent();
			}

			RawConverter.Run();

			if (Global.PrintFile != null) {
				Global.PrintFile.DecrementIndent();
			}

			timer.Stop();
		}

		public static void BeginImportData() {
			var importerFactory = Global.Kernel.Get<ImporterFactory>();

			ImportParkAndRideNodes(importerFactory);
			ImportParcelNodes(importerFactory);
			ImportParcels(importerFactory);
			ImportZones(importerFactory);
			ImportTransitStopAreas(importerFactory);
			ImportHouseholds(importerFactory);
			ImportPersons(importerFactory);

			if (!Global.Configuration.IsInEstimationMode) {
				return;
			}

			ImportHouseholdDays(importerFactory);
			if (Global.UseJointTours)
			{
				ImportJointTours(importerFactory);
				ImportFullHalfTours(importerFactory);
				ImportPartialHalfTours(importerFactory);
			}
			ImportPersonDays(importerFactory);
			ImportTours(importerFactory);
			ImportTrips(importerFactory);
		}

		private static void ImportParkAndRideNodes(ImporterFactory importerFactory) {
			if (!Global.ParkAndRideNodeIsEnabled || !Global.Configuration.ImportParkAndRideNodes) {
				return;
			}

			var parkAndRideNodeImporter = importerFactory.GetImporter<ParkAndRideNode>(Global.GetInputPath(Global.Configuration.InputParkAndRideNodePath), Global.Configuration.InputParkAndRideNodeDelimiter);

			parkAndRideNodeImporter.BeginImport(Global.WorkingParkAndRideNodePath, "Importing park-and-ride nodes...");
		}

		private static void ImportParcelNodes(ImporterFactory importerFactory) {
			if (!Global.ParcelNodeIsEnabled || !Global.Configuration.ImportParcelNodes) {
				return;
			}

			var parcelNodeImporter = importerFactory.GetImporter<ParcelNode>(Global.GetInputPath(Global.Configuration.InputParcelNodePath), Global.Configuration.InputParcelNodeDelimiter);

			parcelNodeImporter.BeginImport(Global.WorkingParcelNodePath, "Importing parcel nodes...");
		}

		private static void ImportParcels(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportParcels) {
				return;
			}

			var parcelImporter = importerFactory.GetImporter<Parcel>(Global.GetInputPath(Global.Configuration.InputParcelPath), Global.Configuration.InputParcelDelimiter);

			parcelImporter.BeginImport(Global.WorkingParcelPath, "Importing parcels...");
		}

		private static void ImportZones(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportZones) {
				return;
			}

			var zoneImporter = importerFactory.GetImporter<Zone>(Global.GetInputPath(Global.Configuration.InputZonePath), Global.Configuration.InputZoneDelimiter);

			zoneImporter.BeginImport(Global.WorkingZonePath, "Importing zones...");
		}

		private static void ImportTransitStopAreas(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportTransitStopAreas) {
				return;
			}

			if (String.IsNullOrEmpty(Global.WorkingTransitStopAreaPath) || String.IsNullOrEmpty(Global.Configuration.InputTransitStopAreaPath))
				return;

			var transitStopAreaImporter = importerFactory.GetImporter<TransitStopArea>(Global.GetInputPath(Global.Configuration.InputTransitStopAreaPath), Global.Configuration.InputTransitStopAreaDelimiter);

			transitStopAreaImporter.BeginImport(Global.WorkingTransitStopAreaPath, "Importing transit stop areas...");
		}

		private static void ImportHouseholds(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportHouseholds) {
				return;
			}

			Global.Kernel.Get<HouseholdPersistenceFactory>().HouseholdPersister.BeginImport(importerFactory, Global.WorkingHouseholdPath, "Importing households...");
		}

		private static void ImportPersons(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportPersons) {
				return;
			}

			Global.Kernel.Get<PersonPersistenceFactory>().PersonPersister.BeginImport(importerFactory, Global.WorkingPersonPath, "Importing persons...");
		}

		private static void ImportHouseholdDays(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportHouseholdDays) {
				return;
			}

			Global.Kernel.Get<HouseholdDayPersistenceFactory>().HouseholdDayPersister.BeginImport(importerFactory, Global.WorkingHouseholdDayPath, "Importing household days...");
		}

		private static void ImportJointTours(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportJointTours) {
				return;
			}

			var jointTourImporter = importerFactory.GetImporter<JointTour>(Global.GetInputPath(Global.Configuration.InputJointTourPath), Global.Configuration.InputJointTourDelimiter);

			jointTourImporter.BeginImport(Global.WorkingJointTourPath, "Importing joint tours...");
		}

		private static void ImportFullHalfTours(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportFullHalfTours) {
				return;
			}

			var fullHalfTourImporter = importerFactory.GetImporter<FullHalfTour>(Global.GetInputPath(Global.Configuration.InputFullHalfTourPath), Global.Configuration.InputFullHalfTourDelimiter);

			fullHalfTourImporter.BeginImport(Global.WorkingFullHalfTourPath, "Importing full half-tours...");
		}

		private static void ImportPartialHalfTours(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportPartialHalfTours) {
				return;
			}

			var partialHalfTourImporter = importerFactory.GetImporter<PartialHalfTour>(Global.GetInputPath(Global.Configuration.InputPartialHalfTourPath), Global.Configuration.InputPartialHalfTourDelimiter);

			partialHalfTourImporter.BeginImport(Global.WorkingPartialHalfTourPath, "Importing partial half-tours...");
		}

		private static void ImportPersonDays(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportPersonDays) {
				return;
			}

			Global.Kernel.Get<PersonDayPersistenceFactory>().PersonDayPersister.BeginImport(importerFactory, Global.WorkingPersonDayPath, "Importing person days...");
		}

		private static void ImportTours(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportTours) {
				return;
			}

			Global.Kernel.Get<TourPersistenceFactory>().TourPersister.BeginImport(importerFactory, Global.WorkingTourPath, "Importing tours...");
		}

		private static void ImportTrips(ImporterFactory importerFactory) {
			if (!Global.Configuration.ImportTrips) {
				return;
			}

			Global.Kernel.Get<TripPersistenceFactory>().TripPersister.BeginImport(importerFactory, Global.WorkingTripPath, "Importing trips...");
		}
		
		public static void BeginBuildIndexes() {
			var timer = new Timer("Building indexes...");

			BuildIndexes();

			timer.Stop();
		}

		private static void BuildIndexes() {
			if (Global.ParcelNodeIsEnabled)
			{
				var parcelNodeReader = Global.Kernel.Get<Reader<ParcelNode>>();
				parcelNodeReader.BuildIndex("parcel_fk", "Id", "NodeId");
			}

			Global.Kernel.Get<PersonPersistenceFactory>().PersonPersister.BuildIndex("household_fk", "Id", "HouseholdId");
			
			if (!Global.Configuration.IsInEstimationMode) {
				return;
			}

			Global.Kernel.Get<HouseholdDayPersistenceFactory>().HouseholdDayPersister.BuildIndex("household_fk", "Id", "HouseholdId");

			if (Global.Configuration.UseJointTours) {
				var jointTourReader = Global.Kernel.Get<Reader<JointTour>>();
				jointTourReader.BuildIndex("household_day_fk", "Id", "HouseholdDayId");

				var fullHalfTourReader = Global.Kernel.Get<Reader<FullHalfTour>>();
				fullHalfTourReader.BuildIndex("household_day_fk", "Id", "HouseholdDayId");

				var partialHalfTourReader = Global.Kernel.Get<Reader<PartialHalfTour>>();
				partialHalfTourReader.BuildIndex("household_day_fk", "Id", "HouseholdDayId");
			}

			Global.Kernel.Get<PersonDayPersistenceFactory>().PersonDayPersister.BuildIndex("household_day_fk", "Id", "HouseholdDayId");

			Global.Kernel.Get<TourPersistenceFactory>().TourPersister.BuildIndex("person_day_fk", "Id", "PersonDayId");

			Global.Kernel.Get<TripPersistenceFactory>().TripPersister.BuildIndex("tour_fk", "Id", "TourId");
		}

		private static void BeginLoadNodeIndex() {
			if (!Global.ParcelNodeIsEnabled || !Global.Configuration.UseShortDistanceNodeToNodeMeasures) {
				return;
			}
			
			var timer = new Timer("Loading node index...");

			LoadNodeIndex();

			timer.Stop();
		}

		private static void LoadNodeIndex() {
			var file = new FileInfo(Global.GetInputPath(Global.Configuration.NodeIndexPath));

			var aNodeId = new List<int>();
			var aNodeFirstRecord = new List<int>();
			var aNodeLastRecord = new List<int>();

			using (var reader = new StreamReader(file.OpenRead())) {
				reader.ReadLine();

				string line;

				while ((line = reader.ReadLine()) != null) {
					var tokens = line.Split(Global.Configuration.NodeIndexDelimiter);

					aNodeId.Add(int.Parse(tokens[0]));
					aNodeFirstRecord.Add(int.Parse(tokens[1]));
					aNodeLastRecord.Add(int.Parse(tokens[2]));
				}
			}

			Global.ANodeId = aNodeId.ToArray();
			Global.ANodeFirstRecord = aNodeFirstRecord.ToArray();
			Global.ANodeLastRecord = aNodeLastRecord.ToArray();
		}

		private static void BeginLoadNodeDistances() {
			if (!Global.ParcelNodeIsEnabled || !Global.Configuration.UseShortDistanceNodeToNodeMeasures) {
				return;
			}

			ICondensedParcelExtensions.InitializeIndex();

			var timer = new Timer("Loading node distances...");

			if (Global.Configuration.NodeDistancesPath.Contains(".dat"))
				LoadNodeDistancesFromBinary();
			else
				LoadNodeDistancesFromHDF5();

			timer.Stop();
		}

		private static void LoadNodeDistancesFromBinary() {
			var file = new FileInfo(Global.GetInputPath(Global.Configuration.NodeDistancesPath));

			using (var reader = new BinaryReader(file.OpenRead())) {
				Global.NodePairBNodeId = new int[file.Length / 8];
				Global.NodePairDistance = new ushort[file.Length / 8];

				var i = 0;
				var length = reader.BaseStream.Length;
				while (reader.BaseStream.Position < length) {
					Global.NodePairBNodeId[i] = reader.ReadInt32();

					var distance = reader.ReadInt32();

					Global.NodePairDistance[i] = (ushort) Math.Min(distance, ushort.MaxValue);

					i++;
				}
			}
		}

		private static void LoadNodeDistancesFromHDF5()
		{
			var file = H5F.open(Global.GetInputPath(Global.Configuration.NodeDistancesPath),
			                    H5F.OpenMode.ACC_RDONLY);

			// Read nodes
			var nodes = H5D.open(file, "node");
			var dspace = H5D.getSpace(nodes);

			long numNodes = H5S.getSimpleExtentDims(dspace)[0];
			Global.NodePairBNodeId = new int[numNodes];
			H5Array<int> wrapArray = new H5Array<int>(Global.NodePairBNodeId);

			H5DataTypeId dataType = new H5DataTypeId(H5T.H5Type.NATIVE_INT);

			H5D.read(nodes, dataType, wrapArray);

			H5S.close(dspace);
			H5D.close(nodes);

			// Read distances
			var dist = H5D.open(file, "distance");
			dspace = H5D.getSpace(nodes);

			Global.NodePairDistance = new ushort[numNodes];
			H5Array<ushort> distArray = new H5Array<ushort>(Global.NodePairDistance);

			dataType = new H5DataTypeId(H5T.H5Type.NATIVE_SHORT);

			H5D.read(nodes, dataType, distArray);

			H5S.close(dspace);
			H5D.close(dist);

			// All done
			H5F.close(file);
		}

		private static void BeginLoadRoster() {
			var timer = new Timer("Loading roster...");

			LoadRoster();

			timer.Stop();
		}

		private static void LoadRoster() {
			var zoneReader = Global.Kernel.Get<Reader<Zone>>();
			var zoneMapping = new Dictionary<int, int>(zoneReader.Count);

			foreach (var zone in zoneReader) {
				zoneMapping.Add(zone.Key, zone.Id);
			}

			var transitStopAreaReader = Global.Kernel.Get<Reader<Zone>>();
			var transitStopAreaMapping = new Dictionary<int, int>(zoneReader.Count);

			foreach (var transitStopArea in transitStopAreaReader) {
				transitStopAreaMapping.Add(transitStopArea.Key, transitStopArea.Id);
			}

			ImpedanceRoster.Initialize(zoneMapping, transitStopAreaMapping);
		}

		private static void BeginCalculateAggregateLogsums(RandomUtility randomUtility) {
			var timer = new Timer("Calculating aggregate logsums...");

			AggregateLogsumsCalculator.Calculate(randomUtility);

			timer.Stop();
		}

		private static void BeginOutputAggregateLogsums() {
			if (!Global.Configuration.ShouldOutputAggregateLogsums) {
				return;
			}

			var timer = new Timer("Outputting aggregate logsums...");

			AggregateLogsumsExporter.Export(Global.GetOutputPath(Global.Configuration.OutputAggregateLogsumsPath));

			timer.Stop();
		}

		private static void BeginCalculateSamplingWeights() {
			var timer = new Timer("Calculating sampling weights...");

			SamplingWeightsCalculator.Calculate("ivtime", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, 180);

			timer.Stop();
		}

		private static void BeginOutputSamplingWeights() {
			if (!Global.Configuration.ShouldOutputSamplingWeights) {
				return;
			}

			var timer = new Timer("Outputting sampling weights...");

			SamplingWeightsExporter.Export(Global.GetOutputPath(Global.Configuration.OutputSamplingWeightsPath));

			timer.Stop();
		}

		private static void BeginRunChoiceModels(RandomUtility randomUtility) {
			if (!Global.Configuration.ShouldRunChoiceModels) {
				return;
			}

			var timer = new Timer("Running choice models...");

			RunChoiceModels(randomUtility);

			timer.Stop();
		}

		private static void RunChoiceModels(RandomUtility randomUtility)
		{
			var index = 0;
			var current = 0;
			var total = Global.Kernel.Get<HouseholdPersistenceFactory>().HouseholdPersister.Count;

			if (Global.Configuration.HouseholdSamplingRateOneInX < 1) {
				Global.Configuration.HouseholdSamplingRateOneInX = 1;
			}

			ChoiceModelFactory.Initialize(Global.ChoiceModelRunner);

			int nBatches = ParallelUtility.NBatches;
			int count = Global.Kernel.Get<HouseholdPersistenceFactory>().HouseholdPersister.Count;
			int batchSize = count/nBatches;
			Dictionary<int, int> randoms = new Dictionary<int, int>();

			List<IHousehold>[] batches = new List<IHousehold>[nBatches];

			for(int i = 0; i < nBatches; i++)
			{
				batches[i] = new List<IHousehold>();
			}

			int j = 0;
			foreach (IHousehold household in Global.Kernel.Get<HouseholdPersistenceFactory>().HouseholdPersister)
			{
				randoms[household.Id] = randomUtility.GetNext();
				int i = j/batchSize;
				if ( i >= nBatches )
					i = nBatches - 1;

				batches[i].Add(household);

				j++;
			}


			Parallel.For(0, nBatches,
			             new ParallelOptions {MaxDegreeOfParallelism = Global.LargeDegreeOfParallelism},
			             batchNumber =>
				             {
											 ParallelUtility.Register(System.Threading.Thread.CurrentThread.ManagedThreadId, batchNumber);
					             foreach (IHousehold household in batches[batchNumber])
					             {
						             if ((household.Id%Global.Configuration.HouseholdSamplingRateOneInX == (Global.Configuration.HouseholdSamplingStartWithY - 1)))
						             {
#if RELEASE
					try {
#endif
							             if (Start == -1 || End == -1 || Index == -1 || index++.IsBetween(Start, End))
							             {
								             int randomSeed = randoms[household.Id];
								             var choiceModelRunner = ChoiceModelFactory.Get(household, randomSeed);

								             choiceModelRunner.RunChoiceModels();
							             }
#if RELEASE
					}
					catch (Exception e) {
						throw new ChoiceModelRunnerException(string.Format("An error occurred in ChoiceModelRunner for household {0}.", household.Id), e);
					}
#endif
						             }

						             if (!Global.Configuration.ShowRunChoiceModelsStatus)
						             {
							             continue;
						             }

						             current++;

						             if (current != 1 && current != total && current%1000 != 0)
						             {
							             continue;
						             }

						             int countf = ChoiceModelFactory.TotalHouseholdDays>0 ? ChoiceModelFactory.TotalHouseholdDays : ChoiceModelFactory.TotalPersonDays;
										 string countStringf = ChoiceModelFactory.TotalHouseholdDays>0 ? "Household" : "Person";

										 Console.Write("\r{0:p} {1}", (double) current/total,
						                           ChoiceModelFactory.TotalInvalidAttempts == 0 
							                           ? null
							                           : Global.Configuration.ReportInvalidPersonDays
																						? string.Format("Total {0} Days: {1}, Total Invalid Attempts: {2}",
							                                           countStringf,countf,
							                                           ChoiceModelFactory.TotalInvalidAttempts)
																						: string.Format("Total {0} Days: {1}",
							                                           countStringf,countf));


					             }
					             
				             });
				             int countg = ChoiceModelFactory.TotalHouseholdDays>0 ? ChoiceModelFactory.TotalHouseholdDays : ChoiceModelFactory.TotalPersonDays;
								 string countStringg = ChoiceModelFactory.TotalHouseholdDays>0 ? "Household" : "Person";
				             Console.Write("\r{0:p} {1}", (double) 1.0,
								                           Global.Configuration.ReportInvalidPersonDays
																						? string.Format("Total {0} Days: {1}, Total Invalid Attempts: {2}",
							                                           countStringg,countg,
							                                           ChoiceModelFactory.TotalInvalidAttempts)
																						: string.Format("Total {0} Days: {1}",
							                                           countStringg,countg));
				             Console.WriteLine();

		}

		private static void BeginPerformHousekeeping() {
			if (!Global.Configuration.ShouldRunChoiceModels) {
				return;
			}
			var timer = new Timer("Performing housekeeping...");

			PerformHousekeeping();

			timer.Stop();
		}

		private static void PerformHousekeeping() {
			ChoiceProbabilityCalculator.Close();

            ChoiceModelHelper.WriteTimesModelsRun();
            ChoiceModelFactory.WriteCounters();
			ChoiceModelFactory.SignalShutdown();

			
			if (Global.Configuration.ShouldOutputTDMTripList) {
				ChoiceModelFactory.TDMTripListExporter.Dispose();
			}
		}

		public static void BeginUpdateShadowPricing() {
			if (!Global.Configuration.ShouldRunChoiceModels) {
				return;
			}

			var timer = new Timer("Updating shadow pricing...");

			ShadowPriceCalculator.CalculateAndWriteShadowPrices();
			ParkAndRideShadowPriceCalculator.CalculateAndWriteShadowPrices();

			timer.Stop();
		}

		private static void OverrideShouldRunRawConversion() {
			if (Global.Configuration.ShouldRunRawConversion) {
				return;
			}

			Global.Configuration.ShouldRunRawConversion = true;

			if (Global.PrintFile != null) {
				Global.PrintFile.WriteLine("ShouldRunRawConversion in the configuration file has been overridden, a raw conversion is required.");
			}
		}

		private static void OverrideImport(Configuration configuration, Expression<Func<Configuration, bool>> expression) {
			var body = (MemberExpression) expression.Body;
			var property = (PropertyInfo) body.Member;
			var value = (bool) property.GetValue(configuration, null);

			if (value) {
				return;
			}

			property.SetValue(configuration, true, null);

			if (Global.PrintFile != null) {
				Global.PrintFile.WriteLine("{0} in the configuration file has been overridden, an import is required.", property.Name);
			}
		}
	}
}