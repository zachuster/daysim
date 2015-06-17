// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


namespace Daysim.Framework.Core {
	public static class Constants {
		public static class DestinationScale {
			public const int PARCEL = 0;
			public const int MICRO_ZONE = 1;
			public const int ZONE = 2;
		}

		public static class PersonType {
			public const int FULL_TIME_WORKER = 1;
			public const int PART_TIME_WORKER = 2;
			public const int RETIRED_ADULT = 3;
			public const int NON_WORKING_ADULT = 4;
			public const int UNIVERSITY_STUDENT = 5;
			public const int DRIVING_AGE_STUDENT = 6;
			public const int CHILD_AGE_5_THROUGH_15 = 7;
			public const int CHILD_UNDER_5 = 8;
		}

		public static class ActumPersonType {
			public const int FULL_TIME_WORKER = 1;
			public const int PART_TIME_WORKER = 2;
			public const int RETIRED_ADULT = 3;
			public const int NON_WORKING_ADULT = 4;
			public const int GYMNASIUM_OR_UNIVERSITY_STUDENT = 5;
			public const int CHILD_AGE_5_THROUGH_15 = 6;
			public const int CHILD_UNDER_5 = 7;
		}


		public static class PatternType {
			public const int MANDATORY = 1;
			public const int NONMANDATORY = 2;
			public const int HOME = 3;
		}

		public static class Purpose {
			public const int TOTAL_PURPOSES = 12;
			public const int NONE_OR_HOME = 0;
			public const int WORK = 1;
			public const int HOME_BASED_COMPOSITE = 1; // used with aggregate logsums
			public const int SCHOOL = 2;
			public const int WORK_BASED = 2; // used with aggregate logsums
			public const int ESCORT = 3;
			public const int PERSONAL_BUSINESS = 4;
			public const int SHOPPING = 5;
			public const int MEAL = 6;
			public const int SOCIAL = 7;
			public const int RECREATION = 8;
			public const int MEDICAL = 9;
			public const int CHANGE_MODE = 10;
			public const int BUSINESS = 11;
		}

		public static class TourCategory {
			public const int PRIMARY = 0;
			public const int SECONDARY = 1;
			public const int WORK_BASED = 2;
			public const int HOME_BASED = 3;
		}

		public static class TourPriority {
			public const int USUAL_LOCATION = 0;
			public const int HOME_BASED_TOUR = 1;
			public const int WORK_BASED_TOUR = 2;
		}

		public static class Mode {
			public const int TOTAL_MODES = 10;
			public const int NONE = 0;
			public const int WALK = 1;
			public const int BIKE = 2;
			public const int SOV = 3;
			public const int HOV2 = 4;
			public const int HOV3 = 5;
			public const int TRANSIT = 6;
			public const int PARK_AND_RIDE = 7;
			public const int SCHOOL_BUS = 8;
			public const int OTHER = 9;
			public const int HOVDRIVER = 4;  // used by ACTUM
			public const int HOVPASSENGER = 5;  // USED BY actum
		}

		public static class DriverType {
			public const int NOT_APPLICABLE = 0;
			public const int DRIVER = 1;
			public const int PASSENGER = 2;
		}

		public static class PathType {
			public const int TOTAL_PATH_TYPES = 8;
			public const int NONE = 0;
			public const int FULL_NETWORK = 1;
			public const int NO_TOLLS = 2;
			public const int LOCAL_BUS = 3;
			public const int LIGHT_RAIL = 4;
			public const int PREMIUM_BUS = 5;
			public const int COMMUTER_RAIL = 6;
			public const int FERRY = 7;
		}

		public static class VotGroup {
			public const int TOTAL_VOT_GROUPS = 6;
			public const int NONE = 0;
			public const int VERY_LOW = 1;
			public const int LOW = 2;
			public const int MEDIUM = 3;
			public const int HIGH = 4;
			public const int VERY_HIGH = 5;
			public const int DEFAULT = -1;
		}

