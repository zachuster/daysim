﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using Daysim.Framework.Core;

namespace Daysim {
	public class SamplingWeightsSettingsSimple : ISamplingWeightsSettings {

		public SamplingWeightsSettingsSimple()
		{
			SizeFactors = new[]
					{
						/*     EDU   food  GOV   IND   MED   OFC   RSC   RET   SVC EMPTOT  HOUSE  K8    UNI LUSE19 LU19Q OPENSP HighSchool*/
						new[] {-30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -00D, -30D, -30D, -30D, -30D, -30D, -30D, -30D},  /*	0 9	16:motorized --work"                        */
						new[] {-02D, -30D, -30D, -30D, -30D, -02D, -30D, -30D, -02D, -02D, -01D, -00D, -06D, -30D, -30D, -30D, -00D},  /*	1 10	17:motorized --school"                      */
						new[] {-30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -02D, -01D, -03D, -04D, -30D, -30D, -30D, -03D},  /*	2 11	18:motorized --escort"                      */
						new[] {-30D, -30D, -30D, -30D, -01D, -30D, -30D, -30D, -30D, -01D, -1.5D, -02D, -03D, -30D, -30D, -30D, -02D}, /*	3 12	19:motorized --pers bus."                   */
						new[] {-30D, -30D, -30D, -30D, -30D, -03D, -30D, -00D, -03D, -04D, -30D, -30D, -30D, -30D, -30D, -30D, -30D},  /*	4 13	20:motorized --shop"                        */
						new[] {-30D, -00D, -30D, -30D, -30D, -03D, -30D, -02D, -03D, -04D, -03D, -30D, -30D, -30D, -30D, -30D, -30D},  /*	5 14	21:motorized --meal"                        */
						new[] {-30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -02D, -01D, -03D, -03D, -30D, -30D, -01D, -03D},  /*	6 15	22:motorized --social"                      */
						new[] {-30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -02D, -07D, -04D, -04D, -02D, -30D, -03D, -04D},  /*	7 16	23:motorized --rec"                         */
						new[] {-30D, -30D, -30D, -30D, -00D, -30D, -30D, -30D, -30D, -07D, -30D, -30D, -30D, -30D, -30D, -30D, -30D},  /*	8 17	24:motorized --medical"                     */
						new[] {-30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -00D, -04D, -30D, -30D, -30D, -30D, -30D, -30D},  /*	9 18	25:walk --work"                                    */
						new[] {-03D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -07D, -04D, -02D, -02D, -30D, -30D, -30D, -02D},  /* 10 19 26:walk --school"                                  */
						new[] {-30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -02D, -01D, -03D, -04D, -30D, -30D, -30D, -03D},  /*	11 20	27:walk --escort"                                  */
						new[] {-30D, -30D, -30D, -30D, -01D, -30D, -30D, -30D, -30D, -01D, -1.5D, -02D, -03D, -30D, -30D, -30D, -02D}, /*	12 21	28:walk --pers bus."                               */
						new[] {-30D, -30D, -30D, -30D, -30D, -03D, -30D, -00D, -03D, -04D, -04D, -30D, -30D, -30D, -30D, -30D, -30D},  /*	13 22	29:walk --shop"                                    */
						new[] {-30D, -00D, -30D, -30D, -30D, -03D, -30D, -02D, -03D, -04D, -03D, -30D, -30D, -30D, -30D, -30D, -30D},  /*	14 23	30:walk --meal"                                    */
						new[] {-30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -02D, -01D, -03D, -03D, -30D, -30D, -01D, -03D},  /*	15 24	31:walk --social"                                  */
						new[] {-30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -30D, -02D, -04D, -04D, -04D, -02D, -30D, -03D, -04D},  /*	16 25	32:walk --rec"                                     */
						new[] {-30D, -30D, -30D, -30D, -00D, -30D, -30D, -30D, -30D, -07D, -04D, -30D, -30D, -30D, -30D, -30D, -30D},  /*	17 26	33:walk --medical"                                 */
						new[] {-02D, -30D, -30D, -30D, -30D, -02D, -30D, -30D, -02D, -02D, -01D, -30D, -00D, -30D, -30D, -30D, -03D}   /* 18 36   :ptypes 1-5 --usu school                         */
					};

			WeightFactors = new[] {
				/*  0     7:motorized WBTour or IntStop --work"          */new[] {2300D}, //{1455}, /*values on the right are temporary values to synchronize with delphi for testing only*/
				/*  1     8:motorized WBTour or IntStop --school"        */new[] {1350D}, //{1111},  
				/*  2     9:motorized WBTour or IntStop --escort"        */new[] {1350D}, //{1216},  
				/*  3    10:motorized WBTour or IntStop --pers bus."     */new[] {1400D}, //{1150},  
				/*  4    11:motorized WBTour or IntStop --shop"          */new[] {1250D}, //{1077},  
				/*  5    12:motorized WBTour or IntStop --meal"          */new[] {1300D}, //{1096.5},
				/*  6    13:motorized WBTour or IntStop --social"        */new[] {1400D}, //{1225.5},
				/*  7    14:motorized WBTour or IntStop --rec"           */new[] {1400D}, //{1292},  
				/*  8    15:motorized WBTour or IntStop --medical"       */new[] {1600D}, //{1329},  
				/*  9 18    25:walk --work"                              */new[] {550D}, //{1455},
				/* 10 19    26:walk --school"                            */new[] {550D}, //{1111},
				/* 11 20    27:walk --escort"                            */new[] {550D}, //{1216},
				/* 12 21    28:walk --pers bus."                         */new[] {550D}, //{1150},
				/* 13 22    29:walk --shop"                              */new[] {550D}, //{1077},
				/* 14 23    30:walk --meal"                              */new[] {550D}, //{1096.5},
				/* 15 24    31:walk --social"                            */new[] {550D}, //{1225.5},
				/* 16 25    32:walk --rec"                               */new[] {550D}, //{1292},
				/* 17 26    33:walk --medical"                           */new[] {550D}, //{1329},
				/* 18 36      :ptypes 1-5 usual school"                  */new[] {1600D} //   
			};
		}

