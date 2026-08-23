namespace backend.DTOs.ShippingDTOs
{
    // --- GHN PROVINCE / DISTRICT / WARD ---
    public class GhnProvinceDto
    {
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class GhnDistrictDto
    {
        public int DistrictID { get; set; }
        public int ProvinceID { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class GhnWardDto
    {
        public string WardCode { get; set; } = string.Empty;
        public int DistrictID { get; set; }
        public string WardName { get; set; } = string.Empty;
    }

    // --- GHN SHIPPING FEE CALCULATION ---
    public class GhnCalculateFeeRequestDto
    {
        public int ToDistrictId { get; set; }
        public required string ToWardCode { get; set; }
        public int WeightGram { get; set; } = 1000;
        public decimal SubTotal { get; set; }
    }

    public class GhnCalculateFeeResponseDto
    {
        public decimal TotalFee { get; set; }
        public decimal OriginalFee { get; set; }
        public bool IsFreeShipping { get; set; }
        public decimal FreeShippingThreshold { get; set; }
        public decimal AmountNeededForFreeShipping { get; set; }
        public string? ExpectedDeliveryTime { get; set; }
    }

    // --- GHN CREATE ORDER ---
    public class GhnCreateOrderResponseDto
    {
        public string? OrderCode { get; set; }
        public string? ExpectedDeliveryDate { get; set; }
        public decimal TotalFee { get; set; }
    }

    // --- GHN GENERIC RESPONSE WRAPPER ---
    public class GhnApiResponse<T>
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
