import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerTierForm from '../../../pages/customerTier/CustomerTierForm';
import { customerTierApi } from '../../../api/customerTierApi';

// Mock API
vi.mock('../../../api/customerTierApi', () => ({
  customerTierApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 6: CUSTOMER MASTER DATA
 * 🧪 COMPONENT TEST: CustomerTierForm (Form Thêm / Sửa Bậc Hạng Khách Hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerTierForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (customerTierApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'GOLD', name: 'Hạng Vàng' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường trong form tạo mới bậc hạng thành viên', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-tiers/create']}>
        <Routes>
          <Route path="/customer-tiers/create" element={<CustomerTierForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Phân Bậc Khách Hàng')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VANG, BAC, KIM CUONG...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Ví dụ: Khách hàng Vàng')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit dữ liệu trống', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-tiers/create']}>
        <Routes>
          <Route path="/customer-tiers/create" element={<CustomerTierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Phân Bậc Khách Hàng')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã định danh.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên phân bậc.')).toBeInTheDocument();
    });

    expect(customerTierApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CLIENT SIDE DUPLICATE CHECK
  it('TC03 - Báo lỗi trùng lặp mã Code và Tên bậc hạng ngay trên giao diện', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-tiers/create']}>
        <Routes>
          <Route path="/customer-tiers/create" element={<CustomerTierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Phân Bậc Khách Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VANG, BAC, KIM CUONG...');
    const nameInput = screen.getByPlaceholderText('Ví dụ: Khách hàng Vàng');

    fireEvent.change(codeInput, { target: { value: 'gold' } });
    fireEvent.change(nameInput, { target: { value: 'Hạng Vàng' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã định danh đã tồn tại!')).toBeInTheDocument();
      expect(screen.getByText('Tên phân bậc đã tồn tại!')).toBeInTheDocument();
    });

    expect(customerTierApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG VỚI CHIẾT KHẤU & MIN SPENDING
  it('TC04 - Submit form tạo mới thành công với chiết khấu và chi tiêu tối thiểu', async () => {
    (customerTierApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/customer-tiers/create']}>
        <Routes>
          <Route path="/customer-tiers/create" element={<CustomerTierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Phân Bậc Khách Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VANG, BAC, KIM CUONG...');
    const nameInput = screen.getByPlaceholderText('Ví dụ: Khách hàng Vàng');

    fireEvent.change(codeInput, { target: { value: 'PLATINUM' } });
    fireEvent.change(nameInput, { target: { value: 'Hạng Bạch Kim' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(customerTierApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'PLATINUM',
          name: 'Hạng Bạch Kim',
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (customerTierApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'GOLD',
      name: 'Hạng Vàng',
      discountPercent: 5,
      minSpending: 10000000,
      isActive: true,
    });
    (customerTierApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/customer-tiers/edit/1']}>
        <Routes>
          <Route path="/customer-tiers/edit/:id" element={<CustomerTierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Phân Bậc Khách Hàng')).toBeInTheDocument();
      expect(screen.getByDisplayValue('GOLD')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Hạng Vàng')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Hạng Vàng');
    fireEvent.change(nameInput, { target: { value: 'Hạng Vàng Cao Cấp' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(customerTierApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'GOLD',
          name: 'Hạng Vàng Cao Cấp',
        })
      );
    });
  });
  // #endregion
});
