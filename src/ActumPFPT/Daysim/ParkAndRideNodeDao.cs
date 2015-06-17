// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System.Collections.Generic;
using System.Linq;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.ParkAndRideShadowPricing;
using Ninject;

namespace Daysim {
	public sealed class ParkAndRideNodeDao {
		private readonly Dictionary<int, ParkAndRideNodeWrapper> _nodes = new Dictionary<int, ParkAndRideNodeWrapper>();
		private readonly Dictionary<int, int[]> _zoneIdKeys = new Dictionary<int, int[]>();
		private readonly Dictionary<int, int[]> _parcelIdKeys = new Dictionary<int, int[]>();

		public ParkAndRideNodeDao() {
			var reader = Global.Kernel.Get<Reader<ParkAndRideNode>>();
			var zoneIdKeys = new Dictionary<int, HashSet<int>>();
			var parcelIdKeys = new Dictionary<int, HashSet<int>>();

			var parkAndRideShadowPrices = ParkAndRideShadowPriceReader.ReadParkAndRideShadowPrices();

			foreach (var parkAndRideNode in reader) {
				var node = new ParkAndRideNodeWrapper(parkAndRideNode);
				var id = node.Id;

				_nodes.Add(id, node);

				var zoneId = node.ZoneId;
				HashSet<int> zoneIdKey;

				if (!zoneIdKeys.TryGetValue(zoneId, out zoneIdKey)) {
					zoneIdKey = new HashSet<int>();

					zoneIdKeys.Add(zoneId, zoneIdKey);
				}

				zoneIdKey.Add(id);

				var parcelId = node.NearestParcelId;
				HashSet<int> parcelIdKey;

				if (!parcelIdKeys.TryGetValue(parcelId, out parcelIdKey)) {
					parcelIdKey = new HashSet<int>();

					parcelIdKeys.Add(parcelId, parcelIdKey);
				}

				node.SetParkAndRideShadowPricing(parkAndRideShadowPrices);

				parcelIdKey.Add(id);
			}

			foreach (var entry in zoneIdKeys) {
				_zoneIdKeys.Add(entry.Key, entry.Value.ToArray());
			}

			foreach (var entry in parcelIdKeys) {
				_parcelIdKeys.Add(entry.Key, entry.Value.ToArray());
			}
		}

		public IEnumerable<ParkAndRideNodeWrapper> Nodes {
			get { return _nodes.Values; }
		}

		public ParkAndRideNodeWrapper Get(int id) {
			ParkAndRideNodeWrapper parkAndRideNode;

			return _nodes.TryGetValue(id, out parkAndRideNode) ? parkAndRideNode : null;
		}

		public ParkAndRideNodeWrapper[] GetAllByZoneId(int zoneId) {
			int[] key;

			if (!_zoneIdKeys.TryGetValue(zoneId, out key)) {
				return new ParkAndRideNodeWrapper[0];
			}

			var nodes = new ParkAndRideNodeWrapper[key.Length];

			for (var i = 0; i < key.Length; i++) {
				nodes[i] = _nodes[key[i]];
			}

			return nodes;
		}

		public ParkAndRideNodeWrapper[] GetAllByNearestParcelId(int parcelId) {
			int[] key;

			if (!_parcelIdKeys.TryGetValue(parcelId, out key)) {
				return new ParkAndRideNodeWrapper[0];
			}

			var nodes = new ParkAndRideNodeWrapper[key.Length];

			for (var i = 0; i < key.Length; i++) {
				nodes[i] = _nodes[key[i]];
			}

			return nodes;
		}
	}
}