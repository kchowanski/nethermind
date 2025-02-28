namespace Nethermind.DataMarketplace.Core.Domain
{
    public class Notification
    {
        public string Type { get; set; }
        public object Data { get; set; }

        public Notification()
        {
        }

        public Notification(string type, object data)
        {
            Type = type;
            Data = data;
        }
    }
}