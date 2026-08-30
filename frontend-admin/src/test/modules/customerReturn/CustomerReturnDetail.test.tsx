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
 * 📦 MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * 🧪 COMPONENT TEST: CustomerReturnDetail (Chi Tiết & Nghiệm Thu QC Phiếu Trả)
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

  // TC03: Mở modal nghiệm thu QC và gọi API inspect-and-complete
  it('TC03 - Mở modal nghiệm thu QC, nhập phân loại số lượng và gọi API customerReturnApi.inspectAndComplete', async () => {
    (customerReturnApi.inspectAndComplete as any).mockResolvedValue({
      message: 'Nghiệm thu hoàn tất',
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
    fireEvent.change(notesInput, { target: { value: 'Đã kiểm tra: 1 hộp bình thường, 1 hộp dập' } });

    const confirmQcBtn = screen.getByRole('button', { name: /Hoàn Tất Nghiệm Thu & Cập Nhật Kho/i });
    fireEvent.click(confirmQcBtn);

    await waitFor(() => {
      expect(customerReturnApi.inspectAndComplete).toHaveBeenCalledWith(50, expect.any(Object));
    });
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
      expect(customerReturnApi.reject).toHaveBeenCalledWith(50, 'Sản phẩm đã quá hạn đổi trả quy định');
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
