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
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;
using Daysim.ModelRunners;
using Ninject;

namespace Daysim.Factories {
	public class PersonDayPersister<TModel> : IPersonDayPersister where TModel : class, IPersonDay, new()
	{
		private Exporter<TModel> _exporter;
		private Reader<TModel> _reader;
		private Hdf5Persister<TModel> _hdf5Exporter;

		public IEnumerable<IPersonDay> Seek(int id, string householdFk)
		{
			if (_reader == null)
			{
				_reader = Global.Kernel.Get<Reader<TModel>>();
			}

			return _reader.Seek(id, householdFk);
		}

		public void Export(IPersonDay personDay)
		{

			if (_exporter == null)
			{
				_exporter = ChoiceModelFactory.ExporterFactory.GetExporter<TModel>(Global.GetOutputPath(Global.Configuration.OutputPersonDayPath),
				                                                                   Global.Configuration.OutputPersonDayDelimiter);
			}
			_exporter.Export((TModel) personDay);
			if (Global.Configuration.WriteTripsToHDF5)
			{
				if (_hdf5Exporter == null)
				{
					_hdf5Exporter = new Hdf5Persister<TModel>();
				}
				_hdf5Exporter.Export((TModel) personDay);
			}
			ChoiceModelFactory.PersonDayFileRecordsWritten++;
			ChoiceModelFactory.PersonDayHomeBasedToursCheckSum+=personDay.HomeBasedTours;
			ChoiceModelFactory.PersonDayWorkBasedToursCheckSum+=personDay.WorkBasedTours;
		}

		public void Dispose()
		{
			if (Global.Configuration.WriteTripsToHDF5)
			{
				_hdf5Exporter.Flush();
			}
			if (_exporter != null)
				_exporter.Dispose();
		}

		public void BeginImport(IImporterFactory importerFactory, string path, string message)
		{
			importerFactory.GetImporter<TModel>(Global.GetInputPath(Global.Configuration.InputPersonDayPath),
			                                    Global.Configuration.InputPersonDayDelimiter).BeginImport(path, message);
		}

		public int Count
		{
			get
			{
				if (_reader == null)
					_reader = Global.Kernel.Get<Reader<TModel>>();
				return _reader.Count;
			}
		}

		public IEnumerator<IPersonDay> GetEnumerator()
		{
			if (_reader == null)
				_reader = Global.Kernel.Get<Reader<TModel>>();
			return _reader.GetEnumerator();
		}

		public void BuildIndex(string indexName, string idName, string parentIdName) {
				Global.Kernel.Get<Reader<TModel>>().BuildIndex(indexName, idName, parentIdName);
		}
	}
}
