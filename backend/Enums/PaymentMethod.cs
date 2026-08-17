namespace backend.Models.Enums
{
    public enum PaymentMethod
    {
        COD = 1,            // Thu tiền khi giao hàng (Cash on Delivery)
        BankTransfer = 2,   // Chuyển khoản ngân hàng
        EWallet = 3,        // Ví điện tử (Momo, ZaloPay, VNPay)
        CreditCard = 4      // Thẻ tín dụng / Ghi nợ quốc tế
    }
}
