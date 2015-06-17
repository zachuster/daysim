using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface IMinuteSpan : IEquatable<IMinuteSpan>
	{
		int Index { get; }

		int Start { get; set; }

		int End { get; set; }

		bool Keep { get; set; }

		int Middle { get; }

		int Duration { get; }
	}
}
