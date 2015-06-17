using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface IHouseholdTotals 
	{
		int FulltimeWorkers { get; set; }

		int PartTimeWorkers { get; set; }

		int RetiredAdults { get; set; }

		int NonworkingAdults { get; set; }

		int UniversityStudents { get; set; }

		int DrivingAgeStudents { get; set; }

		int ChildrenAge5Through15 { get; set; }

		int ChildrenUnder5 { get; set; }

		int ChildrenUnder16 { get; set; }

		int Adults { get; set; }

		int DrivingAgeMembers { get; set; }

		int WorkersPlusStudents { get; set; }

		int FullAndPartTimeWorkers { get; set; }

		int AllWorkers { get; set; }

		int AllStudents { get; set; }

		double PartTimeWorkersPerDrivingAgeMembers { get; set; }

		double RetiredAdultsPerDrivingAgeMembers { get; set; }

		double UniversityStudentsPerDrivingAgeMembers { get; set; }

		double DrivingAgeStudentsPerDrivingAgeMembers { get; set; }

		double ChildrenUnder5PerDrivingAgeMembers { get; set; }

		double HomeBasedPersonsPerDrivingAgeMembers { get; set; }
	}
}
