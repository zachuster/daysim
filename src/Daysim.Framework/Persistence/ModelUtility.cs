﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System.IO;
using System.Runtime.InteropServices;

namespace Daysim.Framework.Persistence {
	public static class ModelUtility {
		public static FileInfo GetIndexFile(string path) {
			var directoryName = Path.GetDirectoryName(path);
			var filename = Path.GetFileNameWithoutExtension(path) + ".pk";
			var indexPath = directoryName == null ? filename : Path.Combine(directoryName, filename);

			return new FileInfo(indexPath);
		}

		public static TModel PtrToStructure<TModel>(ref byte[] buffer) {
			var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

			try {
				var ptr = handle.AddrOfPinnedObject();
				var model = (TModel) Marshal.PtrToStructure(ptr, typeof (TModel));

				return model;
			}
			finally {
				handle.Free();
			}
		}
	}
}