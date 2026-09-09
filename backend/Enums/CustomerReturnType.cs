namespace backend.Models.Enums
{
    /// <summary>
    /// Hình thức hoàn trả / thu hồi nông sản.
    /// </summary>
    public enum CustomerReturnType
    {
        /// <summary>Thu hồi trực tiếp khi giao (Shipper mang về kho luôn, hàng đã ở kho).</summary>
        DoorstepRefusal = 1,

        /// <summary>Thu hồi tại nhà khách (Khách yêu cầu sau giao hàng, cần điều phối xe thu hồi).</summary>
        PostDeliveryReturn = 2
    }
}
