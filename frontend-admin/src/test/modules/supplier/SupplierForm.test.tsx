import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import SupplierForm from '../../../pages/supplier/SupplierForm';
import { supplierApi } from '../../../api/supplierApi';
import { supplierTypeApi } from '../../../api/supplierTypeApi';

// Mock API
vi.mock('../../../api/supplierApi', () => ({
  supplierApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/supplierTypeApi', () => ({
  supplierTypeApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 3: SUPPLIER
 * 🧪 COMPONENT TEST: SupplierForm (Form Thêm mới & Cập nhật Hồ sơ NCC)
 * ============================================================================
 */
describe('Module 03 - SupplierForm Component', () => {
  const mockTypes = [
    { id: 1, code: 'FARM', name: 'Nhà vườn' },
    { id: 2, code: 'COOP', name: 'Hợp tác xã' },
  ];

  const mockExistingSuppliers = [
    { id: 1, code: 'ncc01', name: 'NCC Số 1', phone: '0901234567' },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (supplierTypeApi.getAllList as any).mockResolvedValue(mockTypes);
    (supplierApi.getAllList as any).mockResolvedValue(mockExistingSuppliers);
  });

  // #region TC01: RENDER FORM TẠO MỚI ĐẦY ĐỦ CÁC SECTION
  it('TC01 - Render đầy đủ các section thông tin cơ bản và địa chỉ kho mặc định', async () => {
    render(
      <MemoryRouter initialEntries={['/suppliers/create']}>
        <Routes>
          <Route path="/suppliers/create" element={<SupplierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhà Cung Cấp')).toBeInTheDocument();
      expect(screen.getByText('Thông Tin Doanh Nghiệp')).toBeInTheDocument();
      expect(screen.getByText('Địa Chỉ Kho Chính (Tùy chọn)')).toBeInTheDocument();
      expect(screen.getByText('Thanh Toán & Ghi Chú')).toBeInTheDocument();

      expect(screen.getByPlaceholderText('NCC-001')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Công ty TNHH...')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('0901234567')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Báo lỗi validation khi không nhập các trường bắt buộc', async () => {
    render(
      <MemoryRouter initialEntries={['/suppliers/create']}>
        <Routes>
          <Route path="/suppliers/create" element={<SupplierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhà Cung Cấp')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã nhà cung cấp không được để trống')).toBeInTheDocument();
      expect(screen.getByText('Tên nhà cung cấp không được để trống')).toBeInTheDocument();
      expect(screen.getByText('Số điện thoại không được để trống')).toBeInTheDocument();
      expect(screen.getByText('Email không được để trống')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng chọn loại nhà cung cấp')).toBeInTheDocument();
    });

    expect(supplierApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: ĐẶC THÙ - CLIENT SIDE DUPLICATE CHECK
  it('TC03 - Kiểm tra trùng lặp Mã NCC và Số điện thoại ngay trên form', async () => {
    render(
      <MemoryRouter initialEntries={['/suppliers/create']}>
        <Routes>
          <Route path="/suppliers/create" element={<SupplierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhà Cung Cấp')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('NCC-001');
    const phoneInput = screen.getByPlaceholderText('0901234567');

    // Nhập trùng mã NCC01 và số điện thoại 0901234567
    fireEvent.change(codeInput, { target: { value: 'ncc01' } });
    fireEvent.change(phoneInput, { target: { value: '0901234567' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã nhà cung cấp này đã tồn tại')).toBeInTheDocument();
      expect(screen.getByText('Số điện thoại này đã tồn tại')).toBeInTheDocument();
    });

    expect(supplierApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  it('TC04 - Submit tạo mới nhà cung cấp thành công', async () => {
    (supplierApi.create as any).mockResolvedValue({ id: 10 });

    render(
      <MemoryRouter initialEntries={['/suppliers/create']}>
        <Routes>
          <Route path="/suppliers/create" element={<SupplierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Thêm Mới Nhà Cung Cấp')).toBeInTheDocument();
    });

    // Điền thông tin cơ bản
    fireEvent.change(screen.getByPlaceholderText('NCC-001'), { target: { value: 'NCC_NEW' } });
    fireEvent.change(screen.getByPlaceholderText('Công ty TNHH...'), { target: { value: 'Nông Trại Mới' } });
    fireEvent.change(screen.getByPlaceholderText('0901234567'), { target: { value: '0933999888' } });
    fireEvent.change(screen.getByPlaceholderText('contact@company.com'), { target: { value: 'contact@newfarm.com' } });

    // Chọn phân loại (click dropdown)
    const selectType = screen.getByText('Chọn loại...');
    fireEvent.click(selectType);

    await waitFor(() => {
      expect(screen.getByText('Nhà vườn')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Nhà vườn'));

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(supplierApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'NCC_NEW',
          name: 'Nông Trại Mới',
          phone: '0933999888',
          email: 'contact@newfarm.com',
          supplierTypeId: 1,
        })
      );
    });
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD DỮ LIỆU ĐỂ CHỈNH SỬA
  it('TC05 - Load dữ liệu nhà cung cấp ở chế độ Chỉnh sửa (Edit Mode)', async () => {
    (supplierApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'NCC01',
      name: 'Nông Trại Xanh Đà Lạt',
      phone: '0912345678',
      email: 'dalat@gmail.com',
      supplierTypeId: 1,
      isActive: true,
      addresses: [],
    });

    render(
      <MemoryRouter initialEntries={['/suppliers/edit/1']}>
        <Routes>
          <Route path="/suppliers/edit/:id" element={<SupplierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Nhà Cung Cấp')).toBeInTheDocument();
      expect(screen.getByDisplayValue('NCC01')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Nông Trại Xanh Đà Lạt')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC06: EDIT MODE - SUBMIT CẬP NHẬT THÀNH CÔNG
  it('TC06 - Submit form cập nhật hồ sơ nhà cung cấp thành công', async () => {
    (supplierApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'NCC01',
      name: 'Nông Trại Xanh Đà Lạt',
      phone: '0912345678',
      email: 'dalat@gmail.com',
      supplierTypeId: 1,
      isActive: true,
      addresses: [],
    });
    (supplierApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/suppliers/edit/1']}>
        <Routes>
          <Route path="/suppliers/edit/:id" element={<SupplierForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByDisplayValue('Nông Trại Xanh Đà Lạt')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Nông Trại Xanh Đà Lạt');
    fireEvent.change(nameInput, { target: { value: 'Nông Trại Xanh Đà Lạt (Mở Rộng)' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(supplierApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'NCC01',
          name: 'Nông Trại Xanh Đà Lạt (Mở Rộng)',
        })
      );
    });
  });
  // #endregion
});
