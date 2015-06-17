// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System.IO;
using Daysim.Framework.Sampling;
using Ninject;

namespace Daysim.Framework.Core {
	public static class Global {
		public static IKernel Kernel { get; set; }

		public static Configuration Configuration { get; set; }

		public static ChoiceModelDictionary ChoiceModelDictionary { get; set; }

		public static PrintFile PrintFile { get; set; }

        public static bool TraceResults { get; set; }

        public static int[] ANodeId { get; set; }

		public static int[] ANodeFirstRecord { get; set; }

		public static int[] ANodeLastRecord { get; set; }

		public static int[] NodePairBNodeId { get; set; }

		public static ushort[] NodePairDistance { get; set; }

		public static int NodeNodePreviousOriginParcelId { get; set; }

		public static int NodeNodePreviousDestinationParcelId { get; set; }

		public static double NodeNodePreviousDistance { get; set; }

		public static double[][][][][] AggregateLogsums { get; set; }

		public static SegmentZone[][] SegmentZones { get; set; }

		public static bool ParkAndRideNodeIsEnabled {
			get { return !string.IsNullOrEmpty(Configuration.RawParkAndRideNodePath) && !string.IsNullOrEmpty(Configuration.InputParkAndRideNodePath); }
		}

		public static string DefaultInputParkAndRideNodePath
		{
			get { return GetWorkingPath("park_and_ride_node.tsv"); }
		}

		public static bool ParcelNodeIsEnabled {
			get { return !string.IsNullOrEmpty(Configuration.RawParcelNodePath) && !string.IsNullOrEmpty(Configuration.InputParcelNodePath); }
		}

		public static string DefaultInputParcelNodePath {
			get { return GetWorkingPath("parcel_node.tsv"); }
		}

		public static string DefaultInputParcelPath {
			get { return GetWorkingPath("parcel.tsv"); }
		}

		public static string DefaultInputZonePath {
			get { return GetWorkingPath("zone.tsv"); }
		}

		public static string DefaultInputHouseholdPath {
			get { return GetWorkingPath("household.tsv"); }
		}

		public static string DefaultInputHouseholdDayPath {
			get { return GetWorkingPath("household_day.tsv"); }
		}

		public static string DefaultInputJointTourPath {
			get { return GetWorkingPath("joint_tour.tsv"); }
		}

		public static string DefaultInputFullHalfTourPath {
			get { return GetWorkingPath("full_half_tour.tsv"); }
		}

		public static string DefaultInputPartialHalfTourPath {
			get { return GetWorkingPath("partial_half_tour.tsv"); }
		}

		public static string DefaultInputPersonPath {
			get { return GetWorkingPath("person.tsv"); }
		}
		
		public static string DefaultInputPersonDayPath {
			get { return GetWorkingPath("person_day.tsv"); }
		}

		public static string DefaultInputTourPath {
			get { return GetWorkingPath("tour.tsv"); }
		}

		public static string DefaultInputTripPath {
			get { return GetWorkingPath("trip.tsv"); }
		}

		public static string WorkingParkAndRideNodePath {
			get { return GetWorkingPath("park_and_ride_node.bin"); }
		}

		public static string WorkingParcelNodePath {
			get { return GetWorkingPath("parcel_node.bin"); }
		}

		public static string WorkingParcelPath {
			get { return GetWorkingPath("parcel.bin"); }
		}

		public static string WorkingZonePath {
			get { return GetWorkingPath("zone.bin"); }
		}

		public static string WorkingTransitStopAreaPath {
			get { return GetWorkingPath("transit_stop_area.bin"); }
		}

		public static string WorkingHouseholdPath {
			get { return GetWorkingPath("household.bin"); }
		}

		public static string WorkingHouseholdDayPath {
			get { return GetWorkingPath("household_day.bin"); }
		}

		public static string WorkingJointTourPath {
			get { return GetWorkingPath("joint_tour.bin"); }
		}

		public static string WorkingFullHalfTourPath {
			get { return GetWorkingPath("full_half_tour.bin"); }
		}

		public static string WorkingPartialHalfTourPath {
			get { return GetWorkingPath("partial_half_tour.bin"); }
		}

		public static string WorkingPersonPath {
			get { return GetWorkingPath("person.bin"); }
		}
		
		public static string WorkingPersonDayPath {
			get { return GetWorkingPath("person_day.bin"); }
		}

		public static string WorkingTourPath {
			get { return GetWorkingPath("tour.bin"); }
		}

		public static string WorkingTripPath {
			get { return GetWorkingPath("trip.bin"); }
		}
		
		public static string AggregateLogsumsPath {
			get { return GetWorkingPath("aggregate_logsums.bin"); }
		}

		public static string SamplingWeightsPath {
			get { return GetWorkingPath("sampling_weights_{0}.bin"); }
		}

		public static string ShadowPricesPath {
			get { return GetWorkingPath("shadow_prices.txt"); }
		}

		public static string ParkAndRideShadowPricesPath {
			get { return GetWorkingPath("park_and_ride_shadow_prices.txt"); }
		}

		public static string DataType{
			get
			{
				return string.IsNullOrEmpty(Configuration.DataType) ? "Default" : Configuration.DataType;
			}
		}

