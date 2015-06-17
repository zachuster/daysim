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
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Ninject;

namespace Daysim.DomainModels.Actum {
	public class ActumHouseholdDayWrapper : HouseholdDayWrapper {
		public ActumHouseholdDayWrapper(ActumHouseholdDay householdDay, ActumHouseholdWrapper householdWrapper)
			: base(householdDay, householdWrapper) {
		}

		public int SharedActivityHomeStays {
			get {
				return ((ActumHouseholdDay) _householdDay).SharedActivityHomeStays;
			}
		}

		public int NumberInLargestSharedHomeStay {
			get {
				return ((ActumHouseholdDay) _householdDay).NumberInLargestSharedHomeStay;
			}
		}

		public int StartingMinuteSharedHomeStay {
			get {
				return ((ActumHouseholdDay) _householdDay).StartingMinuteSharedHomeStay;
			}
			set { ((ActumHouseholdDay) _householdDay).StartingMinuteSharedHomeStay = value; }
		}

		public int DurationMinutesSharedHomeStay {
			get {
				return ((ActumHouseholdDay) _householdDay).DurationMinutesSharedHomeStay;
			}
			set { ((ActumHouseholdDay) _householdDay).DurationMinutesSharedHomeStay = value; }
		}

		public int AdultsInSharedHomeStay {
			get {
				return ((ActumHouseholdDay) _householdDay).AdultsInSharedHomeStay;
			}
		}

		public int ChildrenInSharedHomeStay {
			get {
				return ((ActumHouseholdDay) _householdDay).ChildrenInSharedHomeStay;
			}
		}

		public int PrimaryPriorityTimeFlag {
			get {
				return ((ActumHouseholdDay) _householdDay).PrimaryPriorityTimeFlag;
			}
			set { ((ActumHouseholdDay) _householdDay).PrimaryPriorityTimeFlag = value; }
		}

		#region flags, choice model properties, etc.

		public int JointTourFlag {
			get;
			set;
		}

		#endregion


		protected override IPersonDayWrapper CreatePersonDay(IPersonWrapper person) {
			return Global.Kernel.Get<PersonDayWrapperFactory>().PersonDayWrapperCreator.CreateWrapper(new ActumPersonDay {
				Id = person.Id, //++_nextPersonDayId,
				PersonId = person.Id,
				HouseholdDayId = _householdDay.Id,
				HouseholdId = _householdDay.HouseholdId,
				PersonSequence = person.Sequence,
				Day = _householdDay.Day
			}, person, this);
		}

		protected override void CreateNewHouseholdDay(IHouseholdDay temp) {
			_householdDay = new ActumHouseholdDay {
				Id = temp.Id,
				HouseholdId = temp.HouseholdId,
				Day = temp.Day,
				DayOfWeek = temp.DayOfWeek,
				ExpansionFactor = temp.ExpansionFactor
			};
		}

		public override void Reset() {
			//Reset additional things
			base.Reset();
		}
	}
}
