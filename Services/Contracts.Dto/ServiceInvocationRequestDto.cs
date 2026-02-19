namespace Contracts.Dto
{
    public class ServiceInvocationRequestDto
    {
        public string ManagerName { get; set; }
        public string MethodName { get; set; }
        public object?[]? Parameters { get; set; } // Make parameters optional
    }
}
