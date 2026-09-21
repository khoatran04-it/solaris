using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryReceiptDTOs;
using backend.Helpers;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Services
{
    /// <summary>
    /// Service xử lý toàn bộ vòng đời Phiếu Nhập Kho & Kiểm định chất lượng hàng hóa (Goods Receipt Note - GRN).
    /// </summary>
    public class InventoryReceiptService : IInventoryReceiptService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;
        private readonly IUoMConversionService _uomConversionService;

        public InventoryReceiptService(
            SolarisDbContext context,
            IMapper mapper,
            IUoMConversionService? uomConversionService = null)
        {
            _context = context;
            _mapper = mapper;
            _uomConversionService = uomConversionService ?? new UoMConversionService(context, mapper);
        }

        #region Truy vấn (Query)
        /// <inheritdoc />
        public async Task<PagedResult<InventoryReceiptReadDto>> GetPagedAsync(
            string? search, int? warehouseId, int? supplierId, int? status, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryReceipts
                .Include(x => x.Warehouse)
                .Include(x => x.Supplier)
                .Include(x => x.ReceivedBy)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower().Trim();
                query = query.Where(x => x.ReceiptCode.ToLower().Contains(lowerSearch) ||
                                         (x.Supplier != null && x.Supplier.Name.ToLower().Contains(lowerSearch)) ||
                                         (x.Note != null && x.Note.ToLower().Contains(lowerSearch)));
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);

            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(x => x.SupplierId == supplierId.Value);

            if (status.HasValue && Enum.IsDefined(typeof(InventoryReceiptStatus), status.Value))
                query = query.Where(x => x.Status == (InventoryReceiptStatus)status.Value);

            if (startDate.HasValue)
                query = query.Where(x => x.CreatedAt >= startDate.Value.Date);

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < end);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<InventoryReceiptReadDto>>(items);

            return new PagedResult<InventoryReceiptReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<InventoryReceiptReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.InventoryReceipts
                .Include(x => x.Warehouse)
                .Include(x => x.Supplier)
                .Include(x => x.ReceivedBy)
                .Include(x => x.Details).ThenInclude(d => d.Variant)
                .Include(x => x.Details).ThenInclude(d => d.Batch)
                .Include(x => x.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.WarehouseId));
            }

            var entity = await query.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho hoặc bạn không có quyền truy cập kho này.");

            return _mapper.Map<InventoryReceiptReadDto>(entity);
        }
        #endregion

        #region Thao tác Dữ liệu & Quy trình (Command & Workflow)
        /// <inheritdoc />
        public async Task<int> CreateAsync(InventoryReceiptCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new InvalidOperationException("Phiếu nhập kho phải có ít nhất 1 dòng kiểm đếm hàng hóa.");

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && !w.IsDeleted);
            if (warehouse == null)
                throw new InvalidOperationException($"Kho nhận hàng với ID {dto.WarehouseId} không tồn tại hoặc đã bị vô hiệu hóa.");

            if (dto.SupplierId.HasValue && dto.SupplierId.Value > 0)
            {
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == dto.SupplierId.Value && !s.IsDeleted);
                if (!supplierExists)
                    throw new InvalidOperationException($"Nhà cung cấp với ID {dto.SupplierId.Value} không tồn tại.");

                if (!string.IsNullOrWhiteSpace(warehouse.WarehouseType) && warehouse.WarehouseType != WarehouseTypeConstants.MasterHub)
                    throw new InvalidOperationException("Hàng hóa nhập từ Nhà cung cấp chỉ được phép nhập vào Kho Tổng.");
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<InventoryReceipt>(dto);

                entity.ReceiptCode = $"IR-{DateTimeHelper.VietnamDateString}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
                entity.Status = InventoryReceiptStatus.Pending;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;

                // Tự động tính toán Thể tích (CBM) và Cân nặng (Weight) nếu chưa được nhập
                var variantIds = entity.Details.Select(d => d.VariantId).Distinct().ToList();
                var variants = await _context.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id);

                // Kiểm tra hợp lệ: Hàng đạt chuẩn > 0 bắt buộc phải có Lô. Hàng bị từ chối 100% không bắt buộc Lô.
                foreach (var d in entity.Details)
                {
                    if (d.AcceptedQuantity > 0 && (!d.BatchId.HasValue || d.BatchId.Value <= 0))
                        throw new InvalidOperationException($"Mặt hàng có mã ID {d.VariantId} có số lượng đạt chuẩn > 0 bắt buộc phải được gắn vào một Lô hàng (Batch).");

                    if (d.AcceptedQuantity == 0 && d.BatchId.HasValue && d.BatchId.Value <= 0)
                        d.BatchId = null;
                }

                // Ràng buộc bảo mật: Lô hàng bắt buộc phải thuộc đúng Biến thể sản phẩm (Chống Cam gán nhầm Lô Táo)
                var batchIds = entity.Details.Where(d => d.BatchId.HasValue && d.BatchId.Value > 0).Select(d => d.BatchId!.Value).Distinct().ToList();
                if (batchIds.Any())
                {
                    var batches = await _context.ProductBatches
                        .Where(b => batchIds.Contains(b.Id))
                        .ToDictionaryAsync(b => b.Id);

                    foreach (var d in entity.Details.Where(d => d.BatchId.HasValue && d.BatchId.Value > 0))
                    {
                        if (batches.TryGetValue(d.BatchId!.Value, out var b) && b.VariantId != d.VariantId)
                        {
                            throw new InvalidOperationException($"Lô hàng '{b.BatchCode}' không thuộc về sản phẩm có mã ID {d.VariantId}. Không được phép gán lô hàng khác loại sản phẩm.");
                        }
                    }
                }

                // RÀNG BUỘC BẢO VỆ PO: Chặn nhận hàng vượt quá số lượng PO & Chặn nhận vào PO đã Đóng/Hủy
                var poDetailIds = entity.Details
                    .Where(d => d.PurchaseOrderDetailId.HasValue && d.PurchaseOrderDetailId.Value > 0)
                    .Select(d => d.PurchaseOrderDetailId!.Value)
                    .Distinct()
                    .ToList();

                if (poDetailIds.Any())
                {
                    var poDetails = await _context.PurchaseOrderDetails
                        .Include(pod => pod.PurchaseOrder)
                        .Include(pod => pod.Variant)
                        .Where(pod => poDetailIds.Contains(pod.Id))
                        .ToDictionaryAsync(pod => pod.Id);

                    foreach (var d in entity.Details.Where(d => d.PurchaseOrderDetailId.HasValue && d.PurchaseOrderDetailId.Value > 0))
                    {
                        if (poDetails.TryGetValue(d.PurchaseOrderDetailId!.Value, out var poDetail))
                        {
                            var po = poDetail.PurchaseOrder;
                            if (po == null || po.IsDeleted)
                                throw new InvalidOperationException($"Đơn đặt mua hàng liên kết với dòng sản phẩm ID {d.VariantId} không tồn tại hoặc đã bị xóa.");

                            if (po.Status == PurchaseOrderStatus.Completed)
                                throw new InvalidOperationException($"Đơn đặt mua hàng '{po.OrderCode}' đã Hoàn tất (Completed). Không thể tiếp tục lập phiếu nhập kho.");

                            if (po.Status == PurchaseOrderStatus.Cancelled)
                                throw new InvalidOperationException($"Đơn đặt mua hàng '{po.OrderCode}' đã bị Hủy (Cancelled). Không thể lập phiếu nhập kho.");

                            if (po.Status != PurchaseOrderStatus.Approved && po.Status != PurchaseOrderStatus.PartiallyReceived)
                                throw new InvalidOperationException($"Đơn đặt mua hàng '{po.OrderCode}' đang ở trạng thái '{po.Status}'. Chỉ có thể nhập hàng cho đơn ở trạng thái Đã duyệt (Approved) hoặc Đã nhận một phần (PartiallyReceived).");

                            // Kiểm tra vượt mức tiếp nhận (Over-Receiving Shield):
                            // Cho phép dung sai tiếp nhận tối đa 10% theo thông lệ chuỗi lạnh nông sản
                            const decimal MAX_OVER_RECEIVE_TOLERANCE = 1.10m;
                            decimal remainingQty = Math.Max(0, poDetail.OrderQuantity - poDetail.ReceivedQuantity);
                            decimal maxAllowedIncoming = Math.Round(remainingQty * MAX_OVER_RECEIVE_TOLERANCE, 2);
                            decimal currentAttemptQty = d.AcceptedQuantity + d.RejectedQuantity;

                            if (currentAttemptQty > maxAllowedIncoming)
                            {
                                throw new InvalidOperationException($"Số lượng tiếp nhận ({currentAttemptQty}) cho sản phẩm '{poDetail.Variant?.Name ?? d.VariantId.ToString()}' vượt quá số lượng còn lại của đơn mua hàng ({remainingQty}) kèm dung sai 10% (tối đa cho phép: {maxAllowedIncoming}).");
                            }
                        }
                    }
                }

                foreach (var d in entity.Details)
                {
                    if (variants.TryGetValue(d.VariantId, out var variant))
                    {
                        var qty = d.AcceptedQuantity;
                        if (!d.CalculatedCbm.HasValue || d.CalculatedCbm.Value <= 0)
                        {
                            var unitCbm = variant.UnitCbm ?? (
                                (variant.LengthCm > 0 && variant.WidthCm > 0 && variant.HeightCm > 0)
                                    ? (variant.LengthCm.Value * variant.WidthCm.Value * variant.HeightCm.Value) / 1000000m
                                    : 0.02m
                            );
                            d.CalculatedCbm = Math.Round(unitCbm * qty, 4);
                        }

                        if (!d.ActualWeightKg.HasValue || d.ActualWeightKg.Value <= 0)
                        {
                            var unitWeight = variant.GrossWeightKg ?? 1.0m;
                            d.ActualWeightKg = Math.Round(unitWeight * qty, 2);
                        }
                    }
                }

                _context.InventoryReceipts.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <inheritdoc />
        public async Task<bool> CompleteReceiptAsync(int id, int receivedById, string? note)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var receipt = await _context.InventoryReceipts
                    .Include(x => x.Details)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (receipt == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

                if (receipt.Status != InventoryReceiptStatus.Pending && receipt.Status != InventoryReceiptStatus.Inspecting)
                    throw new InvalidOperationException("Phiếu nhập kho phải ở trạng thái Chờ nhập kho (Pending) hoặc Đang kiểm tra (Inspecting) mới có thể hoàn tất.");

                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == receivedById && !u.IsDeleted);
                if (!userExists)
                {
                    var firstUser = await _context.IAUsers.FirstOrDefaultAsync(u => !u.IsDeleted);
                    if (firstUser != null) receivedById = firstUser.Id;
                }

                receipt.Status = InventoryReceiptStatus.Completed;
                receipt.ReceivedById = receivedById;
                receipt.ReceiptDate = DateTime.UtcNow;
                receipt.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(note))
                {
                    receipt.Note = string.IsNullOrWhiteSpace(receipt.Note) ? note.Trim() : $"{receipt.Note} | {note.Trim()}";
                }

                // Cập nhật CBM/Weight thực tế nếu còn thiếu
                var rVariantIds = receipt.Details.Select(d => d.VariantId).Distinct().ToList();
                var rVariants = await _context.ProductVariants.Where(v => rVariantIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id);
                foreach (var d in receipt.Details)
                {
                    if (rVariants.TryGetValue(d.VariantId, out var variant))
                    {
                        var unitCbm = variant.UnitCbm ?? (
                            (variant.LengthCm > 0 && variant.WidthCm > 0 && variant.HeightCm > 0)
                                ? (variant.LengthCm.Value * variant.WidthCm.Value * variant.HeightCm.Value) / 1000000m
                                : 0.02m
                        );
                        var unitWeight = variant.GrossWeightKg ?? 1.0m;
                        if (!d.CalculatedCbm.HasValue || d.CalculatedCbm.Value <= 0)
                        {
                            d.CalculatedCbm = Math.Round(unitCbm * d.AcceptedQuantity, 4);
                        }
                        if (!d.ActualWeightKg.HasValue || d.ActualWeightKg.Value <= 0)
                        {
                            d.ActualWeightKg = Math.Round(unitWeight * d.AcceptedQuantity, 2);
                        }
                    }
                }

                // Cảnh báo sức chứa kho (Capacity Warning Shield)
                var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == receipt.WarehouseId);
                if (warehouse != null)
                {
                    var incomingCbm = receipt.Details.Sum(d => d.CalculatedCbm ?? 0);
                    var totalCapacityCbm = warehouse.TotalCapacityCbm ?? 500m;

                    var existingInventories = await _context.WarehouseInventories
                        .Include(w => w.Variant)
                        .Where(w => w.WarehouseId == receipt.WarehouseId && (w.QuantityAvailable > 0 || w.QuantityReserved > 0 || w.QuantityQC > 0 || w.QuantityDamaged > 0))
                        .AsNoTracking()
                        .ToListAsync();

                    decimal currentOccupiedCbm = 0;
                    foreach (var inv in existingInventories)
                    {
                        var totalQty = inv.QuantityAvailable + inv.QuantityReserved + inv.QuantityQC + inv.QuantityDamaged;
                        var uCbm = inv.Variant?.UnitCbm ?? (
                            (inv.Variant?.LengthCm > 0 && inv.Variant?.WidthCm > 0 && inv.Variant?.HeightCm > 0)
                                ? (inv.Variant.LengthCm.Value * inv.Variant.WidthCm.Value * inv.Variant.HeightCm.Value) / 1000000m
                                : 0.02m
                        );
                        currentOccupiedCbm += totalQty * uCbm;
                    }

                    if (currentOccupiedCbm + incomingCbm > totalCapacityCbm)
                    {
                        var warningMsg = $"[CẢNH BÁO QUÁ TẢI CBM]: Lô hàng ({incomingCbm:N2} m³) làm vượt sức chứa kho ({Math.Round(currentOccupiedCbm + incomingCbm, 2)}/{totalCapacityCbm:N2} m³).";
                        receipt.Note = string.IsNullOrWhiteSpace(receipt.Note) ? warningMsg : $"{receipt.Note} | {warningMsg}";
                    }
                    else if (totalCapacityCbm > 0 && ((currentOccupiedCbm + incomingCbm) / totalCapacityCbm) * 100m >= warehouse.WarningThresholdPercent)
                    {
                        var warningMsg = $"[CẢNH BÁO SỨC CHỨA]: Kho sắp đầy ({Math.Round(((currentOccupiedCbm + incomingCbm) / totalCapacityCbm) * 100m, 1)}% dung tích).";
                        receipt.Note = string.IsNullOrWhiteSpace(receipt.Note) ? warningMsg : $"{receipt.Note} | {warningMsg}";
                    }
                }

                // Kiểm tra xem phiếu nhập kho này có phải xuất phát từ Phiếu Trả Hàng (Customer Return / RMA) hay không
                CustomerReturn? customerReturn = null;
                if (!string.IsNullOrWhiteSpace(receipt.Note))
                {
                    var retMatch = System.Text.RegularExpressions.Regex.Match(receipt.Note, @"RET-[A-Za-z0-9-]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (retMatch.Success)
                    {
                        var retCode = retMatch.Value;
                        customerReturn = await _context.CustomerReturns
                            .FirstOrDefaultAsync(r => r.ReturnCode == retCode && !r.IsDeleted);
                    }
                }

                bool isCustomerReturn = customerReturn != null ||
                    (!string.IsNullOrWhiteSpace(receipt.Note) && (receipt.Note.Contains("RET-", StringComparison.OrdinalIgnoreCase) || receipt.Note.Contains("thu hồi", StringComparison.OrdinalIgnoreCase) || receipt.Note.Contains("trả hàng", StringComparison.OrdinalIgnoreCase) || receipt.Note.Contains("RMA", StringComparison.OrdinalIgnoreCase)));

                var poIdsToUpdate = new HashSet<int>();

                foreach (var detail in receipt.Details)
                {
                    // Quy đổi số lượng nghiệm thu và từ chối về Đơn vị tính cơ sở (Base UoM)
                    decimal baseAcceptedQty = await _uomConversionService.ConvertToBaseQuantityAsync(detail.VariantId, detail.UoMId, detail.AcceptedQuantity);
                    decimal baseDamagedQty = await _uomConversionService.ConvertToBaseQuantityAsync(detail.VariantId, detail.UoMId, detail.RejectedQuantity);

                    // 1. Cập nhật két sắt tồn kho (WarehouseInventory) theo Base UoM
                    if (isCustomerReturn)
                    {
                        // Đổi trả từ khách hàng: Hàng hỏng được thu hồi vào kho kiểm định / hàng hỏng
                        if (detail.BatchId.HasValue && detail.BatchId.Value > 0)
                        {
                            var inventory = await _context.WarehouseInventories
                                .FirstOrDefaultAsync(x => x.WarehouseId == receipt.WarehouseId &&
                                                          x.VariantId == detail.VariantId &&
                                                          x.BatchId == detail.BatchId.Value);

                            if (inventory == null)
                            {
                                inventory = new WarehouseInventory
                                {
                                    WarehouseId = receipt.WarehouseId,
                                    VariantId = detail.VariantId,
                                    BatchId = detail.BatchId.Value,
                                    QuantityAvailable = baseAcceptedQty,
                                    QuantityReserved = 0,
                                    QuantityQC = 0,
                                    QuantityDamaged = baseDamagedQty,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                _context.WarehouseInventories.Add(inventory);
                            }
                            else
                            {
                                inventory.QuantityAvailable += baseAcceptedQty;
                                inventory.QuantityDamaged += baseDamagedQty;
                                inventory.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }
                    else
                    {
                        // Nhập hàng từ Nhà Cung Cấp:
                        // Hàng từ chối (RejectedQuantity) được tài xế chở về ngay tại cửa kho -> KHÔNG VÀO TỒN KHO.
                        // CHỈ cập nhật tồn kho cho phần hàng nghiệm thu đạt chuẩn (baseAcceptedQty > 0).
                        if (baseAcceptedQty > 0)
                        {
                            if (!detail.BatchId.HasValue || detail.BatchId.Value <= 0)
                            {
                                throw new InvalidOperationException($"Mặt hàng có mã ID {detail.VariantId} đạt chuẩn {detail.AcceptedQuantity} chưa được chỉ định Lô hàng hợp lệ.");
                            }

                            var inventory = await _context.WarehouseInventories
                                .FirstOrDefaultAsync(x => x.WarehouseId == receipt.WarehouseId &&
                                                          x.VariantId == detail.VariantId &&
                                                          x.BatchId == detail.BatchId.Value);

                            if (inventory == null)
                            {
                                inventory = new WarehouseInventory
                                {
                                    WarehouseId = receipt.WarehouseId,
                                    VariantId = detail.VariantId,
                                    BatchId = detail.BatchId.Value,
                                    QuantityAvailable = baseAcceptedQty,
                                    QuantityReserved = 0,
                                    QuantityQC = 0,
                                    QuantityDamaged = 0,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                _context.WarehouseInventories.Add(inventory);
                            }
                            else
                            {
                                inventory.QuantityAvailable += baseAcceptedQty;
                                inventory.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                    }

                    // 2. Ghi sổ cái bất biến (InventoryTransaction) theo Base UoM
                    decimal baseTotalIncoming = isCustomerReturn ? (baseAcceptedQty + baseDamagedQty) : baseAcceptedQty;
                    if (baseTotalIncoming > 0 && detail.BatchId.HasValue && detail.BatchId.Value > 0)
                    {
                        var txnType = isCustomerReturn ? TransactionType.CustomerReturn : TransactionType.Receipt;
                        string refCode = customerReturn != null ? customerReturn.ReturnCode : receipt.ReceiptCode;
                        string note = isCustomerReturn
                            ? $"Nhập kho thu hồi đổi trả theo phiếu {receipt.ReceiptCode} (RMA: {refCode}) - Đạt: {baseAcceptedQty}, Hỏng: {baseDamagedQty} Base UoM"
                            : $"Nhập kho hoàn tất theo phiếu {receipt.ReceiptCode} ({detail.AcceptedQuantity} ĐVT gốc -> {baseAcceptedQty} Base UoM)";

                        var invTransaction = new InventoryTransaction
                        {
                            TransactionCode = $"TXN-{DateTimeHelper.VietnamNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                            WarehouseId = receipt.WarehouseId,
                            VariantId = detail.VariantId,
                            BatchId = detail.BatchId.Value,
                            Type = txnType,
                            Quantity = baseTotalIncoming,
                            ReferenceCode = refCode,
                            Note = note,
                            CreatedById = receivedById,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.InventoryTransactions.Add(invTransaction);
                    }

                    // 3. Cập nhật tiến độ dòng PO Detail (giữ nguyên ĐVT của PO)
                    if (detail.PurchaseOrderDetailId.HasValue)
                    {
                        var poDetail = await _context.PurchaseOrderDetails
                            .Include(x => x.PurchaseOrder)
                            .FirstOrDefaultAsync(x => x.Id == detail.PurchaseOrderDetailId.Value);

                        if (poDetail != null)
                        {
                            poDetail.ReceivedQuantity += detail.AcceptedQuantity;
                            poDetail.RejectedQuantity += detail.RejectedQuantity;
                            poIdsToUpdate.Add(poDetail.PurchaseOrderId);
                        }
                    }
                }

                // 4. Nếu phiếu nhập kho thu hồi theo Phiếu Trả Hàng (RMA) -> Cập nhật CustomerReturn sang Completed & Hoàn tiền đơn hàng
                if (customerReturn == null && !string.IsNullOrEmpty(receipt.Note))
                {
                    var retMatch = System.Text.RegularExpressions.Regex.Match(receipt.Note, @"RET-[A-Za-z0-9-]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (retMatch.Success)
                    {
                        var retCode = retMatch.Value;
                        customerReturn = await _context.CustomerReturns
                            .FirstOrDefaultAsync(r => r.ReturnCode == retCode && !r.IsDeleted);
                    }
                }

                if (customerReturn != null)
                {
                    if (customerReturn.Status != CustomerReturnStatus.Completed)
                    {
                        customerReturn.Status = CustomerReturnStatus.Completed;
                        customerReturn.UpdatedAt = DateTime.UtcNow;
                    }
                    if (customerReturn.OrderId > 0)
                    {
                        var order = await _context.Orders
                            .Include(o => o.Details)
                            .FirstOrDefaultAsync(o => o.Id == customerReturn.OrderId);
                        if (order != null)
                        {
                            var allCompletedReturns = await _context.CustomerReturns
                                .Include(r => r.Details)
                                .Where(r => r.OrderId == order.Id && !r.IsDeleted && (r.Status == CustomerReturnStatus.Completed || r.Id == customerReturn.Id))
                                .ToListAsync();

                            bool isFullyReturned = (order.Details != null && order.Details.Any())
                                ? order.Details.All(orderDetail =>
                                {
                                    decimal totalReturnedForVariant = allCompletedReturns
                                        .SelectMany(r => r.Details)
                                        .Where(d => d.VariantId == orderDetail.VariantId)
                                        .Sum(d => (d.AcceptedQuantity + d.DamagedQuantity) > 0 ? (decimal)(d.AcceptedQuantity + d.DamagedQuantity) : (decimal)d.ReturnedQuantity);

                                    return totalReturnedForVariant >= orderDetail.Quantity;
                                })
                                : true;

                            order.PaymentStatus = isFullyReturned ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
                            order.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // 5. Cập nhật trạng thái Đơn mua hàng gốc (PO)
                foreach (var poId in poIdsToUpdate)
                {
                    var po = await _context.PurchaseOrders
                        .Include(x => x.Details)
                        .FirstOrDefaultAsync(x => x.Id == poId);

                    if (po != null && (po.Status == PurchaseOrderStatus.Approved || po.Status == PurchaseOrderStatus.PartiallyReceived))
                    {
                        // Dung sai hoàn tất nhận hàng nông sản (UoM Tolerance: 5%):
                        // Do đặc thù nông sản tươi cân đo thực tế (ví dụ đặt 10kg nhận 9.6kg - 9.8kg),
                        // nếu nhận được >= 95% số lượng đặt thì ghi nhận PO hoàn tất chu trình.
                        const decimal ACCEPTABLE_TOLERANCE_PERCENT = 0.05m;
                        bool isFullyReceived = po.Details.All(d => d.ReceivedQuantity >= (d.OrderQuantity * (1.0m - ACCEPTABLE_TOLERANCE_PERCENT)));
                        if (isFullyReceived)
                        {
                            po.Status = PurchaseOrderStatus.Completed;
                            po.SettledAmount = po.Details.Sum(d => d.ReceivedQuantity * d.UnitPrice);
                        }
                        else
                        {
                            po.Status = PurchaseOrderStatus.PartiallyReceived;
                        }
                        po.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> CancelReceiptAsync(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do hủy phiếu nhập kho không được để trống.", nameof(reason));

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var receipt = await _context.InventoryReceipts.FindAsync(id);
                if (receipt == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

                if (receipt.Status == InventoryReceiptStatus.Completed)
                    throw new InvalidOperationException("Không thể hủy Phiếu Nhập Kho đã hoàn tất vào sổ cái.");

                receipt.Status = InventoryReceiptStatus.Cancelled;
                receipt.CancellationReason = reason.Trim();
                receipt.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var receipt = await _context.InventoryReceipts.FindAsync(id);
                if (receipt == null)
                    throw new KeyNotFoundException("Không tìm thấy Phiếu Nhập Kho.");

                if (receipt.Status == InventoryReceiptStatus.Completed)
                    throw new InvalidOperationException("Không thể xóa Phiếu Nhập Kho đã hoàn tất vào sổ cái.");

                receipt.IsDeleted = true;
                receipt.DeletedAt = DateTime.UtcNow;
                receipt.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion
    }
}
