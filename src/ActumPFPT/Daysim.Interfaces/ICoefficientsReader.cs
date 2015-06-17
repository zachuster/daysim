using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface ICoefficientsReader
	{
		ICoefficient[] Read(string path, out string title, out ICoefficient sizeFunctionMultiplier,
		                   out ICoefficient nestCoefficient);
	}
}
