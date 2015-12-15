using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Solita.Episerver.WebApi.Attributes
{
    /// <summary>
    /// Checks if any of the parameters is null or has bad modelstate.
    /// Returns HttpStatusCode.BadRequest in both cases.
    ///  
    /// It is strongly advised to use dto objects, and hide your possible null objects to the model object.
    /// </summary>
    public class ValidateWebApiModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext ac)
        {
            // If model is null, then the modelstate would be always valid
            if (ac.ActionArguments.Any(kv => kv.Value == null)) {
                ac.Response = ac.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Arguments cannot be null");
                return;
            }

            // The model was not null, so check the modelstate
            if (!ac.ModelState.IsValid) {
                ac.Response = ac.Request.CreateErrorResponse(HttpStatusCode.BadRequest, ac.ModelState);
                return;
            }
        }
    }
}