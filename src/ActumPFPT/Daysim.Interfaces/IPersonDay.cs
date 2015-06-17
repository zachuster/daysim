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
using Daysim.Attributes;

namespace Daysim.Interfaces {
	public interface IPersonDay : IModel
	{
		[ColumnName("person_id")]
		int PersonId { get; set; }

		[ColumnName("household_day_id")]
		int HouseholdDayId { get; set; }

		[ColumnName("hhno")]
		int HouseholdId { get; set; }

		[ColumnName("pno")]
		int PersonSequence { get; set; }

		[ColumnName("day")]
		int Day { get; set; }

		[ColumnName("beghom")]
		int DayBeginsAtHome { get; set; }

		[ColumnName("endhom")]
		int DayEndsAtHome { get; set; }

		[ColumnName("hbtours")]
		int HomeBasedTours { get; set; }

		[ColumnName("wbtours")]
		int WorkBasedTours { get; set; }

		[ColumnName("uwtours")]
		int UsualWorkplaceTours { get; set; }

		[ColumnName("wktours")]
		int WorkTours { get; set; }

		[ColumnName("sctours")]
		int SchoolTours { get; set; }

		[ColumnName("estours")]
		int EscortTours { get; set; }

		[ColumnName("pbtours")]
		int PersonalBusinessTours { get; set; }

		[ColumnName("shtours")]
		int ShoppingTours { get; set; }

		[ColumnName("mltours")]
		int MealTours { get; set; }

		[ColumnName("sotours")]
		int SocialTours { get; set; }

		[ColumnName("retours")]
		int RecreationTours { get; set; }

		[ColumnName("metours")]
		int MedicalTours { get; set; }

		[ColumnName("wkstops")]
		int WorkStops { get; set; }

		[ColumnName("scstops")]
		int SchoolStops { get; set; }

		[ColumnName("esstops")]
		int EscortStops { get; set; }

		[ColumnName("pbstops")]
		int PersonalBusinessStops { get; set; }

		[ColumnName("shstops")]
		int ShoppingStops { get; set; }

		[ColumnName("mlstops")]
		int MealStops { get; set; }

		[ColumnName("sostops")]
		int SocialStops { get; set; }

		[ColumnName("restops")]
		int RecreationStops { get; set; }

		[ColumnName("mestops")]
		int MedicalStops { get; set; }

		[ColumnName("wkathome")]
		int WorkAtHomeDuration { get; set; }

		[ColumnName("pdexpfac")]
		double ExpansionFactor { get; set; }
	}
}