		public static string ChoiceModelRunner
		{
			get
			{
				return string.IsNullOrEmpty(Configuration.ChoiceModelRunner) ? "ChoiceModelRunner" : Configuration.ChoiceModelRunner;
			}
		}

		public static bool UseJointTours
		{
			get
			{
				return Configuration.UseJointTours;
			}
		}

		public static double LengthUnitsPerFoot{
			get
			{
				return Configuration.LengthUnitsPerFoot == 0 ? 1.0 : Configuration.LengthUnitsPerFoot;
			}
		}

		public static double DistanceUnitsPerMile{
			get
			{
				return Configuration.DistanceUnitsPerMile == 0 ? 1.0 : Configuration.DistanceUnitsPerMile;
			}
		}
	
		public static double MonetaryUnitsPerDollar{
			get
			{
				return Configuration.MonetaryUnitsPerDollar == 0 ? 1.0 : Configuration.MonetaryUnitsPerDollar;
			}
		}

		public static double Coefficients_BaseCostCoefficientPerMonetaryUnit{
			get
			{
				return !(Configuration.Coefficients_BaseCostCoefficientPerMonetaryUnit == 0) ? Configuration.Coefficients_BaseCostCoefficientPerMonetaryUnit : Configuration.Coefficients_BaseCostCoefficientPerDollar;
			}
		}

		public static double PathImpedance_WalkMinutesPerDistanceUnit{
			get
			{
				return !(Configuration.PathImpedance_WalkMinutesPerDistanceUnit == 0) ? Configuration.PathImpedance_WalkMinutesPerDistanceUnit : Configuration.PathImpedance_WalkMinutesPerMile;
			}
		}

		public static double PathImpedance_AutoOperatingCostPerDistanceUnit{
			get
			{
				return !(Configuration.PathImpedance_AutoOperatingCostPerDistanceUnit == 0) ? Configuration.PathImpedance_AutoOperatingCostPerDistanceUnit : Configuration.PathImpedance_AutoOperatingCostPerMile;
			}
		}

		public static char SkimDelimiter
		{
			get { return Configuration.SkimDelimiter == 0 ? ' ' : Configuration.SkimDelimiter; }
		}

		public static int NBatches
		{
			get
			{
				if (Configuration.IsInEstimationMode)
					return 1;
				if (Configuration.NBatches == 0)
					return NProcessors*4;
				return Configuration.NBatches;
			}
		}

		public static int SmallDegreeOfParallelism
		{
			get
			{
				if (Configuration.SmallDegreeOfParallelism == 0)
					return NProcessors/2;
				return Configuration.SmallDegreeOfParallelism;
			}
		}
		
		public static int LargeDegreeOfParallelism
		{
			get
			{
				if (Configuration.LargeDegreeOfParallelism == 0)
					return NProcessors;
				return Configuration.LargeDegreeOfParallelism;
			}
		}

		public static int NProcessors
		{
			get { return (Configuration.NProcessors == 0) ? 1 : Configuration.NProcessors; }
		}

		public static bool TextSkimFilesContainHeaderRecord
		{
			get { return Configuration.TextSkimFilesContainHeaderRecord; }
		}

		public static string SamplingWeightsSettingsType
		{
			get {return string.IsNullOrEmpty(Configuration.SamplingWeightsSettingsType) ? "SamplingWeightsSettings" : Configuration.SamplingWeightsSettingsType;}
		}

		public static int MaximumHouseholdSize{
			get
			{
				return !(Configuration.MaximumHouseholdSize == 0) ? Configuration.MaximumHouseholdSize : 20;
			}
		}

		private static string GetSubpath(string file, string subPath)
		{
			if (file.Contains(":\\"))
				return file;
			else if (Configuration.BasePath == null)
			{
				if (subPath == null)
					return file;
				return Path.Combine(subPath, file);
			}
			else if (subPath == null )
				return Path.Combine(Configuration.BasePath, file);
			else
			{
				return Path.Combine(Configuration.BasePath, subPath, file);
			}
		}

		public static string GetInputPath(string file)
		{
			return GetSubpath(file, "");
		}

		public static string GetOutputPath(string file)
		{
			return GetSubpath(file, Configuration.OutputSubpath);
		}

		public static string GetEstimationPath(string file)
		{
			return GetSubpath(file, Configuration.EstimationSubpath);
		}

		public static string GetWorkingPath(string file)
		{
			if (string.IsNullOrEmpty(Configuration.WorkingSubpath))
			{
				return GetSubpath(file, Configuration.WorkingDirectory);
			}
			else
				return GetSubpath(file, Configuration.WorkingSubpath);
		}

		public static double Coefficients_CostCoefficientIncomeMultipleMinimum {
			get
			{
				return Configuration.Coefficients_CostCoefficientIncomeMultipleMinimum == 0 ? 0.1 : Configuration.Coefficients_CostCoefficientIncomeMultipleMinimum;
			}
		}

		public static double Coefficients_CostCoefficientIncomeMultipleMaximum {
			get
			{
				return Configuration.Coefficients_CostCoefficientIncomeMultipleMaximum == 0 ? 10.0 : Configuration.Coefficients_CostCoefficientIncomeMultipleMaximum;
			}
		}

	}
}
