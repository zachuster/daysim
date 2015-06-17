// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using Daysim.DomainModels;
using Daysim.DomainModels.Actum;
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Framework.Roster;
using Ninject.Modules;

namespace Daysim {
	public sealed class DaysimModule : NinjectModule {
		public override void Load() {
			Bind<ImporterFactory>().ToSelf().InSingletonScope();
			Bind<ExporterFactory>().ToSelf().InSingletonScope();

			Bind<Reader<ParkAndRideNode>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingParkAndRideNodePath);
			Bind<Reader<ParcelNode>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingParcelNodePath);
			Bind<Reader<Parcel>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingParcelPath);
			Bind<Reader<Zone>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingZonePath);
			Bind<Reader<TransitStopArea>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingTransitStopAreaPath);
			Bind<Reader<Household>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingHouseholdPath);
			Bind<Reader<ActumHousehold>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingHouseholdPath);
			Bind<Reader<Person>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingPersonPath);
			Bind<Reader<ActumPerson>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingPersonPath);
			Bind<Reader<HouseholdDay>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingHouseholdDayPath);
			Bind<Reader<ActumHouseholdDay>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingHouseholdDayPath);
			Bind<Reader<PersonDay>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingPersonDayPath);
			Bind<Reader<ActumPersonDay>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingPersonDayPath);
			Bind<Reader<Tour>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingTourPath);
			//Bind<Reader<ActumTour>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingTourPath);
			Bind<Reader<Trip>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingTripPath);
			Bind<Reader<ActumTrip>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingTripPath);
			Bind<Reader<JointTour>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingJointTourPath);
			Bind<Reader<FullHalfTour>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingFullHalfTourPath);
			Bind<Reader<PartialHalfTour>>().ToSelf().InSingletonScope().WithConstructorArgument("path", Global.WorkingPartialHalfTourPath);

			Bind<PersonPersistenceFactory>().ToSelf().InSingletonScope();
			Bind<PersonWrapperFactory>().ToSelf().InSingletonScope();
			Bind<HouseholdPersistenceFactory>().ToSelf().InSingletonScope();
			Bind<HouseholdWrapperFactory>().ToSelf().InSingletonScope();
			Bind<TripPersistenceFactory>().ToSelf().InSingletonScope();
			Bind<TripWrapperFactory>().ToSelf().InSingletonScope();
			Bind<TourPersistenceFactory>().ToSelf().InSingletonScope();
			Bind<TourWrapperFactory>().ToSelf().InSingletonScope();
			Bind<PersonDayPersistenceFactory>().ToSelf().InSingletonScope();
			Bind<PersonDayWrapperFactory>().ToSelf().InSingletonScope();
			Bind<HouseholdDayPersistenceFactory>().ToSelf().InSingletonScope();
			Bind<HouseholdDayWrapperFactory>().ToSelf().InSingletonScope();

			Bind<SkimFileReaderFactory>().ToSelf().InSingletonScope();

			Bind<SamplingWeightsSettingsFactory>().ToSelf().InSingletonScope();
		}
	}
}