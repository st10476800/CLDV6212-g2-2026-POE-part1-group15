namespace CoffeeNChillGateway.Models
{
    // The four stages an order moves through, from the till to the student's hands.
    // Keeping them as constants means the spelling is the same everywhere in the app.
    public static class OrderStatus
    {
        public const string Received = "Received";
        public const string Preparing = "Preparing";
        public const string Ready = "Ready";
        public const string Collected = "Collected";
    }
}
