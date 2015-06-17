﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.Collections.Generic;
using System.Linq;
using Daysim.ChoiceModels;
using Daysim.DomainModels.Actum.Models;
using Daysim.DomainModels.Actum.Models.Interfaces;
using Daysim.DomainModels.Actum.Wrappers.Interfaces;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.DomainModels.Wrappers;
using Daysim.Framework.Factories;

namespace Daysim.DomainModels.Actum.Wrappers {
	[Factory(Factory.WrapperFactory, Category = Category.Wrapper, DataType = DataType.Actum)]
	public class PersonDayWrapper : Default.Wrappers.PersonDayWrapper, IActumPersonDayWrapper {
		private IActumPersonDay _personDay;

		[UsedImplicitly]
		public PersonDayWrapper(IPersonDay personDay, IPersonWrapper personWrapper, IHouseholdDayWrapper householdDayWrapper)
			: base(personDay, personWrapper, householdDayWrapper) {
			_personDay = (IActumPersonDay) personDay;
		}

		#region domain model properies

		public int WorkHomeAllDay {
			get { return _personDay.WorkHomeAllDay; }
			set { _personDay.WorkHomeAllDay = value; }
		}

		public int MinutesStudiedHome {
			get { return _personDay.MinutesStudiedHome; }
			set { _personDay.MinutesStudiedHome = value; }
		}

		public int DiaryWeekday {
			get { return _personDay.DiaryWeekday; }
			set { _personDay.DiaryWeekday = value; }
		}

		public int DiaryDaytype {
			get { return _personDay.DiaryDaytype; }
			set { _personDay.DiaryDaytype = value; }
		}

		public int DayStartPurpose {
			get { return _personDay.DayStartPurpose; }
			set { _personDay.DayStartPurpose = value; }
		}

		public int DayJourneyType {
			get { return _personDay.DayJourneyType; }
			set { _personDay.DayJourneyType = value; }
		}

		public int BusinessTours {
			get { return _personDay.BusinessTours; }
			set { _personDay.BusinessTours = value; }
		}

		public int BusinessStops {
			get { return _personDay.BusinessStops; }
			set { _personDay.BusinessStops = value; }
		}

		#endregion

		#region flags, choice model properties, etc.

		public int CreatedBusinessTours { get; set; }

		public int SimulatedBusinessTours { get; set; }

		public int SimulatedBusinessStops { get; set; }

		#endregion

		#region wrapper methods

		public override int GetTotalTours() {
			return base.GetTotalTours() + BusinessTours;
		}

		public override int GetTotalCreatedTours() {
			return base.GetTotalCreatedTours() + CreatedBusinessTours;
		}

		public override int GetTotalCreatedTourPurposes() {
			return base.GetTotalCreatedTourPurposes() + Math.Min(1, CreatedBusinessTours);
		}

		public override int GetTotalSimulatedTours() {
			return base.GetTotalSimulatedTours() + SimulatedBusinessTours;
		}

		public override int GetTotalStops() {
			return base.GetTotalStops() + BusinessStops;
		}

		public override int GetTotalStopPurposes() {
			return base.GetTotalStopPurposes() + (BusinessStops > 0 ? 1 : 0);
		}

		public override int GetTotalSimulatedStops() {
			return base.GetTotalSimulatedStops() + SimulatedBusinessStops;
		}

		public override void GetMandatoryTourSimulatedData(IPersonDayWrapper personDay, List<ITourWrapper> tours) {
			tours.AddRange(CreateToursByPurpose(Global.Settings.Purposes.Work, personDay.UsualWorkplaceTours));

			foreach (var tour in tours) {
				tour.DestinationParcel = personDay.Person.UsualWorkParcel;
				tour.DestinationParcelId = personDay.Person.UsualWorkParcelId;
				tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[personDay.Person.UsualWorkParcel.ZoneId];
				tour.DestinationAddressType = Global.Settings.AddressTypes.UsualWorkplace;
			}

			tours.AddRange(CreateToursByPurpose(Global.Settings.Purposes.Business, ((PersonDayWrapper) personDay).BusinessTours));
			tours.AddRange(CreateToursByPurpose(Global.Settings.Purposes.School, personDay.SchoolTours));

			foreach (var tour in tours.Where(tour => tour.DestinationPurpose == Global.Settings.Purposes.School)) {
				tour.DestinationParcel = personDay.Person.UsualSchoolParcel;
				tour.DestinationParcelId = personDay.Person.UsualSchoolParcelId;
				tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[personDay.Person.UsualSchoolParcel.ZoneId];
				tour.DestinationAddressType = Global.Settings.AddressTypes.UsualSchool;
			}
		}

		public override void IncrementSimulatedStops(int destinationPurpose) {
			if (destinationPurpose == Global.Settings.Purposes.Work) {
				SimulatedWorkStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.Business) {
				SimulatedBusinessStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.School) {
				SimulatedSchoolStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.Escort) {
				SimulatedEscortStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.PersonalBusiness) {
				SimulatedPersonalBusinessStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.Shopping) {
				SimulatedShoppingStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.Meal) {
				SimulatedMealStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.Social) {
				SimulatedSocialStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.Recreation) {
				SimulatedRecreationStops++;
			}
			else if (destinationPurpose == Global.Settings.Purposes.Medical) {
				SimulatedMedicalStops++;
			}
		}

		public virtual bool SimulatedBusinessStopsExist() {
			return SimulatedBusinessStops > 0;
		}

		#endregion

		#region init/utility/export methods

		public override void Reset() {
			CreatedBusinessTours = 0;
			SimulatedBusinessTours = 0;
			SimulatedBusinessStops = 0;
			BusinessTours = 0;
			BusinessStops = 0;

			base.Reset();
		}

		protected override IPersonDay ResetPersonDay() {
			_personDay = new PersonDay {
				Id = Id,
				PersonId = PersonId,
				HouseholdDayId = HouseholdDayId,
				HouseholdId = HouseholdId,
				PersonSequence = PersonSequence,
				Day = Day,
				DayBeginsAtHome = DayBeginsAtHome,
				DayEndsAtHome = DayEndsAtHome,
				ExpansionFactor = ExpansionFactor
			};

			return _personDay;
		}

		#endregion
	}
}