using backend.DTOs.ShopDTOs;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Quản Lý Giỏ Hàng Mua Sắm (Shopping Cart Service).
    /// Chịu trách nhiệm xử lý toàn bộ các thao tác của khách hàng đối với giỏ hàng: 
    /// Xem, Thêm sản phẩm, Thay đổi số lượng, Xóa món, Làm sạch giỏ và Đồng bộ giỏ hàng vãng lai (Guest Cart).
    /// </summary>
    public interface IShopCartService
    {
        #region Truy vấn Giỏ hàng (Read Query)
        /// <summary>
        /// Lấy toàn bộ thông tin chi tiết Giỏ hàng hiện tại của Khách hàng.
        /// Đồng thời thực hiện tính toán lại giá tiền, chương trình khuyến mãi và kiểm tra tồn kho thời gian thực (Available Stock).
        /// </summary>
        /// <param name="customerId">ID Định danh của Khách hàng.</param>
        /// <returns>Cấu trúc tổng quan Giỏ hàng (ShopCartDto) kèm danh sách sản phẩm và tổng tiền thanh toán.</returns>
        Task<ShopCartDto> GetCartAsync(int customerId);
        #endregion

        #region Thao tác Giỏ hàng (Command / Write Workflow)
        /// <summary>
        /// Thêm một sản phẩm (Variant) với Đơn vị tính (UoM) và số lượng cụ thể vào giỏ hàng.
        /// Nghiệp vụ: Nếu sản phẩm đó với đúng UoM đã tồn tại trong giỏ trước đó, 
        /// hệ thống sẽ tự động cộng dồn số lượng thay vì tạo ra dòng mới.
        /// </summary>
        /// <param name="customerId">ID Khách hàng.</param>
        /// <param name="request">Thông tin sản phẩm thêm vào (VariantId, UoMId, Quantity).</param>
        Task<ShopCartDto> AddItemAsync(int customerId, ShopCartAddDto request);

        /// <summary>
        /// Cập nhật số lượng của một dòng sản phẩm cụ thể trong giỏ (Khi khách bấm tăng/giảm trên giao diện).
        /// Nghiệp vụ: Nếu số lượng truyền lên bằng 0 hoặc âm, Service sẽ tự động xóa dòng sản phẩm đó khỏi giỏ.
        /// </summary>
        /// <param name="customerId">ID Khách hàng.</param>
        /// <param name="cartItemId">ID dòng sản phẩm trong giỏ (ShoppingCartItem.Id).</param>
        /// <param name="quantity">Số lượng mới.</param>
        Task<ShopCartDto> UpdateItemQuantityAsync(int customerId, int cartItemId, decimal quantity);

        /// <summary>
        /// Xóa hẳn một dòng sản phẩm khỏi giỏ hàng.
        /// </summary>
        /// <param name="customerId">ID Khách hàng.</param>
        /// <param name="cartItemId">ID dòng sản phẩm cần xóa.</param>
        Task<ShopCartDto> RemoveItemAsync(int customerId, int cartItemId);

        /// <summary>
        /// Làm sạch toàn bộ giỏ hàng (Xóa toàn bộ các món). 
        /// Thường được gọi tự động ngay sau khi Khách hàng bấm Thanh toán thành công (Checkout Success).
        /// </summary>
        /// <param name="customerId">ID Khách hàng.</param>
        Task<ShopCartDto> ClearCartAsync(int customerId);
        #endregion

        #region Nghiệp vụ Trải nghiệm: Đồng bộ Giỏ hàng Vãng lai (Guest Cart Sync)
        /// <summary>
        /// Đồng bộ và gộp giỏ hàng vãng lai vào tài khoản chính thức.
        /// NGHIỆP VỤ UX LÕI: Khi khách chưa đăng nhập, họ chọn vài món bỏ vào giỏ (lưu trên trình duyệt). 
        /// Khi họ bấm Đăng nhập, Frontend gọi hàm này để đẩy toàn bộ danh sách đó lên Server, 
        /// gộp thông minh vào giỏ hàng Server-side của tài khoản hiện tại rồi trả về giỏ hàng hợp nhất.
        /// </summary>
        /// <param name="customerId">ID Khách hàng vừa đăng nhập thành công.</param>
        /// <param name="request">Danh sách các món hàng vãng lai cần đồng bộ.</param>
        Task<ShopCartDto> SyncGuestCartAsync(int customerId, ShopSyncGuestCartDto request);
        #endregion
    }
}