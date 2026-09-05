import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import UoMForm from '../../../pages/uom/UoMForm';
import { uomApi } from '../../../api/uomApi';
import { uomCategoryApi } from '../../../api/uomCategoryApi';

// Mock API
vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/uomCategoryApi', () => ({
  uomCategoryApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 2: UNIT OF MEASURE (UoM)
 * COMPONENT TEST: UoMForm (Form Thêm mới & Chỉnh sửa Đơn vị tính)
 * ============================================================================
 */
describe('Module 02 - UoMForm Component', () => {
  const mockCategories = [
    { id: 1, code: 'WEIGHT', name: 'Khối lượng' },
    { id: 2, code: 'VOLUME', name: 'Thể tích' },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (uomCategoryApi.getAllList as any).mockResolvedValue(mockCategories);
    (uomApi.getAllList as any).mockResolvedValue([]);
  });

  // #region TC01: RENDER FORM THÊM MỚI ĐVT
  /**
   * TC01: Form tạo mới phải hiển thị tiêu đề "Thêm Đơn Vị Tính" và các trường nhập liệu.
   */
  it('TC01 - Render đầy đủ các trường trong form thêm mới ĐVT', async () => {
    render(
      <MemoryRouter initialEntries={['/uoms/create']}>
        <Routes>
          <Route path="/uoms/create" element={<UoMForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Thêm Đơn Vị Tính')).toBeInTheDocument();
    expect(screen.getByText('Thông Tin Cơ Bản')).toBeInTheDocument();
    expect(screen.getByText('Cấu Hình Hệ Thống')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: KG, BOX')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Kilogram, Thùng')).toBeInTheDocument();
    expect(
      screen.getByPlaceholderText('Cách nhau bằng dấu phẩy. VD: kg, kí, kí lô, kilogram')
    ).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION TRƯỜNG BẮT BUỘC
  /**
   * TC02: Bấm Lưu khi chưa nhập thông tin sẽ hiển thị lỗi ở Mã, Tên và Nhóm ĐVT.
   */
  it('TC02 - Báo lỗi validation khi chưa nhập Mã, Tên hoặc chưa chọn Nhóm ĐVT', async () => {
    render(
      <MemoryRouter initialEntries={['/uoms/create']}>
        <Routes>
          <Route path="/uoms/create" element={<UoMForm />} />
        </Routes>
      </MemoryRouter>
    );

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập mã ĐVT.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập tên ĐVT.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng chọn nhóm ĐVT.')).toBeInTheDocument();
    });

    expect(uomApi.create).not.toHaveBeenCalled();
  });
  // #endregion

  // #region TC03: NẠP OPTIONS NHÓM ĐVT
  /**
   * TC03: Kiểm tra dropdown Nhóm ĐVT có gọi API lấy danh mục nhóm.
   */
  it('TC03 - Nạp danh sách Nhóm ĐVT vào dropdown Select', async () => {
    render(
      <MemoryRouter initialEntries={['/uoms/create']}>
        <Routes>
          <Route path="/uoms/create" element={<UoMForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(uomCategoryApi.getAllList).toHaveBeenCalled();
    });

    expect(screen.getByText('Thuộc nhóm')).toBeInTheDocument();
  });
  // #endregion

  // #region TC04: SUBMIT TẠO MỚI THÀNH CÔNG
  /**
   * TC04: Nhập đầy đủ thông tin hợp lệ và submit gọi API uomApi.create.
   */
  it('TC04 - Submit tạo mới ĐVT thành công gọi API', async () => {
    (uomApi.create as any).mockResolvedValue({ id: 1 });

    render(
      <MemoryRouter initialEntries={['/uoms/create']}>
        <Routes>
          <Route path="/uoms/create" element={<UoMForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(uomCategoryApi.getAllList).toHaveBeenCalled();
    });

    const codeInput = screen.getByPlaceholderText('VD: KG, BOX');
    const nameInput = screen.getByPlaceholderText('VD: Kilogram, Thùng');
    const synonymsInput = screen.getByPlaceholderText(
      'Cách nhau bằng dấu phẩy. VD: kg, kí, kí lô, kilogram'
    );

    fireEvent.change(codeInput, { target: { value: 'KG' } });
    fireEvent.change(nameInput, { target: { value: 'Kilogram' } });
    fireEvent.change(synonymsInput, { target: { value: 'ky, can' } });

    // Mở dropdown chọn nhóm
    const categorySelect = screen.getByText('Chọn nhóm quy đổi...');
    fireEvent.click(categorySelect);

    await waitFor(() => {
      expect(screen.getByText('Khối lượng')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Khối lượng'));

    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(uomApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'KG',
          name: 'Kilogram',
          synonyms: 'ky, can',
          categoryId: 1,
          isActive: true,
        })
      );
    });
  });
  // #endregion

  // #region TC05: CHẾ ĐỘ CHỈNH SỬA (EDIT MODE)
  /**
   * TC05: Mở form với param id = 1 -> Gọi API getById và nạp thông tin cũ vào form.
   */
  it('TC05 - Chế độ Edit: Nạp thông tin ĐVT cũ từ API', async () => {
    (uomApi.getById as any).mockResolvedValue({
      id: 1,
      code: 'KG',
      name: 'Kilogram',
      synonyms: 'ký, cân',
      categoryId: 1,
      isActive: true,
    });

    render(
      <MemoryRouter initialEntries={['/uoms/edit/1']}>
        <Routes>
          <Route path="/uoms/edit/:id" element={<UoMForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Chỉnh Sửa Đơn Vị Tính')).toBeInTheDocument();

    await waitFor(() => {
      const codeInput = screen.getByPlaceholderText('VD: KG, BOX') as HTMLInputElement;
      const nameInput = screen.getByPlaceholderText('VD: Kilogram, Thùng') as HTMLInputElement;

      expect(codeInput.value).toBe('KG');
      expect(nameInput.value).toBe('Kilogram');
    });
  });
  // #endregion
});
