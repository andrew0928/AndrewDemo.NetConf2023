using System;

namespace AndrewDemo.NetConf2023.Core.Checkouts
{
    public enum CheckoutCreateStatus
    {
        Succeeded = 0,
        CartNotFound = 1
    }

    public sealed class CheckoutCreateCommand
    {
        public int CartId { get; set; }
        public Member RequestMember { get; set; } = null!;
    }

    public sealed class CheckoutCreateResult
    {
        public CheckoutCreateStatus Status { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int TransactionId { get; private set; }
        public DateTime TransactionStartAt { get; private set; }
        public int ConsumerId { get; private set; }
        public string ConsumerName { get; private set; } = string.Empty;

        public static CheckoutCreateResult CreateSucceeded(int transactionId, DateTime transactionStartAt, int consumerId, string consumerName)
        {
            return new CheckoutCreateResult
            {
                Status = CheckoutCreateStatus.Succeeded,
                TransactionId = transactionId,
                TransactionStartAt = transactionStartAt,
                ConsumerId = consumerId,
                ConsumerName = consumerName
            };
        }

        public static CheckoutCreateResult CreateCartNotFound(string errorMessage)
        {
            return new CheckoutCreateResult
            {
                Status = CheckoutCreateStatus.CartNotFound,
                ErrorMessage = errorMessage
            };
        }
    }

    public enum CheckoutCompleteStatus
    {
        Succeeded = 0,
        TransactionNotFound = 1,
        BuyerMismatch = 2,
        CartNotFound = 3,
        ConsumerNotFound = 4,
        ProductNotFound = 5
    }

    public sealed class CheckoutCompleteCommand
    {
        public int TransactionId { get; set; }
        public int PaymentId { get; set; }
        public int Satisfaction { get; set; }
        public string? ShopComments { get; set; }
        public Member RequestMember { get; set; } = null!;
    }

    public sealed class CheckoutCompleteResult
    {
        public CheckoutCompleteStatus Status { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int TransactionId { get; private set; }
        public int PaymentId { get; private set; }
        public DateTime TransactionCompleteAt { get; private set; }
        public int ConsumerId { get; private set; }
        public string ConsumerName { get; private set; } = string.Empty;
        public Order? OrderDetail { get; private set; }

        public static CheckoutCompleteResult CreateSucceeded(int transactionId, int paymentId, DateTime transactionCompleteAt, int consumerId, string consumerName, Order orderDetail)
        {
            return new CheckoutCompleteResult
            {
                Status = CheckoutCompleteStatus.Succeeded,
                TransactionId = transactionId,
                PaymentId = paymentId,
                TransactionCompleteAt = transactionCompleteAt,
                ConsumerId = consumerId,
                ConsumerName = consumerName,
                OrderDetail = orderDetail
            };
        }

        public static CheckoutCompleteResult CreateTransactionNotFound(string errorMessage)
        {
            return CreateFailure(CheckoutCompleteStatus.TransactionNotFound, errorMessage);
        }

        public static CheckoutCompleteResult CreateCartNotFound(string errorMessage)
        {
            return CreateFailure(CheckoutCompleteStatus.CartNotFound, errorMessage);
        }

        public static CheckoutCompleteResult CreateBuyerMismatch(string errorMessage)
        {
            return CreateFailure(CheckoutCompleteStatus.BuyerMismatch, errorMessage);
        }

        public static CheckoutCompleteResult CreateConsumerNotFound(string errorMessage)
        {
            return CreateFailure(CheckoutCompleteStatus.ConsumerNotFound, errorMessage);
        }

        public static CheckoutCompleteResult CreateProductNotFound(string errorMessage)
        {
            return CreateFailure(CheckoutCompleteStatus.ProductNotFound, errorMessage);
        }

        private static CheckoutCompleteResult CreateFailure(CheckoutCompleteStatus status, string errorMessage)
        {
            return new CheckoutCompleteResult
            {
                Status = status,
                ErrorMessage = errorMessage
            };
        }
    }
}
