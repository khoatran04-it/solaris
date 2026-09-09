namespace backend.DTOs.ShopDTOs
{
    /// <summary>
    /// DTO thông tin Điểm phục vụ / Kho Bán Lẻ dành cho khách hàng Shop B2C.
    /// </summary>
    public class ShopWarehouseDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? WarehouseType { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? StreetAddress { get; set; }
        public string? FullAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsActive { get; set; }
    }
}
