namespace backend.DTOs.PaymentDTOs
{
    public class VnPayPaymentRequestDto
    {
        public required string OrderCode { get; set; }
        public string? OrderDescription { get; set; }
        public string? BankCode { get; set; }
    }

    public class VnPayPaymentResponseDto
    {
        public required string PaymentUrl { get; set; }
        public required string OrderCode { get; set; }
    }

    public class VnPayCallbackResultDto
    {
        public bool IsSuccess { get; set; }
        public string? OrderCode { get; set; }
        public string? TransactionNo { get; set; }
        public string? ResponseCode { get; set; }
        public string? BankCode { get; set; }
        public decimal Amount { get; set; }
        public string? OrderInfo { get; set; }
        public string? Message { get; set; }
    }

    public class VnPayIpnResponseDto
    {
        public required string RspCode { get; set; }
        public required string Message { get; set; }
    }
}
