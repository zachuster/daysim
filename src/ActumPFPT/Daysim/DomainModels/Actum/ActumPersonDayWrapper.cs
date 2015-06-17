// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.ChoiceModels;
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;
using Daysim.ModelRunners;
using Ninject;

namespace Daysim.DomainModels.Actum {
	public class ActumPersonDayWrapper : PersonDayWrapper {
		public ActumPersonDayWrapper(ActumPersonDay personDay, ActumPersonWrapper personWrapper, ActumHouseholdDayWrapper householdDayWrapper)
			: base(personDay, personWrapper, householdDayWrapper) {
		}

		// domain model properies

		//		public int PatternType 
		//		{ 
		//			get {	return ((ActumPersonDay)_personDay).PatternType;}
		//			set {	((ActumPersonDay)_personDay).PatternType = value;}
		//		}
		public int BusinessTours {
			get { return ((ActumPersonDay) _personDay).BusinessTours; }
			set { ((ActumPersonDay) _personDay).BusinessTours = value; }
		}

		public int BusinessStops {
			get { return ((ActumPersonDay) _personDay).BusinessStops; }
			set { ((ActumPersonDay) _personDay).BusinessStops = value; }
		}

		// flags, choice model properties, etc.

		public override int TotalTours {
			get { return base.TotalTours + BusinessTours; }
		}

		public int CreatedBusinessTours { get; set; }

		public new int TotalCreatedTours {
			get { return base.TotalCreatedTours + CreatedBusinessTours; }
		}

		public override int TotalCreatedTourPurposes {
			get { return base.TotalCreatedTourPurposes + Math.Min(1, CreatedBusinessTours); }
		}

		public int SimulatedBusinessTours { get; set; }

		public override int TotalSimulatedTours {
			get { return base.TotalSimulatedTours + SimulatedBusinessTours; }
		}

		public override int TotalStops {
			get { return base.TotalStops + BusinessStops; }
		}

		public int SimulatedBusinessStops { get; private set; }

		public override int TotalSimulatedStops {
			get { return base.TotalSimulatedStops + SimulatedBusinessStops; }
		}

		public bool HasSimulatedBusinessStops {
			get { return SimulatedBusinessStops > 0; }
		}


		protected override ITourWrapper CreateTour(int originAddressType, int originParcelId, int originZoneKey, int purpose) {
			return new ActumTourWrapper(new Tour {
				Id = _personDay.Id * 10 + GetNextTourSequence(), // ++NextTourId,
				PersonId = _personDay.PersonId,
				PersonDayId = _personDay.Id,
				HouseholdId = _personDay.HouseholdId,
				PersonSequence = _personDay.PersonSequence,
				Day = _personDay.Day,
				Sequence = GetCurrentTourSequence(),
				OriginAddressType = originAddressType,
				OriginParcelId = originParcelId,
				OriginZoneKey = originZoneKey,
				OriginDepartureTime = 180,
				DestinationArrivalTime = 180,
				DestinationDepartureTime = 180,
				OriginArrivalTime = 180,
				DestinationPurpose = purpose,
				PathType = 1,
				ExpansionFactor = Household.ExpansionFactor
			}, this, purpose);
		}

		protected override void CreateNewPersonDay(IPersonDay temp) {
			_personDay = new ActumPersonDay {
				Id = temp.Id,
				PersonId = temp.PersonId,
				HouseholdDayId = temp.HouseholdDayId,
				HouseholdId = temp.HouseholdId,
				PersonSequence = temp.PersonSequence,
				Day = temp.Day
			};
		}

		protected override TourWrapper GetNewWrapper(ITour tour, PersonDayWrapper personDayWrapper, int destinationPurpose, bool b) {
			return new ActumTourWrapper(tour, this, tour.DestinationPurpose, true);
		}

		public void GetMandatoryTourSimulatedData(ActumPersonDayWrapper personDay, List<ITourWrapper> tours) {
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.WORK, personDay.UsualWorkplaceTours));
			foreach (var tour in tours) {
				tour.DestinationParcel = personDay.Person.UsualWorkParcel;
				tour.DestinationParcelId = personDay.Person.UsualWorkParcelId;
				tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[personDay.Person.UsualWorkParcel.ZoneId];
				tour.DestinationAddressType = Constants.AddressType.USUAL_WORKPLACE;
			}
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.BUSINESS, personDay.BusinessTours));
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.SCHOOL, personDay.SchoolTours));
			foreach (var tour in tours) {
				if (tour.DestinationPurpose == Constants.Purpose.SCHOOL) {
					tour.DestinationParcel = personDay.Person.UsualSchoolParcel;
					tour.DestinationParcelId = personDay.Person.UsualSchoolParcelId;
					tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[personDay.Person.UsualSchoolParcel.ZoneId];
					tour.DestinationAddressType = Constants.AddressType.USUAL_SCHOOL;
				}
			}
		}


		public override void IncrementSimulatedStops(int destinationPurpose) {
			switch (destinationPurpose) {
				case Constants.Purpose.WORK:
					SimulatedWorkStops++;

					break;

				case Constants.Purpose.BUSINESS:
					SimulatedBusinessStops++;

					break;

				case Constants.Purpose.SCHOOL:
					SimulatedSchoolStops++;

					break;

				case Constants.Purpose.ESCORT:
					SimulatedEscortStops++;

					break;

				case Constants.Purpose.PERSONAL_BUSINESS:
					SimulatedPersonalBusinessStops++;

					break;

				case Constants.Purpose.SHOPPING:
					SimulatedShoppingStops++;

					break;

				case Constants.Purpose.MEAL:
					SimulatedMealStops++;

					break;

				case Constants.Purpose.SOCIAL:
					SimulatedSocialStops++;

					break;
				case Constants.Purpose.RECREATION:
					SimulatedRecreationStops++;

					break;
				case Constants.Purpose.MEDICAL:
					SimulatedMedicalStops++;

					break;
			}
		}
		public override void Reset() {
			//Reset additional things
			CreatedBusinessTours = 0;
			SimulatedBusinessTours = 0;
			SimulatedBusinessStops = 0;
			BusinessTours = 0;
			BusinessStops = 0;
			base.Reset();
		}

	}
}
