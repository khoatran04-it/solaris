import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerGroupForm from '../../../pages/customerGroup/CustomerGroupForm';
import { customerGroupApi } from '../../../api/customerGroupApi';

// Mock API
vi.mock('../../../api/customerGroupApi', () => ({
  customerGroupApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 6: CUSTOMER MASTER DATA
 * COMPONENT TEST: CustomerGroupForm (Form Thêm / Sửa Nhóm Khách Hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerGroupForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (customerGroupApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'VIP', name: 'Khách VIP' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường trong form tạo mới nhóm khách hàng', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-groups/create']}>
        <Routes>
          <Route path="/customer-groups/create" element={<CustomerGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Mới Nhóm Khách Hàng')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('G-001')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Khách sỉ lâu năm')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Ghi chú về nhóm khách hàng này...')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit dữ liệu trống', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-groups/create']}>
        <Routes>
          <Route path="/customer-groups/create" element={<CustomerGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhóm Khách Hàng')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã định danh.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên nhóm khách hàng.')).toBeInTheDocument();
    });

    expect(customerGroupApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CLIENT SIDE DUPLICATE CHECK
  it('TC03 - Báo lỗi trùng lặp mã Code và Tên ngay trên giao diện', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-groups/create']}>
        <Routes>
          <Route path="/customer-groups/create" element={<CustomerGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhóm Khách Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('G-001');
    const nameInput = screen.getByPlaceholderText('VD: Khách sỉ lâu năm');

    fireEvent.change(codeInput, { target: { value: 'vip' } });
    fireEvent.change(nameInput, { target: { value: 'Khách VIP' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã định danh đã tồn tại!')).toBeInTheDocument();
      expect(screen.getByText('Tên nhóm khách hàng đã tồn tại!')).toBeInTheDocument();
    });

    expect(customerGroupApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  it('TC04 - Submit form tạo mới thành công và gọi API create', async () => {
    (customerGroupApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/customer-groups/create']}>
        <Routes>
          <Route path="/customer-groups/create" element={<CustomerGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhóm Khách Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('G-001');
    const nameInput = screen.getByPlaceholderText('VD: Khách sỉ lâu năm');

    fireEvent.change(codeInput, { target: { value: 'B2B' } });
    fireEvent.change(nameInput, { target: { value: 'Khách hàng B2B' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(customerGroupApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'B2B',
          name: 'Khách hàng B2B',
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (customerGroupApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'VIP',
      name: 'Khách VIP',
      description: 'Khách hàng chi tiêu cao',
      isActive: true,
    });
    (customerGroupApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/customer-groups/edit/1']}>
        <Routes>
          <Route path="/customer-groups/edit/:id" element={<CustomerGroupForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Nhóm Khách Hàng')).toBeInTheDocument();
      expect(screen.getByDisplayValue('VIP')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Khách VIP')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Khách VIP');
    fireEvent.change(nameInput, { target: { value: 'Khách VIP Platinum' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(customerGroupApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'VIP',
          name: 'Khách VIP Platinum',
        })
      );
    });
  });
  // #endregion
});
