namespace CommanderPlugin
{
    public class Response
    {
        public OperationStatus Status { get; set; }
        public string ErrorMessage { get; set; }

        public Response()
        {

        }

        public Response(OperationStatus status)
        {
            Status = status;
        }

        public Response(string errorMessage) : this(OperationStatus.Fail)
        {
            ErrorMessage = errorMessage;
        }
    }
}
