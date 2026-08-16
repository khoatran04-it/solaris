using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace backend.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        /*
         * Constructor: Nhận vào RequestDelegate. 
         * "_next" chính là cánh cửa để cho phép Request đi tiếp vào Controller
         */
        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /*
         * Hàm này sẽ tự động chạy mỗi khi có một Request đi qua Trạm gác
         */
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Cho phép Request đi qua Trạm gác để vào Controller
                await _next(context);
            }
            catch (Exception ex)
            {
                // NẾU CONTROLLER BỊ SẬP -> Bắt lấy lỗi tại đây!
                await HandleExceptionAsync(context, ex);
            }
        }

        /*
         * Hàm dọn dẹp hiện trường và trả về JSON cho React
         */
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json"; // Set định dạng trả về là JSON
            context.Response.StatusCode = 500; // Mặc định lỗi sập server là 500

            // Tự định nghĩa cấu trúc lỗi bạn muốn gửi về Frontend
            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Hệ thống đang gặp sự cố, vui lòng thử lại sau!",
                Details = exception.Message // (Thực tế khi lên Production người ta thường giấu cái Details này đi)
            };

            // Đóng gói thành JSON và gửi về
            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
    }
}