import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerTypeForm from '../../../pages/customerType/CustomerTypeForm';
import { customerTypeApi } from '../../../api/customerTypeApi';

// Mock API
vi.mock('../../../api/customerTypeApi', () => ({
  customerTypeApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 6: CUSTOMER MASTER DATA
 * COMPONENT TEST: CustomerTypeForm (Form Thêm / Sửa Phân loại Khách hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerTypeForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (customerTypeApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'SI', name: 'Khách sỉ' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường trong form tạo mới phân loại khách hàng', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-types/create']}>
        <Routes>
          <Route path="/customer-types/create" element={<CustomerTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Phân Loại Khách Hàng')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: HORECA, SI, LE...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Khách sỉ đại lý...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Mô tả về phân loại khách hàng này...')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit dữ liệu trống', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-types/create']}>
        <Routes>
          <Route path="/customer-types/create" element={<CustomerTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Phân Loại Khách Hàng')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã định danh.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên phân loại.')).toBeInTheDocument();
    });

    expect(customerTypeApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CLIENT SIDE DUPLICATE CHECK
  it('TC03 - Báo lỗi trùng lặp mã Code và Tên ngay trên giao diện', async () => {
    render(
      <MemoryRouter initialEntries={['/customer-types/create']}>
        <Routes>
          <Route path="/customer-types/create" element={<CustomerTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Phân Loại Khách Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: HORECA, SI, LE...');
    const nameInput = screen.getByPlaceholderText('VD: Khách sỉ đại lý...');

    fireEvent.change(codeInput, { target: { value: 'si' } });
    fireEvent.change(nameInput, { target: { value: 'Khách sỉ' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã định danh đã tồn tại!')).toBeInTheDocument();
      expect(screen.getByText('Tên phân loại đã tồn tại!')).toBeInTheDocument();
    });

    expect(customerTypeApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  it('TC04 - Submit form tạo mới thành công và gọi API create', async () => {
    (customerTypeApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/customer-types/create']}>
        <Routes>
          <Route path="/customer-types/create" element={<CustomerTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Phân Loại Khách Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: HORECA, SI, LE...');
    const nameInput = screen.getByPlaceholderText('VD: Khách sỉ đại lý...');

    fireEvent.change(codeInput, { target: { value: 'AGENCY' } });
    fireEvent.change(nameInput, { target: { value: 'Đại lý cấp 1' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(customerTypeApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'AGENCY',
          name: 'Đại lý cấp 1',
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (customerTypeApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'SI',
      name: 'Khách sỉ',
      description: 'Mô tả cũ',
      isActive: true,
    });
    (customerTypeApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/customer-types/edit/1']}>
        <Routes>
          <Route path="/customer-types/edit/:id" element={<CustomerTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Phân Loại Khách Hàng')).toBeInTheDocument();
      expect(screen.getByDisplayValue('SI')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Khách sỉ')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Khách sỉ');
    fireEvent.change(nameInput, { target: { value: 'Khách sỉ VIP' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(customerTypeApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'SI',
          name: 'Khách sỉ VIP',
        })
      );
    });
  });
  // #endregion
});
