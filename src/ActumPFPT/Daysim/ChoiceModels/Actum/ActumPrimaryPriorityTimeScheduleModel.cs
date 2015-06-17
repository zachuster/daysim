// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using Daysim.DomainModels.Actum;
using System.Collections.Generic;

namespace Daysim.ChoiceModels.Actum {
	public class ActumPrimaryPriorityTimeScheduleModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumPrimaryPriorityTimeScheduleModel";
		private const int TOTAL_ALTERNATIVES = 206;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 99;
		
		public void Run(ActumHouseholdDayWrapper householdDay) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.ActumPrimaryPriorityTimeScheduleModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

			householdDay.ResetRandom(962);

			if (Global.Configuration.IsInEstimationMode) {
				return;
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(householdDay.Household.Id);

			int[][] pfptSchedule = new int[TOTAL_ALTERNATIVES + 1][];
			// [kids][StartingMinuteSharedHomeStay][DurationMinutesSharedHomeStay]
			pfptSchedule[1] = new int[] { 0, 912, 360 };
			pfptSchedule[2] = new int[] { 0, 1082, 120 };
			pfptSchedule[3] = new int[] { 0, 950, 400 };
			pfptSchedule[4] = new int[] { 0, 550, 120 };
			pfptSchedule[5] = new int[] { 0, 1145, 110 };
			pfptSchedule[6] = new int[] { 0, 830, 30 };
			pfptSchedule[7] = new int[] { 0, 565, 150 };
			pfptSchedule[8] = new int[] { 0, 1050, 300 };
			pfptSchedule[9] = new int[] { 0, 773, 420 };
			pfptSchedule[10] = new int[] { 0, 1190, 60 };
			pfptSchedule[11] = new int[] { 0, 1145, 90 };
			pfptSchedule[12] = new int[] { 0, 940, 360 };
			pfptSchedule[13] = new int[] { 0, 435, 525 };
			pfptSchedule[14] = new int[] { 0, 955, 90 };
			pfptSchedule[15] = new int[] { 0, 1035, 189 };
			pfptSchedule[16] = new int[] { 0, 1120, 120 };
			pfptSchedule[17] = new int[] { 0, 1095, 330 };
			pfptSchedule[18] = new int[] { 0, 643, 217 };
			pfptSchedule[19] = new int[] { 0, 1009, 60 };
			pfptSchedule[20] = new int[] { 0, 1018, 180 };
			pfptSchedule[21] = new int[] { 0, 989, 101 };
			pfptSchedule[22] = new int[] { 0, 1180, 35 };
			pfptSchedule[23] = new int[] { 0, 685, 60 };
			pfptSchedule[24] = new int[] { 0, 610, 240 };
			pfptSchedule[25] = new int[] { 0, 1025, 400 };
			pfptSchedule[26] = new int[] { 0, 970, 360 };
			pfptSchedule[27] = new int[] { 0, 1110, 135 };
			pfptSchedule[28] = new int[] { 0, 1166, 210 };
			pfptSchedule[29] = new int[] { 0, 915, 285 };
			pfptSchedule[30] = new int[] { 0, 925, 180 };
			pfptSchedule[31] = new int[] { 0, 905, 185 };
			pfptSchedule[32] = new int[] { 0, 1010, 60 };
			pfptSchedule[33] = new int[] { 0, 970, 60 };
			pfptSchedule[34] = new int[] { 0, 1131, 120 };
			pfptSchedule[35] = new int[] { 0, 1120, 80 };
			pfptSchedule[36] = new int[] { 0, 971, 174 };
			pfptSchedule[37] = new int[] { 0, 1035, 165 };
			pfptSchedule[38] = new int[] { 0, 710, 90 };
			pfptSchedule[39] = new int[] { 0, 745, 60 };
			pfptSchedule[40] = new int[] { 0, 970, 470 };
			pfptSchedule[41] = new int[] { 0, 1025, 240 };
			pfptSchedule[42] = new int[] { 0, 1060, 330 };
			pfptSchedule[43] = new int[] { 0, 1005, 30 };
			pfptSchedule[44] = new int[] { 0, 1050, 180 };
			pfptSchedule[45] = new int[] { 0, 1015, 240 };
			pfptSchedule[46] = new int[] { 0, 935, 130 };
			pfptSchedule[47] = new int[] { 0, 1174, 50 };
			pfptSchedule[48] = new int[] { 0, 1010, 120 };
			pfptSchedule[49] = new int[] { 0, 1020, 60 };
			pfptSchedule[50] = new int[] { 0, 905, 60 };
			pfptSchedule[51] = new int[] { 0, 960, 120 };
			pfptSchedule[52] = new int[] { 0, 805, 95 };
			pfptSchedule[53] = new int[] { 0, 1090, 190 };
			pfptSchedule[54] = new int[] { 0, 510, 360 };
			pfptSchedule[55] = new int[] { 0, 975, 180 };
			pfptSchedule[56] = new int[] { 0, 495, 300 };
			pfptSchedule[57] = new int[] { 0, 1015, 120 };
			pfptSchedule[58] = new int[] { 0, 915, 90 };
			pfptSchedule[59] = new int[] { 0, 965, 250 };
			pfptSchedule[60] = new int[] { 0, 1025, 60 };
			pfptSchedule[61] = new int[] { 0, 1115, 85 };
			pfptSchedule[62] = new int[] { 0, 878, 290 };
			pfptSchedule[63] = new int[] { 0, 1014, 180 };
			pfptSchedule[64] = new int[] { 0, 1175, 40 };
			pfptSchedule[65] = new int[] { 0, 960, 180 };
			pfptSchedule[66] = new int[] { 0, 1005, 90 };
			pfptSchedule[67] = new int[] { 0, 1035, 165 };
			pfptSchedule[68] = new int[] { 0, 992, 330 };
			pfptSchedule[69] = new int[] { 0, 983, 240 };
			pfptSchedule[70] = new int[] { 0, 1013, 240 };
			pfptSchedule[71] = new int[] { 0, 783, 420 };
			pfptSchedule[72] = new int[] { 1, 1078, 120 };
			pfptSchedule[73] = new int[] { 1, 1060, 290 };
			pfptSchedule[74] = new int[] { 1, 965, 40 };
			pfptSchedule[75] = new int[] { 1, 1175, 90 };
			pfptSchedule[76] = new int[] { 1, 1112, 120 };
			pfptSchedule[77] = new int[] { 1, 984, 180 };
			pfptSchedule[78] = new int[] { 1, 1010, 300 };
			pfptSchedule[79] = new int[] { 1, 1090, 180 };
			pfptSchedule[80] = new int[] { 1, 1120, 80 };
			pfptSchedule[81] = new int[] { 1, 937, 240 };
			pfptSchedule[82] = new int[] { 1, 633, 195 };
			pfptSchedule[83] = new int[] { 1, 970, 120 };
			pfptSchedule[84] = new int[] { 1, 1182, 20 };
			pfptSchedule[85] = new int[] { 1, 990, 270 };
			pfptSchedule[86] = new int[] { 1, 1177, 120 };
			pfptSchedule[87] = new int[] { 1, 1018, 180 };
			pfptSchedule[88] = new int[] { 1, 1231, 44 };
			pfptSchedule[89] = new int[] { 1, 1055, 180 };
			pfptSchedule[90] = new int[] { 1, 1058, 120 };
			pfptSchedule[91] = new int[] { 1, 1052, 88 };
			pfptSchedule[92] = new int[] { 1, 1075, 300 };
			pfptSchedule[93] = new int[] { 1, 1090, 50 };
			pfptSchedule[94] = new int[] { 1, 990, 90 };
			pfptSchedule[95] = new int[] { 1, 1193, 150 };
			pfptSchedule[96] = new int[] { 1, 1120, 240 };
			pfptSchedule[97] = new int[] { 1, 1038, 300 };
			pfptSchedule[98] = new int[] { 1, 1020, 360 };
			pfptSchedule[99] = new int[] { 1, 880, 165 };
			pfptSchedule[100] = new int[] { 1, 1125, 45 };
			pfptSchedule[101] = new int[] { 1, 960, 160 };
			pfptSchedule[102] = new int[] { 1, 939, 265 };
			pfptSchedule[103] = new int[] { 1, 1170, 90 };
			pfptSchedule[104] = new int[] { 1, 1072, 240 };
			pfptSchedule[105] = new int[] { 1, 955, 480 };
			pfptSchedule[106] = new int[] { 1, 1080, 120 };
			pfptSchedule[107] = new int[] { 1, 535, 180 };
			pfptSchedule[108] = new int[] { 1, 1118, 82 };
			pfptSchedule[109] = new int[] { 1, 867, 118 };
			pfptSchedule[110] = new int[] { 1, 876, 324 };
			pfptSchedule[111] = new int[] { 1, 1088, 110 };
			pfptSchedule[112] = new int[] { 1, 1160, 40 };
			pfptSchedule[113] = new int[] { 1, 1070, 300 };
			pfptSchedule[114] = new int[] { 1, 965, 210 };
			pfptSchedule[115] = new int[] { 1, 1032, 60 };
			pfptSchedule[116] = new int[] { 1, 930, 270 };
			pfptSchedule[117] = new int[] { 1, 1110, 180 };
			pfptSchedule[118] = new int[] { 1, 1105, 30 };
			pfptSchedule[119] = new int[] { 1, 1011, 179 };
			pfptSchedule[120] = new int[] { 1, 991, 181 };
			pfptSchedule[121] = new int[] { 1, 977, 240 };
			pfptSchedule[122] = new int[] { 1, 1185, 60 };
			pfptSchedule[123] = new int[] { 1, 773, 480 };
			pfptSchedule[124] = new int[] { 1, 1100, 60 };
			pfptSchedule[125] = new int[] { 1, 1050, 90 };
			pfptSchedule[126] = new int[] { 1, 945, 240 };
			pfptSchedule[127] = new int[] { 1, 1030, 60 };
			pfptSchedule[128] = new int[] { 1, 995, 400 };
			pfptSchedule[129] = new int[] { 1, 1028, 150 };
			pfptSchedule[130] = new int[] { 1, 1060, 60 };
			pfptSchedule[131] = new int[] { 1, 1025, 180 };
			pfptSchedule[132] = new int[] { 1, 1174, 120 };
			pfptSchedule[133] = new int[] { 1, 955, 168 };
			pfptSchedule[134] = new int[] { 1, 1035, 60 };
			pfptSchedule[135] = new int[] { 1, 1018, 107 };
			pfptSchedule[136] = new int[] { 1, 1026, 174 };
			pfptSchedule[137] = new int[] { 1, 1075, 125 };
			pfptSchedule[138] = new int[] { 1, 875, 205 };
			pfptSchedule[139] = new int[] { 1, 1155, 165 };
			pfptSchedule[140] = new int[] { 1, 1000, 210 };
			pfptSchedule[141] = new int[] { 1, 1129, 70 };
			pfptSchedule[142] = new int[] { 1, 1083, 120 };
			pfptSchedule[143] = new int[] { 1, 1025, 300 };
			pfptSchedule[144] = new int[] { 1, 954, 210 };
			pfptSchedule[145] = new int[] { 1, 960, 300 };
			pfptSchedule[146] = new int[] { 1, 1140, 60 };
			pfptSchedule[147] = new int[] { 1, 962, 300 };
			pfptSchedule[148] = new int[] { 1, 840, 360 };
			pfptSchedule[149] = new int[] { 1, 972, 100 };
			pfptSchedule[150] = new int[] { 1, 1070, 120 };
			pfptSchedule[151] = new int[] { 1, 965, 360 };
			pfptSchedule[152] = new int[] { 1, 942, 180 };
			pfptSchedule[153] = new int[] { 1, 1192, 60 };
			pfptSchedule[154] = new int[] { 1, 1061, 90 };
			pfptSchedule[155] = new int[] { 1, 914, 240 };
			pfptSchedule[156] = new int[] { 1, 1055, 290 };
			pfptSchedule[157] = new int[] { 1, 1060, 260 };
			pfptSchedule[158] = new int[] { 1, 1035, 60 };
			pfptSchedule[159] = new int[] { 1, 970, 240 };
			pfptSchedule[160] = new int[] { 1, 1052, 340 };
			pfptSchedule[161] = new int[] { 1, 943, 120 };
			pfptSchedule[162] = new int[] { 1, 917, 300 };
			pfptSchedule[163] = new int[] { 1, 1045, 360 };
			pfptSchedule[164] = new int[] { 1, 970, 230 };
			pfptSchedule[165] = new int[] { 1, 1141, 60 };
			pfptSchedule[166] = new int[] { 1, 860, 260 };
			pfptSchedule[167] = new int[] { 1, 1045, 395 };
			pfptSchedule[168] = new int[] { 1, 230, 480 };
			pfptSchedule[169] = new int[] { 1, 1060, 60 };
			pfptSchedule[170] = new int[] { 1, 1047, 153 };
			pfptSchedule[171] = new int[] { 1, 930, 420 };
			pfptSchedule[172] = new int[] { 1, 1070, 60 };
			pfptSchedule[173] = new int[] { 1, 980, 47 };
			pfptSchedule[174] = new int[] { 1, 1005, 75 };
			pfptSchedule[175] = new int[] { 1, 550, 330 };
			pfptSchedule[176] = new int[] { 1, 1008, 192 };
			pfptSchedule[177] = new int[] { 1, 975, 240 };
			pfptSchedule[178] = new int[] { 1, 960, 215 };
			pfptSchedule[179] = new int[] { 1, 980, 300 };
			pfptSchedule[180] = new int[] { 1, 795, 290 };
			pfptSchedule[181] = new int[] { 1, 890, 330 };
			pfptSchedule[182] = new int[] { 1, 1140, 180 };
			pfptSchedule[183] = new int[] { 1, 855, 255 };
			pfptSchedule[184] = new int[] { 1, 1165, 200 };
			pfptSchedule[185] = new int[] { 1, 980, 240 };
			pfptSchedule[186] = new int[] { 1, 1020, 400 };
			pfptSchedule[187] = new int[] { 1, 967, 380 };
			pfptSchedule[188] = new int[] { 1, 970, 65 };
			pfptSchedule[189] = new int[] { 1, 914, 480 };
			pfptSchedule[190] = new int[] { 1, 843, 240 };
			pfptSchedule[191] = new int[] { 1, 1020, 210 };
			pfptSchedule[192] = new int[] { 1, 935, 265 };
			pfptSchedule[193] = new int[] { 1, 1120, 80 };
			pfptSchedule[194] = new int[] { 1, 925, 180 };
			pfptSchedule[195] = new int[] { 1, 1085, 120 };
			pfptSchedule[196] = new int[] { 1, 925, 390 };
			pfptSchedule[197] = new int[] { 1, 1010, 360 };
			pfptSchedule[198] = new int[] { 1, 1135, 60 };
			pfptSchedule[199] = new int[] { 1, 795, 540 };
			pfptSchedule[200] = new int[] { 1, 1046, 79 };
			pfptSchedule[201] = new int[] { 1, 1065, 300 };
			pfptSchedule[202] = new int[] { 1, 1163, 44 };
			pfptSchedule[203] = new int[] { 1, 997, 260 };
			pfptSchedule[204] = new int[] { 1, 990, 360 };
			pfptSchedule[205] = new int[] { 1, 982, 210 };
			pfptSchedule[206] = new int[] { 1, 990, 180 };

			//if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {

			//				// set choice variable here  (derive from available household properties)
			//				if (householdDay.SharedActivityHomeStays >= 1 
			//					//&& householdDay.DurationMinutesSharedHomeStay >=60 
			//					&& householdDay.AdultsInSharedHomeStay >= 1 
			//					&& householdDay.NumberInLargestSharedHomeStay >= (householdDay.Household.Size)
			//                   )
			//				{
			//					householdDay.PrimaryPriorityTimeFlag = 1;
			//				}
			//				else 	householdDay.PrimaryPriorityTimeFlag = 0;

			//RunModel(choiceProbabilityCalculator, householdDay, householdDay.PrimaryPriorityTimeFlag);

			//choiceProbabilityCalculator.WriteObservation();
			//}
			//else {
			RunModel(choiceProbabilityCalculator, householdDay, pfptSchedule);

			var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
			var choice = (int[]) chosenAlternative.Choice;
			householdDay.StartingMinuteSharedHomeStay = choice[1];
			householdDay.DurationMinutesSharedHomeStay = choice[2];
			//}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumHouseholdDayWrapper householdDay, int[][] pfptSchedule, int[] choice = null) {

			//var householdDay = (ActumHouseholdDayWrapper)tour.HouseholdDay;
			var household = householdDay.Household;

			//Generate utility funtions for the alternatives
			bool[] available = new bool[TOTAL_ALTERNATIVES + 1];
			bool[] chosen = new bool[TOTAL_ALTERNATIVES + 1];
			for (int alt = 1; alt <= TOTAL_ALTERNATIVES; alt++) {

				available[alt] = false;
				chosen[alt] = false;
				// set availability based on household CHILDREN
				if ((household.HasChildren && pfptSchedule[alt][0] == 1) || (!household.HasChildren && pfptSchedule[alt][0] == 0)) {
					available[alt] = true;
				}

				var alternative = choiceProbabilityCalculator.GetAlternative(alt - 1, available[alt], chosen[alt]);
				alternative.Choice = pfptSchedule[alt];

				// add utility terms for this alterative
				alternative.AddUtilityTerm(1, 1);   // asc  
			}
		}
	}
}