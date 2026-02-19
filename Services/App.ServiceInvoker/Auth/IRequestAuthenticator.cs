using Microsoft.AspNetCore.Http;

namespace App.ServiceInvoker.Auth
{
    public interface IRequestAuthenticator
    {
        /// <summary>
        /// Establish HttpContext.User for the current request (if a token is present).
        /// Implementations may choose to throw for invalid tokens, or simply leave the
        /// request unauthenticated.
        /// </summary>
        Task AuthenticateAsync(HttpRequest request);
    }
}
