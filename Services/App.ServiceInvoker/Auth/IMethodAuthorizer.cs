using App.ServiceInvoker.Reflection;

namespace App.ServiceInvoker.Auth
{
    public interface IMethodAuthorizer
    {
        /// <summary>
        /// Enforces method-level access using ServiceInvoker method metadata.
        /// Must throw UnauthorizedAccessException when access is not permitted.
        /// </summary>
        void Authorize(MethodMetadata methodMetadata);
    }
}
