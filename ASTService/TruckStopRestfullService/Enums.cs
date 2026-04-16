using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckStopRestfullService
{
    public enum LoadStates
    {
        Posted = 1,
        Paused = 2,
        Accepted = 3,
        Unassigned = 4,
        Closed = 5
    }
    public enum LoadStateReasons
    {
        NotSpecified = 0,
        Expired = 1,
        Cancelled = 2,
        ShipperChange = 3,
        Delivered = 4,
        Dropped = 5,
        Revoked = 5
    }
}
