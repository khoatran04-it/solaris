import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import SupplierTypeForm from '../../../pages/supplierType/SupplierTypeForm';
import { supplierTypeApi } from '../../../api/supplierTypeApi';

// Mock API
vi.mock('../../../api/supplierTypeApi', () => ({
  supplierTypeApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 3: SUPPLIER
 * 🧪 COMPONENT TEST: SupplierTypeForm (Form Thêm / Sửa Phân loại NCC)
 * ============================================================================
 */
describe('Module 03 - SupplierTypeForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (supplierTypeApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'FARM', name: 'Nhà vườn' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường trong form tạo mới phân loại NCC', async () => {
    render(
      <MemoryRouter initialEntries={['/supplier-types/create']}>
        <Routes>
          <Route path="/supplier-types/create" element={<SupplierTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Mới Loại Nhà Cung Cấp')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: FARM, COOP, IMPORT')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Nhà vườn, Hợp tác xã, Nhập khẩu...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập ghi chú hoặc mô tả chi tiết về loại nhà cung cấp này...')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit dữ liệu trống', async () => {
    render(
      <MemoryRouter initialEntries={['/supplier-types/create']}>
        <Routes>
          <Route path="/supplier-types/create" element={<SupplierTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Loại Nhà Cung Cấp')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã phân loại không được để trống')).toBeInTheDocument();
      expect(screen.getByText('Tên phân loại không được để trống')).toBeInTheDocument();
    });

    expect(supplierTypeApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: ĐẶC THÙ - CLIENT SIDE DUPLICATE CHECK
  it('TC03 - Báo lỗi trùng lặp mã Code và Tên ngay trên giao diện', async () => {
    render(
      <MemoryRouter initialEntries={['/supplier-types/create']}>
        <Routes>
          <Route path="/supplier-types/create" element={<SupplierTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Loại Nhà Cung Cấp')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: FARM, COOP, IMPORT');
    const nameInput = screen.getByPlaceholderText('VD: Nhà vườn, Hợp tác xã, Nhập khẩu...');

    // Nhập trùng mã FARM và trùng tên "Nhà vườn"
    fireEvent.change(codeInput, { target: { value: 'farm' } });
    fireEvent.change(nameInput, { target: { value: 'Nhà vườn' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã phân loại này đã tồn tại')).toBeInTheDocument();
      expect(screen.getByText('Tên phân loại này đã tồn tại')).toBeInTheDocument();
    });

    expect(supplierTypeApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  it('TC04 - Submit form tạo mới thành công và gọi API create', async () => {
    (supplierTypeApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/supplier-types/create']}>
        <Routes>
          <Route path="/supplier-types/create" element={<SupplierTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Loại Nhà Cung Cấp')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('VD: FARM, COOP, IMPORT');
    const nameInput = screen.getByPlaceholderText('VD: Nhà vườn, Hợp tác xã, Nhập khẩu...');

    fireEvent.change(codeInput, { target: { value: 'DISTRIBUTOR' } });
    fireEvent.change(nameInput, { target: { value: 'Nhà phân phối' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(supplierTypeApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'DISTRIBUTOR',
          name: 'Nhà phân phối',
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (supplierTypeApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'FARM',
      name: 'Nhà vườn',
      description: 'Mô tả cũ',
      isActive: true,
    });
    (supplierTypeApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/supplier-types/edit/1']}>
        <Routes>
          <Route path="/supplier-types/edit/:id" element={<SupplierTypeForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Loại Nhà Cung Cấp')).toBeInTheDocument();
      expect(screen.getByDisplayValue('FARM')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Nhà vườn')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Nhà vườn');
    fireEvent.change(nameInput, { target: { value: 'Nhà vườn Đà Lạt' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(supplierTypeApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'FARM',
          name: 'Nhà vườn Đà Lạt',
        })
      );
    });
  });
  // #endregion
});
