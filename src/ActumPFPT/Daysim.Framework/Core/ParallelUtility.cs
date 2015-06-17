// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Threading;
using System.Threading.Tasks;

namespace Daysim.Framework.Core {
	public static class ParallelUtility {

		public static readonly int NBatches = Global.NBatches;
		private static int[] _threadToThreadIDMap = new int[NBatches];
		
		public static void Register(int threadId, int batchNumber)
		{
			try
			{
				_threadToThreadIDMap[batchNumber] = threadId;
			}
			catch (Exception)
			{
				throw new Exception("Invalid BatchNumber " + batchNumber);
			}
		}

		public static int GetBatchFromThreadId()
		{
			for ( int i = 0; i < NBatches; i++ )
			{
				if ( _threadToThreadIDMap[i] == Thread.CurrentThread.ManagedThreadId )
					return i;
			}

			return NBatches;
		}

		public static void While(ParallelOptions parallelOptions, Func<bool> condition, Action<ParallelLoopState> body) {
			Parallel.ForEach(new InfinitePartitioner(), parallelOptions,
				(ignored, state) => {
					if (condition()) {
						body(state);
					}
					else {
						state.Stop();
					}
				});
		}
	}
}