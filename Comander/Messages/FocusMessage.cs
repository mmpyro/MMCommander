namespace Comander.Messages
{
    public struct FocusMessage
    {
        public string ManagerType { get; set; }
    }

    public struct SetFocusMessage
    {
        public string GridName { get; set; }
    }
}
