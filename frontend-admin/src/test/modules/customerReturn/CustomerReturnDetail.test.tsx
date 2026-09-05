import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerReturnDetail from '../../../pages/customerReturn/CustomerReturnDetail';
import { customerReturnApi } from '../../../api/customerReturnApi';
import { CustomerReturnStatus } from '../../../types/customerReturn';

// Mock APIs
vi.mock('../../../api/customerReturnApi', () => ({
  customerReturnApi: {
    getById: vi.fn(),
    approve: vi.fn(),
    inspect: vi.fn(),
    complete: vi.fn(),
    inspectAndComplete: vi.fn(),
    reject: vi.fn(),
  },
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

/**
 * ============================================================================
 * MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * COMPONENT TEST: CustomerReturnDetail (Chi Tiết & Nghiệm Thu QC Phiếu Trả)
 * ============================================================================
 */
describe('Module 13 - CustomerReturnDetail Component', () => {
  const mockReturn = {
    id: 50,
    returnCode: 'RET-20260830-050',
    orderId: 10,
    orderCode: 'ORD-20260830-010',
    customerId: 1,
    customerName: 'Nguyễn Văn A',
    warehouseId: 1,
    warehouseName: 'Kho Tổng TP.HCM',
    receivedById: 2,
    receivedByName: 'Thủ Kho Test',
    status: CustomerReturnStatus.Pending,
    refundAmount: 0,
    reason: 'Trái cây bị chín nhũn khi nhận',
    inspectionNotes: '',
    returnDate: '2026-08-30T08:00:00Z',
    createdAt: '2026-08-30T08:00:00Z',
    updatedAt: '2026-08-30T08:00:00Z',
    details: [
      {
        id: 1,
        variantId: 10,
        variantName: 'Xoài Cát Hòa Lộc',
        variantCode: 'SKU-XOAI',
        batchId: 5,
        batchCode: 'BATCH-XOAI-05',
        uoMId: 1,
        uoMName: 'Hộp 1kg',
        returnedQuantity: 2,
        acceptedQuantity: 0,
        damagedQuantity: 0,
        unitPrice: 150000,
        refundAmount: 0,
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (customerReturnApi.getById as any).mockResolvedValue(mockReturn);
  });

  const renderComponent = () =>
    render(
      <MemoryRouter initialEntries={['/customer-returns/50']}>
        <Routes>
          <Route path="/customer-returns/:id" element={<CustomerReturnDetail />} />
        </Routes>
      </MemoryRouter>
    );

  // TC01: Render chi tiết phiếu trả hàng
  it('TC01 - Render chi tiết phiếu trả hàng với đầy đủ mã phiếu, đơn gốc, khách hàng, kho và trạng thái', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-050')).toBeInTheDocument();
      expect(screen.getByText('ORD-20260830-010')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('Kho Tổng TP.HCM')).toBeInTheDocument();
      expect(screen.getByText('Chờ tiếp nhận')).toBeInTheDocument();
    });
  });

  // TC02: Chuyển sang tab Mặt hàng trả
  it('TC02 - Chuyển sang tab Mặt hàng trả và hiển thị đầy đủ danh sách sản phẩm và lô hàng', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-050')).toBeInTheDocument();
    });

    const itemsTab = screen.getByRole('button', { name: /2\. CHI TIẾT KIỂM ĐỊNH QC/i });
    fireEvent.click(itemsTab);

    await waitFor(() => {
      expect(screen.getByText('Xoài Cát Hòa Lộc')).toBeInTheDocument();
      expect(screen.getByText('BATCH-XOAI-05')).toBeInTheDocument();
    });
  });

  // TC03a: Duyệt yêu cầu khi ở trạng thái Pending
  it('TC03a - Duyệt yêu cầu khi ở trạng thái Pending và gọi API customerReturnApi.approve', async () => {
    (customerReturnApi.approve as any).mockResolvedValue({
      message: 'Đã duyệt yêu cầu thành công',
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-050')).toBeInTheDocument();
    });

    const approveBtn = screen.getByRole('button', { name: /Duyệt Yêu Cầu/i });
    fireEvent.click(approveBtn);

    await waitFor(() => {
      expect(customerReturnApi.approve).toHaveBeenCalledWith(50);
    });
  });

  // TC03b: Mở modal nghiệm thu QC khi ở trạng thái Approved và gọi API inspect
  it('TC03b - Mở modal nghiệm thu QC khi ở trạng thái Approved, nhập phân loại số lượng và gọi API customerReturnApi.inspect', async () => {
    (customerReturnApi.getById as any).mockResolvedValue({
      ...mockReturn,
      status: CustomerReturnStatus.Approved,
    });
    (customerReturnApi.inspect as any).mockResolvedValue({
      message: 'Nghiệm thu kiểm định QC thành công',
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-050')).toBeInTheDocument();
    });

    const qcBtn = screen.getByRole('button', { name: /Kiểm Định QC & Nghiệm Thu/i });
    fireEvent.click(qcBtn);

    // Modal QC xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Nghiệm Thu Kiểm Định QC/i)).toBeInTheDocument();
    });

    const notesInput = screen.getByPlaceholderText(/Hàng đạt 80% chất lượng ban đầu/i);
    fireEvent.change(notesInput, {
      target: { value: 'Đã kiểm tra: 1 hộp bình thường, 1 hộp dập' },
    });

    const confirmQcBtn = screen.getByRole('button', {
      name: /Lưu Kết Quả Kiểm Định & Chuyển Sang Xử Lý/i,
    });
    fireEvent.click(confirmQcBtn);

    await waitFor(() => {
      expect(customerReturnApi.inspect).toHaveBeenCalledWith(50, expect.any(Object));
    });
  });

  // TC03c: Hoàn tất phiếu trả hàng trực tiếp khi ở trạng thái Inspecting
  it('TC03c - Hoàn tất trả hàng khi ở trạng thái Inspecting và gọi API customerReturnApi.complete', async () => {
    (customerReturnApi.getById as any).mockResolvedValue({
      ...mockReturn,
      status: CustomerReturnStatus.Inspecting,
    });
    (customerReturnApi.complete as any).mockResolvedValue({
      message: 'Hoàn tất phiếu trả hàng thành công',
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-050')).toBeInTheDocument();
    });

    const completeBtn = screen.getByRole('button', { name: /Hoàn Tất Trả Hàng/i });
    fireEvent.click(completeBtn);

    await waitFor(() => {
      expect(customerReturnApi.complete).toHaveBeenCalledWith(50);
    });
  });

  // TC03d: Điều hướng sang trang Tạo Phiếu Nhập Kho Thu Hồi khi ở trạng thái Inspecting
  it('TC03d - Bấm nút Tạo Phiếu Nhập Kho Thu Hồi điều hướng sang /inventory-receipts/create?returnId=50', async () => {
    (customerReturnApi.getById as any).mockResolvedValue({
      ...mockReturn,
      status: CustomerReturnStatus.Inspecting,
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-050')).toBeInTheDocument();
    });

    const createReceiptBtn = screen.getByRole('button', { name: /Tạo Phiếu Nhập Kho Thu Hồi/i });
    fireEvent.click(createReceiptBtn);

    expect(mockNavigate).toHaveBeenCalledWith('/inventory-receipts/create?returnId=50');
  });

  // TC04: Mở modal từ chối và gọi API reject
  it('TC04 - Mở modal từ chối, nhập lý do và gọi API customerReturnApi.reject', async () => {
    (customerReturnApi.reject as any).mockResolvedValue({ message: 'Từ chối thành công' });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-050')).toBeInTheDocument();
    });

    const rejectBtn = screen.getByRole('button', { name: /Từ Chối Trả Hàng/i });
    fireEvent.click(rejectBtn);

    // Modal từ chối xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Từ Chối Nhận Hàng Hoàn Trả/i)).toBeInTheDocument();
    });

    const reasonInput = screen.getByPlaceholderText(/Quá thời hạn đổi trả 7 ngày/i);
    fireEvent.change(reasonInput, { target: { value: 'Sản phẩm đã quá hạn đổi trả quy định' } });

    const confirmRejectBtn = screen.getByRole('button', { name: /Xác Nhận Từ Chối/i });
    fireEvent.click(confirmRejectBtn);

    await waitFor(() => {
      expect(customerReturnApi.reject).toHaveBeenCalledWith(
        50,
        'Sản phẩm đã quá hạn đổi trả quy định'
      );
    });
  });

  // TC05: Điều hướng quay lại danh sách phiếu trả
  it('TC05 - Bấm nút Quay lại điều hướng về route /customer-returns', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-050')).toBeInTheDocument();
    });

    const backBtn = screen.getByRole('button', { name: /QUAY LẠI/i });
    fireEvent.click(backBtn);

    expect(mockNavigate).toHaveBeenCalledWith('/customer-returns');
  });
});
