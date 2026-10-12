namespace CLDV6212POE_ST10488555_ST10476800.Models
{
    public class OrderMessage
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public List<string> SelectedItemSKUs { get; set; }
        public double TotalPrice { get; set; }
        public DateTime? OrderTimestamp { get; set; }

        public void ApplyDefaults()
        {
            if (string.IsNullOrWhiteSpace(OrderId))
            {
                OrderId = GenerateOrderId();
            }
            else
            {
                OrderId = OrderId.Trim();
            }

            if (OrderTimestamp == null)
            {
                OrderTimestamp = DateTime.UtcNow;
            }
            else
            {
                OrderTimestamp = OrderTimestamp.Value.ToUniversalTime();
            }

            if (CustomerName != null)
            {
                CustomerName = CustomerName.Trim();
            }

            if (SelectedItemSKUs != null)
            {
                List<string> cleanedSkus = new List<string>();
                for (int i = 0; i < SelectedItemSKUs.Count; i++)
                {
                    string sku = SelectedItemSKUs[i];
                    if (sku != null)
                    {
                        cleanedSkus.Add(sku.Trim().ToUpperInvariant());
                    }
                    else
                    {
                        cleanedSkus.Add(null);
                    }
                }
                SelectedItemSKUs = cleanedSkus;
            }

            TotalPrice = Math.Round(TotalPrice, 2);
        }

        // Checks every field and returns a list of problems. An empty list means the order is valid.
        public List<string> Validate()
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(OrderId))
            {
                errors.Add("OrderId is required.");
            }
            else
            {
                if (!OrderId.StartsWith("ORD-"))
                {
                    errors.Add("OrderId must start with 'ORD-' (for example ORD-2026-8801).");
                }
                if (OrderId.Length > 50)
                {
                    errors.Add("OrderId must be 50 characters or fewer.");
                }
                if (HasCharacterNotAllowedInKey(OrderId))
                {
                    errors.Add("OrderId cannot contain /, \\, #, ? or control characters.");
                }
            }

            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                errors.Add("CustomerName is required.");
            }
            else if (CustomerName.Length < 2 || CustomerName.Length > 100)
            {
                errors.Add("CustomerName must be between 2 and 100 characters.");
            }

            if (SelectedItemSKUs == null || SelectedItemSKUs.Count == 0)
            {
                errors.Add("SelectedItemSKUs must contain at least one item SKU.");
            }
            else
            {
                if (SelectedItemSKUs.Count > 50)
                {
                    errors.Add("An order cannot contain more than 50 items.");
                }
                for (int i = 0; i < SelectedItemSKUs.Count; i++)
                {
                    string sku = SelectedItemSKUs[i];
                    if (string.IsNullOrWhiteSpace(sku))
                    {
                        errors.Add("SelectedItemSKUs[" + i + "] is empty.");
                    }
                    else if (sku.Length > 20)
                    {
                        errors.Add("SelectedItemSKUs[" + i + "] is longer than 20 characters.");
                    }
                    else if (sku.Contains(","))
                    {
                        errors.Add("SelectedItemSKUs[" + i + "] cannot contain a comma.");
                    }
                }
            }

            if (TotalPrice <= 0)
            {
                errors.Add("TotalPrice must be greater than 0.");
            }
            else if (TotalPrice > 10000)
            {
                errors.Add("TotalPrice cannot be more than 10000.");
            }

            if (OrderTimestamp == null)
            {
                errors.Add("OrderTimestamp is required.");
            }
            else
            {
                DateTime timestamp = OrderTimestamp.Value.ToUniversalTime();
                if (timestamp.Year < 2020)
                {
                    errors.Add("OrderTimestamp is too far in the past.");
                }
                if (timestamp > DateTime.UtcNow.AddDays(7))
                {
                    errors.Add("OrderTimestamp cannot be more than 7 days in the future.");
                }
            }

            return errors;
        }

        // Builds an id in the same shape as the brief, for example ORD-2026-482913.
        private static string GenerateOrderId()
        {
            int randomNumber = Random.Shared.Next(100000, 1000000);
            return "ORD-" + DateTime.UtcNow.Year + "-" + randomNumber;
        }

        // Azure Table keys cannot contain these characters, so we reject them up front.
        private static bool HasCharacterNotAllowedInKey(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '/' || c == '\\' || c == '#' || c == '?' || char.IsControl(c))
                {
                    return true;
                }
            }
            return false;
        }
    }
}