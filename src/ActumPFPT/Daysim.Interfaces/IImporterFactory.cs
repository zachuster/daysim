using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface IImporterFactory
	{
		IImporter<TModel> GetImporter<TModel>(string inputPath, char delimiter) where TModel : IModel, new();
	}
}
