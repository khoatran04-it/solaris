using backend.Controllers.Shop;
using backend.DTOs.AiDTOs;
using backend.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module15_AiChatbot
{
    public class ShopAiControllerTests
    {
        private readonly IGeminiChatService _aiService;

        public ShopAiControllerTests()
        {
            _aiService = Substitute.For<IGeminiChatService>();
        }

        private ShopAiController CreateControllerWithClaims(params Claim[] claims)
        {
            var controller = new ShopAiController(_aiService);
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            return controller;
        }

        [Fact]
        public async Task ConfirmInteractiveOrder_WithCustomerIdClaim_ShouldExtractCustomerIdAndReturnOk()
        {
            // Arrange
            var controller = CreateControllerWithClaims(new Claim("CustomerId", "15"));
            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 1,
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto { VariantId = 1, Quantity = 2, UnitPrice = 50000, TotalPrice = 100000 }
                }
            };

            _aiService.ConfirmInteractiveOrderAsync(request, 15)
                .Returns(Task.FromResult(new ConfirmInteractiveOrderResponseDto
                {
                    OrderId = 101,
                    OrderCode = "ORD-TEST-001",
                    TotalAmount = 100000
                }));

            // Act
            var response = await controller.ConfirmInteractiveOrder(request);

            // Assert
            var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
            var result = okResult.Value.Should().BeOfType<ConfirmInteractiveOrderResponseDto>().Subject;
            result.OrderCode.Should().Be("ORD-TEST-001");
            await _aiService.Received(1).ConfirmInteractiveOrderAsync(request, 15);
        }

        [Fact]
        public async Task ConfirmInteractiveOrder_WithNameIdentifierClaim_ShouldExtractCustomerIdAndReturnOk()
        {
            // Arrange
            var controller = CreateControllerWithClaims(new Claim(ClaimTypes.NameIdentifier, "22"));
            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 1,
                Items = new List<InteractiveOrderItemDto>()
            };

            _aiService.ConfirmInteractiveOrderAsync(request, 22)
                .Returns(Task.FromResult(new ConfirmInteractiveOrderResponseDto
                {
                    OrderId = 102,
                    OrderCode = "ORD-TEST-002",
                    TotalAmount = 50000
                }));

            // Act
            var response = await controller.ConfirmInteractiveOrder(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            await _aiService.Received(1).ConfirmInteractiveOrderAsync(request, 22);
        }

        [Fact]
        public async Task ConfirmInteractiveOrder_WithSubClaim_ShouldExtractCustomerIdAndReturnOk()
        {
            // Arrange
            var controller = CreateControllerWithClaims(new Claim("sub", "35"));
            var request = new ConfirmInteractiveOrderRequestDto();

            _aiService.ConfirmInteractiveOrderAsync(request, 35)
                .Returns(Task.FromResult(new ConfirmInteractiveOrderResponseDto
                {
                    OrderId = 103,
                    OrderCode = "ORD-TEST-003",
                    TotalAmount = 75000
                }));

            // Act
            var response = await controller.ConfirmInteractiveOrder(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            await _aiService.Received(1).ConfirmInteractiveOrderAsync(request, 35);
        }

        [Fact]
        public async Task ConfirmInteractiveOrder_WithNameidClaim_ShouldExtractCustomerIdAndReturnOk()
        {
            // Arrange
            var controller = CreateControllerWithClaims(new Claim("nameid", "48"));
            var request = new ConfirmInteractiveOrderRequestDto();

            _aiService.ConfirmInteractiveOrderAsync(request, 48)
                .Returns(Task.FromResult(new ConfirmInteractiveOrderResponseDto
                {
                    OrderId = 104,
                    OrderCode = "ORD-TEST-004",
                    TotalAmount = 90000
                }));

            // Act
            var response = await controller.ConfirmInteractiveOrder(request);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            await _aiService.Received(1).ConfirmInteractiveOrderAsync(request, 48);
        }

        [Fact]
        public async Task ConfirmInteractiveOrder_WhenUnauthorizedAccessExceptionThrown_ShouldReturnBadRequestWithout401()
        {
            // Arrange
            var controller = CreateControllerWithClaims(); // Không có claim nào
            var request = new ConfirmInteractiveOrderRequestDto();

            _aiService.ConfirmInteractiveOrderAsync(request, null)
                .ThrowsAsync(new UnauthorizedAccessException("Vui lòng đăng nhập tài khoản để xác nhận tạo đơn hàng."));

            // Act
            var response = await controller.ConfirmInteractiveOrder(request);

            // Assert - Phải trả về BadRequest (400) chứ KHÔNG ĐƯỢC trả về Unauthorized (401) để tránh kích hoạt interceptor xóa token
            var badRequestResult = response.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }
    }
}
