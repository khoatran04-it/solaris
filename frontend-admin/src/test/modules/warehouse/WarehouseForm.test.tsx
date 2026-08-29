import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import WarehouseForm from '../../../pages/warehouse/WarehouseForm';
import { warehouseApi } from '../../../api/warehouseApi';
import { userApi } from '../../../api/userApi';

// Mock APIs
vi.mock('../../../api/warehouseApi', () => ({
  warehouseApi: {
    getById: vi.fn(),
    getAllList: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/userApi', () => ({
  userApi: {
    getAllList: vi.fn(),
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
 * 📦 MODULE 08: WAREHOUSE & PHYSICAL ADDRESS MANAGEMENT
 * 🧪 COMPONENT TEST: WarehouseForm (Thêm / Sửa Kho Hàng & Địa chỉ)
 * ============================================================================
 */
describe('Module 08 - WarehouseForm Component', () => {
  const mockUsers = [
    { id: 10, fullName: 'Nguyễn Văn Trưởng Kho', username: 'truongkhohn' },
    { id: 11, fullName: 'Trần Thị Quản Lý', username: 'quanlyhcm' },
  ];

  const mockExistingWarehouses = [
    { id: 1, code: 'WH-HN-01', name: 'Tổng Kho Hà Nội' },
    { id: 2, code: 'WH-HCM-01', name: 'Kho Nam Sài Gòn' },
  ];

  const mockWarehouseDetail = {
    id: 1,
    code: 'WH-HN-01',
    name: 'Tổng Kho Hà Nội',
    warehouseType: 'Kho Tổng',
    managerId: 10,
    managerName: 'Nguyễn Văn Trưởng Kho',
    addressId: 1,
    province: 'Hà Nội',
    district: 'Long Biên',
    ward: 'Gia Thụy',
    streetAddress: 'Số 123 Nguyễn Sơn',
    fullAddress: 'Số 123 Nguyễn Sơn, Gia Thụy, Long Biên, Hà Nội',
    latitude: 21.0456,
    longitude: 105.8821,
    isActive: true,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (userApi.getAllList as any).mockResolvedValue(mockUsers);
    (warehouseApi.getAllList as any).mockResolvedValue(mockExistingWarehouses);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render form tạo mới với các trường thông tin cơ bản và địa chỉ vật lý', async () => {
    render(
      <MemoryRouter initialEntries={['/warehouses/create']}>
        <Routes>
          <Route path="/warehouses/create" element={<WarehouseForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Tạo Kho Hàng Mới')).toBeInTheDocument();
    expect(screen.getByText('Định Danh Kho Hàng')).toBeInTheDocument();
    expect(screen.getByText('Địa Chỉ Vật Lý & Tọa Độ GPS')).toBeInTheDocument();

    expect(screen.getByPlaceholderText('VD: HUB-HCM-01')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Kho Tổng Miền Nam')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: TP. Hồ Chí Minh')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Quận Bình Tân')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Phường An Lạc')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: 123 Đường Số 7, KCN Tân Tạo')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: TẠO MỚI KHO HÀNG THÀNH CÔNG
  it('TC02 - Tạo mới kho hàng và địa chỉ vật lý thành công', async () => {
    (warehouseApi.create as any).mockResolvedValue({ id: 100 });

    render(
      <MemoryRouter initialEntries={['/warehouses/create']}>
        <Routes>
          <Route path="/warehouses/create" element={<WarehouseForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
    });

    // 1. Nhập thông tin chung
    fireEvent.change(screen.getByPlaceholderText('VD: HUB-HCM-01'), {
      target: { value: 'WH-DN-01' },
    });
    fireEvent.change(screen.getByPlaceholderText('VD: Kho Tổng Miền Nam'), {
      target: { value: 'Kho Đà Nẵng Central' },
    });

    // Chọn loại kho thông qua FormSelect
    const selectTrigger = screen.getByText('-- Chọn loại kho --');
    fireEvent.click(selectTrigger);

    await waitFor(() => {
      expect(screen.getByText('Kho Tổng (Master Hub)')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Kho Tổng (Master Hub)'));

    // 2. Nhập thông tin địa chỉ
    fireEvent.change(screen.getByPlaceholderText('VD: TP. Hồ Chí Minh'), {
      target: { value: 'Đà Nẵng' },
    });
    fireEvent.change(screen.getByPlaceholderText('VD: Quận Bình Tân'), {
      target: { value: 'Hải Châu' },
    });
    fireEvent.change(screen.getByPlaceholderText('VD: Phường An Lạc'), {
      target: { value: 'Hòa Cường Bắc' },
    });
    fireEvent.change(screen.getByPlaceholderText('VD: 123 Đường Số 7, KCN Tân Tạo'), {
      target: { value: 'Số 99 Đường 2 Tháng 9' },
    });

    // 3. Submit form
    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(warehouseApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'WH-DN-01',
          name: 'Kho Đà Nẵng Central',
          warehouseType: 'Kho Tổng',
          address: expect.objectContaining({
            province: 'Đà Nẵng',
            district: 'Hải Châu',
            ward: 'Hòa Cường Bắc',
            streetAddress: 'Số 99 Đường 2 Tháng 9',
          }),
        })
      );
    });
  });
  // #endregion

  // #region TC03: CHỈNH SỬA KHO HÀNG (EDIT MODE)
  it('TC03 - Load dữ liệu chi tiết và cập nhật thành công ở chế độ chỉnh sửa (mã kho disabled)', async () => {
    (warehouseApi.getById as any).mockResolvedValue(mockWarehouseDetail);
    (warehouseApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/warehouses/edit/1']}>
        <Routes>
          <Route path="/warehouses/edit/:id" element={<WarehouseForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getById).toHaveBeenCalledWith(1);
      expect(screen.getByDisplayValue('WH-HN-01')).toBeInTheDocument();
      expect(screen.getByDisplayValue('WH-HN-01')).toBeDisabled();
      expect(screen.getByDisplayValue('Tổng Kho Hà Nội')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Số 123 Nguyễn Sơn')).toBeInTheDocument();
    });

    // Thay đổi tên kho và địa chỉ
    fireEvent.change(screen.getByDisplayValue('Tổng Kho Hà Nội'), {
      target: { value: 'Tổng Kho Hà Nội (Mở Rộng)' },
    });
    fireEvent.change(screen.getByDisplayValue('Số 123 Nguyễn Sơn'), {
      target: { value: 'Số 456 Nguyễn Sơn' },
    });

    // Submit cập nhật
    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(warehouseApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          name: 'Tổng Kho Hà Nội (Mở Rộng)',
          address: expect.objectContaining({
            streetAddress: 'Số 456 Nguyễn Sơn',
          }),
        })
      );
    });
  });
  // #endregion

  // #region TC04: VALIDATION KHI THIẾU TRƯỜNG BẮT BUỘC
  it('TC04 - Báo lỗi validation khi để trống mã kho, tên kho hoặc thông tin địa chỉ', async () => {
    render(
      <MemoryRouter initialEntries={['/warehouses/create']}>
        <Routes>
          <Route path="/warehouses/create" element={<WarehouseForm />} />
        </Routes>
      </MemoryRouter>
    );

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã kho.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên kho hàng.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng chọn loại kho.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập Tỉnh/Thành phố.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập Quận/Huyện.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập Phường/Xã.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập số nhà, tên đường.')).toBeInTheDocument();
      expect(warehouseApi.create).not.toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC05: VALIDATION TRÙNG MÃ KHO
  it('TC05 - Báo lỗi khi nhập mã kho đã tồn tại trong hệ thống', async () => {
    render(
      <MemoryRouter initialEntries={['/warehouses/create']}>
        <Routes>
          <Route path="/warehouses/create" element={<WarehouseForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
    });

    const codeInput = screen.getByPlaceholderText('VD: HUB-HCM-01');
    fireEvent.change(codeInput, { target: { value: 'wh-hn-01' } }); // Trùng mã có sẵn

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã kho này đã tồn tại!')).toBeInTheDocument();
      expect(warehouseApi.create).not.toHaveBeenCalled();
    });
  });
  // #endregion
});