		public static class TimeDirection {
			public const int BEFORE = 1;
			public const int AFTER = 2;
			public const int BOTH = 3;
		}

		public static class TourDirection {
			public const int TOTAL_TOUR_DIRECTIONS = 2;
			public const int ORIGIN_TO_DESTINATION = 1;
			public const int DESTINATION_TO_ORIGIN = 2;
		}

		public static class PersonGender {
			public const int MALE = 1;
			public const int FEMALE = 2;
		}

		public static class TransitAccess {
			public const int TOTAL_TRANSIT_ACCESSES = 3;
			public const int GT_0_AND_LTE_QTR_MI = 0;
			public const int GT_QTR_MI_AND_LTE_H_MI = 1;
			public const int NONE = 2;
		}

		public static class VotALSegment {
			public const int TOTAL_VOT_ALSEGMENTS = 3;
			public const int LOW = 0;
			public const int MEDIUM = 1;
			public const int HIGH = 2;
			public const int INCOME_LOW_MEDIUM = 20000;
			public const int INCOME_MEDIUM_HIGH = 80000;
			public const double VOT_LOW_MEDIUM = 4.0;
			public const double VOT_MEDIUM_HIGH = 12.0;
			public const double TIME_COEFFICIENT = -0.02;
			public const double COST_COEFFICIENT_LOW = -0.60; //VOT = $2/hr
			public const double COST_COEFFICIENT_MEDIUM = -0.15; //VOT = $8/hr
			public const double COST_COEFFICIENT_HIGH = -0.06; //VOT = $20/hr
		}

		public static class CarOwnership {
			public const int TOTAL_CAR_OWNERSHIPS = 4;
			public const int CHILD = 0;
			public const int NO_CARS = 1;
			public const int LT_ONE_CAR_PER_ADULT = 2;
			public const int ONE_OR_MORE_CARS_PER_ADULT = 3;
		}

		public static class AddressType {
			public const int NONE = 0;
			public const int HOME = 1;
			public const int USUAL_WORKPLACE = 2;
			public const int USUAL_SCHOOL = 3;
			public const int OTHER = 4;
			public const int MISSING = 5;
			public const int CHANGE_MODE = 6;
		}

		public static class ValueOfTime {
			public const int LOW = 1;
			public const int MEDIUM = 2;
			public const int HIGH = 3;
			public const double DEFAULT_VOT = 10;
		}

		public static class Model {
			public const int WORK_TOUR_MODE_MODEL = 0;
			public const int SCHOOL_TOUR_MODE_MODEL = 1;
			public const int WORKBASED_SUBTOUR_MODE_MODEL = 2;
			public const int ESCORT_TOUR_MODE_MODEL = 3;
			public const int OTHER_HOME_BASED_TOUR_MODE = 4;
		}

		/// <summary>
		/// In Daysim, days start at 3:00 AM and end at 2:59 AM.
		/// </summary>
		public static class Time {
			public const int MINUTES_IN_A_DAY = 1440;
			public const int MINIMUM_ACTIVITY_DURATION = 1;

			// durations
			public const int ZERO_HOURS = 0;
			public const int ONE_HOUR = 60 * 1;
			public const int TWO_HOURS = 60 * 2;
			public const int THREE_HOURS = 60 * 3;
			public const int FOUR_HOURS = 60 * 4;
			public const int FIVE_HOURS = 60 * 5;
			public const int SIX_HOURS = 60 * 6;
			public const int SEVEN_HOURS = 60 * 7;
			public const int EIGHT_HOURS = 60 * 8;
			public const int NINE_HOURS = 60 * 9;
			public const int TEN_HOURS = 60 * 10;
			public const int ELEVEN_HOURS = 60 * 11;
			public const int TWELVE_HOURS = 60 * 12;
			public const int THIRTEEN_HOURS = 60 * 13;
			public const int FOURTEEN_HOURS = 60 * 14;
			public const int FIFTEEN_HOURS = 60 * 15;
			public const int SIXTEEN_HOURS = 60 * 16;
			public const int SEVENTEEN_HOURS = 60 * 17;
			public const int EIGHTEEN_HOURS = 60 * 18;
			public const int NINETEEN_HOURS = 60 * 19;
			public const int TWENTY_HOURS = 60 * 20;
			public const int TWENTY_ONE_HOURS = 60 * 21;
			public const int TWENTY_TWO_HOURS = 60 * 22;
			public const int TWENTY_THREE_HOURS = 60 * 23;
			public const int TWENTY_FOUR_HOURS = 60 * 24;

