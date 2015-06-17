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
using Daysim.Framework.Core;
using Daysim.Interfaces;

namespace Daysim.DomainModels.Actum {
	public class ActumPersonWrapper : PersonWrapper
	{
		public ActumPersonWrapper(ActumPerson person, IHouseholdWrapper household) : base(person, household)
      //public ActumPersonWrapper(ActumPerson person, ActumHouseholdWrapper household) : base(person, household)
		{
		}

		public int MainOccupation
		{
			get { return ((ActumPerson) _person).MainOccupation; }
		}

		public int EducationLevel
		{
			get { return ((ActumPerson) _person).EducationLevel; }
		}

		public bool HasBike
		{
			get { return ((ActumPerson) _person).HasBike == 1; }
		}

		public bool HasDriversLicense
		{
			get { return ((ActumPerson) _person).HasDriversLicense == 1; }
		}

		public bool HasCarShare
		{
			get { return ((ActumPerson) _person).HasCarShare == 1; }
		}

		public int Income
		{
			get { return ((ActumPerson) _person).Income; }
		}

		public bool HasMC
		{
			get { return ((ActumPerson) _person).HasMC == 1; }
		}

		public bool HasMoped
		{
			get { return ((ActumPerson) _person).HasMoped == 1; }
		}

		public bool HasWorkParking
		{
			get { return ((ActumPerson) _person).HasWorkParking == 1; }
		}

		public int WorkHoursPerWeek
		{
			get { return ((ActumPerson) _person).WorkHoursPerWeek; }
		}

		public int FlexibleWorkHours
		{
			get { return ((ActumPerson) _person).FlexibleWorkHours; }
		}

		public bool HasSchoolParking
		{
			get { return ((ActumPerson) _person).HasSchoolParking == 1; }
		}

		public int ActumPersonType {
			get {
				return
					_person.PersonType <= Constants.PersonType.NON_WORKING_ADULT
						? _person.PersonType
						: _person.PersonType <= Constants.PersonType.DRIVING_AGE_STUDENT
							? Constants.ActumPersonType.GYMNASIUM_OR_UNIVERSITY_STUDENT
							: _person.PersonType == Constants.PersonType.CHILD_AGE_5_THROUGH_15
								? Constants.ActumPersonType.CHILD_AGE_5_THROUGH_15
								: Constants.ActumPersonType.CHILD_UNDER_5;
			}
		}




	}

}
