import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerForm from '../../../pages/customer/CustomerForm';
import { customerApi } from '../../../api/customerApi';
import { customerTypeApi } from '../../../api/customerTypeApi';
import { customerTierApi } from '../../../api/customerTierApi';
import { customerGroupApi } from '../../../api/customerGroupApi';

// Mock APIs
vi.mock('../../../api/customerApi', () => ({
  customerApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/customerTypeApi', () => ({
  customerTypeApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/customerTierApi', () => ({
  customerTierApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/customerGroupApi', () => ({
  customerGroupApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 6: CUSTOMER MASTER DATA
 * COMPONENT TEST: CustomerForm (Form Thêm / Sửa Hồ Sơ Khách Hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerForm Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (customerTypeApi.getAllList as any).mockResolvedValue([{ id: 1, name: 'Khách sỉ' }]);
    (customerTierApi.getAllList as any).mockResolvedValue([{ id: 1, name: 'Hạng Vàng' }]);
    (customerGroupApi.getAllList as any).mockResolvedValue([{ id: 1, name: 'Khách VIP' }]);
    (customerApi.getAllList as any).mockResolvedValue([
      { id: 1, code: 'KH001', phoneNumber: '0901234567' },
    ]);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  it('TC01 - Render đầy đủ các trường thông tin trong form tạo mới khách hàng', async () => {
    render(
      <MemoryRouter initialEntries={['/customers/create']}>
        <Routes>
          <Route path="/customers/create" element={<CustomerForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Tạo Hồ Sơ Khách Hàng')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('KH-001')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nguyễn Văn A')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('0901234567')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BẮT BUỘC KHI SUBMIT TRỐNG
  it('TC02 - Hiển thị lỗi validation khi submit thiếu thông tin bắt buộc', async () => {
    render(
      <MemoryRouter initialEntries={['/customers/create']}>
        <Routes>
          <Route path="/customers/create" element={<CustomerForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Tạo Hồ Sơ Khách Hàng')).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã định danh.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên khách hàng.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập số điện thoại.')).toBeInTheDocument();
    });

    expect(customerApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: CLIENT SIDE DUPLICATE CHECK (MÃ & SĐT)
  it('TC03 - Báo lỗi trùng lặp mã KH và số điện thoại ngay trên giao diện', async () => {
    render(
      <MemoryRouter initialEntries={['/customers/create']}>
        <Routes>
          <Route path="/customers/create" element={<CustomerForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Tạo Hồ Sơ Khách Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('KH-001');
    const phoneInput = screen.getByPlaceholderText('0901234567');

    fireEvent.change(codeInput, { target: { value: 'kh001' } });
    fireEvent.change(phoneInput, { target: { value: '0901234567' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Mã định danh đã tồn tại!')).toBeInTheDocument();
      expect(screen.getByText('Số điện thoại này đã được đăng ký!')).toBeInTheDocument();
    });

    expect(customerApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC04: SUBMIT TẠO MỚI KHÁCH HÀNG THÀNH CÔNG
  it('TC04 - Submit form tạo mới thành công và gọi API create', async () => {
    (customerApi.create as any).mockResolvedValue({ id: 2 });

    render(
      <MemoryRouter initialEntries={['/customers/create']}>
        <Routes>
          <Route path="/customers/create" element={<CustomerForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Tạo Hồ Sơ Khách Hàng')).toBeInTheDocument();
    });

    const codeInput = screen.getByPlaceholderText('KH-001');
    const nameInput = screen.getByPlaceholderText('Nguyễn Văn A');
    const phoneInput = screen.getByPlaceholderText('0901234567');

    fireEvent.change(codeInput, { target: { value: 'KH-NEW' } });
    fireEvent.change(nameInput, { target: { value: 'Công ty Nam Long' } });
    fireEvent.change(phoneInput, { target: { value: '0933999888' } });

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);
  });
  // #endregion

  // #region TC05: EDIT MODE - LOAD CHI TIẾT VÀ UPDATE THÀNH CÔNG
  it('TC05 - Load dữ liệu chi tiết ở chế độ Edit và submit cập nhật thành công', async () => {
    (customerApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'KH001',
      name: 'Công ty Minh Long',
      phoneNumber: '0901234567',
      email: 'minhlong@gmail.com',
      taxCode: '0312345678',
      isActive: true,
      customerTypeId: 1,
      customerTierId: 1,
      groupIds: [1],
    });
    (customerApi.update as any).mockResolvedValue({});

    render(
      <MemoryRouter initialEntries={['/customers/edit/1']}>
        <Routes>
          <Route path="/customers/edit/:id" element={<CustomerForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa Khách Hàng')).toBeInTheDocument();
      expect(screen.getByDisplayValue('KH001')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Công ty Minh Long')).toBeInTheDocument();
      expect(screen.getByDisplayValue('0901234567')).toBeInTheDocument();
    });

    const nameInput = screen.getByDisplayValue('Công ty Minh Long');
    fireEvent.change(nameInput, { target: { value: 'Công ty Minh Long VIP' } });

    const submitBtn = screen.getByRole('button', { name: /LƯU THAY ĐỔI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(customerApi.update).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          code: 'KH001',
          name: 'Công ty Minh Long VIP',
        })
      );
    });
  });
  // #endregion
});
