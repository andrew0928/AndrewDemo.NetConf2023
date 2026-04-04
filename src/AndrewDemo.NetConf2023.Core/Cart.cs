using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Carts;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public class Cart
    {
        public Cart()
        {
            LineItems ??= new List<LineItem>();
        }

        [BsonId(true)]
        public int Id { get; set; }

        public List<LineItem> LineItems { get; set; } = new List<LineItem>();


        // line-based cart CRUD
        public bool AddProducts(string productId, int qty, DateTime addedAtUtc, string? parentLineId = null)
        {
            if (string.IsNullOrWhiteSpace(productId) || qty == 0)
            {
                return false;
            }

            LineItems ??= new List<LineItem>();

            if (qty < 0)
            {
                return RemoveProducts(productId, -qty);
            }

            LineItems.Add(new LineItem
            {
                LineId = Guid.NewGuid().ToString("N"),
                ParentLineId = string.IsNullOrWhiteSpace(parentLineId) ? null : parentLineId,
                ProductId = productId,
                Quantity = qty,
                AddedAt = addedAtUtc
            });

            return true;
        }

        public bool RemoveProducts(string productId, int qty = 1)
        {
            if (string.IsNullOrWhiteSpace(productId) || qty <= 0)
            {
                return false;
            }

            LineItems ??= new List<LineItem>();

            var matchedLines = LineItems
                .Select((line, index) => new { line, index })
                .Where(x => x.line.ProductId == productId)
                .OrderByDescending(x => x.index)
                .ToList();

            if (matchedLines.Sum(x => x.line.Quantity) < qty)
            {
                return false;
            }

            var remaining = qty;
            foreach (var entry in matchedLines)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (entry.line.Quantity <= remaining)
                {
                    LineItems.RemoveAt(entry.index);
                    remaining -= entry.line.Quantity;
                    continue;
                }

                LineItems[entry.index] = entry.line with
                {
                    Quantity = entry.line.Quantity - remaining
                };
                remaining = 0;
            }

            return true;
        }
    }
}