		#region size factors

		public double[][] SizeFactors
		{
			get; private set;
		}

		#endregion

		#region weight factors

		public double[][] WeightFactors { get; private set; }

		#endregion

		public int GetTourDestinationSegment(int purpose, int priority, int mode, int personType) {
			if ((purpose == Constants.Purpose.WORK || purpose == Constants.Purpose.BUSINESS)  && mode >= Constants.Mode.SOV) {
				return 0;
			}

			if (purpose == Constants.Purpose.SCHOOL && mode >= Constants.Mode.SOV && priority == Constants.TourPriority.USUAL_LOCATION && personType <= Constants.PersonType.UNIVERSITY_STUDENT) {
				return 18;
			}

			if (purpose == Constants.Purpose.SCHOOL && mode >= Constants.Mode.SOV) {
				return 1;
			}

			if (purpose == Constants.Purpose.ESCORT && mode >= Constants.Mode.SOV) {
				return 2;
			}

			if (purpose == Constants.Purpose.PERSONAL_BUSINESS && mode >= Constants.Mode.SOV) {
				return 3;
			}

			if (purpose == Constants.Purpose.SHOPPING && mode >= Constants.Mode.SOV) {
				return 4;
			}

			if (purpose == Constants.Purpose.MEAL && mode >= Constants.Mode.SOV) {
				return 5;
			}

			if (purpose == Constants.Purpose.SOCIAL && mode >= Constants.Mode.SOV) {
				return 6;
			}

			if (purpose == Constants.Purpose.RECREATION && mode >= Constants.Mode.SOV) {
				return 7;
			}

			if (purpose == Constants.Purpose.MEDICAL && mode >= Constants.Mode.SOV) {
				return 8;
			}

			if ((purpose == Constants.Purpose.WORK || purpose == Constants.Purpose.BUSINESS) ) {
				return 9;
			}

			if (purpose == Constants.Purpose.SCHOOL) {
				return 10;
			}

			if (purpose == Constants.Purpose.ESCORT) {
				return 11;
			}

			if (purpose == Constants.Purpose.PERSONAL_BUSINESS) {
				return 12;
			}

			if (purpose == Constants.Purpose.SHOPPING) {
				return 13;
			}

			if (purpose == Constants.Purpose.MEAL) {
				return 14;
			}

			if (purpose == Constants.Purpose.SOCIAL) {
				return 15;
			}

			if (purpose == Constants.Purpose.RECREATION) {
				return 16;
			}

			if (purpose == Constants.Purpose.MEDICAL) {
				return 17;
			}

			throw new SegmentRemainsUnassignedException();
		}

		public int GetIntermediateStopSegment(int purpose, int mode) {
			if ((purpose == Constants.Purpose.WORK || purpose == Constants.Purpose.BUSINESS)  && mode >= Constants.Mode.SOV) {
				return 0;
			}

			if (purpose == Constants.Purpose.SCHOOL && mode >= Constants.Mode.SOV) {
				return 1;
			}

			if (purpose == Constants.Purpose.ESCORT && mode >= Constants.Mode.SOV) {
				return 2;
			}

			if (purpose == Constants.Purpose.PERSONAL_BUSINESS && mode >= Constants.Mode.SOV) {
				return 3;
			}

			if (purpose == Constants.Purpose.SHOPPING && mode >= Constants.Mode.SOV) {
				return 4;
			}

			if (purpose == Constants.Purpose.MEAL && mode >= Constants.Mode.SOV) {
				return 5;
			}

			if (purpose == Constants.Purpose.SOCIAL && mode >= Constants.Mode.SOV) {
				return 6;
			}

			if (purpose == Constants.Purpose.RECREATION && mode >= Constants.Mode.SOV) {
				return 7;
			}

			if (purpose == Constants.Purpose.MEDICAL && mode >= Constants.Mode.SOV) {
				return 8;
			}

			if ((purpose == Constants.Purpose.WORK || purpose == Constants.Purpose.BUSINESS) ) {
				return 9;
			}

			if (purpose == Constants.Purpose.SCHOOL) {
				return 10;
			}

			if (purpose == Constants.Purpose.ESCORT) {
				return 11;
			}

			if (purpose == Constants.Purpose.PERSONAL_BUSINESS) {
				return 12;
			}

			if (purpose == Constants.Purpose.SHOPPING) {
				return 13;
			}

			if (purpose == Constants.Purpose.MEAL) {
				return 14;
			}

			if (purpose == Constants.Purpose.SOCIAL) {
				return 15;
			}

			if (purpose == Constants.Purpose.RECREATION) {
				return 16;
			}

			if (purpose == Constants.Purpose.MEDICAL) {
				return 17;
			}
			throw new SegmentRemainsUnassignedException();
		}
	}
}