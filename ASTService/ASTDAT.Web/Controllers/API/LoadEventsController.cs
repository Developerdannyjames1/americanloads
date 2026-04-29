using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using ASTDAT.Web.Infrastructure;

namespace ASTDAT.Web.Controllers.API
{
    [Authorize]
    [RoutePrefix("api/LoadEvents")]
    public class LoadEventsController : ApiController
    {
        /// <summary>Long-poll friendly: returns events with Id &gt; afterId (in-process log; add SignalR later for true push).</summary>
        [Route("Since")]
        [HttpGet]
        public IHttpActionResult Since(long afterId = 0)
        {
            if (!LoadboardPermissions.CanViewLoads(HttpContext.Current.User))
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Forbidden,
                    "You do not have access to loadboard events."));
            }
            return Ok(new { events = LoadEventLog.GetAfter(afterId) });
        }
    }
}
