﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Linq;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using Daysim.DomainModels.Actum;
using System.Collections.Generic;

namespace Daysim.ChoiceModels.Actum {
	public class ActumPartialJointHalfTourChauffeurModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumPartialJointHalfTourChauffeurModel";
		private const int TOTAL_ALTERNATIVES = 5;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 60;

		public int Run(ActumHouseholdDayWrapper householdDay, int jHTSimulated, int genChoice, int[] participants, int[] jHTChauffeurSequence) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.PartialJointHalfTourChauffeurModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

			householdDay.ResetRandom(930 + jHTSimulated);  // JLB TODO:  what index to use? need to enable ResetRandom for HouseholdDay

			IEnumerable<ActumPersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointHalfTourParticipationPriority).ToList().Cast<ActumPersonDayWrapper>();

			int chauffeurSequence = 0;
			int choice = 0;
			if (Global.Configuration.IsInEstimationMode) {
				chauffeurSequence = jHTChauffeurSequence[jHTSimulated];
				int i = 0;
				foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {
					i++;
					if (personDay.Person.Sequence == chauffeurSequence && i <= 5) {
						choice = i;
					}
				}
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME || choice == 0) {
					return chauffeurSequence;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((householdDay.Household.Id * 397) ^ 2 * jHTSimulated + 1);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {

				RunModel(choiceProbabilityCalculator, householdDay, jHTSimulated, genChoice, participants, choice);

				choiceProbabilityCalculator.WriteObservation();

				return chauffeurSequence;
			}
			else {
				RunModel(choiceProbabilityCalculator, householdDay, genChoice, jHTSimulated, participants);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);

				int i = 0;
				foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {
					i++;
					if ((int) chosenAlternative.Choice == i) {
						chauffeurSequence = personDay.Person.Sequence;
					}
				}
				return chauffeurSequence;
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumHouseholdDayWrapper householdDay, int jHTSimulated, int genChoice, int[] participants, int choice = Constants.DEFAULT_VALUE) {

			IEnumerable<ActumPersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointHalfTourParticipationPriority).ToList().Cast<ActumPersonDayWrapper>();

			int paired = genChoice == 1 ? 1 : 0;
			int halfTour1 = genChoice == 2 ? 1 : 0;
			int halfTour2 = genChoice == 3 ? 1 : 0;


			// set household characteristics here that don't depend on person characteristics

			int hhsize = householdDay.Household.Size;

			int hhinc1 = householdDay.Household.Income <= 300000 ? 1 : 0;
			int hhinc2 = (householdDay.Household.Income > 300000 && householdDay.Household.Income <= 600000) ? 1 : 0;
			int hhinc3 = (householdDay.Household.Income > 600000 && householdDay.Household.Income <= 900000) ? 1 : 0;
			int hhinc4 = (householdDay.Household.Income > 900000 && householdDay.Household.Income <= 1200000) ? 1 : 0;

			int[] pUsualLocation = new int[6];
			int[] pPatternType = new int[6];
			int[] pConstant = new int[6];
			int[] pType8 = new int[6];
			int[] pType7 = new int[6];
			int[] pType6 = new int[6];
			int[] pType5 = new int[6];
			int[] pType4 = new int[6];
			int[] pType3 = new int[6];
			int[] pType2 = new int[6];
			int[] pType1 = new int[6];
			int[] pAdult = new int[6];
			int[] pAdultWithChildrenUnder16 = new int[6];
			int[] pAdultFemale = new int[6];
			int[] pType7AgeUnder12 = new int[6];
			int[] pType7Age12Plus = new int[6];
			int[] pAgeUnder12 = new int[6];

			int count = 0;
			foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {
				count++;
				if (count <= 5) {
					// set characteristics here that depend on person characteristics
					if (personDay.Person.IsFullOrPartTimeWorker) {
						pUsualLocation[count] = personDay.Person.UsualWorkParcelId;
					}
					else if (personDay.Person.IsStudent) {
						pUsualLocation[count] = personDay.Person.UsualSchoolParcelId;
					}
					else if (personDay.Person.IsWorker && personDay.Person.IsNotFullOrPartTimeWorker) {
						pUsualLocation[count] = personDay.Person.UsualWorkParcelId;
					}
					else {
						pUsualLocation[count] = Constants.DEFAULT_VALUE;
					}

					pPatternType[count] = personDay.PatternType;
					pConstant[count] = 1;
					pType8[count] = personDay.Person.IsChildUnder5.ToFlag();
					pType7[count] = personDay.Person.IsChildAge5Through15.ToFlag();
					pType6[count] = personDay.Person.IsDrivingAgeStudent.ToFlag();
					pType5[count] = personDay.Person.IsUniversityStudent.ToFlag();
					pType4[count] = personDay.Person.IsNonworkingAdult.ToFlag();
					pType3[count] = personDay.Person.IsRetiredAdult.ToFlag();
					pType2[count] = personDay.Person.IsPartTimeWorker.ToFlag();
					pType1[count] = personDay.Person.IsFulltimeWorker.ToFlag();
					pAdult[count] = personDay.Person.IsAdult.ToFlag();
					pAdultWithChildrenUnder16[count] = (personDay.Person.IsAdult && personDay.Household.HasChildrenUnder16).ToFlag();
					pAdultFemale[count] = personDay.Person.IsAdultFemale.ToFlag();
					pType7AgeUnder12[count] = (personDay.Person.IsChildAge5Through15 && personDay.Person.Age < 12).ToFlag();
					pType7Age12Plus[count] = (personDay.Person.IsChildAge5Through15 && personDay.Person.Age >= 12).ToFlag();
					pAgeUnder12[count] = (personDay.Person.Age < 12).ToFlag();
				}
			}



			//Generate utility funtions for the alternatives
			bool[] available = new bool[5];
			bool[] chosen = new bool[5];
			int alt = 0;
			for (int i = 0; i < 5; i++) {
				alt = i + 1;
				available[i] = false;
				chosen[i] = false;

				// set availability based on participation in tour
				if (participants[alt] == 1) {
					available[i] = true;
				}
				// restrict availability person is not an adult
				if (pAdult[alt] == 0) {
					available[i] = false;
				}

				// determine choice
				if (choice == alt) { chosen[i] = true; }

				//Get the alternative
				var alternative = choiceProbabilityCalculator.GetAlternative(i, available[i], chosen[i]);

				alternative.Choice = alt;

				//Add utility terms that are not in components
				alternative.AddUtilityTerm(1, 1);
				// Note:  the above gives every available alternative equal utility, via an ASC with value equal to the beta1.
				//		There are so few observations where two people are available to chauffeur that we have no infomration to choose between them
				//    This spec will not estimate, but it can be used in application if we fix the parameter to any value.
				//alternative.AddUtilityTerm(1, pType1[alt]);
				//alternative.AddUtilityTerm(2, paired * pType1[alt]);  //fulltime workers may be less likely to chauffeur on paired joint half tours

			}
		}
	}
}
