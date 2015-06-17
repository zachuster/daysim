// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Ninject;

namespace Daysim.DomainModels.Actum {
	public class ActumHouseholdWrapper : HouseholdWrapper
	{
		public ActumHouseholdWrapper(ActumHousehold household):base(household)
		{
			
		}

		public int MunicipalCode { get { return ((ActumHousehold) _household).MunicipalCode; }}

		public double StationDistance { get { return ((ActumHousehold) _household).StationDistance; }}

		public int ParkingAvailability { get { return ((ActumHousehold) _household).ParkingAvailability; }}

		public int InternetPaymentMethod { get { return ((ActumHousehold) _household).InternetPaymentMethod; }}


		protected override IHouseholdDayWrapper CreateHouseholdDay() {
			return Global.Kernel.Get<HouseholdDayWrapperFactory>().HouseholdDayWrapperCreator.CreateWrapper(new ActumHouseholdDay {
				Id = Id, // ++_nextHouseholdDayId,
				HouseholdId = Id,
				Day = 1
			}, this);
		}
	}
}
