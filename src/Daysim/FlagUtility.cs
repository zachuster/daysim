﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.Framework.Core;

namespace Daysim
{
    public static class FlagUtility
    {
        public static int GetChildFlag(int carOwnership)
        {
            return (carOwnership == Global.Settings.CarOwnerships.Child).ToFlag();
        }

        public static int GetNoCarsFlag(int carOwnership)
        {
            return (carOwnership == Global.Settings.CarOwnerships.NoCars).ToFlag();
        }

        public static int GetCarCompetitionFlag(int carOwnership)
        {
            return (carOwnership == Global.Settings.CarOwnerships.LtOneCarPerAdult).ToFlag();
        }

        public static int GetNoCarCompetitionFlag(int carOwnership)
        {
            return (carOwnership == Global.Settings.CarOwnerships.OneOrMoreCarsPerAdult).ToFlag();
        }

        public static int GetCarDeficitFlag(int carOwnership)
        {
            return (carOwnership == Global.Settings.CarOwnerships.NoCars || carOwnership == Global.Settings.CarOwnerships.LtOneCarPerAdult).ToFlag();
        }
    }
}