			// clock times
			public const int THREE_AM = 1;
			public const int FOUR_AM = 60 * 1 + 1;
			public const int FIVE_AM = 60 * 2 + 1;
			public const int SIX_AM = 60 * 3 + 1;
			public const int SEVEN_AM = 60 * 4 + 1;
			public const int EIGHT_AM = 60 * 5 + 1;
			public const int NINE_AM = 60 * 6 + 1;
			public const int TEN_AM = 60 * 7 + 1;
			public const int ELEVEN_AM = 60 * 8 + 1;
			public const int NOON = 60 * 9 + 1;
			public const int ONE_PM = 60 * 10 + 1;
			public const int TWO_PM = 60 * 11 + 1;
			public const int THREE_PM = 60 * 12 + 1;
			public const int FOUR_PM = 60 * 13 + 1;
			public const int FIVE_PM = 60 * 14 + 1;
			public const int SIX_PM = 60 * 15 + 1;
			public const int SEVEN_PM = 60 * 16 + 1;
			public const int EIGHT_PM = 60 * 17 + 1;
			public const int NINE_PM = 60 * 18 + 1;
			public const int TEN_PM = 60 * 19 + 1;
			public const int ELEVEN_PM = 60 * 20 + 1;
			public const int MIDNIGHT = 60 * 21 + 1;
			public const int ONE_AM = 60 * 22 + 1;
			public const int TWO_AM = 60 * 23 + 1;
			public const int END_OF_RELEVANT_WINDOW = 60 * 21 + 1;

	}

		/// <summary>
		/// A tiny number.
		/// </summary>
		public const double EPSILON = 1E-40;

		/// <summary>
		/// A huge number.
		/// </summary>
		public const double HUGE = 1E40;

		/// <summary>
		/// Allowable minutes in a day.
		/// </summary>
		/// <summary>
		/// Represents a parcel that is out of the current region.
		/// </summary>
		public const int OUT_OF_REGION_PARCEL_ID = -999;

		public const double GENERALIZED_TIME_UNAVAILABLE = -999D;

		public const int DEFAULT_VALUE = -1;

		/// <summary>
		///  Number of seeds in array. Moved from configuration to here.
		/// </summary>
		public const int NUMBER_OF_RANDOM_SEEDS = 1200;

		public static class HouseholdType {
			public const int INDIVIDUAL_WORKER_STUDENT = 1;
			public const int INDIVIDUAL_NONWORKER_NONSTUDENT = 2;
			public const int ONE_ADULT_WITH_CHILDREN = 3;
			public const int TWO_PLUS_WORKER_STUDENT_ADULTS_WITH_CHILDREN = 4;
			public const int TWO_PLUS_ADULTS_ONE_PLUS_WORKERS_STUDENTS_WITH_CHILDREN = 5;
			public const int TWO_PLUS_WORKER_STUDENT_ADULTS_WITHOUT_CHILDREN = 6;
			public const int ONE_PLUS_WORKER_STUDENT_ADULTS_AND_ONE_PLUS_NONWORKER_NONSTUDENT_ADULTS_WITHOUT_CHILDREN = 7;
			public const int TWO_PLUS_NONWORKER_NONSTUDENT_ADULTS_WITHOUT_CHILDREN = 8;
		}

	}
}